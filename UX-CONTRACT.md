# SpoCue interface behavior

| Capability | Canonical owner | Behavior |
| --- | --- | --- |
| Select/Listbox | Native WPF ComboBox | Named modifier and key controls in each shortcut row. The checkbox next to them turns the shortcut on or off; an unchecked row keeps its key choice and is unregistered on apply. |
| Form | SettingsWindow and GlobalHotkeys | Apply registers the complete new shortcut set and saves the preferences. A conflict or write failure keeps the previous set active and leaves the draft editable. Closing Settings hides the window and preserves the draft for the running session. |
| Scrollbar | Native WPF ScrollViewer | The settings body scrolls when the window is shortened. Apply stays visible. |
| Toast | TrackToast | The first available track establishes a baseline. Later track changes show a toast in the chosen screen corner when enabled. A manual shortcut or preview works independently of that setting. One toast is visible at a time; it fades in and out unless Windows animations are off or high contrast is on. It never takes focus. Left-click opens Spotify (or the update page for an update toast); right-click copies the track. |
| Connection | SpotifySession | Reconnect automatically when Spotify opens or Windows media controls recover. Settings shows the connection state and the current track. |
| Startup | StartupRegistration and App | Opt-in sign-in launch for the current Windows user. Launches in the tray. When Settings does not open at launch, a toast confirms SpoCue is running (or says a setting such as a shortcut conflict needs attention); clicking it opens Settings. |
| Spotify volume | SpotifyVolume | Ctrl+Shift+Up/Down (rebindable) moves only Spotify's app volume in 5% steps on every active output device where Spotify has an audio session, preferring sessions that are playing. The toast shows the level as text and a bar. While Spotify reports playback but no local session is producing sound (Spotify Connect to a phone or speaker), nothing is changed and the toast says Spotify is playing on another device. While paused, idle local sessions are adjusted so the level is ready for the next play. Without any local Spotify session the toast asks for a track to be played on this PC. |
| Copy track | App | Ctrl+Shift+C, the toast's right-click, or the tray menu copies `Artist - Title`. |
| Updates | UpdateChecker | Opt-in daily check of GitHub Releases plus a manual check in the tray menu. Never downloads or installs anything itself. |
| Language | Strings | Use Korean when Windows reports Korean and English for other system languages. An explicit choice updates settings and the tray menu when applied and persists across restarts. |
| About | SettingsWindow | The bottom of Settings shows the version and a note that SpoCue is not affiliated with Spotify AB. |

SpoCue uses the Windows media sessions visible to the current desktop user. It keeps the current metadata in memory and persists only local preferences.
