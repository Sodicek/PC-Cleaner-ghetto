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
    public void IsSupported_Linux_MatchesCurrentOS()
    {
        bool expected = OperatingSystem.IsLinux();
        Assert.Equal(expected, SystemInfo.IsSupported(CleanerPlatform.Linux));
    }

    [Fact]
    public void IsSupported_MacOS_MatchesCurrentOS()
    {
        bool expected = OperatingSystem.IsMacOS();
        Assert.Equal(expected, SystemInfo.IsSupported(CleanerPlatform.MacOS));
    }

    [Fact]
    public void IsSupported_UnixLike_MatchesCurrentOS()
    {
        bool expected = OperatingSystem.IsLinux() || OperatingSystem.IsMacOS();
        Assert.Equal(expected, SystemInfo.IsSupported(CleanerPlatform.UnixLike));
    }

    [Fact]
    public void IsSupported_SimulatedWindows_SupportsWindowsAndAllOnly()
    {
        Assert.True(SystemInfo.IsSupported(CleanerPlatform.All, isWindows: true, isLinux: false, isMacOS: false));
        Assert.True(SystemInfo.IsSupported(CleanerPlatform.Windows, isWindows: true, isLinux: false, isMacOS: false));
        Assert.False(SystemInfo.IsSupported(CleanerPlatform.Linux, isWindows: true, isLinux: false, isMacOS: false));
        Assert.False(SystemInfo.IsSupported(CleanerPlatform.MacOS, isWindows: true, isLinux: false, isMacOS: false));
        Assert.False(SystemInfo.IsSupported(CleanerPlatform.UnixLike, isWindows: true, isLinux: false, isMacOS: false));
    }

    [Fact]
    public void IsSupported_SimulatedLinux_SupportsLinuxUnixLikeAndAll()
    {
        Assert.True(SystemInfo.IsSupported(CleanerPlatform.All, isWindows: false, isLinux: true, isMacOS: false));
        Assert.False(SystemInfo.IsSupported(CleanerPlatform.Windows, isWindows: false, isLinux: true, isMacOS: false));
        Assert.True(SystemInfo.IsSupported(CleanerPlatform.Linux, isWindows: false, isLinux: true, isMacOS: false));
        Assert.False(SystemInfo.IsSupported(CleanerPlatform.MacOS, isWindows: false, isLinux: true, isMacOS: false));
        Assert.True(SystemInfo.IsSupported(CleanerPlatform.UnixLike, isWindows: false, isLinux: true, isMacOS: false));
    }

    [Fact]
    public void IsSupported_SimulatedMacOS_SupportsMacOSUnixLikeAndAll()
    {
        Assert.True(SystemInfo.IsSupported(CleanerPlatform.All, isWindows: false, isLinux: false, isMacOS: true));
        Assert.False(SystemInfo.IsSupported(CleanerPlatform.Windows, isWindows: false, isLinux: false, isMacOS: true));
        Assert.False(SystemInfo.IsSupported(CleanerPlatform.Linux, isWindows: false, isLinux: false, isMacOS: true));
        Assert.True(SystemInfo.IsSupported(CleanerPlatform.MacOS, isWindows: false, isLinux: false, isMacOS: true));
        Assert.True(SystemInfo.IsSupported(CleanerPlatform.UnixLike, isWindows: false, isLinux: false, isMacOS: true));
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
    public void PlatformName_CrossPlatformValues_ReturnExpectedNames()
    {
        Localizer.SetLanguage(AppLanguage.English);
        Assert.Equal("Linux", SystemInfo.PlatformName(CleanerPlatform.Linux));
        Assert.Equal("macOS", SystemInfo.PlatformName(CleanerPlatform.MacOS));
        Assert.Equal("Linux/macOS", SystemInfo.PlatformName(CleanerPlatform.UnixLike));
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

    [Fact]
    public void GetUnixUserCachePath_SimulatedLinux_UsesXdgCacheHome()
    {
        string path = SystemInfo.GetUnixUserCachePath("/home/tester", "/custom/cache", isMacOS: false);

        Assert.Equal("/custom/cache", path);
    }

    [Fact]
    public void GetUnixUserCachePath_SimulatedLinux_FallsBackToDotCache()
    {
        string path = SystemInfo.GetUnixUserCachePath("/home/tester", xdgCacheHome: null, isMacOS: false);

        Assert.Equal(Path.Combine("/home/tester", ".cache"), path);
    }

    [Fact]
    public void GetUnixUserCachePath_SimulatedMacOS_UsesLibraryCaches()
    {
        string path = SystemInfo.GetUnixUserCachePath("/Users/tester", "/ignored/cache", isMacOS: true);

        Assert.Equal(Path.Combine("/Users/tester", "Library", "Caches"), path);
    }

    [Fact]
    public void GetSafeUnixCachePaths_CurrentPlatform_DoesNotReturnWholeCacheRoot()
    {
        IReadOnlyList<string> paths = SystemInfo.GetSafeUnixCachePaths();
        string cacheRoot = SystemInfo.GetUnixUserCachePath();

        Assert.DoesNotContain(paths, path => string.Equals(path, cacheRoot, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GetMacUserLogsPath_ReturnsLibraryLogsWhenHomeExists()
    {
        string path = SystemInfo.GetMacUserLogsPath();

        Assert.True(string.IsNullOrWhiteSpace(path) || path.EndsWith(Path.Combine("Library", "Logs"), StringComparison.Ordinal));
    }
}

public class CleanerCatalogTests
{
    [Fact]
    public void CreateCleanersForPlatform_SimulatedWindows_ReturnsOnlyWindowsCompatibleCleaners()
    {
        IReadOnlyList<ICleaner> cleaners = CleanerCatalog.CreateCleanersForPlatform(isWindows: true, isLinux: false, isMacOS: false);

        Assert.NotEmpty(cleaners);
        Assert.Contains(cleaners, cleaner => cleaner.Platform == CleanerPlatform.Windows);
        Assert.DoesNotContain(cleaners, cleaner => cleaner.Platform is CleanerPlatform.Linux or CleanerPlatform.MacOS or CleanerPlatform.UnixLike);
        Assert.All(cleaners, cleaner => Assert.True(SystemInfo.IsSupported(cleaner.Platform, isWindows: true, isLinux: false, isMacOS: false)));
    }

    [Fact]
    public void CreateCleanersForPlatform_SimulatedLinux_ReturnsOnlyLinuxCompatibleCleaners()
    {
        IReadOnlyList<ICleaner> cleaners = CleanerCatalog.CreateCleanersForPlatform(isWindows: false, isLinux: true, isMacOS: false);

        Assert.NotEmpty(cleaners);
        Assert.Contains(cleaners, cleaner => cleaner.Platform == CleanerPlatform.UnixLike);
        Assert.DoesNotContain(cleaners, cleaner => cleaner.Platform is CleanerPlatform.Windows or CleanerPlatform.MacOS);
        Assert.All(cleaners, cleaner => Assert.True(SystemInfo.IsSupported(cleaner.Platform, isWindows: false, isLinux: true, isMacOS: false)));
    }

    [Fact]
    public void CreateCleanersForPlatform_SimulatedMacOS_ReturnsOnlyMacCompatibleCleaners()
    {
        IReadOnlyList<ICleaner> cleaners = CleanerCatalog.CreateCleanersForPlatform(isWindows: false, isLinux: false, isMacOS: true);

        Assert.NotEmpty(cleaners);
        Assert.Contains(cleaners, cleaner => cleaner.Platform == CleanerPlatform.UnixLike);
        Assert.Contains(cleaners, cleaner => cleaner.Platform == CleanerPlatform.MacOS);
        Assert.DoesNotContain(cleaners, cleaner => cleaner.Platform is CleanerPlatform.Windows or CleanerPlatform.Linux);
        Assert.All(cleaners, cleaner => Assert.True(SystemInfo.IsSupported(cleaner.Platform, isWindows: false, isLinux: false, isMacOS: true)));
    }
}
