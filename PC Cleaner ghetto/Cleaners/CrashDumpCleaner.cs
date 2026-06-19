using PCCleaner.Core;
using PCCleaner.Utilities;

namespace PCCleaner.Cleaners;

internal sealed class CrashDumpCleaner : ICleaner
{
    public string Name => Localizer.T("cleaner.crashDumps.name");

    public string Description => Localizer.T("cleaner.crashDumps.description");

    public string Risk => Localizer.T("cleaner.crashDumps.risk");

    public CleanerPlatform Platform => CleanerPlatform.Windows;

    public bool RequiresAdministrator => false;

    public bool IsRecommended => true;

    public CleanResult Clean(CleanOptions options)
    {
        CleanResult result = new(Name, options.PreviewOnly);
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        string[] paths =
        {
            Path.Combine(localAppData, "CrashDumps"),
            Path.Combine(localAppData, "Microsoft", "Windows", "WER", "ReportArchive"),
            Path.Combine(localAppData, "Microsoft", "Windows", "WER", "ReportQueue")
        };

        foreach (string path in paths)
        {
            result.Merge(FileDeleteHelper.CleanDirectory(Name, path, options));
        }

        return result;
    }
}
