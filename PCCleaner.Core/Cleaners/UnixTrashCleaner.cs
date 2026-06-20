using PCCleaner.Core;
using PCCleaner.Utilities;

namespace PCCleaner.Cleaners;

internal sealed class UnixTrashCleaner : ICleaner
{
    public string Name => Localizer.T("cleaner.unixTrash.name");

    public string Description => Localizer.T("cleaner.unixTrash.description");

    public string Risk => Localizer.T("cleaner.unixTrash.risk");

    public CleanerPlatform Platform => CleanerPlatform.UnixLike;

    public bool RequiresAdministrator => false;

    public bool IsRecommended => true;

    public CleanResult Clean(CleanOptions options)
    {
        CleanResult result = new(Name, options.PreviewOnly);
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        if (string.IsNullOrWhiteSpace(home))
            return result;

        if (OperatingSystem.IsMacOS())
        {
            // macOS: ~/.Trash/
            result.Merge(FileDeleteHelper.CleanDirectory(Name, Path.Combine(home, ".Trash"), options));
        }
        else
        {
            // Linux XDG spec: ~/.local/share/Trash/
            string trashBase = Path.Combine(home, ".local", "share", "Trash");
            result.Merge(FileDeleteHelper.CleanDirectory(Name, Path.Combine(trashBase, "files"), options));
            result.Merge(FileDeleteHelper.CleanDirectory(Name, Path.Combine(trashBase, "info"), options));
        }

        return result;
    }
}
