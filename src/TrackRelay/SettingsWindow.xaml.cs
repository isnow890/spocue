using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    private ToastCorner _selectedCorner;

    public SettingsWindow(App app, Preferences preferences, bool startWithWindows)
    {
        _app = app;
        _selectedCorner = preferences.ToastCorner;
        InitializeComponent();
        _rows = Preferences.Actions.Select(action =>
            new ShortcutRow(action, preferences.Shortcuts[action])).ToArray();
        RefreshLanguage(preferences.Language);
        TrackChanges.IsChecked = preferences.ShowTrackChanges;
        StartWithWindows.IsChecked = startWithWindows;
        CheckUpdatesEnabled.IsChecked = preferences.CheckUpdates;
        if (App.StoreBuild)
        {
            StartWithWindows.Visibility = Visibility.Collapsed;
            CheckUpdatesEnabled.Visibility = Visibility.Collapsed;
            StoreStartupSettings.Visibility = Visibility.Visible;
        }
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
        if (ToastCornerChoice.SelectedValue is ToastCorner currentCorner)
        {
            _selectedCorner = currentCorner;
        }

        ShortcutRows.ItemsSource = null;
        ShortcutRows.ItemsSource = _rows;
        ToastCornerChoice.ItemsSource = new Option<ToastCorner>[]
        {
            new(ToastCorner.BottomRight, Strings.Get("CornerBottomRight")),
            new(ToastCorner.TopRight, Strings.Get("CornerTopRight")),
            new(ToastCorner.BottomLeft, Strings.Get("CornerBottomLeft")),
            new(ToastCorner.TopLeft, Strings.Get("CornerTopLeft"))
        };
        ToastCornerChoice.SelectedValue = _selectedCorner;
        LanguageChoice.ItemsSource = new Option<AppLanguage>[]
        {
            new(AppLanguage.System, Strings.Get("LanguageSystem")),
            new(AppLanguage.Korean, "한국어"),
            new(AppLanguage.English, "English")
        };
        LanguageChoice.SelectedValue = language;
        AboutLine.Text = Strings.Format("AboutLine", UpdateChecker.CurrentVersion);
    }

    public void SetConnectionStatus(string text) => SetLiveText(ConnectionStatus, text);

    public void SetFeedback(string text, bool error)
    {
        Feedback.SetResourceReference(TextBlock.ForegroundProperty, error ? "ErrorBrush" : "MutedBrush");
        SetLiveText(Feedback, text);
    }

    private void PreviewClick(object sender, RoutedEventArgs e) => _app.ShowPreview();

    private async void OpenStartupSettings(object sender, RoutedEventArgs e)
    {
        if (!await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:startupapps")))
            SetFeedback(Strings.Get("LoadOptionsError"), true);
    }

    private void ApplyClick(object sender, RoutedEventArgs e)
    {
        var corner = (ToastCorner)(ToastCornerChoice.SelectedValue ?? ToastCorner.BottomRight);
        _selectedCorner = corner;
        var preferences = new Preferences(TrackChanges.IsChecked == true,
            _rows.ToDictionary(row => row.Action, row => row.Binding),
            (AppLanguage)(LanguageChoice.SelectedValue ?? AppLanguage.System),
            CheckUpdatesEnabled.IsChecked == true,
            corner);
        var error = _app.Apply(preferences, StartWithWindows.IsChecked == true);
        SetFeedback(error ?? Strings.Get("ChangesApplied"), error is not null);
    }

    private static void SetLiveText(TextBlock label, string text)
    {
        label.Text = text;
        UIElementAutomationPeer.FromElement(label)?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
    }

    private sealed class ShortcutRow : INotifyPropertyChanged
    {
        private bool _isEnabled;

        public ShortcutRow(ShortcutAction action, Shortcut? shortcut)
        {
            Action = action;
            var fallback = Preferences.DefaultBindings[action];
            _isEnabled = shortcut is not null;
            Modifiers = shortcut?.Modifiers ?? fallback.Modifiers;
            Key = shortcut?.Key ?? fallback.Key;
        }

        public ShortcutAction Action { get; }
        public string Label => Preferences.Label(Action);
        public string ModifierLabel => Strings.Format("ShortcutModifiers", Label);
        public string KeyLabel => Strings.Format("ShortcutKey", Label);
        public IEnumerable<Option<Modifiers>> ModifierOptions => Preferences.ModifierOptions;
        public IEnumerable<Option<Key>> KeyOptions => Preferences.KeyOptions;

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEnabled)));
                }
            }
        }

        public Modifiers Modifiers { get; set; }
        public Key Key { get; set; }
        public Shortcut? Binding => IsEnabled ? new Shortcut(Modifiers, Key) : null;

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
