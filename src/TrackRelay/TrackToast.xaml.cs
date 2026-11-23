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
    private Action? _clickAction;
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

    public void ShowTrack(Track track, bool canOpenSpotify, Action? clickAction = null,
        ToastCorner corner = ToastCorner.BottomRight)
    {
        _track = track;
        _clickAction = clickAction;
        OpenSpotifyButton.IsEnabled = canOpenSpotify || clickAction is not null;
        RefreshPalette();
        TrackTitle.Text = track.Title;
        TrackSubtitle.Text = track.Artist;
        Cover.Artwork = track.Artwork;
        var workArea = SystemParameters.WorkArea;
        Width = Math.Min(360, workArea.Width - 24);
        Left = corner is ToastCorner.BottomLeft or ToastCorner.TopLeft
            ? workArea.Left + 16
            : workArea.Right - Width - 16;
        Top = corner is ToastCorner.TopLeft or ToastCorner.TopRight
            ? workArea.Top + 16
            : workArea.Bottom - Height - 16;
        _dismiss.Stop();
        Show();
        _dismiss.Start();
    }

    public void RefreshPalette() => ArtworkPalette.Apply(ToastFrame, _track?.ArtworkColor);

    private void OpenSpotifyClick(object sender, RoutedEventArgs e)
    {
        var customClick = _clickAction;
        _clickAction = null;
        _dismiss.Stop();
        Hide();
        if (customClick is not null)
        {
            customClick();
        }
        else
        {
            OpenSpotifyRequested?.Invoke();
        }
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
