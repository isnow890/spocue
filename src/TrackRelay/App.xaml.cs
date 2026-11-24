using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

namespace TrackRelay;

public partial class App : Application
{
    private Mutex? _instance;
    private bool _ownsInstance;
    private SpotifySession? _spotify;
    private GlobalHotkeys? _hotkeys;
    private UpdateChecker? _updater;
    private bool _startWithWindows;
    private Forms.NotifyIcon? _tray;
    private Drawing.Icon? _trayIcon;
    private TrackToast? _toast;
    private SettingsWindow? _settings;
    private Preferences _preferences = Preferences.Default;
    private string? _settingsNotice;

    internal bool IsExiting { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _instance = new Mutex(initiallyOwned: true, @"Local\TrackRelay", out _ownsInstance);
        if (!_ownsInstance)
        {
            Shutdown();
            return;
        }

        UpdateTheme();
        SystemParameters.StaticPropertyChanged += OnSystemParametersChanged;
        Strings.Apply(AppLanguage.System);
        _preferences = Preferences.Load(out var firstRun, out _settingsNotice);
        if (_preferences.Language != AppLanguage.System)
        {
            Strings.Apply(_preferences.Language);
        }
        _hotkeys = new GlobalHotkeys();
        _hotkeys.Pressed += OnShortcut;
        var hotkeyNotice = _hotkeys.Apply(_preferences);
        string? optionNotice = null;
        try
        {
            _startWithWindows = StartupRegistration.IsEnabled();
        }
        catch (Exception exception) when (exception is Win32Exception or IOException or UnauthorizedAccessException or SecurityException)
        {
            optionNotice = Strings.Get("LoadOptionsError");
        }

        _settingsNotice = string.Join(Environment.NewLine, new[] { _settingsNotice, hotkeyNotice, optionNotice }
            .Where(notice => !string.IsNullOrWhiteSpace(notice)));

        _spotify = new SpotifySession();
        _spotify.StatusChanged += status => _settings?.SetConnectionStatus(status);
        _spotify.CurrentTrackChanged += (track, showToast) =>
        {
            _settings?.SetCurrentTrack(track);
            if (showToast && track is not null && _preferences.ShowTrackChanges)
            {
                ShowToast(track);
            }
        };

        _trayIcon = CreateTrayIcon();
        _tray = new Forms.NotifyIcon
        {
            Text = "SpoCue",
            Icon = _trayIcon,
            ContextMenuStrip = CreateTrayMenu(),
            Visible = true
        };
        _tray.MouseDoubleClick += (_, args) =>
        {
            if (args.Button == Forms.MouseButtons.Left)
            {
                OpenSettings();
            }
        };

        _updater = new UpdateChecker();
        _updater.UpdateAvailable += OnUpdateAvailable;
        _updater.SetEnabled(_preferences.CheckUpdates);

        _spotify.Start();
        if (e.Args.Contains("--settings", StringComparer.OrdinalIgnoreCase) ||
            (!e.Args.Contains(StartupRegistration.LaunchArgument, StringComparer.OrdinalIgnoreCase) &&
             (firstRun || _settingsNotice.Length > 0)))
        {
            OpenSettings();
        }
        else
        {
            // A tray-only launch (e.g. Windows sign-in) is otherwise invisible: confirm it is running,
            // and surface any startup problem (such as a shortcut conflict) instead of failing silently.
            ShowStartupToast();
        }
    }

    private void ShowStartupToast()
    {
        var titleKey = string.IsNullOrWhiteSpace(_settingsNotice) ? "StartupTitle" : "StartupNoticeTitle";
        ShowToast(new Track(Strings.Get(titleKey), Strings.Get("StartupSubtitle"), ""),
            canOpenSpotify: false, clickAction: OpenSettings);
    }

    internal string? Apply(Preferences preferences, bool startWithWindows)
    {
        try
        {
            var error = _hotkeys!.Apply(preferences, () =>
            {
                StartupRegistration.Save(startWithWindows, preferences.Save);
            });
            if (error is null)
            {
                var languageChanged = preferences.Language != _preferences.Language;
                _preferences = preferences;
                _startWithWindows = startWithWindows;
                _settingsNotice = null;
                _updater?.SetEnabled(preferences.CheckUpdates);
                if (languageChanged)
                {
                    RefreshLanguage(preferences.Language);
                }
            }
            return error;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SecurityException)
        {
            return Strings.Get("SaveSettingsError");
        }
    }

    internal void ShowPreview() => ShowToast(_spotify?.CurrentTrack ??
        new Track(Strings.Get("PreviewTitle"), Strings.Get("PreviewSubtitle"), ""));

    private void ShowCurrentTrack() => ShowToast(_spotify?.CurrentTrack ??
        new Track(Strings.Get("WaitingSpotify"), Strings.Get("PlayFirst"), ""));

    private void AdjustSpotifyVolume(int direction)
    {
        var current = _spotify?.CurrentTrack;
        // While Spotify is playing, only a session that is actually producing sound here counts.
        // When paused, idle sessions may still be adjusted so the level is ready for the next play.
        var result = SpotifyVolume.TryAdjust(direction, allowIdleSessions: _spotify?.IsPlaying != true, out var percent);
        if (result == VolumeResult.PlayingElsewhere)
        {
            ShowToast(new Track(Strings.Get("SpotifyVolumeElsewhereTitle"), Strings.Get("SpotifyVolumeElsewhereHint"), "",
                current?.Artwork, current?.ArtworkColor), canOpenSpotify: current is not null);
            return;
        }
        if (result != VolumeResult.Adjusted)
        {
            ShowToast(new Track(Strings.Get("SpotifyVolumeUnavailable"), "SpoCue", "",
                current?.Artwork, current?.ArtworkColor), canOpenSpotify: current is not null);
            return;
        }

        var volumeLabel = Strings.Format("SpotifyVolumeToast", percent);
        var toastTrack = current is not null
            ? current with { Artist = volumeLabel }
            : new Track("Spotify", volumeLabel, "");
        ShowToast(toastTrack, canOpenSpotify: true, volumePercent: percent);
    }

    private void CopyCurrentTrack()
    {
        var current = _spotify?.CurrentTrack;
        if (current is null || string.IsNullOrWhiteSpace(current.Title))
        {
            ShowToast(new Track(Strings.Get("WaitingSpotify"), Strings.Get("PlayFirst"), ""), canOpenSpotify: false);
            return;
        }

        var text = string.IsNullOrWhiteSpace(current.Artist)
            ? current.Title
            : $"{current.Artist} - {current.Title}";
        try
        {
            Clipboard.SetDataObject(text, copy: true);
            ShowToast(current with { Artist = $"{Strings.Get("TrackCopiedToast")}: {text}" }, canOpenSpotify: true);
        }
        catch (Exception exception) when (exception is System.Runtime.InteropServices.ExternalException or ThreadStateException)
        {
            ShowToast(new Track("SpoCue", Strings.Get("CopyTrackError"), "",
                current.Artwork, current.ArtworkColor), canOpenSpotify: false);
        }
    }

    private void ShowToast(Track track, bool canOpenSpotify = true, Action? clickAction = null, int? volumePercent = null)
    {
        if (IsExiting)
        {
            return;
        }
        if (_toast is null)
        {
            _toast = new TrackToast();
            _toast.OpenSpotifyRequested += OpenSpotify;
            _toast.CopyTrackRequested += CopyCurrentTrack;
        }
        _toast.ShowTrack(track, canOpenSpotify, clickAction, _preferences.ToastCorner, volumePercent);
    }

    private void OnUpdateAvailable(ReleaseUpdate update)
    {
        RebuildTrayMenu();
        ShowToast(new Track(
            Strings.Format("UpdateAvailableTitle", update.Version),
            Strings.Get("UpdateAvailableSubtitle"),
            ""),
            canOpenSpotify: false,
            clickAction: () => UpdateChecker.OpenReleasePage(update.Url));
    }

    private async void CheckForUpdatesManual()
    {
        if (_updater is null)
        {
            return;
        }

        if (_updater.LatestUpdate is { } cached)
        {
            UpdateChecker.OpenReleasePage(cached.Url);
            return;
        }

        var (update, failed) = await _updater.CheckNowAsync();
        if (failed)
        {
            ShowToast(new Track("SpoCue", Strings.Get("UpdateCheckError"), ""), canOpenSpotify: false);
            return;
        }

        if (update is not null)
        {
            OnUpdateAvailable(update);
            return;
        }

        ShowToast(new Track(
            Strings.Format("UpToDateTitle", UpdateChecker.CurrentVersion),
            Strings.Get("UpToDateSubtitle"),
            ""), canOpenSpotify: false);
    }

    private void RebuildTrayMenu()
    {
        if (_tray is null)
        {
            return;
        }
        var previousMenu = _tray.ContextMenuStrip;
        _tray.ContextMenuStrip = CreateTrayMenu();
        previousMenu?.Dispose();
    }

    private void OpenSpotify()
    {
        if (!SpotifyWindow.Open())
        {
            ShowToast(new Track("SpoCue", Strings.Get("OpenSpotifyError"), ""), canOpenSpotify: false);
        }
    }

    private Forms.ContextMenuStrip CreateTrayMenu()
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add(Strings.Get("OpenSpotify"), null, (_, _) => OpenSpotify());
        menu.Items.Add(Strings.Get("ActionShowTrack"), null, (_, _) => ShowCurrentTrack());
        menu.Items.Add(Strings.Get("ActionCopyTrack"), null, (_, _) => CopyCurrentTrack());
        menu.Items.Add(Strings.Get("SettingsMenu"), null, (_, _) => OpenSettings());
        var updateLabel = _updater?.LatestUpdate is { } update
            ? Strings.Format("UpdateAvailableMenu", update.Version)
            : Strings.Get("CheckUpdatesMenu");
        menu.Items.Add(updateLabel, null, (_, _) => CheckForUpdatesManual());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(Strings.Get("ExitMenu"), null, (_, _) => ExitApplication());
        return menu;
    }

    private void RefreshLanguage(AppLanguage language)
    {
        Strings.Apply(language);
        _settings?.RefreshLanguage(language);
        _settings?.SetConnectionStatus(_spotify!.Status);
        _settings?.SetCurrentTrack(_spotify!.CurrentTrack);
        RebuildTrayMenu();
    }

    private async void OnShortcut(ShortcutAction action)
    {
        switch (action)
        {
            case ShortcutAction.ShowTrack:
                ShowCurrentTrack();
                return;
            case ShortcutAction.SpotifyVolumeUp:
                AdjustSpotifyVolume(1);
                return;
            case ShortcutAction.SpotifyVolumeDown:
                AdjustSpotifyVolume(-1);
                return;
            case ShortcutAction.CopyTrack:
                CopyCurrentTrack();
                return;
        }

        var error = await _spotify!.RunAsync(action);
        if (error is not null)
        {
            ShowToast(new Track("SpoCue", error, ""), canOpenSpotify: false);
        }
    }

    private void OpenSettings()
    {
        if (_settings is null)
        {
            _settings = new SettingsWindow(this, _preferences, _startWithWindows)
            {
                Icon = Imaging.CreateBitmapSourceFromHIcon(_trayIcon!.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions())
            };
            _settings.SetConnectionStatus(_spotify!.Status);
            _settings.SetCurrentTrack(_spotify.CurrentTrack);
            if (!string.IsNullOrWhiteSpace(_settingsNotice))
            {
                _settings.SetFeedback(_settingsNotice, error: true);
            }
        }

        _settings.Show();
        if (_settings.WindowState == WindowState.Minimized)
        {
            _settings.WindowState = WindowState.Normal;
        }
        _settings.Activate();
    }

    private void OnSystemParametersChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(SystemParameters.HighContrast))
        {
            UpdateTheme();
            _settings?.SetCurrentTrack(_spotify?.CurrentTrack);
            _toast?.RefreshPalette();
        }
    }

    private void UpdateTheme()
    {
        // Fluent supplies the native dark controls and non-client chrome in .NET 10.
#pragma warning disable WPF0001
        ThemeMode = SystemParameters.HighContrast ? System.Windows.ThemeMode.None : System.Windows.ThemeMode.Dark;
#pragma warning restore WPF0001
        (string Key, Brush Value)[] overrides =
        [
            ("CanvasBrush", SystemColors.WindowBrush),
            ("SurfaceBrush", SystemColors.WindowBrush),
            ("InkBrush", SystemColors.WindowTextBrush),
            ("MutedBrush", SystemColors.WindowTextBrush),
            ("BorderBrush", SystemColors.WindowTextBrush),
            ("AccentBrush", SystemColors.HighlightBrush),
            ("AccentTextBrush", SystemColors.HighlightTextBrush),
            ("AccentSoftBrush", SystemColors.WindowBrush),
            ("ErrorBrush", SystemColors.WindowTextBrush)
        ];
        foreach (var (key, value) in overrides)
        {
            if (SystemParameters.HighContrast)
            {
                Resources[key] = value;
            }
            else
            {
                Resources.Remove(key);
            }
        }
    }

    private static Drawing.Icon CreateTrayIcon()
    {
        using var stream = GetResourceStream(new Uri("Assets/SpoCue.ico", UriKind.Relative)).Stream;
        using var icon = new Drawing.Icon(stream, 32, 32);
        return (Drawing.Icon)icon.Clone();
    }

    private void ExitApplication()
    {
        IsExiting = true;
        Shutdown();
    }

    protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
    {
        IsExiting = true;
        base.OnSessionEnding(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        IsExiting = true;
        SystemParameters.StaticPropertyChanged -= OnSystemParametersChanged;
        _spotify?.Dispose();
        _hotkeys?.Dispose();
        _updater?.Dispose();
        if (_tray is not null)
        {
            _tray.Visible = false;
            _tray.ContextMenuStrip?.Dispose();
            _tray.Dispose();
        }
        _trayIcon?.Dispose();
        if (_ownsInstance)
        {
            _instance?.ReleaseMutex();
        }
        _instance?.Dispose();
        base.OnExit(e);
    }

}
