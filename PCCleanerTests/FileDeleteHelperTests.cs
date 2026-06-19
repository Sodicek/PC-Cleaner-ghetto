using PCCleaner.Core;
using PCCleaner.Utilities;
using Xunit;

namespace PCCleanerTests;

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
