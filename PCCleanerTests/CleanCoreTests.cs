using PCCleaner.Core;
using PCCleaner.Utilities;
using Xunit;

namespace PCCleanerTests;

public class CleanResultTests
{
    [Fact]
    public void AddDeletedFile_IncrementsBothCountAndBytes()
    {
        var result = new CleanResult("test");
        result.AddDeletedFile(500);
        Assert.Equal(1, result.FilesDeleted);
        Assert.Equal(500, result.BytesFreed);
    }

    [Fact]
    public void AddDeletedFile_NegativeBytesClampedToZero()
    {
        var result = new CleanResult("test");
        result.AddDeletedFile(-100);
        Assert.Equal(1, result.FilesDeleted);
        Assert.Equal(0, result.BytesFreed);
    }

    [Fact]
    public void AddDeletedFile_MultipleCalls_Accumulates()
    {
        var result = new CleanResult("test");
        result.AddDeletedFile(100);
        result.AddDeletedFile(200);
        result.AddDeletedFile(300);
        Assert.Equal(3, result.FilesDeleted);
        Assert.Equal(600, result.BytesFreed);
    }

    [Fact]
    public void AddSkippedFile_IncrementsSkippedOnly()
    {
        var result = new CleanResult("test");
        result.AddSkippedFile();
        Assert.Equal(1, result.FilesSkipped);
        Assert.Equal(0, result.FilesDeleted);
        Assert.Equal(0, result.BytesFreed);
    }

    [Fact]
    public void AddDeletedDirectory_IncrementsDirectoryCount()
    {
        var result = new CleanResult("test");
        result.AddDeletedDirectory();
        Assert.Equal(1, result.DirectoriesDeleted);
    }

    [Fact]
    public void AddFailure_IncrementsFailureCount()
    {
        var result = new CleanResult("test");
        result.AddFailure();
        Assert.Equal(1, result.Failures);
    }

    [Fact]
    public void Merge_CombinesAllCounters()
    {
        var a = new CleanResult("a");
        a.AddDeletedFile(100);
        a.AddSkippedFile();
        a.AddDeletedDirectory();
        a.AddFailure();
        a.AddNote("note-a");

        var b = new CleanResult("b");
        b.AddDeletedFile(200);
        b.AddSkippedFile();
        b.AddNote("note-b");

        a.Merge(b);

        Assert.Equal(2, a.FilesDeleted);
        Assert.Equal(300, a.BytesFreed);
        Assert.Equal(2, a.FilesSkipped);
        Assert.Equal(1, a.DirectoriesDeleted);
        Assert.Equal(1, a.Failures);
        Assert.Equal(2, a.Notes.Count);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AddNote_IgnoresBlankStrings(string note)
    {
        var result = new CleanResult("test");
        result.AddNote(note);
        Assert.Empty(result.Notes);
    }

    [Fact]
    public void AddNote_AddsNonBlankNote()
    {
        var result = new CleanResult("test");
        result.AddNote("something important");
        Assert.Single(result.Notes);
        Assert.Equal("something important", result.Notes[0]);
    }

    [Fact]
    public void ToConsoleMessage_PreviewOnly_ContainsWouldDelete()
    {
        Localizer.SetLanguage(AppLanguage.English);
        var result = new CleanResult("MyCleaner", previewOnly: true);
        Assert.Contains("would delete", result.ToConsoleMessage());
    }

    [Fact]
    public void ToConsoleMessage_RealClean_ContainsDeleted()
    {
        Localizer.SetLanguage(AppLanguage.English);
        var result = new CleanResult("MyCleaner", previewOnly: false);
        Assert.Contains("deleted", result.ToConsoleMessage());
    }

    [Fact]
    public void ToConsoleMessage_ContainsCleanerName()
    {
        Localizer.SetLanguage(AppLanguage.English);
        var result = new CleanResult("UniqueName999");
        Assert.Contains("UniqueName999", result.ToConsoleMessage());
    }

    [Fact]
    public void ToConsoleMessage_ContainsCorrectCounts()
    {
        Localizer.SetLanguage(AppLanguage.English);
        var result = new CleanResult("test", previewOnly: false);
        result.AddDeletedFile(1024);
        result.AddDeletedFile(512);
        result.AddSkippedFile();
        result.AddDeletedDirectory();
        string msg = result.ToConsoleMessage();
        Assert.Contains("2", msg);
        Assert.Contains("1", msg);
    }
}

public class CleanOptionsTests
{
    [Fact]
    public void ShouldSkipBecauseRecent_RecentFile_ReturnsTrue()
    {
        var options = new CleanOptions(previewOnly: false, TimeSpan.FromHours(24), includeRecentFiles: false);
        string path = WriteTempFile("recent");
        File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddHours(-1));

        try
        {
            Assert.True(options.ShouldSkipBecauseRecent(new FileInfo(path)));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void ShouldSkipBecauseRecent_OldFile_ReturnsFalse()
    {
        var options = new CleanOptions(previewOnly: false, TimeSpan.FromHours(24), includeRecentFiles: false);
        string path = WriteTempFile("old");
        File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddHours(-48));

        try
        {
            Assert.False(options.ShouldSkipBecauseRecent(new FileInfo(path)));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void ShouldSkipBecauseRecent_IncludeRecentTrue_AlwaysReturnsFalse()
    {
        var options = new CleanOptions(previewOnly: false, TimeSpan.FromHours(24), includeRecentFiles: true);
        string path = WriteTempFile("brand-new");
        File.SetLastWriteTimeUtc(path, DateTime.UtcNow);

        try
        {
            Assert.False(options.ShouldSkipBecauseRecent(new FileInfo(path)));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void ShouldSkipBecauseRecent_ExactlyAtBoundary_ReturnsFalse()
    {
        var options = new CleanOptions(previewOnly: false, TimeSpan.FromHours(24), includeRecentFiles: false);
        string path = WriteTempFile("boundary");
        File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddHours(-25));

        try
        {
            Assert.False(options.ShouldSkipBecauseRecent(new FileInfo(path)));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void ZeroAgeFilter_TreatsAllFilesAsOld()
    {
        var options = new CleanOptions(previewOnly: false, TimeSpan.Zero, includeRecentFiles: false);
        string path = WriteTempFile("any");
        File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddSeconds(-1));

        try
        {
            Assert.False(options.ShouldSkipBecauseRecent(new FileInfo(path)));
        }
        finally { File.Delete(path); }
    }

    private static string WriteTempFile(string content)
    {
        string path = Path.GetTempFileName();
        File.WriteAllText(path, content);
        return path;
    }
}

public class CleanRunReportTests
{
    [Fact]
    public void AddResult_MergesIntoTotal()
    {
        var report = new CleanRunReport(new CleanResult("Total"));
        var result = new CleanResult("CleanerA");
        result.AddDeletedFile(512);
        result.AddSkippedFile();

        report.AddResult(result);

        Assert.Single(report.Results);
        Assert.Equal(1, report.Total.FilesDeleted);
        Assert.Equal(512, report.Total.BytesFreed);
        Assert.Equal(1, report.Total.FilesSkipped);
    }

    [Fact]
    public void AddResult_MultipleCleaners_TotalAccumulates()
    {
        var report = new CleanRunReport(new CleanResult("Total"));

        var r1 = new CleanResult("A");
        r1.AddDeletedFile(100);

        var r2 = new CleanResult("B");
        r2.AddDeletedFile(200);

        report.AddResult(r1);
        report.AddResult(r2);

        Assert.Equal(2, report.Results.Count);
        Assert.Equal(2, report.Total.FilesDeleted);
        Assert.Equal(300, report.Total.BytesFreed);
    }

    [Fact]
    public void AddSkipped_AppearsInSkippedList()
    {
        var report = new CleanRunReport(new CleanResult("Total"));
        report.AddSkipped("CleanerX", "not supported on this OS");

        Assert.Single(report.SkippedCleaners);
        Assert.Contains("CleanerX", report.SkippedCleaners[0]);
        Assert.Contains("not supported on this OS", report.SkippedCleaners[0]);
    }

    [Fact]
    public void SetDiskSnapshots_StoresBothValues()
    {
        var report = new CleanRunReport(new CleanResult("Total"));
        report.SetDiskBefore(new DiskSpaceSnapshot("C:\\", 1_000_000));
        report.SetDiskAfter(new DiskSpaceSnapshot("C:\\", 2_000_000));

        Assert.Equal(1_000_000, report.DiskBefore!.FreeBytes);
        Assert.Equal(2_000_000, report.DiskAfter!.FreeBytes);
        Assert.Equal("C:\\", report.DiskBefore.DriveName);
    }

    [Fact]
    public void InitialState_TotalIsEmpty()
    {
        var report = new CleanRunReport(new CleanResult("Total"));
        Assert.Empty(report.Results);
        Assert.Empty(report.SkippedCleaners);
        Assert.Equal(0, report.Total.FilesDeleted);
        Assert.Null(report.DiskBefore);
        Assert.Null(report.DiskAfter);
    }
}

public class CleanerRunnerTests
{
    [Fact]
    public void Run_ConfirmedCleaner_AddsResultToReport()
    {
        var cleaner = new TestCleaner(result =>
        {
            result.AddDeletedFile(256);
            result.AddDeletedDirectory();
        });
        CleanerRunner runner = new();

        CleanRunReport report = runner.Run(new[] { cleaner }, new CleanOptions(previewOnly: true, TimeSpan.Zero, includeRecentFiles: true));

        Assert.Single(report.Results);
        Assert.Equal(1, cleaner.RunCount);
        Assert.Equal(1, report.Total.FilesDeleted);
        Assert.Equal(1, report.Total.DirectoriesDeleted);
        Assert.Equal(256, report.Total.BytesFreed);
    }

    [Fact]
    public void Run_WhenConfirmationReturnsFalse_SkipsCleanerWithoutRunning()
    {
        var cleaner = new TestCleaner(_ => { });
        List<string> skipped = new();
        CleanerRunner runner = new();
        CleanerRunCallbacks callbacks = new()
        {
            ConfirmCleaner = _ => false,
            Skipped = (skippedCleaner, reason) => skipped.Add($"{skippedCleaner.Name}:{reason}")
        };

        CleanRunReport report = runner.Run(new[] { cleaner }, new CleanOptions(previewOnly: true, TimeSpan.Zero, includeRecentFiles: true), callbacks);

        Assert.Equal(0, cleaner.RunCount);
        Assert.Empty(report.Results);
        Assert.Single(report.SkippedCleaners);
        Assert.Single(skipped);
    }

    [Fact]
    public void Run_WarningProvider_EmitsWarningsBeforeCleanerRuns()
    {
        var cleaner = new WarningTestCleaner();
        List<string> events = new();
        CleanerRunner runner = new();
        CleanerRunCallbacks callbacks = new()
        {
            Warning = warning => events.Add($"warning:{warning}"),
            Starting = startingCleaner => events.Add($"start:{startingCleaner.Name}")
        };

        runner.Run(new[] { cleaner }, new CleanOptions(previewOnly: true, TimeSpan.Zero, includeRecentFiles: true), callbacks);

        Assert.Equal(new[] { "warning:close stuff first", $"start:{cleaner.Name}" }, events);
    }

    private class TestCleaner : ICleaner
    {
        private readonly Action<CleanResult> _mutateResult;

        public TestCleaner(Action<CleanResult> mutateResult)
        {
            _mutateResult = mutateResult;
        }

        public string Name => "Test Cleaner";

        public string Description => "Test cleaner";

        public string Risk => "Test risk";

        public CleanerPlatform Platform => CleanerPlatform.All;

        public bool RequiresAdministrator => false;

        public bool IsRecommended => true;

        public int RunCount { get; private set; }

        public CleanResult Clean(CleanOptions options)
        {
            RunCount++;
            CleanResult result = new(Name, options.PreviewOnly);
            _mutateResult(result);
            return result;
        }
    }

    private sealed class WarningTestCleaner : TestCleaner, ICleanerWarningProvider
    {
        public WarningTestCleaner()
            : base(_ => { })
        {
        }

        public IReadOnlyList<string> GetWarnings(CleanOptions options)
        {
            return new[] { "close stuff first" };
        }
    }
}
