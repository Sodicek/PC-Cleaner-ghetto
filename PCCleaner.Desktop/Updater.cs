using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;

namespace PCCleaner.Desktop;

internal static class Updater
{
    public static async Task<(bool Success, string Message)> DownloadAndApplyAsync(
        UpdateInfo update,
        Action<string> progress,
        CancellationToken ct = default)
    {
        if (update.DownloadUrl is null)
            return (false, $"No binary found for this platform.\nVisit: {update.ReleasePageUrl}");

        string? exePath = Environment.ProcessPath;
        bool isDotnetHost = string.IsNullOrEmpty(exePath) ||
            Path.GetFileNameWithoutExtension(exePath)
                .Equals("dotnet", StringComparison.OrdinalIgnoreCase);

        if (isDotnetHost)
            return (false, "Auto-update only works with the compiled binary.\nBuild Release and run PCCleaner.Desktop directly.");

        string exeDir  = Path.GetDirectoryName(exePath)!;
        string tmpPath = Path.Combine(exeDir, update.AssetName + ".download");

        try
        {
            progress("Connecting...");
            using var http = new HttpClient();
            http.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("PC-Cleaner", AppVersion.Current));
            http.Timeout = TimeSpan.FromMinutes(3);

            using var resp = await http.GetAsync(update.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, ct);
            resp.EnsureSuccessStatusCode();

            long total = resp.Content.Headers.ContentLength ?? -1;
            await using var src  = await resp.Content.ReadAsStreamAsync(ct);
            await using var dest = File.Create(tmpPath);

            byte[] buf      = new byte[81920];
            long   received = 0;
            int    read;
            while ((read = await src.ReadAsync(buf, ct)) > 0)
            {
                await dest.WriteAsync(buf.AsMemory(0, read), ct);
                received += read;
                if (total > 0)
                    progress($"Downloading... {received * 100 / total}%  ({FormatBytes(received)} / {FormatBytes(total)})");
            }

            progress("Applying update...");

            if (OperatingSystem.IsWindows())
                ApplyWindows(exePath!, tmpPath, exeDir, update.AssetName!);
            else
                ApplyUnix(exePath!, tmpPath);

            return (true, string.Empty);
        }
        catch (OperationCanceledException)
        {
            TryDelete(tmpPath);
            return (false, "Update cancelled.");
        }
        catch (Exception ex)
        {
            TryDelete(tmpPath);
            return (false, $"Update failed: {ex.Message}");
        }
    }

    // ── Platform-specific apply ────────────────────────────────────────────────

    private static void ApplyWindows(string exePath, string tmpPath, string exeDir, string assetName)
    {
        // Can't replace a running .exe on Windows — use a helper batch script
        string staged     = Path.Combine(exeDir, assetName);
        string scriptPath = Path.Combine(exeDir, "_pccleaner_update.cmd");

        File.Move(tmpPath, staged, overwrite: true);

        File.WriteAllText(scriptPath,
            "@echo off\r\n" +
            "timeout /t 2 /nobreak >nul\r\n" +
           $"move /y \"{staged}\" \"{exePath}\"\r\n" +
           $"start \"\" \"{exePath}\"\r\n" +
            "del \"%~f0\"\r\n");

        Process.Start(new ProcessStartInfo
        {
            FileName        = scriptPath,
            UseShellExecute = true,
            WindowStyle     = ProcessWindowStyle.Hidden
        });

        Terminal.Gui.Application.RequestStop();
    }

    private static void ApplyUnix(string exePath, string tmpPath)
    {
        // Linux/macOS: inode-safe replace — running process keeps old inode open,
        // new binary is in place for next launch (or immediate restart below).
        File.Copy(tmpPath, exePath, overwrite: true);
        TryDelete(tmpPath);

        try { Process.Start("chmod", $"+x \"{exePath}\"")?.WaitForExit(2000); }
        catch { /* chmod not critical */ }

        Process.Start(new ProcessStartInfo { FileName = exePath, UseShellExecute = false });
        Terminal.Gui.Application.RequestStop();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { }
    }

    private static string FormatBytes(long bytes) => bytes switch
    {
        < 1024             => $"{bytes} B",
        < 1024 * 1024      => $"{bytes / 1024.0:F1} KB",
        < 1024L * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        _                  => $"{bytes / (1024.0 * 1024 * 1024):F2} GB"
    };
}
