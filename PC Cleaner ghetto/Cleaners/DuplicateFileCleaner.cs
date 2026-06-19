using System.Security.Cryptography;
using PCCleaner.Core;
using PCCleaner.Utilities;

namespace PCCleaner.Cleaners;

internal sealed class DuplicateFileCleaner : ICleaner
{
    public string Name => Localizer.T("cleaner.duplicates.name");

    public string Description => Localizer.T("cleaner.duplicates.description");

    public string Risk => Localizer.T("cleaner.duplicates.risk");

    public CleanerPlatform Platform => CleanerPlatform.All;

    public bool RequiresAdministrator => false;

    public bool IsRecommended => false;

    public CleanResult Clean(CleanOptions options)
    {
        CleanResult result = new(Name, options.PreviewOnly);
        string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        if (!Directory.Exists(documentsPath))
        {
            return result;
        }

        foreach (IGrouping<long, string> sizeGroup in GetFilesFromDirectory(documentsPath).GroupBy(GetFileSize).Where(group => group.Key > 0 && group.Count() > 1))
        {
            DeleteHashDuplicates(sizeGroup, result, options);
        }

        return result;
    }

    private static void DeleteHashDuplicates(IEnumerable<string> filePaths, CleanResult result, CleanOptions options)
    {
        HashSet<string> seenHashes = new(StringComparer.OrdinalIgnoreCase);

        foreach (string filePath in filePaths.OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            string? hash = TryGetSha256(filePath);
            if (hash is null)
            {
                result.AddFailure();
                continue;
            }

            if (seenHashes.Add(hash))
            {
                continue;
            }

            FileDeleteHelper.TryDeleteFile(filePath, result, options);
        }
    }

    private static IEnumerable<string> GetFilesFromDirectory(string directoryPath)
    {
        IEnumerable<string> files;
        IEnumerable<string> directories;

        try
        {
            files = Directory.EnumerateFiles(directoryPath);
        }
        catch
        {
            files = Enumerable.Empty<string>();
        }

        foreach (string filePath in files)
        {
            yield return filePath;
        }

        try
        {
            directories = Directory.EnumerateDirectories(directoryPath);
        }
        catch
        {
            directories = Enumerable.Empty<string>();
        }

        foreach (string childDirectory in directories)
        {
            foreach (string filePath in GetFilesFromDirectory(childDirectory))
            {
                yield return filePath;
            }
        }
    }

    private static long GetFileSize(string filePath)
    {
        try
        {
            return new FileInfo(filePath).Length;
        }
        catch
        {
            return -1;
        }
    }

    private static string? TryGetSha256(string filePath)
    {
        try
        {
            using FileStream stream = File.OpenRead(filePath);
            using SHA256 sha256 = SHA256.Create();
            byte[] hash = sha256.ComputeHash(stream);
            return Convert.ToHexString(hash);
        }
        catch
        {
            return null;
        }
    }
}
