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
    private VolumeKeys? _volumeKeys;
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
        _volumeKeys = new VolumeKeys(Dispatcher, () =>
            ShowToast(new Track("SpoCue", Strings.Get("VolumeError"), ""), canOpenSpotify: false));
        string? optionNotice = null;
        try
        {
            _startWithWindows = StartupRegistration.IsEnabled();
            _volumeKeys.SetEnabled(_preferences.VolumeFunctionKeys);
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

        _spotify.Start();
        if (e.Args.Contains("--settings", StringComparer.OrdinalIgnoreCase) ||
            (!e.Args.Contains(StartupRegistration.LaunchArgument, StringComparer.OrdinalIgnoreCase) &&
             (firstRun || _settingsNotice.Length > 0)))
        {
            OpenSettings();
        }
    }

    internal string? Apply(Preferences preferences, bool startWithWindows)
    {
        try
        {
            var error = _hotkeys!.Apply(preferences, () =>
            {
                var previousVolumeKeys = _volumeKeys!.Enabled;
                _volumeKeys.SetEnabled(preferences.VolumeFunctionKeys);
                try
                {
                    StartupRegistration.Save(startWithWindows, preferences.Save);
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SecurityException)
                {
                    _volumeKeys.SetEnabled(previousVolumeKeys);
                    throw;
                }
            });
            if (error is null)
            {
                var languageChanged = preferences.Language != _preferences.Language;
                _preferences = preferences;
                _startWithWindows = startWithWindows;
                _settingsNotice = null;
                if (languageChanged)
                {
                    RefreshLanguage(preferences.Language);
                }
            }
            return error;
        }
        catch (Win32Exception)
        {
            return Strings.Get("SaveVolumeError");
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

    private void ShowToast(Track track, bool canOpenSpotify = true)
    {
        if (IsExiting)
        {
            return;
        }
        if (_toast is null)
        {
            _toast = new TrackToast();
            _toast.OpenSpotifyRequested += OpenSpotify;
        }
        _toast.ShowTrack(track, canOpenSpotify);
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
        menu.Items.Add(Strings.Get("SettingsMenu"), null, (_, _) => OpenSettings());
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
        var previousMenu = _tray!.ContextMenuStrip;
        _tray.ContextMenuStrip = CreateTrayMenu();
        previousMenu?.Dispose();
    }

    private async void OnShortcut(ShortcutAction action)
    {
        if (action == ShortcutAction.ShowTrack)
        {
            ShowCurrentTrack();
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
        _volumeKeys?.Dispose();
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
