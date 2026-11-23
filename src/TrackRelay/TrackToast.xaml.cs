using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;

namespace TrackRelay;

internal sealed partial class TrackToast : Window
{
    private Track? _track;
    private readonly DispatcherTimer _dismiss = new() { Interval = TimeSpan.FromSeconds(4) };
    public event Action? OpenSpotifyRequested;
    public event Action? CopyTrackRequested;

    public TrackToast()
    {
        InitializeComponent();
        _dismiss.Tick += (_, _) =>
        {
            _dismiss.Stop();
            Hide();
        };
        Closed += (_, _) => _dismiss.Stop();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var handle = new WindowInteropHelper(this).Handle;
        const int extendedStyle = -20;
        const int noActivate = 0x08000000;
        SetWindowLong(handle, extendedStyle, GetWindowLong(handle, extendedStyle) | noActivate);
    }

    public void ShowTrack(Track track, bool canOpenSpotify)
    {
        _track = track;
        OpenSpotifyButton.IsEnabled = canOpenSpotify;
        RefreshPalette();
        TrackTitle.Text = track.Title;
        TrackSubtitle.Text = track.Artist;
        Cover.Artwork = track.Artwork;
        var workArea = SystemParameters.WorkArea;
        Width = Math.Min(360, workArea.Width - 24);
        Left = workArea.Right - Width - 16;
        Top = workArea.Bottom - Height - 16;
        _dismiss.Stop();
        Show();
        _dismiss.Start();
    }

    public void RefreshPalette() => ArtworkPalette.Apply(ToastFrame, _track?.ArtworkColor);

    private void OpenSpotifyClick(object sender, RoutedEventArgs e)
    {
        _dismiss.Stop();
        Hide();
        OpenSpotifyRequested?.Invoke();
    }

    private void CopyTrackRightClick(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        CopyTrackRequested?.Invoke();
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
    private static extern int SetWindowLong(IntPtr hwnd, int index, int value);
}
