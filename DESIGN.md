---
name: SpoCue
register: product
locale: en, ko
---

# SpoCue design

SpoCue is a Windows tray tool for Spotify listeners working in another app. The settings window leads with the current track and puts shortcut assignments in an aligned list. The song toast uses the same artwork tile and text hierarchy in a smaller surface.

## Colors and typography

The source of truth is `src/TrackRelay/Theme.xaml`. Canvas and Surface use dark blue-gray, with amber for the primary action, connection status and artwork fallback. Ink and Muted set the reading order. Error is reserved for text that explains a recoverable failure. `App.xaml.cs` maps these roles to Windows system brushes in high contrast. WPF Fluent styles supply the native combo boxes and window chrome.

Toast titles use Segoe UI Variable Display (Segoe UI on Windows 10) so track names read like native Windows UI. Segoe UI handles body text, controls and Windows font fallback, including Korean.

## Layout and behavior

The current track identifies the Spotify connection at a glance. The settings body scrolls independently of the Apply button. WPF owns dropdown and scrollbar behavior. The artwork tile uses the actual cover and derives a tinted surface and accent from it. A waveform and the standard theme appear when the session has no cover. High contrast uses Windows system colors. The toast shows the cover, track and artist. It dismisses after four seconds without taking focus. The whole track card opens Spotify when clicked.
