using PCCleaner.Core;
using PCCleaner.Utilities;

namespace PCCleaner.Cleaners;

internal sealed class LinuxRecentFilesCleaner : ICleaner
{
    public string Name => Localizer.T("cleaner.linuxRecentFiles.name");

    public string Description => Localizer.T("cleaner.linuxRecentFiles.description");

    public string Risk => Localizer.T("cleaner.linuxRecentFiles.risk");

    public CleanerPlatform Platform => CleanerPlatform.Linux;

    public bool RequiresAdministrator => false;

    public bool IsRecommended => false;

    public CleanResult Clean(CleanOptions options)
    {
        CleanResult result = new(Name, options.PreviewOnly);
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        if (string.IsNullOrWhiteSpace(home))
            return result;

        string recentFilePath = Path.Combine(home, ".local", "share", "recently-used.xbel");
        FileDeleteHelper.TryDeleteFile(recentFilePath, result, options);

        return result;
    }
}
