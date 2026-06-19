using PCCleaner.Core;
using PCCleaner.Utilities;

namespace PCCleaner.Cleaners;

internal sealed class WindowsLogCleaner : ICleaner
{
    public string Name => Localizer.T("cleaner.windowsLogs.name");

    public string Description => Localizer.T("cleaner.windowsLogs.description");

    public string Risk => Localizer.T("cleaner.windowsLogs.risk");

    public CleanerPlatform Platform => CleanerPlatform.Windows;

    public bool RequiresAdministrator => true;

    public bool IsRecommended => true;

    public CleanResult Clean(CleanOptions options)
    {
        CleanResult result = new(Name, options.PreviewOnly);
        string windowsPath = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

        string[] paths =
        {
            Path.Combine(windowsPath, "Logs"),
            Path.Combine(windowsPath, "Temp")
        };

        string[] patterns = { "*.log", "*.tmp", "*.old", "*.bak" };

        foreach (string path in paths)
        {
            result.Merge(FileDeleteHelper.CleanFiles(Name, path, patterns, recursive: true, options));
        }

        return result;
    }
}
