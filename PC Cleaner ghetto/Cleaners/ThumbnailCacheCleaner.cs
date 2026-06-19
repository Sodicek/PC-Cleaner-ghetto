using PCCleaner.Core;
using PCCleaner.Utilities;

namespace PCCleaner.Cleaners;

internal sealed class ThumbnailCacheCleaner : ICleaner
{
    public string Name => Localizer.T("cleaner.thumbnailCache.name");

    public string Description => Localizer.T("cleaner.thumbnailCache.description");

    public string Risk => Localizer.T("cleaner.thumbnailCache.risk");

    public CleanerPlatform Platform => CleanerPlatform.Windows;

    public bool RequiresAdministrator => false;

    public bool IsRecommended => true;

    public CleanResult Clean(CleanOptions options)
    {
        CleanResult result = new(Name, options.PreviewOnly);
        string explorerPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft",
            "Windows",
            "Explorer");

        if (!Directory.Exists(explorerPath))
        {
            return result;
        }

        IEnumerable<string> files;
        try
        {
            files = Directory.EnumerateFiles(explorerPath, "thumbcache_*.db");
        }
        catch
        {
            result.AddFailure();
            return result;
        }

        foreach (string filePath in files)
        {
            FileDeleteHelper.TryDeleteFile(filePath, result, options);
        }

        return result;
    }
}
