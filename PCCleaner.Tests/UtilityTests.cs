using PCCleaner.Utilities;
using Xunit;

namespace PCCleaner.Tests;

public class FormatBytesTests
{
    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(1, "1 B")]
    [InlineData(512, "512 B")]
    [InlineData(1023, "1023 B")]
    [InlineData(1024, "1 KB")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(10240, "10 KB")]
    [InlineData(1048576, "1 MB")]
    [InlineData(1572864, "1.5 MB")]
    [InlineData(1073741824, "1 GB")]
    [InlineData(1099511627776L, "1 TB")]
    public void FormatBytes_ReturnsExpectedString(long bytes, string expected)
    {
        Assert.Equal(expected, SystemInfo.FormatBytes(bytes));
    }

    [Fact]
    public void FormatBytes_LargeValue_UsesTB()
    {
        string result = SystemInfo.FormatBytes(2L * 1024 * 1024 * 1024 * 1024);
        Assert.Contains("TB", result);
    }
}

public class LocalizerTests
{
    [Fact]
    public void T_KnownKey_ReturnsTranslation()
    {
        Localizer.SetLanguage(AppLanguage.English);
        Assert.Equal("CLEAN", Localizer.T("prompt.typeCleanWord"));
    }

    [Fact]
    public void T_UnknownKey_ReturnsKeyItself()
    {
        Localizer.SetLanguage(AppLanguage.English);
        string key = "this.key.absolutely.does.not.exist";
        Assert.Equal(key, Localizer.T(key));
    }

    [Fact]
    public void T_WithArgs_FormatsPlaceholders()
    {
        Localizer.SetLanguage(AppLanguage.English);
        string result = Localizer.T("status.unsupportedPlatform", "Windows", "Linux");
        Assert.Contains("Windows", result);
        Assert.Contains("Linux", result);
    }

    [Fact]
    public void T_WithMultipleArgs_FormatsAll()
    {
        Localizer.SetLanguage(AppLanguage.English);
        string result = Localizer.T("result.message", "MyCleaner", "deleted", 42, 7, 3, "1.5 MB", 0);
        Assert.Contains("MyCleaner", result);
        Assert.Contains("42", result);
        Assert.Contains("7", result);
        Assert.Contains("1.5 MB", result);
    }

    [Fact]
    public void SetLanguage_Czech_ReturnsCzechStrings()
    {
        AppLanguage original = Localizer.CurrentLanguage;
        try
        {
            Localizer.SetLanguage(AppLanguage.Czech);
            Assert.Equal("CISTIT", Localizer.T("prompt.typeCleanWord"));
        }
        finally
        {
            Localizer.SetLanguage(original);
        }
    }

    [Fact]
    public void SetLanguage_English_ReturnsEnglishStrings()
    {
        AppLanguage original = Localizer.CurrentLanguage;
        try
        {
            Localizer.SetLanguage(AppLanguage.English);
            Assert.Equal("CLEAN", Localizer.T("prompt.typeCleanWord"));
        }
        finally
        {
            Localizer.SetLanguage(original);
        }
    }

    [Fact]
    public void T_MissingKey_DoesNotThrow()
    {
        Localizer.SetLanguage(AppLanguage.English);
        string result = Localizer.T("totally.missing.key", "arg1", "arg2");
        Assert.NotNull(result);
    }

    [Fact]
    public void AdminCancelledKey_ExistsInBothLanguages()
    {
        Localizer.SetLanguage(AppLanguage.English);
        string en = Localizer.T("admin.cancelled");
        Assert.NotEqual("admin.cancelled", en);

        AppLanguage original = Localizer.CurrentLanguage;
        try
        {
            Localizer.SetLanguage(AppLanguage.Czech);
            string cs = Localizer.T("admin.cancelled");
            Assert.NotEqual("admin.cancelled", cs);
        }
        finally
        {
            Localizer.SetLanguage(original);
        }
    }

    [Fact]
    public void UnixUserCacheKeys_ExistInBothLanguages()
    {
        Localizer.SetLanguage(AppLanguage.English);
        Assert.NotEqual("cleaner.unixUserCache.name", Localizer.T("cleaner.unixUserCache.name"));

        AppLanguage original = Localizer.CurrentLanguage;
        try
        {
            Localizer.SetLanguage(AppLanguage.Czech);
            Assert.NotEqual("cleaner.unixUserCache.name", Localizer.T("cleaner.unixUserCache.name"));
        }
        finally
        {
            Localizer.SetLanguage(original);
        }
    }

    [Fact]
    public void MacUserLogsKeys_ExistInBothLanguages()
    {
        Localizer.SetLanguage(AppLanguage.English);
        Assert.NotEqual("cleaner.macUserLogs.name", Localizer.T("cleaner.macUserLogs.name"));

        AppLanguage original = Localizer.CurrentLanguage;
        try
        {
            Localizer.SetLanguage(AppLanguage.Czech);
            Assert.NotEqual("cleaner.macUserLogs.name", Localizer.T("cleaner.macUserLogs.name"));
        }
        finally
        {
            Localizer.SetLanguage(original);
        }
    }
}

// ── SystemInfo — drive snapshot ───────────────────────────────────────────────

public class DiskSnapshotTests
{
    [Fact]
    public void GetCurrentDriveSnapshot_ReturnsNonNullWithPositiveFreeSpace()
    {
        var snapshot = SystemInfo.GetCurrentDriveSnapshot();
        Assert.NotNull(snapshot);
        Assert.True(snapshot!.FreeBytes > 0, "Drive must have some free space");
        Assert.False(string.IsNullOrWhiteSpace(snapshot.DriveName));
    }
}
