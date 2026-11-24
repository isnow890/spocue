<p align="center">
  <img src="문서/이미지/아이콘.png" width="116" height="116" alt="SpoCue app icon" />
</p>

<h1 align="center">SpoCue</h1>

<p align="center">
  <strong>Spotify hotkeys, per-app volume control, and album-colored track toasts for Windows.</strong>
</p>

<p align="center">
  <a href="https://github.com/isnow890/spocue/actions/workflows/build.yml"><img src="https://img.shields.io/github/actions/workflow/status/isnow890/spocue/build.yml?label=build&style=flat-square" alt="CI Build" /></a>
  <a href="https://github.com/isnow890/spocue/releases/latest"><img src="https://img.shields.io/github/v/release/isnow890/spocue?style=flat-square" alt="Latest Release" /></a>
  <img src="https://img.shields.io/badge/Windows-10%20%2F%2011%20(x64)-0078D4?style=flat-square&logo=windows&logoColor=white" alt="Windows 10 / 11" />
  <img src="https://img.shields.io/badge/.NET-10%20WPF-512BD4?style=flat-square&logo=dotnet&logoColor=white" alt=".NET 10 WPF" />
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-MIT-blue?style=flat-square" alt="MIT License" /></a>
</p>

<p align="center">
  <strong>English</strong> · <a href="README.ko.md">한국어</a>
</p>

---

## Why SpoCue?

If you used **Toastify** years ago, you probably miss controlling Spotify from the keyboard without alt-tabbing out of your editor or game. Toastify stopped working on modern Spotify builds, and Windows media keys only help if your keyboard actually has them.

**SpoCue** is a small, native **C# / WPF** tray app built for Windows 10 and 11. No Electron, no background service, and no Spotify Developer API setup.

- **Global Spotify Shortcuts**: Play/pause, skip tracks, adjust **Spotify's own app volume** (`Ctrl + Shift + Up/Down`), copy the current song title (`Ctrl + Shift + C`), or pop up the current song from any window. Each shortcut row has its own checkbox so you can toggle individual hotkeys on or off without losing your key binding.
- **Per-App Spotify Volume Control**: Uses the Windows CoreAudio session API (`ISimpleAudioVolume`) to turn **only Spotify.exe** up or down in 5% steps while leaving your game, voice chat, or browser volume untouched. It finds Spotify on whichever output it plays through (speakers, headset, or a per-app output set in Windows), and the toast shows a volume bar.
- **Album-Colored Track Toasts**:
  - Picks its background and accent colors directly from the current album cover (`ArtworkPalette`), and switches to system colors automatically when Windows High Contrast is on.
  - Shows up for 4 seconds on track change without stealing window or keyboard focus (`WS_EX_NOACTIVATE`), wrapping longer song titles cleanly across two lines, and lets you choose any screen corner (**Bottom Right**, **Top Right**, **Bottom Left**, or **Top Left**).
  - **Left-Click to Open, Right-Click to Copy**: Left-click the toast to bring the Spotify desktop window straight to the front, or **right-click the toast** to copy `Artist - Title` to your clipboard.
- **No Account or API Key Needed**: Talks directly to the Spotify desktop app (`Spotify.exe` and the Microsoft Store build) through the Windows media session API (SMTC). You don't need to log in or create a Spotify Developer token.
- **Start with Windows, Korean/English UI & Update Checker**: Can launch quietly in the tray when you sign in, follows your Windows language (Korean or English), and checks GitHub Releases once a day (toggleable in Settings) or on demand from the tray menu.

<p align="center">
  <img src="문서/이미지/토스트-알림.png" width="320" alt="SpoCue track toast" />
</p>

<p align="center">
  <img src="문서/이미지/설정-화면-en.png" width="460" alt="SpoCue settings window" />
</p>

### At a Glance

| Feature | SpoCue | Classic Toastify | Windows Media Overlay |
| :--- | :---: | :---: | :---: |
| Works with current Spotify desktop (Win32 & Store) | ✅ | ⚠️ (Broken on newer builds) | ✅ |
| Custom shortcuts on keyboards without media keys | ✅ | ✅ | — |
| Per-app Spotify volume control (leaves game/system volume alone) | ✅ | ✅ | — |
| One-key / right-click track info copy to clipboard | ✅ | ✅ | — |
| Safe rollback when a shortcut conflicts | ✅ | — | — |
| Album-colored toast that never steals focus | ✅ | — | — |
| Click toast to bring Spotify to the front | ✅ | ✅ | — |
| No API token, no external dependencies, MIT open source | ✅ | ✅ (MIT) | Proprietary |

---

## Installation

### Option 1: Installer (`Setup.exe` — Recommended)

1. Download `spocue-vX.Y.Z-win-x64-setup.exe` from [Releases](https://github.com/isnow890/spocue/releases/latest).
2. Run the installer. It installs to `%LOCALAPPDATA%\Programs\SpoCue` without asking for admin rights, adds a Start Menu shortcut, and registers an uninstaller in Windows *Installed Apps*.
3. Launch **SpoCue** at the end of setup or from the Start Menu.

### Option 2: Portable ZIP (`.zip`)

1. Download `spocue-vX.Y.Z-win-x64.zip` from [Releases](https://github.com/isnow890/spocue/releases/latest).
2. Extract the folder wherever you want to keep it and run `SpoCue.exe`. The .NET 10 runtime is bundled in both the installer and the ZIP.

### Option 3: Build from Source

Requires Windows 10 (2004+) or Windows 11, and the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```powershell
git clone https://github.com/isnow890/spocue.git
cd spocue

dotnet build TrackRelay.slnx -c Release
dotnet run --project src/TrackRelay/TrackRelay.csproj -- --settings
```

To build the installer (`Setup.exe`), portable `.zip`, and `SHA256SUMS.txt` (requires Inno Setup 6):

```powershell
powershell -ExecutionPolicy Bypass -File 배포/패키지-빌드.ps1 -Version 0.5.0
```

---

## First Launch & Shortcuts

The first time you run SpoCue, the Settings window opens and a waveform icon appears in the system tray (check the hidden tray overflow `^` if it isn't visible). Closing the Settings window leaves SpoCue running in the tray — double-click the tray icon or right-click and choose **Settings** whenever you want it back.

| Action | Default Shortcut | Available Keys |
| :--- | :--- | :--- |
| **Play / Pause** | `Ctrl + Shift + F8` | Checkbox toggle + `Ctrl+Alt`, `Ctrl+Shift`, `Alt+Shift`, `Ctrl+Alt+Shift`, or `Win+Shift` + `F1`–`F12`, Arrows, `A`–`Z`, `0`–`9` |
| **Next Track** | `Ctrl + Shift + F9` | Same modifier + key options |
| **Previous Track** | `Ctrl + Shift + F7` | Same modifier + key options |
| **Show Current Track** | `Ctrl + Shift + F10` | Pops up the 4-second track toast on demand, even if automatic track change toasts are turned off |
| **Spotify Volume Up** | `Ctrl + Shift + Up` | Raises only Spotify's volume by 5% (Windows CoreAudio per-app session) |
| **Spotify Volume Down** | `Ctrl + Shift + Down` | Lowers only Spotify's volume by 5% without changing game or system volume |
| **Copy Current Track** | `Ctrl + Shift + C` | Copies `Artist - Title` to the clipboard (also available by **right-clicking** a toast or from the tray menu) |

---

## Command-Line Options & Paths

```powershell
# Open the Settings window on launch
SpoCue.exe --settings

# Start quietly in the tray (used by "Start with Windows")
SpoCue.exe --startup
```

- **Settings File**: `%LOCALAPPDATA%\SpoCue\settings.json`
- **Startup Registry Entry**: `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` (`SpoCue`)

---

## Privacy & Notes

1. **No Telemetry & Opt-Out Update Check**: Track titles and 144px cover thumbnails stay in memory while playing, and only your settings are saved to `%LOCALAPPDATA%\SpoCue\settings.json`. When *"Check for updates automatically"* is enabled, SpoCue sends one unauthenticated `GET` request per day to `https://api.github.com/repos/isnow890/spocue/releases/latest` to compare the version tag. You can turn this off anytime in Settings.
2. **Desktop App Only**: SpoCue only listens to the Spotify desktop app (`Spotify.exe` or the Store version) so browser tabs like YouTube don't hijack your music shortcuts. The Spotify web player isn't supported.
3. **Moving the Portable Folder**: *"Start with Windows"* saves the path of the `SpoCue.exe` you ran it from. If you move a portable folder later, toggle *"Start with Windows"* off and back on.

<details>
<summary><strong>Troubleshooting / FAQ</strong></summary>

- **It says *"Waiting for Spotify"* even though Spotify is open**: Play any song once in Spotify desktop so Windows creates the media session, and make sure *"Show desktop overlay when using media keys"* is turned on in Spotify's settings.
- **Saving a shortcut says it can't be registered**: Windows only lets one program register a global shortcut at a time. Another app (like GeForce Overlay, PowerToys, or a screen recorder) is already using that combo.
- **No toast appears when the song changes**: Check that *"Show notification when track changes"* is enabled. The song already playing when SpoCue starts is treated as the starting point, and toasts appear from the next track change onward. Use **Preview toast** or `Ctrl + Shift + F10` to test right away.
- **Clicking the toast doesn't open Spotify**: If Spotify was closed or hidden to the tray without a main window, open Spotify once first.

</details>

---

## Project Layout

```text
src/TrackRelay/
├── App.xaml(.cs)             # Single-instance check, tray icon, dark theme & High Contrast handling
├── SpotifySession.cs         # Windows SMTC session lookup, playback controls & 144px cover loader
├── SpotifyVolume.cs          # Windows CoreAudio (ISimpleAudioVolume) per-app volume control for Spotify.exe
├── SpotifyWindow.cs          # Finds and activates the main Spotify.exe window
├── GlobalHotkeys.cs          # Win32 RegisterHotKey bindings with rollback on conflict
├── ArtworkPalette.cs         # Extracts background and accent colors from the album cover
├── ArtworkBadge.xaml(.cs)    # Album cover thumbnail with waveform fallback
├── TrackToast.xaml(.cs)      # Non-focus-stealing 4-second track notification window
├── SettingsWindow.xaml(.cs)  # Live track card, checkbox shortcut rows & preferences UI
├── StartupRegistration.cs    # HKCU\...\Run startup entry helper
├── Preferences.cs            # Reads and writes %LOCALAPPDATA%\SpoCue\settings.json
├── Strings.cs                # Korean & English UI strings
├── Theme.xaml                # Colors and control styles
└── UpdateChecker.cs          # GitHub Releases update checker (daily opt-out + manual tray check)
```

---

## Code Signing Policy

Free code signing is provided by [SignPath.io](https://signpath.io/), and the certificate is provided by the [SignPath Foundation](https://signpath.org/).

- **Authors, Reviewers, and Approvers**: [@isnow890](https://github.com/isnow890) (Project Maintainer)
- **Privacy Policy**: This program will not transfer any information to other networked systems unless specifically requested by the user or the person installing or operating it (limited to optional GitHub Releases version checks when enabled).

---

## License

[MIT License](LICENSE). Inspired by [Toastify](https://github.com/nachmore/toastify).

## Trademark notice

SpoCue is an independent project. It is not affiliated with, endorsed by, or sponsored by Spotify AB. Spotify is a trademark of Spotify AB and is used here only to describe which app SpoCue works with.
