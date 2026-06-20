using PCCleaner.Core;
using PCCleaner.Utilities;
using Xunit;

namespace PCCleaner.Tests;

public class FileDeleteHelperTests : IDisposable
{
    private readonly string _dir;

    public FileDeleteHelperTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), $"PCTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }

    // --- Preview mode ---

    [Fact]
    public void CleanDirectory_Preview_DoesNotDeleteFiles()
    {
        string file = MakeOldFile("old.txt", "data");
        var options = Preview();
        var result = FileDeleteHelper.CleanDirectory("test", _dir, options);

        Assert.True(File.Exists(file), "preview must not delete files");
        Assert.Equal(1, result.FilesDeleted);
        Assert.Equal(0, result.Failures);
    }

    [Fact]
    public void CleanDirectory_Preview_ReportsCorrectByteCount()
    {
        MakeOldFile("a.txt", new string('x', 1024));
        MakeOldFile("b.txt", new string('y', 512));
        var result = FileDeleteHelper.CleanDirectory("test", _dir, Preview());

        Assert.Equal(2, result.FilesDeleted);
        Assert.Equal(1024 + 512, result.BytesFreed);
    }

    // --- Real clean mode ---

    [Fact]
    public void CleanDirectory_Real_DeletesOldFiles()
    {
        string file = MakeOldFile("gone.txt", "bye");
        var result = FileDeleteHelper.CleanDirectory("test", _dir, Real());

        Assert.False(File.Exists(file), "real clean must delete old files");
        Assert.Equal(1, result.FilesDeleted);
        Assert.Equal(0, result.Failures);
    }

    [Fact]
    public void CleanDirectory_Real_BytesFreedMatchesFileSize()
    {
        byte[] data = new byte[4096];
        string path = Path.Combine(_dir, "sized.bin");
        File.WriteAllBytes(path, data);
        SetOld(path);

        var result = FileDeleteHelper.CleanDirectory("test", _dir, Real());
        Assert.Equal(4096, result.BytesFreed);
    }

    // --- Age filter ---

    [Fact]
    public void CleanDirectory_AgeFilter_SkipsRecentKeepsOld()
    {
        string recent = MakeFile("recent.txt", "new");
        SetAge(recent, hoursAgo: 1);

        string old = MakeOldFile("old.txt", "old");

        var result = FileDeleteHelper.CleanDirectory("test", _dir, Real());

        Assert.True(File.Exists(recent), "recent file must be skipped");
        Assert.False(File.Exists(old), "old file must be deleted");
        Assert.Equal(1, result.FilesDeleted);
        Assert.Equal(1, result.FilesSkipped);
    }

    [Fact]
    public void CleanDirectory_IncludeRecent_DeletesEverything()
    {
        string brand_new = MakeFile("brand.txt", "hot off the press");
        SetAge(brand_new, hoursAgo: 0);

        var options = new CleanOptions(previewOnly: false, TimeSpan.FromHours(24), includeRecentFiles: true);
        var result = FileDeleteHelper.CleanDirectory("test", _dir, options);

        Assert.False(File.Exists(brand_new));
        Assert.Equal(0, result.FilesSkipped);
    }

    // --- Directory recursion ---

    [Fact]
    public void CleanDirectory_RecursesIntoSubdirectories()
    {
        string sub = Path.Combine(_dir, "sub");
        Directory.CreateDirectory(sub);
        string nested = MakeOldFile(Path.Combine("sub", "nested.txt"), "deep");

        var result = FileDeleteHelper.CleanDirectory("test", _dir, Real());

        Assert.False(File.Exists(nested));
        Assert.False(Directory.Exists(sub), "empty subdir should be deleted too");
        Assert.Equal(1, result.FilesDeleted);
        Assert.Equal(1, result.DirectoriesDeleted);
    }

    [Fact]
    public void CleanDirectory_DeeplyNested_DeletesAll()
    {
        string deep = Path.Combine(_dir, "a", "b", "c");
        Directory.CreateDirectory(deep);
        string file = Path.Combine(deep, "file.txt");
        File.WriteAllText(file, "deep");
        SetOld(file);

        var result = FileDeleteHelper.CleanDirectory("test", _dir, Real());

        Assert.False(File.Exists(file));
        Assert.Equal(1, result.FilesDeleted);
    }

    [Fact]
    public void CleanDirectory_Preview_DoesNotDeleteSubdirectories()
    {
        string sub = Path.Combine(_dir, "keepme");
        Directory.CreateDirectory(sub);
        MakeOldFile(Path.Combine("keepme", "file.txt"), "x");

        FileDeleteHelper.CleanDirectory("test", _dir, Preview());

        Assert.True(Directory.Exists(sub));
    }

    // --- Edge cases ---

    [Fact]
    public void CleanDirectory_NonExistentPath_ReturnsEmptyWithNoFailures()
    {
        string missing = Path.Combine(_dir, "does_not_exist");
        var result = FileDeleteHelper.CleanDirectory("test", missing, Real());

        Assert.Equal(0, result.FilesDeleted);
        Assert.Equal(0, result.Failures);
    }

    [Fact]
    public void CleanDirectory_EmptyDirectory_ReturnsZeroCounts()
    {
        var result = FileDeleteHelper.CleanDirectory("test", _dir, Real());

        Assert.Equal(0, result.FilesDeleted);
        Assert.Equal(0, result.Failures);
    }

    [Fact]
    public void CleanDirectory_NullOrEmptyPath_ReturnsEmptyWithNoFailures()
    {
        var resultEmpty = FileDeleteHelper.CleanDirectory("test", "", Real());
        var resultWhitespace = FileDeleteHelper.CleanDirectory("test", "   ", Real());

        Assert.Equal(0, resultEmpty.FilesDeleted);
        Assert.Equal(0, resultWhitespace.Failures);
    }

    // --- CleanFiles with patterns ---

    [Fact]
    public void CleanFiles_MatchingPattern_DeletesOnlyMatchingFiles()
    {
        string log = MakeOldFile("app.log", "log data");
        string txt = MakeOldFile("readme.txt", "keep me");

        var result = FileDeleteHelper.CleanFiles("test", _dir, new[] { "*.log" }, recursive: false, Real());

        Assert.False(File.Exists(log), "*.log must be deleted");
        Assert.True(File.Exists(txt), "*.txt must survive");
        Assert.Equal(1, result.FilesDeleted);
    }

    [Fact]
    public void CleanFiles_MultiplePatterns_DeletesAllMatching()
    {
        string log = MakeOldFile("app.log", "x");
        string tmp = MakeOldFile("setup.tmp", "x");
        string txt = MakeOldFile("readme.txt", "keep");

        var result = FileDeleteHelper.CleanFiles("test", _dir, new[] { "*.log", "*.tmp" }, recursive: false, Real());

        Assert.False(File.Exists(log));
        Assert.False(File.Exists(tmp));
        Assert.True(File.Exists(txt));
        Assert.Equal(2, result.FilesDeleted);
    }

    [Fact]
    public void CleanFiles_Recursive_FindsFilesInSubdirs()
    {
        string sub = Path.Combine(_dir, "sub");
        Directory.CreateDirectory(sub);
        string log = MakeOldFile(Path.Combine("sub", "nested.log"), "x");

        var result = FileDeleteHelper.CleanFiles("test", _dir, new[] { "*.log" }, recursive: true, Real());

        Assert.False(File.Exists(log));
        Assert.Equal(1, result.FilesDeleted);
    }

    [Fact]
    public void CleanFiles_NonRecursive_IgnoresSubdirFiles()
    {
        string sub = Path.Combine(_dir, "sub");
        Directory.CreateDirectory(sub);
        string nested = MakeOldFile(Path.Combine("sub", "deep.log"), "x");

        var result = FileDeleteHelper.CleanFiles("test", _dir, new[] { "*.log" }, recursive: false, Real());

        Assert.True(File.Exists(nested), "non-recursive must not reach subdirs");
        Assert.Equal(0, result.FilesDeleted);
    }

    // --- deleteRootDirectory flag ---

    [Fact]
    public void CleanDirectory_DeleteRootDirectory_True_RemovesRootAfterEmpty()
    {
        MakeOldFile("file.txt", "x");

        FileDeleteHelper.CleanDirectory("test", _dir, Real(), deleteRootDirectory: true);

        Assert.False(Directory.Exists(_dir), "root directory must be removed when deleteRootDirectory is true");
    }

    [Fact]
    public void CleanDirectory_DeleteRootDirectory_False_PreservesRoot()
    {
        MakeOldFile("file.txt", "x");

        FileDeleteHelper.CleanDirectory("test", _dir, Real(), deleteRootDirectory: false);

        Assert.True(Directory.Exists(_dir), "root directory must be kept when deleteRootDirectory is false");
    }

    [Fact]
    public void CleanDirectory_DeleteRootDirectory_True_CountsRootAsDeletedDirectory()
    {
        MakeOldFile("file.txt", "x");

        var result = FileDeleteHelper.CleanDirectory("test", _dir, Real(), deleteRootDirectory: true);

        Assert.Equal(1, result.DirectoriesDeleted);
    }

    // --- IsDirectoryLink ---

    [Fact]
    public void IsDirectoryLink_RegularDirectory_ReturnsFalse()
    {
        Assert.False(FileDeleteHelper.IsDirectoryLink(_dir));
    }

    [Fact]
    public void IsDirectoryLink_NonExistentPath_ReturnsTrueAsSafeDefault()
    {
        string gone = Path.Combine(Path.GetTempPath(), $"Gone_{Guid.NewGuid():N}");
        // Cannot read attributes of missing path → safe default is true (skip it)
        Assert.True(FileDeleteHelper.IsDirectoryLink(gone));
    }

    // --- v1.0.2: Per-file error log ---

    [Fact]
    public void AddFailure_WithPathAndReason_AddsNoteWithFilename()
    {
        var result = new CleanResult("test");
        result.AddFailure(@"C:\temp\cache.db", "Access to the path is denied");

        Assert.Equal(1, result.Failures);
        Assert.Single(result.Notes);
        Assert.Contains("cache.db", result.Notes[0]);
        Assert.Contains("Access to the path is denied", result.Notes[0]);
    }

    [Fact]
    public void AddFailure_NoArgs_OnlyIncrementsCount()
    {
        var result = new CleanResult("test");
        result.AddFailure();

        Assert.Equal(1, result.Failures);
        Assert.Empty(result.Notes);
    }

    [Fact]
    public void AddFailure_WithDirectoryPath_AddsNoteWithDirectoryName()
    {
        var result = new CleanResult("test");
        result.AddFailure(@"C:\temp\subdir", "Directory not empty");

        Assert.Equal(1, result.Failures);
        Assert.Contains("subdir", result.Notes[0]);
        Assert.Contains("Directory not empty", result.Notes[0]);
    }

    [Fact]
    public void CleanDirectory_LockedFile_RecordsFilenameInNotes()
    {
        string file = MakeOldFile("locked.txt", "data");

        // Hold an exclusive lock on the file during clean
        using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None);

        var result = FileDeleteHelper.CleanDirectory("test", _dir, Real());

        // The file is locked — delete fails even after retry
        Assert.Equal(0, result.FilesDeleted);
        Assert.Equal(1, result.Failures);
        Assert.Contains(result.Notes, n => n.Contains("locked.txt"));
    }

    [Fact]
    public void CleanDirectory_MultipleFailures_AllFilenamesInNotes()
    {
        string f1 = MakeOldFile("lock1.txt", "x");
        string f2 = MakeOldFile("lock2.txt", "y");

        using var s1 = new FileStream(f1, FileMode.Open, FileAccess.Read, FileShare.None);
        using var s2 = new FileStream(f2, FileMode.Open, FileAccess.Read, FileShare.None);

        var result = FileDeleteHelper.CleanDirectory("test", _dir, Real());

        Assert.Equal(2, result.Failures);
        Assert.Contains(result.Notes, n => n.Contains("lock1.txt"));
        Assert.Contains(result.Notes, n => n.Contains("lock2.txt"));
    }

    // --- Helpers ---

    private CleanOptions Real() =>
        new(previewOnly: false, TimeSpan.FromHours(24), includeRecentFiles: false);

    private CleanOptions Preview() =>
        new(previewOnly: true, TimeSpan.FromHours(24), includeRecentFiles: false);

    private string MakeFile(string relativePath, string content)
    {
        string full = Path.Combine(_dir, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content);
        return full;
    }

    private string MakeOldFile(string relativePath, string content)
    {
        string path = MakeFile(relativePath, content);
        SetOld(path);
        return path;
    }

    private static void SetOld(string path) => SetAge(path, hoursAgo: 48);

    private static void SetAge(string path, double hoursAgo) =>
        File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddHours(-hoursAgo));
}
