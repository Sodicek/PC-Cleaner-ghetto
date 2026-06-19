using PCCleaner.Cleaners;
using PCCleaner.Core;
using PCCleaner.Utilities;
using Xunit;

namespace PCCleanerTests;

public class DirectoryCleanerTests : IDisposable
{
    private readonly string _dir;

    public DirectoryCleanerTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), $"PCTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    public void Clean_Preview_DoesNotDeleteFiles()
    {
        string file = MakeOldFile("test.txt");
        var cleaner = MakeCleaner(_dir);

        var result = cleaner.Clean(Preview());

        Assert.True(File.Exists(file));
        Assert.Equal(1, result.FilesDeleted);
    }

    [Fact]
    public void Clean_Real_DeletesOldFiles()
    {
        string file = MakeOldFile("delete-me.txt");
        var cleaner = MakeCleaner(_dir);

        var result = cleaner.Clean(Real());

        Assert.False(File.Exists(file));
        Assert.Equal(1, result.FilesDeleted);
        Assert.Equal(0, result.Failures);
    }

    [Fact]
    public void Clean_MultipleDirectories_CleansAll()
    {
        string dir2 = Path.Combine(Path.GetTempPath(), $"PCTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir2);

        try
        {
            string f1 = MakeOldFile("file1.txt");
            string f2 = Path.Combine(dir2, "file2.txt");
            File.WriteAllText(f2, "x");
            File.SetLastWriteTimeUtc(f2, DateTime.UtcNow.AddHours(-48));

            var cleaner = MakeCleaner(_dir, dir2);
            var result = cleaner.Clean(Real());

            Assert.Equal(2, result.FilesDeleted);
        }
        finally
        {
            if (Directory.Exists(dir2))
                Directory.Delete(dir2, recursive: true);
        }
    }

    [Fact]
    public void Clean_AgeFilter_SkipsRecentFiles()
    {
        string old = MakeOldFile("old.txt");
        string recent = MakeFile("recent.txt");
        File.SetLastWriteTimeUtc(recent, DateTime.UtcNow.AddMinutes(-10));

        var cleaner = MakeCleaner(_dir);
        var result = cleaner.Clean(Real());

        Assert.False(File.Exists(old));
        Assert.True(File.Exists(recent));
        Assert.Equal(1, result.FilesDeleted);
        Assert.Equal(1, result.FilesSkipped);
    }

    [Fact]
    public void Clean_Properties_ExposeCorrectMetadata()
    {
        Localizer.SetLanguage(AppLanguage.English);
        var cleaner = new DirectoryCleaner(
            "app.title",
            "app.subtitle",
            "status.done",
            new[] { _dir },
            platform: CleanerPlatform.All,
            requiresAdministrator: false,
            isRecommended: true);

        Assert.Equal("PC Cleaner", cleaner.Name);
        Assert.Equal(CleanerPlatform.All, cleaner.Platform);
        Assert.False(cleaner.RequiresAdministrator);
        Assert.True(cleaner.IsRecommended);
        Assert.NotEmpty(cleaner.Description);
        Assert.NotEmpty(cleaner.Risk);
    }

    [Fact]
    public void Clean_MissingDirectory_ReturnsEmptyWithoutCrashing()
    {
        string missing = Path.Combine(_dir, "nope");
        var cleaner = MakeCleaner(missing);

        var result = cleaner.Clean(Real());

        Assert.Equal(0, result.FilesDeleted);
        Assert.Equal(0, result.Failures);
    }

    // --- Helpers ---

    private CleanOptions Real() =>
        new(previewOnly: false, TimeSpan.FromHours(24), includeRecentFiles: false);

    private CleanOptions Preview() =>
        new(previewOnly: true, TimeSpan.FromHours(24), includeRecentFiles: false);

    private DirectoryCleaner MakeCleaner(params string[] dirs) =>
        new("app.title", "app.subtitle", "status.done", dirs);

    private string MakeFile(string name)
    {
        string path = Path.Combine(_dir, name);
        File.WriteAllText(path, "content");
        return path;
    }

    private string MakeOldFile(string name)
    {
        string path = MakeFile(name);
        File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddHours(-48));
        return path;
    }
}

public class SystemInfoTests
{
    [Fact]
    public void IsSupported_All_ReturnsTrue()
    {
        Assert.True(SystemInfo.IsSupported(CleanerPlatform.All));
    }

    [Fact]
    public void IsSupported_Windows_MatchesCurrentOS()
    {
        bool expected = OperatingSystem.IsWindows();
        Assert.Equal(expected, SystemInfo.IsSupported(CleanerPlatform.Windows));
    }

    [Fact]
    public void PlatformName_All_ReturnsNonEmptyString()
    {
        Localizer.SetLanguage(AppLanguage.English);
        string name = SystemInfo.PlatformName(CleanerPlatform.All);
        Assert.NotEmpty(name);
        Assert.NotEqual("platform.all", name);
    }

    [Fact]
    public void PlatformName_Windows_ReturnsWindows()
    {
        Localizer.SetLanguage(AppLanguage.English);
        Assert.Equal("Windows", SystemInfo.PlatformName(CleanerPlatform.Windows));
    }

    [Fact]
    public void LocalAuthor_IsNotEmpty()
    {
        Assert.False(string.IsNullOrWhiteSpace(SystemInfo.LocalAuthor));
    }

    [Fact]
    public void MachineName_IsNotEmpty()
    {
        Assert.False(string.IsNullOrWhiteSpace(SystemInfo.MachineName));
    }

    [Fact]
    public void LoggedInUser_IsNotEmpty()
    {
        Assert.False(string.IsNullOrWhiteSpace(SystemInfo.LoggedInUser));
    }

    [Fact]
    public void GetCurrentDriveSnapshot_ReturnsNonNullOnRunningSystem()
    {
        var snapshot = SystemInfo.GetCurrentDriveSnapshot();
        Assert.NotNull(snapshot);
        Assert.True(snapshot!.FreeBytes > 0);
        Assert.False(string.IsNullOrWhiteSpace(snapshot.DriveName));
    }
}
