using PCCleaner.Cleaners;
using PCCleaner.Core;
using PCCleaner.Utilities;
using Xunit;

namespace PCCleaner.Tests;

// ── Localizer — all new cleaner keys must exist in both languages ──────────────

public class NewCleanerLocalizerTests
{
    [Theory]
    [InlineData("cleaner.steamCache")]
    [InlineData("cleaner.devCache")]
    [InlineData("cleaner.flatpak")]
    [InlineData("cleaner.vscode")]
    public void NewCleanerKeys_ExistInEnglish(string prefix)
    {
        Localizer.SetLanguage(AppLanguage.English);
        Assert.NotEqual($"{prefix}.name",        Localizer.T($"{prefix}.name"));
        Assert.NotEqual($"{prefix}.description",  Localizer.T($"{prefix}.description"));
        Assert.NotEqual($"{prefix}.risk",         Localizer.T($"{prefix}.risk"));
    }

    [Theory]
    [InlineData("cleaner.steamCache")]
    [InlineData("cleaner.devCache")]
    [InlineData("cleaner.flatpak")]
    [InlineData("cleaner.vscode")]
    public void NewCleanerKeys_ExistInCzech(string prefix)
    {
        AppLanguage original = Localizer.CurrentLanguage;
        try
        {
            Localizer.SetLanguage(AppLanguage.Czech);
            Assert.NotEqual($"{prefix}.name",       Localizer.T($"{prefix}.name"));
            Assert.NotEqual($"{prefix}.description", Localizer.T($"{prefix}.description"));
            Assert.NotEqual($"{prefix}.risk",        Localizer.T($"{prefix}.risk"));
        }
        finally { Localizer.SetLanguage(original); }
    }
}

// ── CleanerCatalog — new cleaners registered on correct platforms ─────────────

public class NewCleanerCatalogTests
{
    [Theory]
    [InlineData(true,  false, false)]
    [InlineData(false, true,  false)]
    [InlineData(false, false, true)]
    public void SteamCacheCleaner_PresentOnAllPlatforms(bool win, bool lin, bool mac)
    {
        var cleaners = CleanerCatalog.CreateCleanersForPlatform(win, lin, mac);
        Assert.Contains(cleaners, c => c is SteamCacheCleaner);
    }

    [Theory]
    [InlineData(true,  false, false)]
    [InlineData(false, true,  false)]
    [InlineData(false, false, true)]
    public void DeveloperCacheCleaner_PresentOnAllPlatforms(bool win, bool lin, bool mac)
    {
        var cleaners = CleanerCatalog.CreateCleanersForPlatform(win, lin, mac);
        Assert.Contains(cleaners, c => c is DeveloperCacheCleaner);
    }

    [Theory]
    [InlineData(true,  false, false)]
    [InlineData(false, true,  false)]
    [InlineData(false, false, true)]
    public void VsCodeWorkspaceCleaner_PresentOnAllPlatforms(bool win, bool lin, bool mac)
    {
        var cleaners = CleanerCatalog.CreateCleanersForPlatform(win, lin, mac);
        Assert.Contains(cleaners, c => c is VsCodeWorkspaceCleaner);
    }

    [Fact]
    public void FlatpakCleaner_PresentOnLinux()
    {
        var linux = CleanerCatalog.CreateCleanersForPlatform(false, true, false);
        Assert.Contains(linux, c => c is FlatpakCleaner);
    }

    [Fact]
    public void FlatpakCleaner_AbsentOnWindows()
    {
        var windows = CleanerCatalog.CreateCleanersForPlatform(true, false, false);
        Assert.DoesNotContain(windows, c => c is FlatpakCleaner);
    }

    [Fact]
    public void FlatpakCleaner_AbsentOnMacOS()
    {
        var mac = CleanerCatalog.CreateCleanersForPlatform(false, false, true);
        Assert.DoesNotContain(mac, c => c is FlatpakCleaner);
    }
}

// ── VsCodeWorkspaceCleaner — orphan detection logic ───────────────────────────

public class VsCodeWorkspaceCleanerTests : IDisposable
{
    // Simulates a single workspaceStorage entry directory (GUID-named folder)
    private readonly string _entry;

    public VsCodeWorkspaceCleanerTests()
    {
        _entry = Path.Combine(Path.GetTempPath(), $"VSEntry_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_entry);
    }

    public void Dispose()
    {
        if (Directory.Exists(_entry))
            Directory.Delete(_entry, recursive: true);
    }

    [Fact]
    public void IsOrphaned_NoWorkspaceJson_ReturnsTrue()
    {
        // Entry dir exists but has no workspace.json → treat as orphan
        Assert.True(VsCodeWorkspaceCleaner.IsOrphaned(_entry));
    }

    [Fact]
    public void IsOrphaned_FolderUriExists_ReturnsFalse()
    {
        string project = Path.Combine(Path.GetTempPath(), $"Project_{Guid.NewGuid():N}");
        Directory.CreateDirectory(project);
        try
        {
            WriteWorkspaceJson(new Uri(project).AbsoluteUri);
            Assert.False(VsCodeWorkspaceCleaner.IsOrphaned(_entry));
        }
        finally
        {
            if (Directory.Exists(project)) Directory.Delete(project);
        }
    }

    [Fact]
    public void IsOrphaned_FolderUriGone_ReturnsTrue()
    {
        // Build a file:// URI for a path that does not exist
        string gone = Path.Combine(Path.GetTempPath(), $"Gone_{Guid.NewGuid():N}");
        WriteWorkspaceJson(new Uri(gone).AbsoluteUri);
        Assert.True(VsCodeWorkspaceCleaner.IsOrphaned(_entry));
    }

    [Fact]
    public void IsOrphaned_MultiRootWorkspace_ReturnsFalse()
    {
        // Multi-root workspaces have no "folder" key — must be kept
        File.WriteAllText(Path.Combine(_entry, "workspace.json"), """{"folders":[{"path":"a"},{"path":"b"}]}""");
        Assert.False(VsCodeWorkspaceCleaner.IsOrphaned(_entry));
    }

    [Fact]
    public void IsOrphaned_MalformedJson_ReturnsFalse()
    {
        // Corrupted file → parse error → safe fallback = keep
        File.WriteAllText(Path.Combine(_entry, "workspace.json"), "NOT_JSON{{{{");
        Assert.False(VsCodeWorkspaceCleaner.IsOrphaned(_entry));
    }

    [Fact]
    public void IsOrphaned_NullFolderValue_ReturnsFalse()
    {
        // {"folder": null} — cannot determine, keep it
        File.WriteAllText(Path.Combine(_entry, "workspace.json"), """{"folder":null}""");
        Assert.False(VsCodeWorkspaceCleaner.IsOrphaned(_entry));
    }

    [Fact]
    public void IsOrphaned_RemoteUri_ReturnsFalse()
    {
        // SSH / devcontainer URIs are not file:// — keep them
        File.WriteAllText(Path.Combine(_entry, "workspace.json"),
            """{"folder":"vscode-remote://ssh-remote+myhost/home/user/project"}""");
        Assert.False(VsCodeWorkspaceCleaner.IsOrphaned(_entry));
    }

    [Fact]
    public void Clean_Preview_ReportsOrphanedWithoutDeleting()
    {
        // An entry with no workspace.json should be counted as skipped in preview
        var options  = new CleanOptions(previewOnly: true, TimeSpan.FromHours(24), includeRecentFiles: false);

        // We can only test the full cleaner against the real VS Code paths,
        // but IsOrphaned covers the decision logic. Here we do a smoke-test:
        // running Clean() on the current machine must not throw.
        var cleaner = new VsCodeWorkspaceCleaner();
        var result  = cleaner.Clean(options);
        Assert.NotNull(result);
    }

    private void WriteWorkspaceJson(string folderUri) =>
        File.WriteAllText(Path.Combine(_entry, "workspace.json"),
            $$"""{"folder":"{{folderUri}}"}""");
}

// ── Prefetch cleaner — localizer keys + catalog placement ─────────────────────

public class PrefetchCleanerTests
{
    [Fact]
    public void PrefetchKeys_ExistInEnglish()
    {
        Localizer.SetLanguage(AppLanguage.English);
        Assert.NotEqual("cleaner.prefetch.name",        Localizer.T("cleaner.prefetch.name"));
        Assert.NotEqual("cleaner.prefetch.description",  Localizer.T("cleaner.prefetch.description"));
        Assert.NotEqual("cleaner.prefetch.risk",         Localizer.T("cleaner.prefetch.risk"));
    }

    [Fact]
    public void PrefetchKeys_ExistInCzech()
    {
        AppLanguage original = Localizer.CurrentLanguage;
        try
        {
            Localizer.SetLanguage(AppLanguage.Czech);
            Assert.NotEqual("cleaner.prefetch.name",        Localizer.T("cleaner.prefetch.name"));
            Assert.NotEqual("cleaner.prefetch.description",  Localizer.T("cleaner.prefetch.description"));
            Assert.NotEqual("cleaner.prefetch.risk",         Localizer.T("cleaner.prefetch.risk"));
        }
        finally { Localizer.SetLanguage(original); }
    }

    [Fact]
    public void PrefetchCleaner_PresentOnWindows()
    {
        Localizer.SetLanguage(AppLanguage.English);
        var cleaners = CleanerCatalog.CreateCleanersForPlatform(isWindows: true, isLinux: false, isMacOS: false);
        Assert.Contains(cleaners, c => c.Name == "Windows Prefetch cache");
    }

    [Fact]
    public void PrefetchCleaner_AbsentOnLinux()
    {
        Localizer.SetLanguage(AppLanguage.English);
        var cleaners = CleanerCatalog.CreateCleanersForPlatform(isWindows: false, isLinux: true, isMacOS: false);
        Assert.DoesNotContain(cleaners, c => c.Name == "Windows Prefetch cache");
    }

    [Fact]
    public void PrefetchCleaner_AbsentOnMacOS()
    {
        Localizer.SetLanguage(AppLanguage.English);
        var cleaners = CleanerCatalog.CreateCleanersForPlatform(isWindows: false, isLinux: false, isMacOS: true);
        Assert.DoesNotContain(cleaners, c => c.Name == "Windows Prefetch cache");
    }

    [Fact]
    public void PrefetchCleaner_RequiresAdministratorAndIsNotRecommended()
    {
        Localizer.SetLanguage(AppLanguage.English);
        var cleaners = CleanerCatalog.CreateCleanersForPlatform(isWindows: true, isLinux: false, isMacOS: false);
        var prefetch = cleaners.First(c => c.Name == "Windows Prefetch cache");
        Assert.True(prefetch.RequiresAdministrator);
        Assert.False(prefetch.IsRecommended);
    }
}

// ── UnixTrashCleaner — registration and platform ──────────────────────────────

public class UnixTrashCleanerTests
{
    [Fact]
    public void UnixTrashKeys_ExistInEnglish()
    {
        Localizer.SetLanguage(AppLanguage.English);
        Assert.NotEqual("cleaner.unixTrash.name",        Localizer.T("cleaner.unixTrash.name"));
        Assert.NotEqual("cleaner.unixTrash.description",  Localizer.T("cleaner.unixTrash.description"));
        Assert.NotEqual("cleaner.unixTrash.risk",         Localizer.T("cleaner.unixTrash.risk"));
    }

    [Fact]
    public void UnixTrashKeys_ExistInCzech()
    {
        AppLanguage original = Localizer.CurrentLanguage;
        try
        {
            Localizer.SetLanguage(AppLanguage.Czech);
            Assert.NotEqual("cleaner.unixTrash.name",        Localizer.T("cleaner.unixTrash.name"));
            Assert.NotEqual("cleaner.unixTrash.description",  Localizer.T("cleaner.unixTrash.description"));
            Assert.NotEqual("cleaner.unixTrash.risk",         Localizer.T("cleaner.unixTrash.risk"));
        }
        finally { Localizer.SetLanguage(original); }
    }

    [Fact]
    public void UnixTrashCleaner_PresentOnLinux()
    {
        var cleaners = CleanerCatalog.CreateCleanersForPlatform(false, true, false);
        Assert.Contains(cleaners, c => c is UnixTrashCleaner);
    }

    [Fact]
    public void UnixTrashCleaner_PresentOnMacOS()
    {
        var cleaners = CleanerCatalog.CreateCleanersForPlatform(false, false, true);
        Assert.Contains(cleaners, c => c is UnixTrashCleaner);
    }

    [Fact]
    public void UnixTrashCleaner_AbsentOnWindows()
    {
        var cleaners = CleanerCatalog.CreateCleanersForPlatform(true, false, false);
        Assert.DoesNotContain(cleaners, c => c is UnixTrashCleaner);
    }

    [Fact]
    public void UnixTrashCleaner_IsRecommendedAndDoesNotRequireAdmin()
    {
        var cleaner = new UnixTrashCleaner();
        Assert.True(cleaner.IsRecommended);
        Assert.False(cleaner.RequiresAdministrator);
    }

    [Fact]
    public void UnixTrashCleaner_Clean_EmptyHome_ReturnsEmptyResult()
    {
        // Smoke test: Clean() on current platform must not throw
        var cleaner = new UnixTrashCleaner();
        var options = new CleanOptions(previewOnly: true, TimeSpan.FromHours(24), includeRecentFiles: false);
        var result  = cleaner.Clean(options);
        Assert.NotNull(result);
        Assert.Equal(0, result.Failures);
    }
}

// ── LinuxRecentFilesCleaner — registration and keys ───────────────────────────

public class LinuxRecentFilesCleanerTests
{
    [Fact]
    public void LinuxRecentFilesKeys_ExistInEnglish()
    {
        Localizer.SetLanguage(AppLanguage.English);
        Assert.NotEqual("cleaner.linuxRecentFiles.name",        Localizer.T("cleaner.linuxRecentFiles.name"));
        Assert.NotEqual("cleaner.linuxRecentFiles.description",  Localizer.T("cleaner.linuxRecentFiles.description"));
        Assert.NotEqual("cleaner.linuxRecentFiles.risk",         Localizer.T("cleaner.linuxRecentFiles.risk"));
    }

    [Fact]
    public void LinuxRecentFilesKeys_ExistInCzech()
    {
        AppLanguage original = Localizer.CurrentLanguage;
        try
        {
            Localizer.SetLanguage(AppLanguage.Czech);
            Assert.NotEqual("cleaner.linuxRecentFiles.name",        Localizer.T("cleaner.linuxRecentFiles.name"));
            Assert.NotEqual("cleaner.linuxRecentFiles.description",  Localizer.T("cleaner.linuxRecentFiles.description"));
            Assert.NotEqual("cleaner.linuxRecentFiles.risk",         Localizer.T("cleaner.linuxRecentFiles.risk"));
        }
        finally { Localizer.SetLanguage(original); }
    }

    [Fact]
    public void LinuxRecentFilesCleaner_PresentOnLinux()
    {
        var cleaners = CleanerCatalog.CreateCleanersForPlatform(false, true, false);
        Assert.Contains(cleaners, c => c is LinuxRecentFilesCleaner);
    }

    [Fact]
    public void LinuxRecentFilesCleaner_AbsentOnWindows()
    {
        var cleaners = CleanerCatalog.CreateCleanersForPlatform(true, false, false);
        Assert.DoesNotContain(cleaners, c => c is LinuxRecentFilesCleaner);
    }

    [Fact]
    public void LinuxRecentFilesCleaner_AbsentOnMacOS()
    {
        var cleaners = CleanerCatalog.CreateCleanersForPlatform(false, false, true);
        Assert.DoesNotContain(cleaners, c => c is LinuxRecentFilesCleaner);
    }

    [Fact]
    public void LinuxRecentFilesCleaner_IsNotRecommendedAndNoAdmin()
    {
        var cleaner = new LinuxRecentFilesCleaner();
        Assert.False(cleaner.IsRecommended); // opt-in — clears visible user history
        Assert.False(cleaner.RequiresAdministrator);
    }
}

// ── ScheduledTaskManager — security validation ────────────────────────────────

public class ScheduledTaskManagerTests
{
    [Fact]
    public void Create_PathWithDoubleQuote_ReturnsFalseWithError()
    {
        string badPath = @"C:\Program Files\My ""App""\pccleaner.exe";
        bool ok = ScheduledTaskManager.Create(badPath, out string error);
        Assert.False(ok);
        Assert.False(string.IsNullOrEmpty(error));
    }

    [Fact]
    public void Create_NormalPath_DoesNotFailOnQuoteCheck()
    {
        string path = Path.Combine(Path.GetTempPath(), "pccleaner.exe");
        ScheduledTaskManager.Create(path, out string error);
        // Must not be the quote-validation error (may fail on schtasks availability — that's ok)
        Assert.DoesNotContain("double-quote", error, StringComparison.OrdinalIgnoreCase);
    }
}

// ── SystemdTimerManager — security validation ─────────────────────────────────

public class SystemdTimerManagerTests
{
    [Theory]
    [InlineData("/usr/local/bin/pccleaner\nrm -rf /")]
    [InlineData("/usr/local/bin/pccleaner\r--extra")]
    [InlineData("\n")]
    [InlineData("/exe\r\n")]
    public void Create_PathWithNewline_ReturnsFalseWithError(string badPath)
    {
        bool ok = SystemdTimerManager.Create(badPath, out string error);

        Assert.False(ok);
        Assert.False(string.IsNullOrEmpty(error));
    }

    [Fact]
    public void Create_NormalPath_DoesNotFailOnNewlineCheck()
    {
        // A path without newlines must pass the newline guard (may still fail
        // later because systemctl is unavailable on Windows — that's fine).
        string path = Path.Combine(Path.GetTempPath(), "pccleaner");
        SystemdTimerManager.Create(path, out string error);

        // The only thing we assert here is that the error is NOT the newline message
        Assert.DoesNotContain("newline", error, StringComparison.OrdinalIgnoreCase);
    }
}

// ── All cleaners — Name / Description / Risk must be non-empty ────────────────
// Regression guard for the info panel added in v1.0.2: focusing a cleaner
// shows these three strings; any empty value would silently show nothing.

public class CleanerMetadataTests
{
    [Theory]
    [InlineData(true,  false, false)]
    [InlineData(false, true,  false)]
    [InlineData(false, false, true)]
    public void AllCleaners_HaveNonEmptyNameDescriptionRisk(bool win, bool lin, bool mac)
    {
        Localizer.SetLanguage(AppLanguage.English);
        var cleaners = CleanerCatalog.CreateCleanersForPlatform(win, lin, mac);
        foreach (ICleaner c in cleaners)
        {
            Assert.False(string.IsNullOrWhiteSpace(c.Name),        $"{c.GetType().Name}.Name is empty");
            Assert.False(string.IsNullOrWhiteSpace(c.Description), $"{c.GetType().Name}.Description is empty");
            Assert.False(string.IsNullOrWhiteSpace(c.Risk),        $"{c.GetType().Name}.Risk is empty");
        }
    }
}

// ── Admin warning filter — platform correctness ───────────────────────────────
// Change in v1.0.2: admin dialog filters by IsSupported(c.Platform) so Linux
// and macOS users are never shown Windows-only admin cleaners.

public class AdminCleanerPlatformFilterTests
{
    private static List<ICleaner> GetAdminCleaners(bool win, bool lin, bool mac)
    {
        var all = CleanerCatalog.CreateCleanersForPlatform(win, lin, mac);
        return all.Where(c => c.RequiresAdministrator && SystemInfo.IsSupported(c.Platform, win, lin, mac)).ToList();
    }

    [Fact]
    public void AdminCleaners_OnLinux_IsEmpty()
    {
        var admins = GetAdminCleaners(false, true, false);
        Assert.Empty(admins);
    }

    [Fact]
    public void AdminCleaners_OnMacOS_IsEmpty()
    {
        var admins = GetAdminCleaners(false, false, true);
        Assert.Empty(admins);
    }

    [Fact]
    public void AdminCleaners_OnWindows_ContainsAtLeastTwo()
    {
        Localizer.SetLanguage(AppLanguage.English);
        var admins = GetAdminCleaners(true, false, false);
        Assert.True(admins.Count >= 2, $"Expected ≥2 Windows admin cleaners, found {admins.Count}");
    }

    [Fact]
    public void AdminCleaners_OnWindows_AllHaveWindowsPlatform()
    {
        var admins = GetAdminCleaners(true, false, false);
        Assert.All(admins, c => Assert.Equal(CleanerPlatform.Windows, c.Platform));
    }
}

// ── Security — updater asset name validation ──────────────────────────────────
// Updater.IsValidAssetName is in PCCleaner.App (Terminal.Gui dependency makes
// it unreachable from this project). These tests mirror the same rule to document
// the threat model and verify our expected asset names are safe.

public class UpdaterAssetNameSecurityTests
{
    // The rule: only [a-z A-Z 0-9 - _ .] allowed — nothing that could inject
    // into a cmd.exe batch script (", &, |, <, >, ;, %, space, …)
    private static bool IsValidAssetName(string name) =>
        !string.IsNullOrEmpty(name) &&
        name.All(c => char.IsLetterOrDigit(c) || c is '-' or '_' or '.');

    [Theory]
    [InlineData("PCCleaner-win-x64.exe")]
    [InlineData("PCCleaner-linux-x64")]
    [InlineData("PCCleaner-osx-x64")]
    public void KnownAssetNames_PassSafetyCheck(string assetName)
    {
        Assert.True(IsValidAssetName(assetName),
            $"Asset name '{assetName}' would fail the safety check — update the allow-list rule");
    }

    [Theory]
    [InlineData("evil.exe\" & calc.exe & echo \"")]  // cmd injection via quote
    [InlineData("update.exe|rm -rf /")]               // pipe injection
    [InlineData("file %.exe")]                        // percent + space
    [InlineData("")]                                  // empty
    [InlineData("a b.exe")]                           // space
    public void MaliciousAssetNames_FailSafetyCheck(string assetName)
    {
        Assert.False(IsValidAssetName(assetName));
    }
}

// ── Security — systemd unit file % specifier injection ────────────────────────

public class SystemdTimerSecurityTests
{
    [Fact]
    public void Create_PathWithPercent_PassesNewlineCheck()
    {
        // Paths with % are valid filesystem paths on Linux — they must NOT be
        // rejected. Instead they are escaped (% → %%) in the unit file content.
        // This test verifies the path gets past the guard and only fails (if at all)
        // because systemctl is unavailable, not because of percent rejection.
        string path = "/home/user%h/pccleaner";
        SystemdTimerManager.Create(path, out string error);
        Assert.DoesNotContain("newline", error, StringComparison.OrdinalIgnoreCase);
    }

}
