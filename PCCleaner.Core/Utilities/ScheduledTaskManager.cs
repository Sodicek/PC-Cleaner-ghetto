using System.Diagnostics;

namespace PCCleaner.Utilities;

internal static class ScheduledTaskManager
{
    public const string TaskName = "PC Cleaner Auto-Clean";

    public static bool Exists()
    {
        try
        {
            var psi = BuildPsi($"/Query /TN \"{TaskName}\" /FO LIST");
            using var p = Process.Start(psi)!;
            p.StandardOutput.ReadToEnd();
            p.WaitForExit(4000);
            return p.ExitCode == 0;
        }
        catch { return false; }
    }

    public static bool Create(string exePath, out string error)
    {
        error = string.Empty;
        if (exePath.Contains('"'))
        {
            error = "Executable path must not contain double-quote characters.";
            return false;
        }
        try
        {
            string args = $"/Create /TN \"{TaskName}\" /TR \"\\\"{exePath}\\\" --auto-clean\" /SC WEEKLY /D SUN /ST 09:00 /F";
            var psi = BuildPsi(args);
            using var p = Process.Start(psi)!;
            string stderr = p.StandardError.ReadToEnd();
            p.WaitForExit(6000);
            if (p.ExitCode != 0) { error = stderr.Trim(); return false; }
            return true;
        }
        catch (Exception ex) { error = ex.Message; return false; }
    }

    public static bool Remove(out string error)
    {
        error = string.Empty;
        try
        {
            var psi = BuildPsi($"/Delete /TN \"{TaskName}\" /F");
            using var p = Process.Start(psi)!;
            string stderr = p.StandardError.ReadToEnd();
            p.WaitForExit(5000);
            if (p.ExitCode != 0) { error = stderr.Trim(); return false; }
            return true;
        }
        catch (Exception ex) { error = ex.Message; return false; }
    }

    private static ProcessStartInfo BuildPsi(string args) => new("schtasks.exe", args)
    {
        UseShellExecute        = false,
        CreateNoWindow         = true,
        RedirectStandardOutput = true,
        RedirectStandardError  = true
    };
}
