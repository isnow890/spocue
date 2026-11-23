# SpoCue

SpoCue is a Windows tray utility for controlling the Spotify desktop app from any window. It shows an album-art notification when the track changes and can show the current track on demand.

## Download

Download the Windows x64 ZIP from [GitHub Releases](https://github.com/isnow890/spocue/releases/latest), extract the entire folder, and run `SpoCue.exe`. The ZIP includes the .NET runtime and works on Windows 10 version 2004 or later and Windows 11.

## Default shortcuts

| Action | Shortcut |
| --- | --- |
| Play / pause | Ctrl + Shift + F8 |
| Next track | Ctrl + Shift + F9 |
| Previous track | Ctrl + Shift + F7 |
| Show current track | Ctrl + Shift + F10 |

Open Spotify and play a track. Launch SpoCue and use the tray icon to open Settings, preview a notification, or exit. The icon may be under the taskbar’s hidden icons. In Settings, you can change shortcuts, turn individual shortcuts off, and disable automatic track notifications. The toast appears on the primary display for four seconds. Closing the settings window keeps SpoCue running in the tray. The current track card and toast take their accent and background tint from the cover art.

SpoCue uses the [Windows media session API](https://learn.microsoft.com/en-us/uwp/api/windows.media.control.globalsystemmediatransportcontrolssessionmanager) for the Spotify desktop session. The browser player is outside the current scope. SpoCue needs no Spotify developer credentials or separate sign-in. It makes no Spotify Web API calls, and settings are stored locally in `%LOCALAPPDATA%\SpoCue\settings.json`. Track information and album art are held in memory.

## Optional Windows settings

- **Start with Windows** starts SpoCue in the tray when you sign in. Enable it from the copy of `SpoCue.exe` you intend to keep.
- **Use F10–F12 for system volume** maps plain F10 to mute, F11 to volume down and F12 to volume up. Modifier combinations such as Ctrl + Shift + F10 still work. These keys control the Windows output volume across apps.

Both settings are off by default. To open Spotify, click a track toast or choose **Open Spotify** from the tray menu.

The interface follows the Windows display language by default, using Korean when Windows reports Korean and English otherwise. Choose Korean or English under **Language** for an explicit preference.

## Build and run

Use Windows 10 version 2004 or later, or Windows 11, and the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```powershell
dotnet build TrackRelay.slnx -c Release
dotnet run --project src/TrackRelay/TrackRelay.csproj -- --settings
```

To build a portable Windows x64 folder that includes the .NET runtime:

```powershell
dotnet publish src/TrackRelay/TrackRelay.csproj -c Release -r win-x64 --self-contained true -o artifacts/SpoCue-win-x64
```

Launch `SpoCue.exe` from the published folder.

## License

[MIT](LICENSE). The idea is inspired by [Toastify](https://github.com/nachmore/toastify). SpoCue is an independent project.
