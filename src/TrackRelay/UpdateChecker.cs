using System;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace TrackRelay;

internal sealed record ReleaseUpdate(string Version, string Url);

internal sealed class UpdateChecker : IDisposable
{
    private const string LatestReleaseApiUrl = "https://api.github.com/repos/isnow890/spocue/releases/latest";
    private const string ReleasesPageUrl = "https://github.com/isnow890/spocue/releases/latest";

    private static readonly HttpClient Client = CreateClient();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromHours(24) };
    private bool _enabled;
    private bool _checking;
    private bool _disposed;
    private string? _lastNotifiedVersion;

    public UpdateChecker()
    {
        _timer.Tick += async (_, _) => await CheckAutomaticAsync();
    }

    public static string CurrentVersion { get; } = ResolveCurrentVersion();

    public ReleaseUpdate? LatestUpdate { get; private set; }

    public event Action<ReleaseUpdate>? UpdateAvailable;

    public void SetEnabled(bool enabled)
    {
        if (_disposed || _enabled == enabled)
        {
            return;
        }

        _enabled = enabled;
        if (enabled)
        {
            _timer.Start();
            _ = CheckAutomaticAsync();
        }
        else
        {
            _timer.Stop();
        }
    }

    public async Task<(ReleaseUpdate? Update, bool Failed)> CheckNowAsync()
    {
        var result = await FetchLatestAsync();
        if (result.Update is not null)
        {
            LatestUpdate = result.Update;
            _lastNotifiedVersion = result.Update.Version;
        }
        return result;
    }

    public static bool OpenReleasePage(string? url = null)
    {
        var target = string.IsNullOrWhiteSpace(url) ? ReleasesPageUrl : url;
        if (!target.StartsWith("https://github.com/isnow890/spocue/", StringComparison.OrdinalIgnoreCase))
        {
            target = ReleasesPageUrl;
        }

        try
        {
            Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
            return true;
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return false;
        }
    }

    private async Task CheckAutomaticAsync()
    {
        if (!_enabled || _checking || _disposed)
        {
            return;
        }

        _checking = true;
        try
        {
            var (update, _) = await FetchLatestAsync();
            if (!_disposed && _enabled && update is not null &&
                !string.Equals(_lastNotifiedVersion, update.Version, StringComparison.OrdinalIgnoreCase))
            {
                LatestUpdate = update;
                _lastNotifiedVersion = update.Version;
                UpdateAvailable?.Invoke(update);
            }
        }
        finally
        {
            _checking = false;
        }
    }

    private static async Task<(ReleaseUpdate? Update, bool Failed)> FetchLatestAsync()
    {
        try
        {
            using var response = await Client.GetAsync(LatestReleaseApiUrl);
            if (!response.IsSuccessStatusCode)
            {
                return (null, true);
            }

            await using var stream = await response.Content.ReadAsStreamAsync();
            using var document = await JsonDocument.ParseAsync(stream);
            var root = document.RootElement;
            if (!root.TryGetProperty("tag_name", out var tagElement))
            {
                return (null, true);
            }

            var rawTag = tagElement.GetString()?.Trim();
            if (string.IsNullOrWhiteSpace(rawTag))
            {
                return (null, true);
            }

            var cleanLatest = StripVersionText(rawTag);
            var cleanCurrent = StripVersionText(CurrentVersion);
            if (!Version.TryParse(cleanLatest, out var latestVersion) ||
                !Version.TryParse(cleanCurrent, out var currentVersion))
            {
                return (null, true);
            }

            if (latestVersion <= currentVersion)
            {
                return (null, false);
            }

            var htmlUrl = root.TryGetProperty("html_url", out var urlElement)
                ? urlElement.GetString()
                : ReleasesPageUrl;
            return (new ReleaseUpdate(cleanLatest, string.IsNullOrWhiteSpace(htmlUrl) ? ReleasesPageUrl : htmlUrl), false);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException)
        {
            return (null, true);
        }
    }

    private static string ResolveCurrentVersion()
    {
        var info = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(info))
        {
            return StripVersionText(info);
        }

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        return version is null ? "0.3.0" : $"{version.Major}.{version.Minor}.{Math.Max(0, version.Build)}";
    }

    private static string StripVersionText(string value)
    {
        var clean = value.Trim().TrimStart('v', 'V');
        var separator = clean.IndexOfAny(['-', '+']);
        return separator >= 0 ? clean[..separator] : clean;
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("SpoCue-UpdateChecker");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return client;
    }

    public void Dispose()
    {
        _disposed = true;
        _timer.Stop();
    }
}
