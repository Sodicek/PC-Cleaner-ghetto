using System.Text.Json;
using PCCleaner.Core;
using PCCleaner.Utilities;

namespace PCCleaner.Cleaners;

internal sealed class VsCodeWorkspaceCleaner : ICleaner
{
    public string Name        => Localizer.T("cleaner.vscode.name");
    public string Description => Localizer.T("cleaner.vscode.description");
    public string Risk        => Localizer.T("cleaner.vscode.risk");

    public CleanerPlatform Platform             => CleanerPlatform.All;
    public bool            RequiresAdministrator => false;
    public bool            IsRecommended         => false;

    public CleanResult Clean(CleanOptions options)
    {
        var result = new CleanResult(Name, options.PreviewOnly);

        foreach (string storageRoot in GetWorkspaceStoragePaths())
        {
            if (!Directory.Exists(storageRoot))
                continue;

            foreach (string entry in Directory.EnumerateDirectories(storageRoot))
            {
                if (!IsOrphaned(entry))
                    continue;

                if (options.PreviewOnly)
                {
                    result.AddNote($"Orphaned: {Path.GetFileName(entry)}");
                    result.AddSkippedFile();
                }
                else
                {
                    try
                    {
                        long size = GetDirSize(entry);
                        Directory.Delete(entry, recursive: true);
                        result.AddDeletedFile(size);
                        result.AddDeletedDirectory();
                    }
                    catch { result.AddFailure(); }
                }
            }
        }

        return result;
    }

    private static IEnumerable<string> GetWorkspaceStoragePaths()
    {
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        if (OperatingSystem.IsWindows())
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            yield return Path.Combine(appData, "Code", "User", "workspaceStorage");
        }
        else if (OperatingSystem.IsMacOS())
        {
            yield return Path.Combine(home, "Library", "Application Support", "Code", "User", "workspaceStorage");
        }
        else // Linux
        {
            string config = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME")
                            ?? Path.Combine(home, ".config");
            yield return Path.Combine(config, "Code", "User", "workspaceStorage");
            yield return Path.Combine(config, "Code - OSS", "User", "workspaceStorage");
            yield return Path.Combine(config, "VSCodium", "User", "workspaceStorage");
        }
    }

    internal static bool IsOrphaned(string workspaceDir)
    {
        try
        {
            string jsonPath = Path.Combine(workspaceDir, "workspace.json");
            if (!File.Exists(jsonPath)) return true; // no manifest — orphaned

            using var doc = JsonDocument.Parse(File.ReadAllText(jsonPath));
            if (doc.RootElement.TryGetProperty("folder", out var folderEl))
            {
                string? uri = folderEl.GetString();
                if (uri is null) return false;
                if (Uri.TryCreate(uri, UriKind.Absolute, out var parsed) && parsed.IsFile)
                    return !Directory.Exists(parsed.LocalPath);
            }

            // workspace.json exists but points to a multi-root or remote — keep it
            return false;
        }
        catch { return false; }
    }

    private static long GetDirSize(string path)
    {
        long total = 0;
        try
        {
            foreach (string f in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            {
                try { total += new FileInfo(f).Length; }
                catch { }
            }
        }
        catch { }
        return total;
    }
}
