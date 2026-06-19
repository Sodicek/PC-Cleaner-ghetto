using System.Globalization;
using System.Runtime.InteropServices;
using PCCleaner.Core;

namespace PCCleaner.Utilities;

internal static class SystemInfo
{
    public static string Description =>
        $"{RuntimeInformation.OSDescription.Trim()} ({RuntimeInformation.OSArchitecture})";

    public static string LocalAuthor => GetSafeEnvironmentValue(() => Environment.UserName, "unknown");

    public static string LoggedInUser
    {
        get
        {
            string userName = GetSafeEnvironmentValue(() => Environment.UserName, "unknown");
            string domain = GetSafeEnvironmentValue(() => Environment.UserDomainName, string.Empty);

            return string.IsNullOrWhiteSpace(domain)
                ? userName
                : $"{domain}\\{userName}";
        }
    }

    public static string MachineName => GetSafeEnvironmentValue(() => Environment.MachineName, "unknown");

    public static bool IsWindows => OperatingSystem.IsWindows();

    public static bool IsLinux => OperatingSystem.IsLinux();

    public static bool IsMacOS => OperatingSystem.IsMacOS();

    public static bool IsUnixLike => IsLinux || IsMacOS;

    public static CleanerPlatform CurrentCleanerPlatform
    {
        get
        {
            if (IsWindows)
            {
                return CleanerPlatform.Windows;
            }

            if (IsLinux)
            {
                return CleanerPlatform.Linux;
            }

            if (IsMacOS)
            {
                return CleanerPlatform.MacOS;
            }

            return CleanerPlatform.All;
        }
    }

    public static string CurrentPlatformName => PlatformName(CurrentCleanerPlatform);

    public static bool IsSupported(CleanerPlatform platform)
    {
        return IsSupported(platform, IsWindows, IsLinux, IsMacOS);
    }

    internal static bool IsSupported(CleanerPlatform platform, bool isWindows, bool isLinux, bool isMacOS)
    {
        return platform switch
        {
            CleanerPlatform.All => true,
            CleanerPlatform.Windows => isWindows,
            CleanerPlatform.Linux => isLinux,
            CleanerPlatform.MacOS => isMacOS,
            CleanerPlatform.UnixLike => isLinux || isMacOS,
            _ => false
        };
    }

    public static string PlatformName(CleanerPlatform platform)
    {
        return platform switch
        {
            CleanerPlatform.All => Localizer.T("platform.all"),
            CleanerPlatform.Windows => Localizer.T("platform.windows"),
            CleanerPlatform.Linux => Localizer.T("platform.linux"),
            CleanerPlatform.MacOS => Localizer.T("platform.macos"),
            CleanerPlatform.UnixLike => Localizer.T("platform.unixLike"),
            _ => Localizer.T("platform.unknown")
        };
    }

    public static string GetUnixUserCachePath()
    {
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string? xdgCacheHome = Environment.GetEnvironmentVariable("XDG_CACHE_HOME");
        return GetUnixUserCachePath(home, xdgCacheHome, IsMacOS);
    }

    internal static string GetUnixUserCachePath(string home, string? xdgCacheHome, bool isMacOS)
    {
        if (string.IsNullOrWhiteSpace(home))
        {
            return string.Empty;
        }

        if (isMacOS)
        {
            return Path.Combine(home, "Library", "Caches");
        }

        return string.IsNullOrWhiteSpace(xdgCacheHome)
            ? Path.Combine(home, ".cache")
            : xdgCacheHome;
    }

    public static IReadOnlyList<string> GetSafeUnixCachePaths()
    {
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string cache = GetUnixUserCachePath();

        if (string.IsNullOrWhiteSpace(home) || string.IsNullOrWhiteSpace(cache))
        {
            return Array.Empty<string>();
        }

        if (IsMacOS)
        {
            return new[]
            {
                Path.Combine(home, "Library", "Caches", "com.apple.QuickLook.thumbnailcache"),
                Path.Combine(home, "Library", "Caches", "com.apple.helpd"),
                Path.Combine(home, "Library", "Caches", "com.apple.iconservices.store")
            };
        }

        return new[]
        {
            Path.Combine(cache, "thumbnails"),
            Path.Combine(cache, "fontconfig"),
            Path.Combine(cache, "mesa_shader_cache"),
            Path.Combine(cache, "mesa_shader_cache_db"),
            Path.Combine(cache, "NVIDIA", "GLCache"),
            Path.Combine(cache, "pip")
        };
    }

    public static string GetMacUserLogsPath()
    {
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return string.IsNullOrWhiteSpace(home)
            ? string.Empty
            : Path.Combine(home, "Library", "Logs");
    }

    public static DiskSpaceSnapshot? GetCurrentDriveSnapshot()
    {
        try
        {
            string basePath = AppContext.BaseDirectory;
            string? root = Path.GetPathRoot(basePath);

            if (string.IsNullOrWhiteSpace(root))
            {
                return null;
            }

            DriveInfo drive = new(root);
            return drive.IsReady ? new DiskSpaceSnapshot(drive.Name, drive.AvailableFreeSpace) : null;
        }
        catch
        {
            return null;
        }
    }

    public static string FormatBytes(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        double value = bytes;
        int unit = 0;

        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return $"{value.ToString("0.##", CultureInfo.InvariantCulture)} {units[unit]}";
    }

    private static string GetSafeEnvironmentValue(Func<string> valueFactory, string fallback)
    {
        try
        {
            string value = valueFactory();
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }
        catch
        {
            return fallback;
        }
    }
}
