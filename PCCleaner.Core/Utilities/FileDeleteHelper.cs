using PCCleaner.Core;

namespace PCCleaner.Utilities;

internal static class FileDeleteHelper
{
    private static readonly EnumerationOptions TopDirectoryEnumeration = new()
    {
        IgnoreInaccessible = true,
        RecurseSubdirectories = false,
        ReturnSpecialDirectories = false
    };

    public static CleanResult CleanDirectory(string cleanerName, string directoryPath, CleanOptions options, bool deleteRootDirectory = false)
    {
        CleanResult result = new(cleanerName, options.PreviewOnly);

        if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
        {
            return result;
        }

        DeleteFiles(directoryPath, result, options);
        DeleteChildDirectories(directoryPath, result, options);

        if (deleteRootDirectory)
        {
            TryDeleteDirectory(directoryPath, result, options);
        }

        return result;
    }

    public static CleanResult CleanFiles(string cleanerName, string directoryPath, IReadOnlyList<string> searchPatterns, bool recursive, CleanOptions options)
    {
        CleanResult result = new(cleanerName, options.PreviewOnly);

        if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
        {
            return result;
        }

        foreach (string searchPattern in searchPatterns)
        {
            IEnumerable<string> filePaths = recursive
                ? SafeEnumerateFilesRecursive(directoryPath, searchPattern)
                : SafeEnumerateFiles(directoryPath, searchPattern);

            foreach (string filePath in filePaths)
            {
                TryDeleteFile(filePath, result, options);
            }
        }

        return result;
    }

    private static void DeleteFiles(string directoryPath, CleanResult result, CleanOptions options)
    {
        foreach (string filePath in SafeEnumerateFiles(directoryPath))
        {
            TryDeleteFile(filePath, result, options);
        }
    }

    private static void DeleteChildDirectories(string directoryPath, CleanResult result, CleanOptions options)
    {
        foreach (string childDirectory in SafeEnumerateDirectories(directoryPath))
        {
            if (IsDirectoryLink(childDirectory))
            {
                continue;
            }

            CleanResult childResult = CleanDirectory(result.CleanerName, childDirectory, options, deleteRootDirectory: true);
            result.Merge(childResult);
        }
    }

    public static void TryDeleteFile(string filePath, CleanResult result, CleanOptions options)
    {
        try
        {
            FileInfo file = new(filePath);
            long size = file.Exists ? file.Length : 0;

            if (file.LinkTarget is not null)
            {
                result.AddSkippedFile();
                return;
            }

            if (file.Exists && options.ShouldSkipBecauseRecent(file))
            {
                result.AddSkippedFile();
                return;
            }

            if (options.PreviewOnly)
            {
                result.AddDeletedFile(size);
                return;
            }

            file.Attributes = FileAttributes.Normal;
            try
            {
                file.Delete();
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
            {
                // Retry once — covers transient antivirus/indexer locks (IOException)
                // and cases where clearing attributes didn't take effect immediately (UnauthorizedAccessException)
                Thread.Sleep(500);
                var retry = new FileInfo(filePath);
                retry.Attributes = FileAttributes.Normal;
                retry.Delete(); // if still fails, outer catch records it
            }

            result.AddDeletedFile(size);
        }
        catch (Exception ex)
        {
            result.AddFailure(filePath, ex.Message);
        }
    }

    private static void TryDeleteDirectory(string directoryPath, CleanResult result, CleanOptions options)
    {
        try
        {
            if (options.PreviewOnly)
            {
                result.AddDeletedDirectory();
                return;
            }

            Directory.Delete(directoryPath, recursive: false);
            result.AddDeletedDirectory();
        }
        catch (Exception ex)
        {
            result.AddFailure(directoryPath, ex.Message);
        }
    }

    internal static bool IsDirectoryLink(string directoryPath)
    {
        try
        {
            DirectoryInfo directory = new(directoryPath);
            return directory.LinkTarget is not null
                || directory.Attributes.HasFlag(FileAttributes.ReparsePoint);
        }
        catch
        {
            return true;
        }
    }

    private static IEnumerable<string> SafeEnumerateFiles(string directoryPath)
    {
        string[] files;

        try
        {
            files = Directory.GetFiles(directoryPath, "*", TopDirectoryEnumeration);
        }
        catch
        {
            yield break;
        }

        foreach (string file in files)
        {
            yield return file;
        }
    }

    private static IEnumerable<string> SafeEnumerateFiles(string directoryPath, string searchPattern)
    {
        string[] files;

        try
        {
            files = Directory.GetFiles(directoryPath, searchPattern, TopDirectoryEnumeration);
        }
        catch
        {
            yield break;
        }

        foreach (string file in files)
        {
            yield return file;
        }
    }

    private static IEnumerable<string> SafeEnumerateFilesRecursive(string directoryPath, string searchPattern)
    {
        foreach (string filePath in SafeEnumerateFiles(directoryPath, searchPattern))
        {
            yield return filePath;
        }

        foreach (string childDirectory in SafeEnumerateDirectories(directoryPath))
        {
            if (IsDirectoryLink(childDirectory))
            {
                continue;
            }

            foreach (string filePath in SafeEnumerateFilesRecursive(childDirectory, searchPattern))
            {
                yield return filePath;
            }
        }
    }

    private static IEnumerable<string> SafeEnumerateDirectories(string directoryPath)
    {
        string[] directories;

        try
        {
            directories = Directory.GetDirectories(directoryPath, "*", TopDirectoryEnumeration);
        }
        catch
        {
            yield break;
        }

        foreach (string directory in directories)
        {
            yield return directory;
        }
    }
}
