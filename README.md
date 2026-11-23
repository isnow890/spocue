<p align="center">
  <img src="문서/이미지/아이콘.png" width="116" height="116" alt="SpoCue app icon" />
</p>

<h1 align="center">SpoCue</h1>

<p align="center">
  <strong>Lightweight native Windows tray app for Spotify global hotkeys, album-tinted track toasts &amp; F10–F12 volume keys.</strong>
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

**SpoCue** brings back the fast, unobtrusive workflow of classic **Toastify** for modern Windows 10 & 11 and current Spotify desktop builds — written as a lightweight, zero-account, native **C# / WPF** tray application with no Electron overhead and no Spotify Web API setup.

- **Global Spotify Hotkeys From Any App**: Control **Play / Pause**, **Next Track**, **Previous Track**, and **Show Current Track** globally while coding, gaming, or working in full-screen apps. Each shortcut can be customized or turned off individually.
- **Album-Art Tinted Track Toast**:
  - Automatically extracts a dominant palette from the current album cover (`ArtworkPalette`) to tint the toast card and settings preview.
  - Appears cleanly on the primary display for 4 seconds when the track changes without stealing keyboard or window focus.
  - **Click to Focus Spotify**: Click anywhere on the toast notification (or current track card) to bring the Spotify desktop window right to the front.
- **Zero Account / Zero Web API Setup**: Talks directly to the Spotify desktop session via the native Windows `GlobalSystemMediaTransportControlsSessionManager` (SMTC) API. No Spotify Premium API tokens, OAuth logins, or network polling required.
- **macOS-Style `F10`–`F12` System Volume Keys**: Optional low-level keyboard hook that maps bare `F10` (Mute), `F11` (Volume Down), and `F12` (Volume Up) to system audio while allowing modifier combinations like `Ctrl + Shift + F10` to pass through normally.
- **Start with Windows & Bilingual UI**: Optional per-user startup registration (`HKCU\...\Run` with `--startup`) and automatic **Korean / English** localization with manual language override.

<p align="center">
  <img src="문서/이미지/토스트-알림.png" width="320" alt="SpoCue Album-Tinted Track Toast" />
</p>

<p align="center">
  <img src="문서/이미지/설정-화면.png" width="460" alt="SpoCue Settings Window" />
</p>

### Quick Comparison

| Capability | SpoCue | Classic Toastify | Windows Game Bar / Media Keys |
| :--- | :---: | :---: | :---: |
| Works with modern Spotify desktop (Windows 10/11 SMTC) | ✅ | ⚠️ (Broken on newer builds) | ✅ |
| Custom global hotkeys on keyboards without media keys | ✅ | ✅ | — |
| Album-art adaptive color toast without stealing focus | ✅ | — | — |
| Click toast to bring Spotify window to front | ✅ | ✅ | — |
| Optional macOS-style `F10` / `F11` / `F12` volume control | ✅ | — | — |
| Zero Web API token / zero third-party account, MIT open source | ✅ | ✅ | Proprietary |

---

## Installation

### Option 1: Windows Installer (`Setup.exe` — Recommended)

1. Download `spocue-vX.Y.Z-win-x64-setup.exe` from [GitHub Releases](https://github.com/isnow890/spocue/releases/latest).
2. Run the installer. It installs SpoCue into `%LOCALAPPDATA%\Programs\SpoCue` (no administrator prompt required), creates Start Menu / optional Desktop shortcuts, and registers an uninstaller in Windows Installed Apps.
3. Launch **SpoCue** from the final installer step or Start Menu.

### Option 2: Portable ZIP (`win-x64.zip`)

1. Download `spocue-vX.Y.Z-win-x64.zip` from [GitHub Releases](https://github.com/isnow890/spocue/releases/latest).
2. Extract the archive to a permanent folder and run `SpoCue.exe`. Both the installer and portable ZIP bundle the .NET runtime (self-contained).

### Option 3: Build from Source

Requires Windows 10 version 2004+ or Windows 11 and the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```powershell
git clone https://github.com/isnow890/spocue.git
cd spocue

dotnet build TrackRelay.slnx -c Release
dotnet run --project src/TrackRelay/TrackRelay.csproj -- --settings
```

Package a self-contained portable ZIP, Windows installer (`Setup.exe`), and `SHA256SUMS.txt`:

```powershell
powershell -ExecutionPolicy Bypass -File 배포/패키지-빌드.ps1 -Version 0.1.0
```

---

## Default Shortcuts & Options

| Action | Default Shortcut | Notes |
| :--- | :--- | :--- |
| **Play / Pause** | `Ctrl + Shift + F8` | Custom modifier (`Ctrl+Alt`, `Ctrl+Shift`, `Alt+Shift`, `Ctrl+Alt+Shift`) + key, or `Off` |
| **Next Track** | `Ctrl + Shift + F9` | Skips to the next track in Spotify desktop |
| **Previous Track** | `Ctrl + Shift + F7` | Returns to the previous track in Spotify desktop |
| **Show Current Track** | `Ctrl + Shift + F10` | Shows the 4-second album-art toast on demand |
| **System Volume (Optional)** | `F10` / `F11` / `F12` | Mute / Volume Down / Volume Up when enabled in Settings |

- **Start with Windows**: Launches `SpoCue.exe --startup` quietly in the system tray at user sign-in.
- **Language**: Follows the Windows display language by default (Korean on Korean Windows, English otherwise), or explicitly select **한국어** / **English**.
- **Local Configuration**: Preferences are saved in `%LOCALAPPDATA%\SpoCue\settings.json`. No telemetry or external network requests are made.

---

## Project Structure

```text
src/TrackRelay/
├── App.xaml(.cs)             # Single-instance tray entry point, dynamic theme & high-contrast sync
├── SpotifySession.cs         # Windows SMTC media session discovery, playback controls & cover loader
├── SpotifyWindow.cs          # Win32 window handle lookup & foreground activation for Spotify.exe
├── GlobalHotkeys.cs          # Win32 RegisterHotKey manager with atomic apply & rollback on conflict
├── VolumeKeys.cs             # Optional WH_KEYBOARD_LL hook for bare F10 / F11 / F12 volume control
├── ArtworkPalette.cs         # Cover image color sampling & contrast-aware surface/accent tinting
├── TrackToast.xaml(.cs)      # Non-activating 4-second track notification toast window
├── SettingsWindow.xaml(.cs)  # Live track preview card, hotkey form & preferences UI
├── StartupRegistration.cs    # Current-user Windows Run registry integration
├── Preferences.cs            # JSON settings persistence (%LOCALAPPDATA%\SpoCue\settings.json)
└── Strings.cs                # Built-in English & Korean localization dictionary
```

---

## License

Released under the [MIT License](LICENSE). Inspired by the classic [Toastify](https://github.com/nachmore/toastify) utility; SpoCue is an independent clean-room project.
