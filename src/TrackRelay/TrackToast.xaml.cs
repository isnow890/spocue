using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace TrackRelay;

internal sealed partial class TrackToast : Window
{
    private Track? _track;
    private Action? _clickAction;
    private readonly DispatcherTimer _dismiss = new() { Interval = TimeSpan.FromSeconds(4) };
    private int _fadeGeneration;
    public event Action? OpenSpotifyRequested;
    public event Action? CopyTrackRequested;

    public TrackToast()
    {
        InitializeComponent();
        _dismiss.Tick += (_, _) =>
        {
            _dismiss.Stop();
            FadeTo(0, TimeSpan.FromMilliseconds(180), Hide);
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
        ToastCorner corner = ToastCorner.BottomRight, int? volumePercent = null)
    {
        _track = track;
        _clickAction = clickAction;
        OpenSpotifyButton.IsEnabled = canOpenSpotify || clickAction is not null;
        RefreshPalette();
        TrackTitle.Text = track.Title;
        TrackSubtitle.Text = track.Artist;
        Cover.Artwork = track.Artwork;
        if (volumePercent is { } volume)
        {
            // Keep the title to one line so the meter fits inside the fixed toast height.
            TrackTitle.MaxHeight = TrackTitle.LineHeight;
            VolumeFilled.Width = new GridLength(Math.Clamp(volume, 0, 100), GridUnitType.Star);
            VolumeRemaining.Width = new GridLength(100 - Math.Clamp(volume, 0, 100), GridUnitType.Star);
            VolumeMeter.Visibility = Visibility.Visible;
        }
        else
        {
            TrackTitle.MaxHeight = TrackTitle.LineHeight * 2;
            VolumeMeter.Visibility = Visibility.Collapsed;
        }
        var workArea = SystemParameters.WorkArea;
        Width = Math.Min(360, workArea.Width - 24);
        Left = corner is ToastCorner.BottomLeft or ToastCorner.TopLeft
            ? workArea.Left + 16
            : workArea.Right - Width - 16;
        Top = corner is ToastCorner.TopLeft or ToastCorner.TopRight
            ? workArea.Top + 16
            : workArea.Bottom - Height - 16;
        _dismiss.Stop();
        if (!IsVisible)
        {
            Opacity = 0;
            Show();
        }
        FadeTo(1, TimeSpan.FromMilliseconds(140));
        _dismiss.Start();
    }

    private void FadeTo(double opacity, TimeSpan duration, Action? completed = null)
    {
        // A newer fade (e.g. a track change during fade-out) supersedes the pending completion.
        var generation = ++_fadeGeneration;
        if (!SystemParameters.ClientAreaAnimation || SystemParameters.HighContrast)
        {
            BeginAnimation(OpacityProperty, null);
            Opacity = opacity;
            completed?.Invoke();
            return;
        }

        var animation = new DoubleAnimation(opacity, duration)
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        if (completed is not null)
        {
            animation.Completed += (_, _) =>
            {
                if (generation == _fadeGeneration)
                {
                    completed();
                }
            };
        }
        BeginAnimation(OpacityProperty, animation);
    }

    public void RefreshPalette() => ArtworkPalette.Apply(ToastFrame, _track?.ArtworkColor);

    private void OpenSpotifyClick(object sender, RoutedEventArgs e)
    {
        var customClick = _clickAction;
        _clickAction = null;
        _dismiss.Stop();
        ++_fadeGeneration;
        BeginAnimation(OpacityProperty, null);
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
