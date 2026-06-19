using System.Security.Cryptography;
using PCCleaner.Core;
using PCCleaner.Utilities;

namespace PCCleaner.Cleaners;

internal sealed class DuplicateFileCleaner : ICleaner
{
    private const int HashBufferSize = 1024 * 1024;

    public string Name => Localizer.T("cleaner.duplicates.name");

    public string Description => Localizer.T("cleaner.duplicates.description");

    public string Risk => Localizer.T("cleaner.duplicates.risk");

    public CleanerPlatform Platform => CleanerPlatform.All;

    public bool RequiresAdministrator => false;

    public bool IsRecommended => false;

    public CleanResult Clean(CleanOptions options)
    {
        CleanResult result = new(Name, options.PreviewOnly);
        string documentsPath = GetDocumentsPath();

        if (!Directory.Exists(documentsPath))
        {
            return result;
        }

        foreach (IGrouping<long, FileCandidate> sizeGroup in GetFilesFromDirectory(documentsPath)
            .GroupBy(file => file.Size)
            .Where(group => group.Key > 0 && group.Count() > 1))
        {
            DeleteHashDuplicates(sizeGroup, result, options);
        }

        return result;
    }

    private static void DeleteHashDuplicates(IEnumerable<FileCandidate> files, CleanResult result, CleanOptions options)
    {
        HashSet<string> seenHashes = new(StringComparer.OrdinalIgnoreCase);

        foreach (FileCandidate file in files.OrderBy(candidate => candidate.Path, StringComparer.OrdinalIgnoreCase))
        {
            if (options.ShouldSkipBecauseRecent(file.Info))
            {
                result.AddSkippedFile();
                continue;
            }

            string? hash = TryGetSha256(file.Path);
            if (hash is null)
            {
                result.AddFailure();
                continue;
            }

            if (seenHashes.Add(hash))
            {
                continue;
            }

            FileDeleteHelper.TryDeleteFile(file.Path, result, options);
        }
    }

    private static IEnumerable<FileCandidate> GetFilesFromDirectory(string directoryPath)
    {
        string[] files;
        string[] directories;

        try
        {
            files = Directory.GetFiles(directoryPath);
        }
        catch
        {
            files = Array.Empty<string>();
        }

        foreach (string filePath in files)
        {
            FileCandidate? candidate = TryCreateFileCandidate(filePath);
            if (candidate is not null)
            {
                yield return candidate;
            }
        }

        try
        {
            directories = Directory.GetDirectories(directoryPath);
        }
        catch
        {
            directories = Array.Empty<string>();
        }

        foreach (string childDirectory in directories)
        {
            if (FileDeleteHelper.IsDirectoryLink(childDirectory))
            {
                continue;
            }

            foreach (FileCandidate candidate in GetFilesFromDirectory(childDirectory))
            {
                yield return candidate;
            }
        }
    }

    private static FileCandidate? TryCreateFileCandidate(string filePath)
    {
        try
        {
            FileInfo file = new(filePath);
            if (!file.Exists || file.LinkTarget is not null)
            {
                return null;
            }

            return new FileCandidate(filePath, file.Length, file);
        }
        catch
        {
            return null;
        }
    }

    private static string? TryGetSha256(string filePath)
    {
        try
        {
            using FileStream stream = new(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete,
                HashBufferSize,
                FileOptions.SequentialScan);
            using SHA256 sha256 = SHA256.Create();
            byte[] hash = sha256.ComputeHash(stream);
            return Convert.ToHexString(hash);
        }
        catch
        {
            return null;
        }
    }

    private static string GetDocumentsPath()
    {
        string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        if (OperatingSystem.IsWindows())
        {
            return documents;
        }

        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        // On Linux/macOS, MyDocuments can fall back to home dir when ~/Documents doesn't exist.
        // Scanning the entire home dir would be too broad and slow.
        if (string.IsNullOrWhiteSpace(documents)
            || string.Equals(documents, home, StringComparison.OrdinalIgnoreCase))
        {
            return Path.Combine(home, "Documents");
        }

        return documents;
    }

    private sealed record FileCandidate(string Path, long Size, FileInfo Info);
}
