# SpoCue interface behavior

| Capability | Canonical owner | Behavior |
| --- | --- | --- |
| Select/Listbox | Native WPF ComboBox | Named modifier and key controls in each shortcut row. Selecting Off disables the shortcut on apply. |
| Form | SettingsWindow and GlobalHotkeys | Apply registers the complete new shortcut set and saves the preferences. A conflict or write failure keeps the previous set active and leaves the draft editable. Closing Settings hides the window and preserves the draft for the running session. |
| Scrollbar | Native WPF ScrollViewer | The settings body scrolls when the window is shortened. Apply stays visible. |
| Toast | TrackToast | The first available track establishes a baseline. Later track changes show a toast when enabled. A manual shortcut or preview works independently of that setting. One toast is visible at a time. It leaves the foreground window active until clicked to open Spotify. |
| Connection | SpotifySession | Reconnect automatically when Spotify opens or Windows media controls recover. Settings shows the connection state and the current track. |
| Startup | StartupRegistration | Opt-in sign-in launch for the current Windows user. Launches in the tray. |
| Volume keys | VolumeKeys | Opt-in F10 mute, F11 lower, F12 raise. Modifier combinations pass through. |
| Language | Strings | Use Korean when Windows reports Korean and English for other system languages. An explicit choice updates settings and the tray menu when applied and persists across restarts. |

SpoCue uses the Windows media sessions visible to the current desktop user. It keeps the current metadata in memory and persists only local preferences.
