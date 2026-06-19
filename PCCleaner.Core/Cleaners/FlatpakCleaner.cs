using System.Diagnostics;
using PCCleaner.Core;
using PCCleaner.Utilities;

namespace PCCleaner.Cleaners;

internal sealed class FlatpakCleaner : ICleaner
{
    public string Name        => Localizer.T("cleaner.flatpak.name");
    public string Description => Localizer.T("cleaner.flatpak.description");
    public string Risk        => Localizer.T("cleaner.flatpak.risk");

    public CleanerPlatform Platform             => CleanerPlatform.Linux;
    public bool            RequiresAdministrator => false;
    public bool            IsRecommended         => true;

    public CleanResult Clean(CleanOptions options)
    {
        var result = new CleanResult(Name, options.PreviewOnly);

        if (!IsFlatpakAvailable())
        {
            result.AddNote("flatpak not found — skipped.");
            return result;
        }

        if (options.PreviewOnly)
        {
            string runtimeList = RunFlatpak("--user list --runtime --columns=application,version");
            if (!string.IsNullOrWhiteSpace(runtimeList))
            {
                result.AddNote("Installed user runtimes (unused will be removed on clean):");
                foreach (string line in runtimeList.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                    result.AddNote($"  {line.Trim()}");
            }
            else
            {
                result.AddNote("No user runtimes installed.");
            }
        }
        else
        {
            string output = RunFlatpak("--user uninstall --unused -y --noninteractive");
            foreach (string line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                string trimmed = line.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                    result.AddNote(trimmed);
            }
        }

        return result;
    }

    private static bool IsFlatpakAvailable()
    {
        try
        {
            using var p = Process.Start(new ProcessStartInfo("flatpak", "--version")
            {
                UseShellExecute        = false,
                RedirectStandardOutput = true,
                RedirectStandardError  = true
            });
            p?.WaitForExit(2000);
            return p?.ExitCode == 0;
        }
        catch { return false; }
    }

    private static string RunFlatpak(string args)
    {
        try
        {
            using var p = Process.Start(new ProcessStartInfo("flatpak", args)
            {
                UseShellExecute        = false,
                RedirectStandardOutput = true,
                RedirectStandardError  = true
            });
            string output = p?.StandardOutput.ReadToEnd() ?? string.Empty;
            p?.WaitForExit(30_000);
            return output;
        }
        catch { return string.Empty; }
    }
}
