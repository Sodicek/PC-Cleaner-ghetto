using PCCleaner.Core;

namespace PCCleaner.Utilities;

internal static class CleanReportWriter
{
    private const string AppFolder = "pc-cleaner-ghetto";

    public static bool TrySave(CleanRunReport report, CleanOptions options, out string path, out string error)
    {
        path  = string.Empty;
        error = string.Empty;

        try
        {
            string reportRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                AppFolder,
                "reports");

            Directory.CreateDirectory(reportRoot);

            var   now      = DateTime.Now;
            string fileName = $"report-{now:yyyy-MM-dd_HH-mm-ss}.txt";
            path = Path.Combine(reportRoot, fileName);

            File.WriteAllLines(path, BuildLines(report, options, now));
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static IEnumerable<string> BuildLines(CleanRunReport report, CleanOptions options, DateTime now)
    {
        const string Divider  = "─────────────────────────────────────────────────────────";
        const string ThinLine = "·····················································";

        // ── Header ────────────────────────────────────────────────────────────
        yield return Divider;
        yield return "  PC Cleaner — run report";
        yield return Divider;
        yield return $"  Date    : {now:dddd, d MMMM yyyy}";
        yield return $"  Time    : {now:HH:mm:ss}";
        yield return $"  System  : {SystemInfo.Description}";
        yield return $"  User    : {SystemInfo.LocalAuthor}";
        yield return $"  Mode    : {(options.PreviewOnly ? "Preview  (nothing deleted)" : "Clean  (files deleted)")}";
        yield return $"  Age     : {GetAgeFilterDescription(options)}";
        yield return Divider;
        yield return string.Empty;

        // ── Summary ───────────────────────────────────────────────────────────
        yield return "  SUMMARY";
        yield return ThinLine;
        yield return $"  {report.Total.ToConsoleMessage()}";
        yield return string.Empty;

        // ── Per-cleaner results ───────────────────────────────────────────────
        yield return "  CLEANER RESULTS";
        yield return ThinLine;
        foreach (CleanResult result in report.Results)
        {
            yield return $"  {result.ToConsoleMessage()}";
            foreach (string note in result.Notes)
                yield return $"    · {note}";
        }

        // ── Skipped ───────────────────────────────────────────────────────────
        if (report.SkippedCleaners.Count > 0)
        {
            yield return string.Empty;
            yield return "  SKIPPED";
            yield return ThinLine;
            foreach (string skipped in report.SkippedCleaners)
                yield return $"  · {skipped}";
        }

        // ── Disk change ───────────────────────────────────────────────────────
        if (report.DiskBefore is not null && report.DiskAfter is not null)
        {
            long delta = report.DiskAfter.FreeBytes - report.DiskBefore.FreeBytes;
            string sign = delta >= 0 ? "+" : "-";

            yield return string.Empty;
            yield return "  DISK SPACE";
            yield return ThinLine;
            yield return $"  Drive   : {report.DiskBefore.DriveName}";
            yield return $"  Before  : {SystemInfo.FormatBytes(report.DiskBefore.FreeBytes)} free";
            yield return $"  After   : {SystemInfo.FormatBytes(report.DiskAfter.FreeBytes)} free";
            yield return $"  Change  : {sign}{SystemInfo.FormatBytes(Math.Abs(delta))}";
        }

        yield return string.Empty;
        yield return Divider;
        yield return "  github.com/Sodicek/PC-Cleaner-ghetto";
        yield return Divider;
    }

    private static string GetAgeFilterDescription(CleanOptions options) =>
        options.IncludeRecentFiles
            ? "disabled (all files)"
            : $"skip files newer than {options.MinimumFileAge.TotalHours:0.#} h";
}
