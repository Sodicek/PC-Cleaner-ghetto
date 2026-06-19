using PCCleaner.Core;
using PCCleaner.Utilities;

namespace PCCleaner.Cleaners;

internal sealed class SteamCacheCleaner : ICleaner
{
    public string Name        => Localizer.T("cleaner.steamCache.name");
    public string Description => Localizer.T("cleaner.steamCache.description");
    public string Risk        => Localizer.T("cleaner.steamCache.risk");

    public CleanerPlatform Platform             => CleanerPlatform.All;
    public bool            RequiresAdministrator => false;
    public bool            IsRecommended         => false;

    public CleanResult Clean(CleanOptions options)
    {
        var result = new CleanResult(Name, options.PreviewOnly);
        foreach (string path in GetCachePaths())
            result.Merge(FileDeleteHelper.CleanDirectory(Name, path, options));
        return result;
    }

    private static IEnumerable<string> GetCachePaths()
    {
        if (OperatingSystem.IsLinux())
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            // Native Steam
            yield return Path.Combine(home, ".local", "share", "Steam", "steamapps", "shadercache");
            yield return Path.Combine(home, ".local", "share", "Steam", "steamapps", "downloading");
            yield return Path.Combine(home, ".local", "share", "Steam", "steamapps", "temp");
            // ~/.steam/steam is usually a symlink but enumerate anyway
            yield return Path.Combine(home, ".steam", "steam", "steamapps", "shadercache");
            // Flatpak Steam (common on Nobara)
            yield return Path.Combine(home, ".var", "app", "com.valvesoftware.Steam",
                ".local", "share", "Steam", "steamapps", "shadercache");
            yield return Path.Combine(home, ".var", "app", "com.valvesoftware.Steam",
                ".local", "share", "Steam", "steamapps", "downloading");
        }
        else if (OperatingSystem.IsWindows())
        {
            string pf86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            // Try common install locations — Steam can be on any drive
            foreach (string root in new[] {
                Path.Combine(pf86, "Steam"),
                Path.Combine(local, "Steam"),
                @"C:\Steam", @"D:\Steam", @"E:\Steam", @"F:\Steam",
                @"C:\Program Files\Steam"
            }.Where(Directory.Exists))
            {
                yield return Path.Combine(root, "steamapps", "shadercache");
                yield return Path.Combine(root, "steamapps", "downloading");
                yield return Path.Combine(root, "steamapps", "temp");
                yield return Path.Combine(root, "htmlcache");
            }
        }
        else if (OperatingSystem.IsMacOS())
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            yield return Path.Combine(home, "Library", "Application Support", "Steam", "steamapps", "shadercache");
            yield return Path.Combine(home, "Library", "Application Support", "Steam", "steamapps", "downloading");
        }
    }
}
