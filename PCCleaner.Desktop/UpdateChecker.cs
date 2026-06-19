using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace PCCleaner.Desktop;

internal sealed record UpdateInfo(
    string LatestVersion,
    string ReleasePageUrl,
    string? DownloadUrl,
    string? AssetName);

internal static class UpdateChecker
{
    private static readonly HttpClient Http = CreateClient();

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        client.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("PC-Cleaner", AppVersion.Current));
        return client;
    }

    public static async Task<UpdateInfo?> CheckAsync(CancellationToken ct = default)
    {
        try
        {
            string apiUrl = $"https://api.github.com/repos/{AppVersion.GitHubOwner}/{AppVersion.GitHubRepo}/releases/latest";
            using var response = await Http.GetAsync(apiUrl, ct);

            if (!response.IsSuccessStatusCode) return null;

            string json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            string tag         = root.GetProperty("tag_name").GetString() ?? "";
            string releasePage = root.GetProperty("html_url").GetString()  ?? "";
            string latest      = tag.TrimStart('v');

            if (!IsNewer(latest, AppVersion.Current)) return null;

            string? downloadUrl = null;
            string? assetName   = null;
            string  expected    = PlatformAssetName();

            if (root.TryGetProperty("assets", out var assets))
            {
                foreach (var asset in assets.EnumerateArray())
                {
                    string name = asset.GetProperty("name").GetString() ?? "";
                    if (string.Equals(name, expected, StringComparison.OrdinalIgnoreCase))
                    {
                        downloadUrl = asset.GetProperty("browser_download_url").GetString();
                        assetName   = name;
                        break;
                    }
                }
            }

            return new UpdateInfo(latest, releasePage, downloadUrl, assetName);
        }
        catch
        {
            return null;
        }
    }

    internal static string PlatformAssetName()
    {
        if (OperatingSystem.IsWindows()) return "PCCleaner-win-x64.exe";
        if (OperatingSystem.IsMacOS())  return "PCCleaner-osx-x64";
        return "PCCleaner-linux-x64";
    }

    private static bool IsNewer(string latest, string current) =>
        Version.TryParse(latest,  out var vLatest) &&
        Version.TryParse(current, out var vCurrent) &&
        vLatest > vCurrent;
}
