using PCCleaner.Core;
using PCCleaner.Utilities;

namespace PCCleaner.Cleaners;

internal sealed class RecentItemsCleaner : ICleaner
{
    public string Name => Localizer.T("cleaner.recentItems.name");

    public string Description => Localizer.T("cleaner.recentItems.description");

    public string Risk => Localizer.T("cleaner.recentItems.risk");

    public CleanerPlatform Platform => CleanerPlatform.Windows;

    public bool RequiresAdministrator => false;

    public bool IsRecommended => true;

    public CleanResult Clean(CleanOptions options)
    {
        CleanResult result = new(Name, options.PreviewOnly);
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        result.Merge(FileDeleteHelper.CleanDirectory(Name, Path.Combine(appData, "Microsoft", "Windows", "Recent"), options));

        return result;
    }
}
