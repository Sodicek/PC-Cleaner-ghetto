using PCCleaner.Utilities;

namespace PCCleaner.Core;

internal sealed class CleanerRunner
{
    public CleanRunReport Run(IReadOnlyList<ICleaner> cleaners, CleanOptions options, CleanerRunCallbacks? callbacks = null)
    {
        callbacks ??= new CleanerRunCallbacks();

        CleanRunReport report = new(new CleanResult(Localizer.T("result.total"), options.PreviewOnly));
        report.SetDiskBefore(SystemInfo.GetCurrentDriveSnapshot());

        foreach (ICleaner cleaner in cleaners)
        {
            if (!SystemInfo.IsSupported(cleaner.Platform))
            {
                string reason = Localizer.T("status.unsupportedPlatform", SystemInfo.PlatformName(cleaner.Platform), SystemInfo.Description);
                report.AddSkipped(cleaner.Name, reason);
                callbacks.Skipped?.Invoke(cleaner, reason);
                continue;
            }

            if (cleaner.RequiresAdministrator && !AdminHelper.IsRunningAsAdministrator())
            {
                string reason = Localizer.T("status.adminRequired");
                report.AddSkipped(cleaner.Name, reason);
                callbacks.Skipped?.Invoke(cleaner, reason);
                continue;
            }

            if (cleaner is ICleanerWarningProvider warningProvider)
            {
                foreach (string warning in warningProvider.GetWarnings(options))
                {
                    callbacks.Warning?.Invoke(warning);
                }
            }

            if (callbacks.ConfirmCleaner is not null && !callbacks.ConfirmCleaner(cleaner))
            {
                string reason = Localizer.T("status.notConfirmed");
                report.AddSkipped(cleaner.Name, reason);
                callbacks.Skipped?.Invoke(cleaner, reason);
                continue;
            }

            callbacks.Starting?.Invoke(cleaner);
            CleanResult result = cleaner.Clean(options);
            report.AddResult(result);
            callbacks.Completed?.Invoke(result);
        }

        report.SetDiskAfter(SystemInfo.GetCurrentDriveSnapshot());
        return report;
    }
}
