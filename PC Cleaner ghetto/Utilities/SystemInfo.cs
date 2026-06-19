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

    public static bool IsSupported(CleanerPlatform platform)
    {
        return platform switch
        {
            CleanerPlatform.All => true,
            CleanerPlatform.Windows => IsWindows,
            _ => false
        };
    }

    public static string PlatformName(CleanerPlatform platform)
    {
        return platform switch
        {
            CleanerPlatform.All => Localizer.T("platform.all"),
            CleanerPlatform.Windows => Localizer.T("platform.windows"),
            _ => Localizer.T("platform.unknown")
        };
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
