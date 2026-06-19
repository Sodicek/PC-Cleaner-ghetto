using PCCleaner.Core;
using PCCleaner.Utilities;
using System.Diagnostics;

namespace PCCleaner.Cleaners;

internal sealed class BrowserCacheCleaner : ICleaner, ICleanerWarningProvider
{
    private static readonly string[] BrowserProcessNames =
    {
        "chrome",
        "msedge",
        "brave",
        "opera",
        "firefox",
        "vivaldi",
        "chromium"
    };

    public string Name => Localizer.T("cleaner.browserCache.name");

    public string Description => Localizer.T("cleaner.browserCache.description");

    public string Risk => Localizer.T("cleaner.browserCache.risk");

    public CleanerPlatform Platform => CleanerPlatform.All;

    public bool RequiresAdministrator => false;

    public bool IsRecommended => true;

    public IReadOnlyList<string> GetWarnings(CleanOptions options)
    {
        List<string> runningBrowsers = BrowserProcessNames
            .Where(IsProcessRunning)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (runningBrowsers.Count == 0)
        {
            return Array.Empty<string>();
        }

        return new[] { Localizer.T("warning.browserRunning", string.Join(", ", runningBrowsers)) };
    }

    public CleanResult Clean(CleanOptions options)
    {
        CleanResult result = new(Name, options.PreviewOnly);
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        foreach (string cachePath in GetCachePaths(localAppData))
        {
            result.Merge(FileDeleteHelper.CleanDirectory(Name, cachePath, options));
        }

        return result;
    }

    private static IEnumerable<string> GetCachePaths(string localAppData)
    {
        foreach (string cachePath in GetChromiumCachePaths(localAppData))
        {
            yield return cachePath;
        }

        foreach (string firefoxCachePath in GetFirefoxCachePaths(localAppData))
        {
            yield return firefoxCachePath;
        }
    }

    private static IEnumerable<string> GetChromiumCachePaths(string localAppData)
    {
        if (OperatingSystem.IsMacOS())
        {
            foreach (string cachePath in GetMacBrowserCachePaths())
            {
                yield return cachePath;
            }

            yield break;
        }

        if (OperatingSystem.IsLinux())
        {
            foreach (string cachePath in GetLinuxBrowserCachePaths())
            {
                yield return cachePath;
            }

            yield break;
        }

        string[] userDataRoots =
        {
            Path.Combine(localAppData, "Google", "Chrome", "User Data"),
            Path.Combine(localAppData, "Google", "Chrome Beta", "User Data"),
            Path.Combine(localAppData, "Google", "Chrome Dev", "User Data"),
            Path.Combine(localAppData, "Google", "Chrome SxS", "User Data"),
            Path.Combine(localAppData, "Google", "Chrome for Testing", "User Data"),
            Path.Combine(localAppData, "Chromium", "User Data"),
            Path.Combine(localAppData, "Microsoft", "Edge", "User Data"),
            Path.Combine(localAppData, "BraveSoftware", "Brave-Browser", "User Data"),
            Path.Combine(localAppData, "Vivaldi", "User Data")
        };

        foreach (string root in userDataRoots)
        {
            foreach (string profilePath in EnumerateDirectories(root))
            {
                yield return Path.Combine(profilePath, "Cache");
                yield return Path.Combine(profilePath, "Code Cache");
                yield return Path.Combine(profilePath, "GPUCache");
            }
        }

        foreach (string operaVariant in new[] { "Opera Stable", "Opera GX Stable", "Opera Beta", "Opera Developer" })
        {
            string operaRoot = Path.Combine(localAppData, "Opera Software", operaVariant);
            yield return Path.Combine(operaRoot, "Cache");
            yield return Path.Combine(operaRoot, "Code Cache");
            yield return Path.Combine(operaRoot, "GPUCache");
        }
    }

    private static IEnumerable<string> GetFirefoxCachePaths(string localAppData)
    {
        if (OperatingSystem.IsMacOS() || OperatingSystem.IsLinux())
        {
            yield break;
        }

        string profilesRoot = Path.Combine(localAppData, "Mozilla", "Firefox", "Profiles");

        if (!Directory.Exists(profilesRoot))
        {
            yield break;
        }

        foreach (string profilePath in EnumerateDirectories(profilesRoot))
        {
            yield return Path.Combine(profilePath, "cache2");
            yield return Path.Combine(profilePath, "startupCache");
        }
    }

    private static IEnumerable<string> EnumerateDirectories(string path)
    {
        try
        {
            return Directory.Exists(path) ? Directory.EnumerateDirectories(path) : Enumerable.Empty<string>();
        }
        catch
        {
            return Enumerable.Empty<string>();
        }
    }

    private static bool IsProcessRunning(string processName)
    {
        try
        {
            Process[] processes = Process.GetProcessesByName(processName);
            bool running = processes.Length > 0;
            foreach (Process process in processes)
            {
                process.Dispose();
            }
            return running;
        }
        catch
        {
            return false;
        }
    }

    private static IEnumerable<string> GetLinuxBrowserCachePaths()
    {
        string cache  = SystemInfo.GetUnixUserCachePath();
        string home   = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string flatpak = Path.Combine(home, ".var", "app");

        // Native installs (system packages, tarballs, RPM)
        foreach (string profileDir in EnumerateDirectories(Path.Combine(cache, "google-chrome")))
        {
            yield return Path.Combine(profileDir, "Cache");
            yield return Path.Combine(profileDir, "Code Cache");
            yield return Path.Combine(profileDir, "GPUCache");
        }
        foreach (string profileDir in EnumerateDirectories(Path.Combine(cache, "chromium")))
        {
            yield return Path.Combine(profileDir, "Cache");
            yield return Path.Combine(profileDir, "Code Cache");
        }
        yield return Path.Combine(cache, "microsoft-edge");
        yield return Path.Combine(cache, "BraveSoftware", "Brave-Browser");
        yield return Path.Combine(cache, "vivaldi");
        yield return Path.Combine(cache, "opera");
        yield return Path.Combine(cache, "opera-stable");

        // Native Firefox — enumerate profiles so we only delete cache2, not crash-reports etc.
        foreach (string profileDir in EnumerateDirectories(Path.Combine(cache, "mozilla", "firefox")))
        {
            yield return Path.Combine(profileDir, "cache2");
            yield return Path.Combine(profileDir, "startupCache");
        }

        // Flatpak installs — primary install method on Fedora/Nobara
        foreach (string profileDir in EnumerateDirectories(
            Path.Combine(flatpak, "com.google.Chrome", "cache", "google-chrome")))
        {
            yield return Path.Combine(profileDir, "Cache");
            yield return Path.Combine(profileDir, "Code Cache");
            yield return Path.Combine(profileDir, "GPUCache");
        }
        foreach (string profileDir in EnumerateDirectories(
            Path.Combine(flatpak, "org.chromium.Chromium", "cache", "chromium")))
        {
            yield return Path.Combine(profileDir, "Cache");
            yield return Path.Combine(profileDir, "Code Cache");
        }
        yield return Path.Combine(flatpak, "com.brave.Browser",    "cache", "BraveSoftware", "Brave-Browser");
        yield return Path.Combine(flatpak, "com.microsoft.Edge",   "cache", "microsoft-edge");
        yield return Path.Combine(flatpak, "com.opera.Opera",      "cache", "opera");
        yield return Path.Combine(flatpak, "com.vivaldi.Vivaldi",  "cache", "vivaldi");

        // Flatpak Firefox — enumerate profiles
        foreach (string profileDir in EnumerateDirectories(
            Path.Combine(flatpak, "org.mozilla.firefox", "cache", "mozilla", "firefox")))
        {
            yield return Path.Combine(profileDir, "cache2");
            yield return Path.Combine(profileDir, "startupCache");
        }
    }

    private static IEnumerable<string> GetMacBrowserCachePaths()
    {
        string cache = SystemInfo.GetUnixUserCachePath();

        string[] directCachePaths =
        {
            Path.Combine(cache, "Google", "Chrome"),
            Path.Combine(cache, "Chromium"),
            Path.Combine(cache, "Microsoft Edge"),
            Path.Combine(cache, "BraveSoftware", "Brave-Browser"),
            Path.Combine(cache, "Firefox", "Profiles"),
            Path.Combine(cache, "com.operasoftware.Opera")
        };

        foreach (string path in directCachePaths)
        {
            yield return path;
        }
    }
}
