using PCCleaner.Core;

namespace PCCleaner.Utilities;

internal static class FileDeleteHelper
{
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
            foreach (string filePath in SafeEnumerateFiles(directoryPath, searchPattern, recursive))
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
            file.Delete();
            result.AddDeletedFile(size);
        }
        catch
        {
            result.AddFailure();
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
        catch
        {
            result.AddFailure();
        }
    }

    private static IEnumerable<string> SafeEnumerateFiles(string directoryPath)
    {
        try
        {
            return Directory.EnumerateFiles(directoryPath);
        }
        catch
        {
            return Enumerable.Empty<string>();
        }
    }

    private static IEnumerable<string> SafeEnumerateFiles(string directoryPath, string searchPattern, bool recursive)
    {
        try
        {
            SearchOption searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            return Directory.EnumerateFiles(directoryPath, searchPattern, searchOption);
        }
        catch
        {
            return Enumerable.Empty<string>();
        }
    }

    private static IEnumerable<string> SafeEnumerateDirectories(string directoryPath)
    {
        try
        {
            return Directory.EnumerateDirectories(directoryPath);
        }
        catch
        {
            return Enumerable.Empty<string>();
        }
    }
}
