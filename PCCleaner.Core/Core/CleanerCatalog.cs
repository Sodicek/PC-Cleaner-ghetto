using PCCleaner.Cleaners;
using PCCleaner.Utilities;

namespace PCCleaner.Core;

internal static class CleanerCatalog
{
    public static IReadOnlyList<ICleaner> CreateCleanersForCurrentPlatform()
    {
        return CreateCleanersForPlatform(SystemInfo.IsWindows, SystemInfo.IsLinux, SystemInfo.IsMacOS);
    }

    internal static IReadOnlyList<ICleaner> CreateCleanersForPlatform(bool isWindows, bool isLinux, bool isMacOS)
    {
        return CreateAllCleaners()
            .Where(cleaner => SystemInfo.IsSupported(cleaner.Platform, isWindows, isLinux, isMacOS))
            .ToList();
    }

    private static IReadOnlyList<ICleaner> CreateAllCleaners()
    {
        return new List<ICleaner>
        {
            new DirectoryCleaner(
                "cleaner.userTemp.name",
                "cleaner.userTemp.description",
                "cleaner.userTemp.risk",
                new[] { Path.GetTempPath() }),
            new UnixUserCacheCleaner(),
            new DirectoryCleaner(
                "cleaner.windowsTemp.name",
                "cleaner.windowsTemp.description",
                "cleaner.windowsTemp.risk",
                new[] { Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp") },
                platform: CleanerPlatform.Windows,
                requiresAdministrator: true),
            new CrashDumpCleaner(),
            new WindowsLogCleaner(),
            new BrowserCacheCleaner(),
            new MacUserLogCleaner(),
            new ThumbnailCacheCleaner(),
            new RecentItemsCleaner(),
            new RecycleBinCleaner(),
            new UnixTrashCleaner(),
            new LinuxRecentFilesCleaner(),
            new StartupCleaner(),
            new ComponentStoreCleanupCleaner(),
            new DuplicateFileCleaner(),
            new ScheduledTaskCleaner(),
            new DirectoryCleaner(
                "cleaner.prefetch.name",
                "cleaner.prefetch.description",
                "cleaner.prefetch.risk",
                new[] { Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch") },
                platform: CleanerPlatform.Windows,
                requiresAdministrator: true,
                isRecommended: false),
            new SteamCacheCleaner(),
            new DeveloperCacheCleaner(),
            new FlatpakCleaner(),
            new VsCodeWorkspaceCleaner()
        };
    }
}
