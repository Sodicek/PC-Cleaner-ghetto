using PCCleaner.Utilities;
using Xunit;

namespace PCCleanerTests;

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
        Assert.Equal("PC Cleaner", Localizer.T("app.title"));
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
        string result = Localizer.T("system.detected", "Windows 11");
        Assert.Contains("Windows 11", result);
    }

    [Fact]
    public void T_WithMultipleArgs_FormatsAll()
    {
        Localizer.SetLanguage(AppLanguage.English);
        string result = Localizer.T("console.tooSmallSize", 40, 10, 50, 18);
        Assert.Contains("40", result);
        Assert.Contains("10", result);
        Assert.Contains("50", result);
        Assert.Contains("18", result);
    }

    [Fact]
    public void SetLanguage_Czech_ReturnsCzechStrings()
    {
        AppLanguage original = Localizer.CurrentLanguage;
        try
        {
            Localizer.SetLanguage(AppLanguage.Czech);
            Assert.Equal("Hotovo.", Localizer.T("status.done"));
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
            Assert.Equal("Done.", Localizer.T("status.done"));
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
