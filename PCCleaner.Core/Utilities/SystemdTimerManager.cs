using System.Diagnostics;

namespace PCCleaner.Utilities;

internal static class SystemdTimerManager
{
    private const string ServiceName = "pc-cleaner";
    private const string TimerUnit   = "pc-cleaner.timer";

    public static bool Exists()
    {
        try
        {
            string output = RunSystemctl("--user is-enabled pc-cleaner.timer");
            return output.StartsWith("enabled", StringComparison.OrdinalIgnoreCase)
                || output.StartsWith("static",  StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }

    public static bool Create(string exePath, out string error)
    {
        error = string.Empty;
        // Guard against newline injection into the unit file
        if (exePath.Contains('\n') || exePath.Contains('\r'))
        {
            error = "Executable path must not contain newline characters.";
            return false;
        }

        try
        {
            string unitDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".config", "systemd", "user");
            Directory.CreateDirectory(unitDir);

            // Quote the path so systemd handles spaces correctly
            string quotedPath = "\"" + exePath.Replace("\"", "\\\"") + "\"";

            File.WriteAllText(Path.Combine(unitDir, $"{ServiceName}.service"),
                $"[Unit]\n" +
                $"Description=PC Cleaner auto-clean\n\n" +
                $"[Service]\n" +
                $"Type=oneshot\n" +
                $"ExecStart={quotedPath} --auto-clean\n\n" +
                $"[Install]\n" +
                $"WantedBy=default.target\n");

            File.WriteAllText(Path.Combine(unitDir, TimerUnit),
                $"[Unit]\n" +
                $"Description=PC Cleaner weekly timer\n\n" +
                $"[Timer]\n" +
                $"OnCalendar=Sun *-*-* 09:00:00\n" +
                $"Persistent=true\n\n" +
                $"[Install]\n" +
                $"WantedBy=timers.target\n");

            RunSystemctl("--user daemon-reload");
            string result = RunSystemctl($"--user enable --now {TimerUnit}");
            return true;
        }
        catch (Exception ex) { error = ex.Message; return false; }
    }

    public static bool Remove(out string error)
    {
        error = string.Empty;
        try
        {
            RunSystemctl($"--user disable --now {TimerUnit}");

            string unitDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".config", "systemd", "user");

            TryDelete(Path.Combine(unitDir, $"{ServiceName}.service"));
            TryDelete(Path.Combine(unitDir, TimerUnit));
            RunSystemctl("--user daemon-reload");
            return true;
        }
        catch (Exception ex) { error = ex.Message; return false; }
    }

    private static string RunSystemctl(string args)
    {
        try
        {
            using var p = Process.Start(new ProcessStartInfo("systemctl", args)
            {
                UseShellExecute        = false,
                RedirectStandardOutput = true,
                RedirectStandardError  = true
            });
            string output = p?.StandardOutput.ReadToEnd() ?? string.Empty;
            p?.WaitForExit(8_000);
            return output.Trim();
        }
        catch { return string.Empty; }
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { }
    }
}
