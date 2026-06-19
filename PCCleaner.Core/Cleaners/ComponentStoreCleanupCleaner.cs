using System.Diagnostics;
using PCCleaner.Core;
using PCCleaner.Utilities;

namespace PCCleaner.Cleaners;

internal sealed class ComponentStoreCleanupCleaner : ICleaner
{
    public string Name => Localizer.T("cleaner.component.name");

    public string Description => Localizer.T("cleaner.component.description");

    public string Risk => Localizer.T("cleaner.component.risk");

    public CleanerPlatform Platform => CleanerPlatform.Windows;

    public bool RequiresAdministrator => true;

    public bool IsRecommended => false;

    public CleanResult Clean(CleanOptions options)
    {
        CleanResult result = new(Name, options.PreviewOnly);

        if (options.PreviewOnly)
        {
            result.AddNote(Localizer.T("note.componentPreview"));
            return result;
        }

        try
        {
            ProcessStartInfo startInfo = new()
            {
                FileName = "schtasks.exe",
                Arguments = @"/Run /TN \Microsoft\Windows\Servicing\StartComponentCleanup",
                CreateNoWindow = true,
                UseShellExecute = false
            };

            using Process? process = Process.Start(startInfo);
            process?.WaitForExit(10_000);

            bool succeeded = process is not null
                && (!process.HasExited || process.ExitCode == 0);

            if (!succeeded)
            {
                result.AddFailure();
            }
            else
            {
                result.AddNote(Localizer.T("note.componentAccepted"));
            }
        }
        catch
        {
            result.AddFailure();
        }

        return result;
    }
}
