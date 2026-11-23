using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Windows.Media.Control;
using Windows.Storage.Streams;

namespace TrackRelay;

internal sealed record Track(string Title, string Artist, string Album, ImageSource? Artwork = null, Color? ArtworkColor = null)
{
    public bool Matches(Track? other) => other is not null &&
        (Title, Artist, Album) == (other.Title, other.Artist, other.Album);
}

internal sealed class SpotifySession : IDisposable
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private GlobalSystemMediaTransportControlsSessionManager? _manager;
    private bool _refreshing;
    private bool _disposed;
    private int _retrySeconds;

    public SpotifySession() => _timer.Tick += OnTick;

    public Track? CurrentTrack { get; private set; }
    private string _statusKey = "Connecting";
    public string Status => Strings.Get(_statusKey);
    public event Action<Track?, bool>? CurrentTrackChanged;
    public event Action<string>? StatusChanged;

    public void Start()
    {
        _timer.Start();
        OnTick(this, EventArgs.Empty);
    }

    private async void OnTick(object? sender, EventArgs e)
    {
        if (_refreshing || _disposed)
        {
            return;
        }
        if (_retrySeconds > 0)
        {
            _retrySeconds--;
            return;
        }

        _refreshing = true;
        try
        {
            _manager ??= await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            if (_disposed)
            {
                return;
            }

            var session = FindSession();
            if (session is null)
            {
                UpdateTrack(null);
                SetStatus("WaitingSpotify");
                return;
            }

            var media = await session.TryGetMediaPropertiesAsync();
            if (_disposed || !Equals(session, FindSession()))
            {
                return;
            }
            if (string.IsNullOrWhiteSpace(media?.Title))
            {
                SetStatus("SpotifyConnected");
                return;
            }

            SetStatus("SpotifyConnected");
            var track = new Track(media.Title, media.Artist ?? "", media.AlbumTitle ?? "");
            if (track.Matches(CurrentTrack))
            {
                return;
            }

            var artwork = await ReadArtworkAsync(media.Thumbnail);
            if (_disposed || !Equals(session, FindSession()))
            {
                return;
            }

            UpdateTrack(track with { Artwork = artwork, ArtworkColor = ArtworkPalette.Sample(artwork) });
        }
        catch (Exception exception) when (exception is COMException or UnauthorizedAccessException or InvalidOperationException
            or System.ComponentModel.Win32Exception)
        {
            if (!_disposed)
            {
                _manager = null;
                UpdateTrack(null);
                _retrySeconds = 4;
                SetStatus("Reconnecting");
            }
        }
        finally
        {
            _refreshing = false;
        }
    }

    public async Task<string?> RunAsync(ShortcutAction action)
    {
        try
        {
            var session = FindSession();
            if (session is null)
            {
                return Strings.Get("PlayFirst");
            }

            var accepted = action switch
            {
                ShortcutAction.TogglePlayback => await session.TryTogglePlayPauseAsync(),
                ShortcutAction.NextTrack => await session.TrySkipNextAsync(),
                ShortcutAction.PreviousTrack => await session.TrySkipPreviousAsync(),
                _ => throw new ArgumentOutOfRangeException(nameof(action))
            };
            return accepted ? null : Strings.Format("MediaCommandError", Preferences.Label(action));
        }
        catch (Exception exception) when (exception is COMException or UnauthorizedAccessException or InvalidOperationException
            or System.ComponentModel.Win32Exception)
        {
            return Strings.Get("MediaUnavailable");
        }
    }

    private GlobalSystemMediaTransportControlsSession? FindSession()
    {
        if (_manager is null)
        {
            return null;
        }

        var current = _manager.GetCurrentSession();
        return IsSpotify(current) ? current : _manager.GetSessions().FirstOrDefault(IsSpotify);
    }

    private static bool IsSpotify(GlobalSystemMediaTransportControlsSession? session)
    {
        var appId = session?.SourceAppUserModelId;
        return string.Equals(appId, "Spotify.exe", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(appId, "Spotify", StringComparison.OrdinalIgnoreCase) ||
               appId?.StartsWith("SpotifyAB.SpotifyMusic_zpdnekdrzrea0!", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static async Task<ImageSource?> ReadArtworkAsync(IRandomAccessStreamReference? thumbnail)
    {
        if (thumbnail is null)
        {
            return null;
        }

        try
        {
            using var stream = await thumbnail.OpenReadAsync();
            if (stream.Size is 0 or > 8 * 1024 * 1024)
            {
                return null;
            }

            using var reader = new DataReader(stream);
            var size = (uint)stream.Size;
            if (await reader.LoadAsync(size) != size)
            {
                return null;
            }

            var bytes = new byte[size];
            reader.ReadBytes(bytes);
            using var buffer = new MemoryStream(bytes);
            var artwork = new BitmapImage();
            artwork.BeginInit();
            artwork.CacheOption = BitmapCacheOption.OnLoad;
            artwork.DecodePixelWidth = 144;
            artwork.StreamSource = buffer;
            artwork.EndInit();
            artwork.Freeze();
            return artwork;
        }
        catch (Exception exception) when (exception is COMException or IOException or NotSupportedException
            or UnauthorizedAccessException or ArgumentException or InvalidOperationException
            or System.ComponentModel.Win32Exception)
        {
            return null;
        }
    }

    private void UpdateTrack(Track? track)
    {
        var previous = CurrentTrack;
        if (previous is null && track is null)
        {
            return;
        }
        CurrentTrack = track;
        CurrentTrackChanged?.Invoke(track, previous is not null && track is not null);
    }

    private void SetStatus(string key)
    {
        if (_statusKey != key)
        {
            _statusKey = key;
            StatusChanged?.Invoke(Status);
        }
    }

    public void Dispose()
    {
        _disposed = true;
        _timer.Stop();
        _timer.Tick -= OnTick;
    }
}
