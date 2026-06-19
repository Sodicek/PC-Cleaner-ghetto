using PCCleaner.Core;
using PCCleaner.Utilities;

namespace PCCleaner.Cleaners;

internal sealed class UnixUserCacheCleaner : ICleaner
{
    public string Name => Localizer.T("cleaner.unixUserCache.name");

    public string Description => Localizer.T("cleaner.unixUserCache.description");

    public string Risk => Localizer.T("cleaner.unixUserCache.risk");

    public CleanerPlatform Platform => CleanerPlatform.UnixLike;

    public bool RequiresAdministrator => false;

    public bool IsRecommended => true;

    public CleanResult Clean(CleanOptions options)
    {
        CleanResult result = new(Name, options.PreviewOnly);
        foreach (string cachePath in SystemInfo.GetSafeUnixCachePaths())
        {
            result.Merge(FileDeleteHelper.CleanDirectory(Name, cachePath, options));
        }

        return result;
    }
}
