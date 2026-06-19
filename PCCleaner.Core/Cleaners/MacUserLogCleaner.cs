using PCCleaner.Core;
using PCCleaner.Utilities;

namespace PCCleaner.Cleaners;

internal sealed class MacUserLogCleaner : ICleaner
{
    public string Name => Localizer.T("cleaner.macUserLogs.name");

    public string Description => Localizer.T("cleaner.macUserLogs.description");

    public string Risk => Localizer.T("cleaner.macUserLogs.risk");

    public CleanerPlatform Platform => CleanerPlatform.MacOS;

    public bool RequiresAdministrator => false;

    public bool IsRecommended => true;

    public CleanResult Clean(CleanOptions options)
    {
        CleanResult result = new(Name, options.PreviewOnly);
        string logsPath = SystemInfo.GetMacUserLogsPath();

        if (string.IsNullOrWhiteSpace(logsPath))
        {
            return result;
        }

        result.Merge(FileDeleteHelper.CleanFiles(
            Name,
            logsPath,
            new[] { "*.log", "*.log.*", "*.trace", "*.crash", "*.ips" },
            recursive: true,
            options));

        return result;
    }
}
