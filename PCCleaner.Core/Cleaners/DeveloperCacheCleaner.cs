using PCCleaner.Core;
using PCCleaner.Utilities;

namespace PCCleaner.Cleaners;

internal sealed class DeveloperCacheCleaner : ICleaner
{
    public string Name        => Localizer.T("cleaner.devCache.name");
    public string Description => Localizer.T("cleaner.devCache.description");
    public string Risk        => Localizer.T("cleaner.devCache.risk");

    public CleanerPlatform Platform             => CleanerPlatform.All;
    public bool            RequiresAdministrator => false;
    public bool            IsRecommended         => true;

    public CleanResult Clean(CleanOptions options)
    {
        var result = new CleanResult(Name, options.PreviewOnly);
        foreach (string path in GetCachePaths())
            result.Merge(FileDeleteHelper.CleanDirectory(Name, path, options));
        return result;
    }

    private static IEnumerable<string> GetCachePaths()
    {
        string home  = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        if (OperatingSystem.IsWindows())
        {
            yield return Path.Combine(local, "npm-cache");
            yield return Path.Combine(local, "pip", "Cache");
            yield return Path.Combine(local, "Yarn", "Cache");
        }
        else
        {
            string cache = SystemInfo.GetUnixUserCachePath();
            yield return Path.Combine(home, ".npm", "_cacache");
            yield return Path.Combine(cache, "pip");
            yield return Path.Combine(cache, "yarn");
        }

        // Cross-platform: cargo compressed registry archives (safe to delete — re-downloaded on demand)
        yield return Path.Combine(home, ".cargo", "registry", "cache");

        // Gradle downloaded module files
        yield return Path.Combine(home, ".gradle", "caches", "modules-2", "files-2.1");
        yield return Path.Combine(home, ".gradle", "caches", "modules-2", "metadata-2.95");
    }
}
