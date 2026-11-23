using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;

namespace TrackRelay;

internal sealed partial class SettingsWindow : Window
{
    private readonly App _app;
    private readonly ShortcutRow[] _rows;

    public SettingsWindow(App app, Preferences preferences, bool startWithWindows)
    {
        _app = app;
        InitializeComponent();
        _rows = Preferences.Actions.Select(action =>
            new ShortcutRow(action, preferences.Shortcuts[action])).ToArray();
        RefreshLanguage(preferences.Language);
        TrackChanges.IsChecked = preferences.ShowTrackChanges;
        StartWithWindows.IsChecked = startWithWindows;
        CheckUpdatesEnabled.IsChecked = preferences.CheckUpdates;
        VolumeKeysEnabled.IsChecked = preferences.VolumeFunctionKeys;
        Closing += (_, e) =>
        {
            if (!_app.IsExiting)
            {
                e.Cancel = true;
                Hide();
            }
        };
    }

    public void SetCurrentTrack(Track? track)
    {
        ArtworkPalette.Apply(CurrentTrackFrame, track?.ArtworkColor);
        CurrentArtwork.Artwork = track?.Artwork;
        CurrentTitle.Text = track?.Title ?? Strings.Get("NothingPlaying");
        CurrentArtist.Text = track?.Artist ?? Strings.Get("PlayFirst");
        CurrentTitle.ToolTip = track?.Title;
        CurrentArtist.ToolTip = track?.Artist;
    }

    public void RefreshLanguage(AppLanguage language)
    {
        ShortcutRows.ItemsSource = null;
        ShortcutRows.ItemsSource = _rows;
        LanguageChoice.ItemsSource = new Option<AppLanguage>[]
        {
            new(AppLanguage.System, Strings.Get("LanguageSystem")),
            new(AppLanguage.Korean, "한국어"),
            new(AppLanguage.English, "English")
        };
        LanguageChoice.SelectedValue = language;
    }

    public void SetConnectionStatus(string text) => SetLiveText(ConnectionStatus, text);

    public void SetFeedback(string text, bool error)
    {
        Feedback.SetResourceReference(TextBlock.ForegroundProperty, error ? "ErrorBrush" : "MutedBrush");
        SetLiveText(Feedback, text);
    }

    private void PreviewClick(object sender, RoutedEventArgs e) => _app.ShowPreview();

    private void ApplyClick(object sender, RoutedEventArgs e)
    {
        var preferences = new Preferences(TrackChanges.IsChecked == true,
            _rows.ToDictionary(row => row.Action, row => row.Binding), VolumeKeysEnabled.IsChecked == true,
            (AppLanguage)(LanguageChoice.SelectedValue ?? AppLanguage.System),
            CheckUpdatesEnabled.IsChecked == true);
        var error = _app.Apply(preferences, StartWithWindows.IsChecked == true);
        SetFeedback(error ?? Strings.Get("ChangesApplied"), error is not null);
    }

    private static void SetLiveText(TextBlock label, string text)
    {
        label.Text = text;
        UIElementAutomationPeer.FromElement(label)?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
    }

    private sealed class ShortcutRow(ShortcutAction action, Shortcut? shortcut)
    {
        public ShortcutAction Action { get; } = action;
        public string Label => Preferences.Label(Action);
        public string ModifierLabel => Strings.Format("ShortcutModifiers", Label);
        public string KeyLabel => Strings.Format("ShortcutKey", Label);
        public IEnumerable<Option<Modifiers>> ModifierOptions => Preferences.ModifierOptions;
        public IEnumerable<Option<Key>> KeyOptions => Preferences.KeyOptions.Select(option =>
            option.Value == Key.None ? new Option<Key>(Key.None, Strings.Get("KeyOff")) : option);
        public Modifiers Modifiers { get; set; } = shortcut?.Modifiers ?? Preferences.ModifierOptions[0].Value;
        public Key Key { get; set; } = shortcut?.Key ?? Key.None;
        public Shortcut? Binding => Key == Key.None ? null : new Shortcut(Modifiers, Key);
    }
}
