namespace PCCleaner.Utilities;

using System.Threading;

internal enum AppLanguage
{
    English,
    Czech
}

internal static class Localizer
{
    private static readonly Dictionary<string, string> English = new()
    {
        // ── Platform names ────────────────────────────────────────────────────
        ["platform.all"]      = "all OS",
        ["platform.windows"]  = "Windows",
        ["platform.linux"]    = "Linux",
        ["platform.macos"]    = "macOS",
        ["platform.unixLike"] = "Linux/macOS",
        ["platform.unknown"]  = "unknown OS",

        // ── Admin helper ──────────────────────────────────────────────────────
        ["admin.notWindows"]     = "Administrator restart is only available on Windows.",
        ["admin.already"]        = "Already running as administrator.",
        ["admin.restartNoPath"]  = "Could not detect the current program path.",
        ["admin.cancelled"]      = "UAC prompt was cancelled.",

        // ── Runner status ─────────────────────────────────────────────────────
        ["status.adminRequired"]        = "administrator rights required",
        ["status.notConfirmed"]         = "not confirmed",
        ["status.unsupportedPlatform"]  = "requires {0}; current system is {1}",
        ["status.windowsOnly"]          = "This cleaner is Windows-only.",

        // ── Clean result ──────────────────────────────────────────────────────
        ["result.total"]      = "Total",
        ["result.wouldDelete"] = "would delete",
        ["result.deleted"]    = "deleted",
        ["result.message"]    = "{0}: {1} {2} files and {3} folders, skipped {4} recent files, freed {5}, failures: {6}.",

        // ── Notes ─────────────────────────────────────────────────────────────
        ["note.recycleBin"]        = "Recycle Bin contains about {0} item(s), {1} total.",
        ["note.componentPreview"]  = @"Would run: schtasks.exe /Run /TN \Microsoft\Windows\Servicing\StartComponentCleanup",
        ["note.componentAccepted"] = "Windows accepted the component cleanup task. Cleanup may continue in the background.",

        // ── Warnings ──────────────────────────────────────────────────────────
        ["warning.browserRunning"] = "Browser process(es) are running: {0}. Close browsers first for a cleaner and safer cache cleanup.",

        // ── Confirm prompt ────────────────────────────────────────────────────
        ["prompt.typeCleanWord"] = "CLEAN",

        // ── Cleaner strings ───────────────────────────────────────────────────
        ["cleaner.userTemp.name"]        = "User temporary files",
        ["cleaner.userTemp.description"] = "Deletes files from the current user's temp folder.",
        ["cleaner.userTemp.risk"]        = "Apps may recreate temp files; open installers or apps can fail if their temp files are in use.",

        ["cleaner.unixUserCache.name"]        = "Safe Linux/macOS cache buckets",
        ["cleaner.unixUserCache.description"] = "Deletes targeted rebuildable caches like thumbnails, fontconfig, shader caches, pip cache, and selected macOS system cache buckets.",
        ["cleaner.unixUserCache.risk"]        = "Apps may rebuild these caches and launch a little slower after cleaning; close apps first when possible.",

        ["cleaner.windowsTemp.name"]        = "Windows temporary files",
        ["cleaner.windowsTemp.description"] = "Deletes files from C:\\Windows\\Temp. Administrator rights may be needed.",
        ["cleaner.windowsTemp.risk"]        = "Protected or in-use files may fail; running installs/updates should be closed first.",

        ["cleaner.crashDumps.name"]        = "Crash dumps",
        ["cleaner.crashDumps.description"] = "Deletes Windows error report dumps and app crash dump files.",
        ["cleaner.crashDumps.risk"]        = "You may lose crash files useful for debugging broken apps.",

        ["cleaner.windowsLogs.name"]        = "Windows log files",
        ["cleaner.windowsLogs.description"] = "Deletes common .log, .tmp, and .old files from Windows temp-style log folders.",
        ["cleaner.windowsLogs.risk"]        = "Troubleshooting history can be removed; admin rights are required.",

        ["cleaner.browserCache.name"]        = "Browser caches",
        ["cleaner.browserCache.description"] = "Deletes common Chrome, Edge, Brave, Opera, Chromium, Vivaldi, and Firefox cache folders.",
        ["cleaner.browserCache.risk"]        = "Browsers may reload websites more slowly and should be closed while cleaning.",

        ["cleaner.macUserLogs.name"]        = "macOS user logs",
        ["cleaner.macUserLogs.description"] = "Deletes old .log, .trace, .crash, and .ips files from ~/Library/Logs.",
        ["cleaner.macUserLogs.risk"]        = "Troubleshooting history can be removed; recent files are skipped unless you disable the age filter.",

        ["cleaner.thumbnailCache.name"]        = "Thumbnail cache",
        ["cleaner.thumbnailCache.description"] = "Deletes Windows Explorer thumbnail database files when they are not locked.",
        ["cleaner.thumbnailCache.risk"]        = "Explorer may rebuild thumbnails later, causing temporary slowness.",

        ["cleaner.recentItems.name"]        = "Recent items",
        ["cleaner.recentItems.description"] = "Deletes Windows recent-file shortcuts and jump-list cache files.",
        ["cleaner.recentItems.risk"]        = "Recent file lists and jump-list history will disappear.",

        ["cleaner.recycleBin.name"]        = "Recycle Bin",
        ["cleaner.recycleBin.description"] = "Empties the Windows Recycle Bin without prompts.",
        ["cleaner.recycleBin.risk"]        = "Files in the Recycle Bin become much harder to recover.",

        ["cleaner.startup.name"]        = "App startup entry",
        ["cleaner.startup.description"] = "Removes this cleaner from the current user's Windows startup entries if present.",
        ["cleaner.startup.risk"]        = "Only this cleaner's startup entry is removed; it will not auto-start afterward.",

        ["cleaner.component.name"]        = "Windows component cleanup task",
        ["cleaner.component.description"] = "Starts the official Windows StartComponentCleanup scheduled task for WinSxS/component cleanup.",
        ["cleaner.component.risk"]        = "Windows update cleanup can take time and may remove rollback files for old components.",

        ["cleaner.duplicates.name"]        = "Duplicate files in Documents",
        ["cleaner.duplicates.description"] = "Deletes duplicate files only when their size and SHA-256 hash match. Keeps the first file found.",
        ["cleaner.duplicates.risk"]        = "Personal files can be deleted if they are exact duplicates; preview first.",

        ["cleaner.scheduledTask.name"]        = "Scheduled auto-clean task",
        ["cleaner.scheduledTask.description"] = "Removes the PC Cleaner Auto-Clean scheduled task from Windows Task Scheduler.",
        ["cleaner.scheduledTask.risk"]        = "Only the PC Cleaner scheduled task is removed; no other scheduled tasks are affected.",

        ["cleaner.prefetch.name"]        = "Windows Prefetch cache",
        ["cleaner.prefetch.description"] = "Deletes .pf prefetch files from C:\\Windows\\Prefetch. Windows rebuilds them on next app launch.",
        ["cleaner.prefetch.risk"]        = "Apps may start a fraction slower on first launch after cleaning; requires administrator rights.",

        ["cleaner.steamCache.name"]        = "Steam shader & download cache",
        ["cleaner.steamCache.description"] = "Deletes Steam shader cache, incomplete downloads, and temp files for native and Flatpak Steam.",
        ["cleaner.steamCache.risk"]        = "Shader cache is rebuilt on first game launch (expect stuttering); partially downloaded games must re-download missing pieces.",

        ["cleaner.devCache.name"]        = "Developer tool caches",
        ["cleaner.devCache.description"] = "Clears npm, pip, yarn, cargo registry, and Gradle module download caches.",
        ["cleaner.devCache.risk"]        = "Next build or install will re-download packages from the internet; no source code or installed packages are touched.",

        ["cleaner.flatpak.name"]        = "Flatpak unused runtimes",
        ["cleaner.flatpak.description"] = "Removes Flatpak runtimes and extensions that are no longer required by any installed app.",
        ["cleaner.flatpak.risk"]        = "Runtimes are re-downloaded if a matching app is installed later; no app data is touched.",

        ["cleaner.vscode.name"]        = "VS Code orphaned workspace storage",
        ["cleaner.vscode.description"] = "Deletes workspaceStorage entries whose project folder no longer exists on disk.",
        ["cleaner.vscode.risk"]        = "VS Code extension state for deleted projects is removed; active projects are not touched.",

        ["cleaner.unixTrash.name"]        = "Trash / Bin",
        ["cleaner.unixTrash.description"] = "Empties the user Trash folder (~/.local/share/Trash on Linux, ~/.Trash on macOS).",
        ["cleaner.unixTrash.risk"]        = "Trashed files become much harder to recover — the same risk as emptying the Recycle Bin on Windows.",

        ["cleaner.linuxRecentFiles.name"]        = "Linux recent files list",
        ["cleaner.linuxRecentFiles.description"] = "Deletes ~/.local/share/recently-used.xbel, which GNOME/GTK apps use to track recently opened files.",
        ["cleaner.linuxRecentFiles.risk"]        = "Recent-file menus in all GTK apps (Files, GIMP, Inkscape, etc.) will be cleared. GTK rebuilds the file on next use.",
    };

    private static readonly Dictionary<string, string> Czech = new()
    {
        // ── Platform names ────────────────────────────────────────────────────
        ["platform.all"]      = "vsechny OS",
        ["platform.windows"]  = "Windows",
        ["platform.linux"]    = "Linux",
        ["platform.macos"]    = "macOS",
        ["platform.unixLike"] = "Linux/macOS",
        ["platform.unknown"]  = "neznamy OS",

        // ── Admin helper ──────────────────────────────────────────────────────
        ["admin.notWindows"]    = "Restart jako spravce je dostupny jen na Windows.",
        ["admin.already"]       = "Program uz bezi jako spravce.",
        ["admin.restartNoPath"] = "Nepodarilo se zjistit cestu k programu.",
        ["admin.cancelled"]     = "Potvrzovaci dialog UAC byl zrusen.",

        // ── Runner status ─────────────────────────────────────────────────────
        ["status.adminRequired"]       = "jsou potreba prava spravce",
        ["status.notConfirmed"]        = "nepotvrzeno",
        ["status.unsupportedPlatform"] = "vyzaduje {0}; aktualni system je {1}",
        ["status.windowsOnly"]         = "Tento cistic je jen pro Windows.",

        // ── Clean result ──────────────────────────────────────────────────────
        ["result.total"]      = "Celkem",
        ["result.wouldDelete"] = "by odstranil",
        ["result.deleted"]    = "odstranil",
        ["result.message"]    = "{0}: {1} {2} souboru a {3} slozek, preskoceno {4} cerstvych souboru, uvolneno {5}, selhani: {6}.",

        // ── Notes ─────────────────────────────────────────────────────────────
        ["note.recycleBin"]        = "Kos obsahuje priblizne {0} polozek, celkem {1}.",
        ["note.componentPreview"]  = @"Spustilo by se: schtasks.exe /Run /TN \Microsoft\Windows\Servicing\StartComponentCleanup",
        ["note.componentAccepted"] = "Windows prijal ulohu cisteni komponent. Cisteni muze pokracovat na pozadi.",

        // ── Warnings ──────────────────────────────────────────────────────────
        ["warning.browserRunning"] = "Bezi prohlizece: {0}. Zavri je pro cistsi a bezpecnejsi mazani cache.",

        // ── Confirm prompt ────────────────────────────────────────────────────
        ["prompt.typeCleanWord"] = "CISTIT",

        // ── Cleaner strings ───────────────────────────────────────────────────
        ["cleaner.userTemp.name"]        = "Docasne soubory uzivatele",
        ["cleaner.userTemp.description"] = "Odstrani soubory z docasne slozky aktualniho uzivatele.",
        ["cleaner.userTemp.risk"]        = "Aplikace si docasne soubory znovu vytvori; otevrene instalatory nebo aplikace muzou selhat.",

        ["cleaner.unixUserCache.name"]        = "Bezpecne cache Linux/macOS",
        ["cleaner.unixUserCache.description"] = "Odstrani cilene obnovitelne cache jako nahledy, fontconfig, shader cache, pip cache a vybrane systemove cache macOS.",
        ["cleaner.unixUserCache.risk"]        = "Aplikace si tyto cache znovu vytvori a po cisteni se mohou spoustet o trochu pomaleji; pokud to jde, zavri je.",

        ["cleaner.windowsTemp.name"]        = "Docasne soubory Windows",
        ["cleaner.windowsTemp.description"] = "Odstrani soubory z C:\\Windows\\Temp. Mohou byt potreba prava spravce.",
        ["cleaner.windowsTemp.risk"]        = "Chranene nebo pouzivane soubory mohou selhat; pred cistenim zavri instalace a aktualizace.",

        ["cleaner.crashDumps.name"]        = "Vypisy padu",
        ["cleaner.crashDumps.description"] = "Odstrani vypisy padu aplikaci a hlaseni chyb Windows.",
        ["cleaner.crashDumps.risk"]        = "Prijdes o soubory uzitecne pro ladeni rozbitych aplikaci.",

        ["cleaner.windowsLogs.name"]        = "Logy Windows",
        ["cleaner.windowsLogs.description"] = "Odstrani bezne soubory .log, .tmp, .old a .bak z logovacich/temp slozek Windows.",
        ["cleaner.windowsLogs.risk"]        = "Muze zmizet historie pro reseni problemu; vyzaduje prava spravce.",

        ["cleaner.browserCache.name"]        = "Cache prohlizecu",
        ["cleaner.browserCache.description"] = "Odstrani cache slozky pro Chrome, Edge, Brave, Operu, Chromium, Vivaldi a Firefox.",
        ["cleaner.browserCache.risk"]        = "Weby se muzou nacitat pomaleji; prohlizece by mely byt zavrene.",

        ["cleaner.macUserLogs.name"]        = "Uzivatelske logy macOS",
        ["cleaner.macUserLogs.description"] = "Odstrani stare soubory .log, .trace, .crash a .ips z ~/Library/Logs.",
        ["cleaner.macUserLogs.risk"]        = "Muze zmizet historie pro reseni problemu; cerstve soubory se preskoci, pokud nevypnes vekovy filtr.",

        ["cleaner.thumbnailCache.name"]        = "Cache nahledu",
        ["cleaner.thumbnailCache.description"] = "Odstrani databaze nahledu Pruzkumnika Windows, pokud nejsou zamcene.",
        ["cleaner.thumbnailCache.risk"]        = "Pruzkumnik bude nahledy znovu stavet, docasne muze zpomalit.",

        ["cleaner.recentItems.name"]        = "Nedavne polozky",
        ["cleaner.recentItems.description"] = "Odstrani zastupce nedavnych souboru a jump-list cache.",
        ["cleaner.recentItems.risk"]        = "Zmizi seznamy nedavnych souboru a historie jump-listu.",

        ["cleaner.recycleBin.name"]        = "Kos",
        ["cleaner.recycleBin.description"] = "Vyprazdni Kos Windows bez dotazu.",
        ["cleaner.recycleBin.risk"]        = "Soubory v Kosi bude mnohem tezsi obnovit.",

        ["cleaner.startup.name"]        = "Polozka po spusteni",
        ["cleaner.startup.description"] = "Odebere tento cistic ze spousteni aktualniho uzivatele, pokud tam je.",
        ["cleaner.startup.risk"]        = "Odebere se jen start tohoto cistice; po spusteni Windows uz se sam nezapne.",

        ["cleaner.component.name"]        = "Cisteni komponent Windows",
        ["cleaner.component.description"] = "Spusti oficialni ulohu StartComponentCleanup pro cisteni WinSxS/komponent.",
        ["cleaner.component.risk"]        = "Cisteni aktualizaci muze trvat a muze odstranit rollback soubory starych komponent.",

        ["cleaner.duplicates.name"]        = "Duplicitni soubory v Dokumentech",
        ["cleaner.duplicates.description"] = "Odstrani duplicity jen pri shodne velikosti a otisku SHA-256. Prvni nalezeny soubor ponecha.",
        ["cleaner.duplicates.risk"]        = "Osobni soubory mohou byt smazane, pokud jsou presne duplicity; nejdriv pouzij nahled.",

        ["cleaner.scheduledTask.name"]        = "Naplanovana automaticka cistka",
        ["cleaner.scheduledTask.description"] = "Odstrani naplanovany ukol PC Cleaner Auto-Clean z Planovace uloh Windows.",
        ["cleaner.scheduledTask.risk"]        = "Odebere se pouze ukol PC Cleaner; ostatni naplanovane ukoly nejsou dotceny.",

        ["cleaner.prefetch.name"]        = "Windows Prefetch cache",
        ["cleaner.prefetch.description"] = "Maze soubory .pf z C:\\Windows\\Prefetch. Windows je pri pristim spusteni aplikace znovu vytvori.",
        ["cleaner.prefetch.risk"]        = "Aplikace se mohou pri prvnim spusteni po cisteni o trochu pomaleji nacist; vyzaduje prava spravce.",

        ["cleaner.steamCache.name"]        = "Steam shader a stahovaci cache",
        ["cleaner.steamCache.description"] = "Maze shader cache, neuplne stazene soubory a docasne soubory Steamu (nativni i Flatpak).",
        ["cleaner.steamCache.risk"]        = "Shader cache se pri prvnim spusteni hry znovu postavi (ocekavej zasekavani); castecne stazene hry si musi restahovat chybejici casti.",

        ["cleaner.devCache.name"]        = "Cache vyvojarskych nastroju",
        ["cleaner.devCache.description"] = "Cisti npm, pip, yarn, cargo registry a Gradle cache stazených modulu.",
        ["cleaner.devCache.risk"]        = "Pri pristim buildu nebo instalaci se balicky restahnou z internetu; zdrojove kody ani nainstalovane balicky se netykaji.",

        ["cleaner.flatpak.name"]        = "Nepouzivane Flatpak runtime",
        ["cleaner.flatpak.description"] = "Odstrani Flatpak runtime a rozsireni, ktera uz zadna nainstalovana aplikace nepotrebuje.",
        ["cleaner.flatpak.risk"]        = "Runtime se restahne, pokud ji bude pozdeji potrebovat nejaka aplikace; data aplikaci se netykaji.",

        ["cleaner.vscode.name"]        = "VS Code osirely workspaceStorage",
        ["cleaner.vscode.description"] = "Maze zaznamy workspaceStorage, jejichz slozka projektu uz na disku neexistuje.",
        ["cleaner.vscode.risk"]        = "Odebere se stav rozsireni VS Code pro smazane projekty; aktivni projekty nejsou dotceny.",

        ["cleaner.unixTrash.name"]        = "Kos (Linux/macOS)",
        ["cleaner.unixTrash.description"] = "Vyprazdni kos uzivatele (~/.local/share/Trash na Linuxu, ~/.Trash na macOS).",
        ["cleaner.unixTrash.risk"]        = "Smazane soubory bude mnohem tezsi obnovit — stejne riziko jako vyprazdnit Kos ve Windows.",

        ["cleaner.linuxRecentFiles.name"]        = "Linux seznam nedavnych souboru",
        ["cleaner.linuxRecentFiles.description"] = "Smaze ~/.local/share/recently-used.xbel, ktery aplikace GNOME/GTK pouzivaji ke sledovani nedavno otevrenych souboru.",
        ["cleaner.linuxRecentFiles.risk"]        = "Seznamy nedavnych souboru ve vsech GTK aplikacich se smazi. GTK soubor znovu vytvori pri pristim pouziti.",
    };

    private static readonly AsyncLocal<AppLanguage?> LanguageOverride = new();

    public static AppLanguage CurrentLanguage => LanguageOverride.Value ?? AppLanguage.English;

    public static void SetLanguage(AppLanguage language)
    {
        LanguageOverride.Value = language;
    }

    public static string T(string key)
    {
        Dictionary<string, string> dictionary = CurrentLanguage == AppLanguage.Czech ? Czech : English;
        return dictionary.TryGetValue(key, out string? value) ? value : key;
    }

    public static string T(string key, params object[] args)
    {
        return string.Format(T(key), args);
    }
}
