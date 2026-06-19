using PCCleaner.Core;

namespace PCCleaner.Utilities;

internal static class CleanReportWriter
{
    public static bool TrySave(CleanRunReport report, CleanOptions options, out string path, out string error)
    {
        path = string.Empty;
        error = string.Empty;

        try
        {
            string reportRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PC Cleaner Ghetto",
                "Reports");

            Directory.CreateDirectory(reportRoot);

            string fileName = $"pc-cleaner-report-{DateTime.Now:yyyyMMdd-HHmmss}.txt";
            path = Path.Combine(reportRoot, fileName);

            List<string> lines = new()
            {
                Localizer.T("app.title"),
                Localizer.T("app.credit"),
                Localizer.T("system.detected", SystemInfo.Description),
                $"{Localizer.T("report.created")}: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                $"{Localizer.T("report.mode")}: {(options.PreviewOnly ? Localizer.T("report.preview") : Localizer.T("report.clean"))}",
                $"{Localizer.T("report.ageFilter")}: {GetAgeFilterDescription(options)}",
                string.Empty,
                Localizer.T("section.summary"),
                report.Total.ToConsoleMessage(),
                string.Empty,
                Localizer.T("report.cleanerResults")
            };

            foreach (CleanResult result in report.Results)
            {
                lines.Add(result.ToConsoleMessage());

                foreach (string note in result.Notes)
                {
                    lines.Add($"  - {note}");
                }
            }

            if (report.SkippedCleaners.Count > 0)
            {
                lines.Add(string.Empty);
                lines.Add(Localizer.T("report.skipped"));
                lines.AddRange(report.SkippedCleaners.Select(skipped => $"  - {skipped}"));
            }

            if (report.DiskBefore is not null && report.DiskAfter is not null)
            {
                long delta = report.DiskAfter.FreeBytes - report.DiskBefore.FreeBytes;
                lines.Add(string.Empty);
                lines.Add(Localizer.T("report.disk"));
                lines.Add(Localizer.T(
                    "report.diskChange",
                    report.DiskBefore.DriveName,
                    SystemInfo.FormatBytes(report.DiskBefore.FreeBytes),
                    SystemInfo.FormatBytes(report.DiskAfter.FreeBytes),
                    SystemInfo.FormatBytes(Math.Abs(delta)),
                    delta >= 0 ? "+" : "-"));
            }

            File.WriteAllLines(path, lines);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static string GetAgeFilterDescription(CleanOptions options)
    {
        return options.IncludeRecentFiles
            ? Localizer.T("report.ageFilterDisabled")
            : Localizer.T("report.ageFilterHours", options.MinimumFileAge.TotalHours);
    }
}
