# PC Cleaner — Uživatelská příručka

**Verze:** 1.0.2  
**Autor:** soda144p  
**Platformy:** Windows · Linux · macOS

---

## Obsah

1. [Stažení a spuštění](#1-stažení-a-spuštění)
2. [Splash screen](#2-splash-screen)
3. [Hlavní okno](#3-hlavní-okno)
4. [Panel cleanerů](#4-panel-cleanerů)
5. [Panel ovládání](#5-panel-ovládání)
6. [Výstupní panel a status bar](#6-výstupní-panel-a-status-bar)
7. [Menu File](#7-menu-file)
8. [Klávesové zkratky](#8-klávesové-zkratky)
9. [Přehled všech cleanerů](#9-přehled-všech-cleanerů)
10. [Věkový filtr souborů](#10-věkový-filtr-souborů)
11. [Potvrzení smazání](#11-potvrzení-smazání)
12. [Admin warning dialog](#12-admin-warning-dialog)
13. [Reporty](#13-reporty)
14. [Nastavení](#14-nastavení)
15. [Příkazová řádka (CLI)](#15-příkazová-řádka-cli)
16. [Headless / automatický mód](#16-headless--automatický-mód)
17. [Naplánovaný úklid](#17-naplánovaný-úklid)
18. [Automatická aktualizace](#18-automatická-aktualizace)
19. [Tipy a doporučení](#19-tipy-a-doporučení)

---

## 1. Stažení a spuštění

### Stažení binárky (doporučeno)

Stáhni aktuální verzi ze stránky [Releases](https://github.com/Sodicek/PC-Cleaner-ghetto/releases/latest). Nepotřebuješ .NET SDK ani nic instalovat.

| Platforma | Soubor |
|-----------|--------|
| Windows | `PCCleaner-win-x64.exe` |
| Linux x64 | `PCCleaner-linux-x64` |

**Windows:** spusť `.exe` přímo (nebo přes příkazovou řádku).

**Linux:**
```bash
chmod +x PCCleaner-linux-x64
./PCCleaner-linux-x64
```

### Doporučený terminál

- **Windows:** Windows Terminal (ne starý `cmd.exe` — špatně zobrazuje znaky)
- **Linux:** Konsole, GNOME Terminal, Alacritty
- **macOS:** iTerm2, nativní Terminal

### Spuštění ze zdrojového kódu

Potřebuješ .NET 8 SDK:

```powershell
dotnet run --project PCCleaner.App
```

---

## 2. Splash screen

Po spuštění se zobrazí animovaný úvodní screen:

```
  ██████╗  ██████╗    ██████╗██╗     ███████╗ █████╗ ███╗   ██╗███████╗██████╗
  ...
  sweeping your drive clean since forever
  by soda144p  ·  POCITAC  ·  Windows
  press any key to skip
  [████████████████████        ]
  loading cleaners...
```

- **Hvězdy** se pohybují přes obrazovku (dvouvrstvý parallax).
- **Progress bar** se plní automaticky za ~2 sekundy.
- **Stiskni libovolnou klávesu** pro přeskočení a okamžitý přechod do hlavního okna.

---

## 3. Hlavní okno

Po splashi se zobrazí hlavní TUI rozhraní rozdělené do tří oblastí:

```
┌─ File ────────────────────────────────────────────────────────────────────┐
│  soda144p   ·   Windows   ·   MUJ-PC                                     │
│  ·  *  ✦  ·  ·  *  ·  ✦  ·  ·  (animovaný starfield)                    │
│                                                                            │
│  ┌── Cleaners ─────────────────────┐  ┌── Controls ──────────────────┐   │
│  │  safe    User temporary files   │  │  [ Scan (preview) ]          │   │
│  │  safe    Browser caches         │  │  [ Run clean      ]          │   │
│  │  admin!  Windows temp files     │  │  · · · · · · · · · · · ·    │   │
│  │  ...                            │  │  Age (hours)                 │   │
│  │                                 │  │  [24           ]             │   │
│  │                                 │  │  [ ] Include recent files    │   │
│  │                                 │  │  · · · · · · · · · · · ·    │   │
│  │                                 │  │  [ Recommended    ]          │   │
│  │                                 │  │  [ All for this OS]          │   │
│  │                                 │  │  [ Clear          ]          │   │
│  │                                 │  │  · · · · · · · · · · · ·    │   │
│  │                                 │  │  Browser caches              │   │
│  │                                 │  │  Removes browser cache files │   │
│  │                                 │  │  Risk: Low                   │   │
│  └─────────────────────────────────┘  └──────────────────────────────┘   │
│  ┌── Output ──────────────────────────────────────────────────────────┐   │
│  │  Ready. Select cleaners and press Scan or Run clean.              │   │
│  └────────────────────────────────────────────────────────────────────┘   │
│ Ctrl+Q Quit │ F1 Shortcuts │ MUJ-PC · ○ user │ 5/18 selected │ Freed: — │
└────────────────────────────────────────────────────────────────────────────┘
```

---

## 4. Panel cleanerů

Levá část okna zobrazuje seznam cleanerů dostupných pro tvůj operační systém. Každý řádek vypadá takto:

```
[x]  safe    User temporary files
[ ]  admin!  Windows temporary files
[x]  manual  Duplicate files in Documents
```

### Odznaky (badges)

| Odznak | Barva | Význam |
|--------|-------|--------|
| `safe` | azurová | Bezpečný, doporučený cleaner |
| `manual` | šedá | Ruční cleaner, používej s rozvahou |
| `admin` | bílá | Vyžaduje práva správce — ty je máš |
| `admin!` | šedá | Vyžaduje práva správce — nejsi správce, zakázáno |

### Info panel

Při přechodu na (focusu) libovolný checkbox se spodní část pravého panelu (Controls) automaticky aktualizuje a zobrazí:

- **Název** cleaneru
- **Popis** — co přesně maže
- **Míra rizika** — jak moc je operace bezpečná

Hodí se pro rychlé ověření co daný cleaner dělá, aniž by bylo potřeba opustit aplikaci.

### Scrollování

Pokud se všechny cleanery nevejdou do okna, panel scrolluje. Klávesami **↑ / ↓** nebo kolečkem myši.

### Zaškrtávání

Klikni myší na checkbox nebo ho vyber a stiskni mezerník. Výběr se automaticky ukládá do nastavení.

---

## 5. Panel ovládání

Pravá část okna.

### Tlačítka akcí

| Tlačítko | Funkce |
|----------|--------|
| **Scan (preview)** | Spustí sken — ukáže co by se smazalo, ale nic nemaže. Bezpečné pro první použití. |
| **Run clean** | Spustí skutečné mazání po potvrzení. |

Pokud operace běží, obě tlačítka se změní na **`[X] Cancel`** — kliknutím zrušíš aktuální běh.

### Věkový filtr

**Age (hours)** — pole pro zadání počtu hodin. Soubory mladší než tento limit se přeskočí. Výchozí hodnota je **24 hodin**. Hodnota se ukládá okamžitě při každé změně.

- Zadej `0` pro mazání všeho bez ohledu na stáří.
- Zadej `168` pro mazání jen souborů starších 7 dní.

**Include recent files** — zaškrtnutím zcela vypneš věkový filtr. Hodnota v poli Age pak nemá efekt. Hodí se pro hluboký úklid, ale buď opatrný — applikace které právě běží mohou ztratit temp soubory.

### Hromadný výběr

| Tlačítko | Co udělá |
|----------|----------|
| **Recommended** | Zaškrtne jen doporučené cleanery (`safe`) |
| **All for this OS** | Zaškrtne všechny cleanery podporované na tvém OS |
| **Clear** | Odškrtne vše |

---

## 6. Výstupní panel a status bar

### Výstupní panel

Spodní část okna. Zobrazuje průběh skenování / mazání v reálném čase:

```
Scan mode — no files will be deleted.

→ User temporary files...
  deleted 143 files and 0 folders, skipped 12 recent files, freed 45.2 MB, failures: 0.
→ Browser caches...
  ⚠  Browser process(es) are running: chrome. Close browsers first.
  deleted 0 files and 0 folders, skipped 0 recent files, freed 0 B, failures: 0.
→ Developer tool caches...
  deleted 892 files and 34 folders, skipped 0 recent files, freed 1.3 GB, failures: 0.

━━━ Total: deleted 1035 files and 34 folders, freed 1.34 GB, failures: 0. ━━━
Drive free: 45.3 GB
Report saved → C:\Users\...\AppData\Local\pc-cleaner-ghetto\reports\report-2025-06-20.txt
```

Po reálném čistění se za souhrnným řádkem zobrazí aktuální volné místo na disku (`Drive free: X`).

Pokud některé soubory selžou, výstup zobrazí jejich jméno a důvod:
```
  ✗ cache.db: Access to the path is denied.
  ✗ session.lock: The process cannot access the file...
```

Panel se automaticky scrolluje na konec při každém novém výstupu.

### Status bar

Spodní lišta (vždy viditelná):

```
Ctrl+Q Quit │ F1 Shortcuts │ MUJ-PC · ○ user │ 5 / 18 selected │ Freed: 1.34 GB │ All-time: 8.7 GB
```

| Položka | Popis |
|---------|-------|
| `Ctrl+Q Quit` | Klávesová zkratka pro ukončení |
| `F1 Shortcuts` | Otevře overlay se všemi klávesovými zkratkami |
| `MUJ-PC` | Název počítače |
| `● admin` / `○ user` | Jestli běžíš jako správce nebo normální uživatel |
| `5 / 18 selected` | Počet zaškrtnutých cleanerů ze všech dostupných; aktualizuje se živě |
| `Freed: X` | Místo uvolněné v aktuálním běhu |
| `All-time: X` | Celkové místo uvolněné od začátku používání |
| `✗ N errors` | Počet selhání v posledním běhu (zobrazí se jen když jsou chyby) |

Když je dostupná aktualizace, `Freed` se nahradí upozorněním: **`★ v1.0.3 ready`**.

---

## 7. Menu File

Klikni na **File** v horní liště nebo stiskni **Alt+F**.

### Language: EN / CS

Přepíná jazyk rozhraní mezi angličtinou a češtinou. Výstup cleanerů se okamžitě přepne; pro přeložení popisků cleanerů v levém panelu je potřeba restartovat aplikaci.

Jazyk se uloží do nastavení a platí i při příštím spuštění.

### Schedule Auto-Clean...

Otevře dialog pro nastavení automatického týdenního úklidu (viz [kapitola 17](#17-naplánovaný-úklid)).

### Restart as Administrator *(pouze Windows)*

Restartuje aplikaci se správcovskými právy (UAC dialog). Potřebuješ to pro cleanery označené `admin` — Windows temp soubory, logy, Prefetch.

Po restartu status bar ukáže `● admin` a tyto cleanery se odblokují.

### Disk Overview...

Otevře dialog s přehledem místa na disku:

```
Drive free  : 45.3 GB
Freed by PC Cleaner (all-time): 8.7 GB

Top folders in C:\Users\David  [Done — 24 folders]
──────────────────────────────────────────────────────────────
  ████████████████████████  10.2 GB  Downloads
  ██████████████░░░░░░░░░░   6.8 GB  Videos
  ████████░░░░░░░░░░░░░░░░   4.1 GB  AppData
  ...
```

Skenování probíhá **paralelně** — složky se přidávají průběžně v reálném čase. Zobrazí se top 16 největších složek v domovském adresáři, seřazených sestupně.

### Check for Updates

Aplikace na pozadí při startu automaticky zkontroluje nové vydání na GitHubu. Tato položka menu otevře výsledek kontroly:

- **Aktuální verze** — zobrazí "You are running the latest version."
- **Nová verze k dispozici** — zobrazí verze a tlačítko **Download & Install** (viz [kapitola 18](#18-automatická-aktualizace)).

### Changelog...

Otevře scrollovatelný přehled všech verzí a změn přímo v aplikaci (obsah souboru `CHANGELOG.md`).

### Quit (Ctrl+Q)

Ukončí aplikaci. Pokud běží skenování, nejdřív stiskni **Cancel** v panelu ovládání.

---

## 8. Klávesové zkratky

Stiskni **F1** nebo **?** kdykoliv pro zobrazení přehledu zkratek.

| Zkratka | Funkce |
|---------|--------|
| `Tab` / `Shift+Tab` | Přesun mezi ovládacími prvky |
| `↑` / `↓` | Pohyb v seznamu cleanerů |
| `Space` | Zaškrtnutí / odškrtnutí checkboxu, kliknutí tlačítka |
| `Enter` | Potvrzení dialogu / aktivace tlačítka |
| `F1` nebo `?` | Zobrazí tento přehled zkratek |
| `Ctrl+Q` | Ukončení aplikace |
| Klik na **Scan** nebo **Run clean** (za běhu) | Zrušení probíhající operace |

---

## 9. Přehled všech cleanerů

Aplikace zobrazuje pouze cleanery **podporované na tvém OS**. Níže je kompletní seznam.

### Windows

| # | Název | Doporučený | Správce | Co maže |
|---|-------|-----------|---------|---------|
| 1 | **User temporary files** | ✓ | Ne | `%TEMP%` — dočasné soubory aktuálního uživatele |
| 2 | **Windows temporary files** | ✓ | **Ano** | `C:\Windows\Temp` — systémové dočasné soubory |
| 3 | **Crash dumps** | ✓ | Ne | Výpisy pádů aplikací a Windows Error Reporting |
| 4 | **Windows log files** | ✓ | **Ano** | `.log`, `.tmp`, `.old`, `.bak` soubory ze systémových log složek |
| 5 | **Browser caches** | ✓ | Ne | Cache Chrome, Edge, Brave, Opera GX, Chromium, Vivaldi, Firefox |
| 6 | **Thumbnail cache** | ✓ | Ne | `Thumbs.db` databáze náhledů Průzkumníka Windows |
| 7 | **Recent items** | ✓ | Ne | Zkratky naposledy otevřených souborů + jump-list cache |
| 8 | **Recycle Bin** | ✓ | Ne | Vyprázdní Koš Windows |
| 9 | **App startup entry** | ✓ | Ne | Odstraní záznam PC Cleaneru ze spouštění Windows (pokud tam je) |
| 10 | **Developer tool caches** | ✓ | Ne | Cache npm, pip, yarn, cargo registry, Gradle modulů |
| 11 | **VS Code orphaned workspace storage** | Ne | Ne | WorkspaceStorage záznamy VS Code pro složky co už neexistují |
| 12 | **Windows component cleanup task** | Ne | **Ano** | Spustí `StartComponentCleanup` (WinSxS čistění, může trvat déle) |
| 13 | **Duplicate files in Documents** | Ne | Ne | Duplikáty v `Documents` — dle SHA-256 hashe, zachová první nalezený |
| 14 | **Scheduled auto-clean task** | Ne | Ne | Odstraní naplánovaný úkol PC Cleaneru z Plánovače úloh |
| 15 | **Windows Prefetch cache** | Ne | **Ano** | `.pf` soubory v `C:\Windows\Prefetch` — Windows je znovu vytvoří |
| 16 | **Steam shader & download cache** | Ne | Ne | Steam shader cache, neúplné downloady, temp soubory |

### Linux

| # | Název | Doporučený | Co maže |
|---|-------|-----------|---------|
| 1 | **User temporary files** | ✓ | `$TMPDIR` / `/tmp` uživatelské dočasné soubory |
| 2 | **Safe Linux cache buckets** | ✓ | Thumbnails (`~/.cache/thumbnails`), fontconfig, mesa shader cache, pip cache |
| 3 | **Browser caches** | ✓ | Chrome, Chromium, Firefox (nativní + Flatpak verze), Brave, Edge, Opera, Vivaldi |
| 4 | **Developer tool caches** | ✓ | npm, pip, yarn, cargo registry, Gradle |
| 5 | **Flatpak unused runtimes** | ✓ | Flatpak runtime a extensions které žádná appka nepotřebuje |
| 6 | **Trash / Koš** | ✓ | `~/.local/share/Trash/files/` a `~/.local/share/Trash/info/` (XDG spec) |
| 7 | **VS Code orphaned workspace storage** | Ne | WorkspaceStorage VS Code pro smazané projekty |
| 8 | **Duplicate files in Documents** | Ne | Duplikáty v `~/Documents` |
| 9 | **Steam shader & download cache** | Ne | Steam cache (nativní + Flatpak) |
| 10 | **Linux recent files list** | Ne | `~/.local/share/recently-used.xbel` — historie souborů v GTK appkách (GIMP, Nautilus…) |

### macOS

| # | Název | Doporučený | Co maže |
|---|-------|-----------|---------|
| 1 | **User temporary files** | ✓ | `$TMPDIR` uživatelské dočasné soubory |
| 2 | **Safe macOS cache buckets** | ✓ | Vybrané složky v `~/Library/Caches` (QuickLook, helpd, iconservices) |
| 3 | **macOS user logs** | ✓ | `.log`, `.trace`, `.crash`, `.ips` soubory v `~/Library/Logs` |
| 4 | **Browser caches** | ✓ | Chrome, Brave, Chromium, Vivaldi, Firefox |
| 5 | **Developer tool caches** | ✓ | npm, pip, yarn, cargo registry, Gradle |
| 6 | **Trash / Koš** | ✓ | `~/.Trash/` |
| 7 | **VS Code orphaned workspace storage** | Ne | WorkspaceStorage VS Code pro smazané projekty |
| 8 | **Duplicate files in Documents** | Ne | Duplikáty v `~/Documents` |
| 9 | **Steam shader & download cache** | Ne | Steam cache |

---

## 10. Věkový filtr souborů

**Výchozí nastavení: 24 hodin.**

Bezpečnostní filtr chrání soubory které jsou čerstvé — mohly by být právě používané. Soubory mladší než zadaný limit se přeskočí a do výstupu se zapíše `skipped N recent files`.

### Jak změnit

V panelu ovládání uprav pole **Age (hours)**:

| Hodnota | Efekt |
|---------|-------|
| `0` | Maže vše bez ohledu na stáří |
| `1` | Přeskočí soubory mladší 1 hodiny |
| `24` | Výchozí — přeskočí soubory z posledních 24 hodin |
| `168` | Přeskočí soubory z posledního týdne |

Hodnota se ukládá automaticky při každé změně — není potřeba spouštět scan.

### Include recent files

Zaškrtni **Include recent files** pro úplné vypnutí filtru. Hodnota v poli Age pak nemá efekt. Hodí se pro hluboký úklid, ale buď opatrný — applikace které právě běží mohou ztratit temp soubory.

---

## 11. Potvrzení smazání

Před skutečným mazáním aplikace zobrazí potvrzovací dialog:

```
┌─ Confirm deletion ─────────────────────────────────────┐
│                                                         │
│  Type CLEAN to confirm deleting files:                  │
│                                                         │
│  [                                              ]       │
│                                                 Cancel  OK │
└─────────────────────────────────────────────────────────┘
```

Musíš napsat přesně:
- **anglicky:** `CLEAN`
- **česky:** `CISTIT`

Velká/malá písmena **nevadí** — `clean` i `CLEAN` fungují. Pokud zadáš cokoliv jiného, mazání se nespustí.

> **Scan (preview) toto potvrzení nevyžaduje** — je bezpečné ho spustit kdykoliv.

---

## 12. Admin warning dialog

*(pouze Windows)*

Při prvním kliknutí na **Run clean** bez správcovských práv se zobrazí jednorázový informační dialog:

```
┌─ Not running as administrator ────────────────────────────────┐
│                                                               │
│  These cleaners are disabled — they need admin rights:        │
│    • Windows temporary files                                  │
│    • Windows log files                                        │
│    • Windows Prefetch cache                                   │
│                                                               │
│  Restart as administrator to enable them,                     │
│  or continue cleaning with what's available.                  │
│                                                               │
│           [ Cancel ]  [ Continue anyway ]  [ Restart as Admin ]│
└───────────────────────────────────────────────────────────────┘
```

| Tlačítko | Co udělá |
|----------|----------|
| **Restart as Admin** | Otevře UAC dialog a restartuje aplikaci se správcovskými právy |
| **Continue anyway** | Spustí čistění bez admin cleanerů |
| **Cancel** | Zruší celou operaci |

Dialog se zobrazí **jen jednou za session** — po kliknutí Continue se při dalším Run clean nezobrazí znovu.

Na Linuxu a macOS se tento dialog **nezobrazuje** — žádný z dostupných cleanerů na těchto platformách nepotřebuje zvýšená práva.

---

## 13. Reporty

Po každém dokončeném skenování nebo mazání se automaticky uloží report:

**Umístění:**
- Windows: `%LocalAppData%\pc-cleaner-ghetto\reports\`
- Linux: `~/.local/share/pc-cleaner-ghetto/reports/`
- macOS: `~/Library/Application Support/pc-cleaner-ghetto/reports/`

**Formát názvu souboru:** `report-RRRR-MM-DD_HH-MM-SS.txt`

**Obsah reportu:**
```
Created: 2025-06-20 14:32:11
Mode: Clean

─── Cleaner Results ────────────────────────────────────
User temporary files: deleted 143 files and 0 folders,
  skipped 12 recent files, freed 45.2 MB, failures: 0.

Developer tool caches: deleted 892 files and 34 folders,
  skipped 0 recent files, freed 1.3 GB, failures: 0.

─── Skipped Cleaners ───────────────────────────────────
Windows temporary files: administrator rights required

─── Disk Space ─────────────────────────────────────────
Drive C:: before 43.1 GB, after 44.5 GB, change +1.34 GB.

Age filter: skip files newer than 24 hour(s)
```

Reporty se **nesmažou automaticky** — je na tobě je spravovat nebo mazat ručně.

---

## 14. Nastavení

Nastavení se ukládá automaticky při každé změně (checkbox, věkový filtr, jazyk). Soubor:

- Windows: `%AppData%\pc-cleaner-ghetto\settings.json`
- Linux: `~/.config/pc-cleaner-ghetto/settings.json`
- macOS: `~/Library/Application Support/pc-cleaner-ghetto/settings.json`

**Příklad obsahu:**
```json
{
  "Language": "Czech",
  "AgeHours": 24,
  "IncludeRecent": false,
  "CheckedCleaners": [
    "DirectoryCleaner",
    "BrowserCacheCleaner",
    "DeveloperCacheCleaner"
  ],
  "TotalBytesFreed": 9126805504
}
```

| Klíč | Popis |
|------|-------|
| `Language` | `"English"` nebo `"Czech"` |
| `AgeHours` | Věkový filtr v hodinách (výchozí: 24) |
| `IncludeRecent` | `true` = filtr vypnutý |
| `CheckedCleaners` | Seznam zaškrtnutých cleanerů dle názvu třídy |
| `TotalBytesFreed` | Celoživotní počítadlo uvolněných bajtů |

Soubor lze upravit ručně v textovém editoru — aplikace ho načte při příštím startu.

---

## 15. Příkazová řádka (CLI)

```
PCCleaner.exe [přepínače]
```

| Přepínač | Popis |
|----------|-------|
| `--preview` `-n` `--dry-run` | Spustí TUI rovnou v preview módu (Scan je předvybraný) |
| `--age-hours <číslo>` | Přepíše věkový filtr na zadaný počet hodin |
| `--include-recent` | Vypne věkový filtr (ignoruje Age (hours)) |
| `--lang en` nebo `--lang cs` | Nastaví jazyk a uloží ho do nastavení |
| `--auto-clean` | Headless mód — spustí čistění bez TUI a skončí |

### Příklady

```powershell
# Spustit rovnou v preview módu
PCCleaner.exe --preview

# Mazat pouze soubory starší 7 dní
PCCleaner.exe --age-hours 168

# Přepnout na češtinu a spustit
PCCleaner.exe --lang cs

# Headless čistění (pro skripty / naplánované úlohy)
PCCleaner.exe --auto-clean

# Headless čistění souborů starších 48 hodin
PCCleaner.exe --auto-clean --age-hours 48
```

---

## 16. Headless / automatický mód

Přepínač `--auto-clean` spustí čistění **bez grafického rozhraní**:

- Žádné TUI, žádné dialogy, žádné potvrzení.
- Automaticky vybere všechny **doporučené cleanery** (`safe`) které **nepotřebují správce**.
- Po dokončení uloží report a skončí.
- Výstup jde na stdout — lze ho přesměrovat: `PCCleaner.exe --auto-clean >> cleanup.log`

**Vhodné pro:**
- Naplánované úlohy (viz [kapitola 17](#17-naplánovaný-úklid))
- Skripty v CI/CD
- Automatizaci přes Task Scheduler nebo cron

> **Poznámka:** Headless mód záměrně přeskočí cleanery vyžadující správce, i když aplikaci spustíš jako správce. Je to bezpečnostní opatření — pro admin cleanery v headless módu spusť TUI jako správce a nastav scheduled task s elevated pravami.

---

## 17. Naplánovaný úklid

Otevři **File › Schedule Auto-Clean...**

### Windows (Task Scheduler)

```
┌─ Auto-Clean Schedule ───────────────────────────────────────────┐
│  Task:   PCCleanerAutoClean                                     │
│  Status: Scheduled (weekly, Sunday 09:00)                       │
│                                                                 │
│  Runs recommended cleaners silently via --auto-clean.           │
│           [ Create weekly task ] [ Remove task ] [ Close ]      │
└─────────────────────────────────────────────────────────────────┘
```

- **Create weekly task** — vytvoří úlohu v Plánovači úloh Windows. Spouští se každou **neděli v 9:00**.
- **Remove task** — odstraní úlohu.

> **Důležité:** Naplánovaný úklid funguje pouze s kompilovanou binárkou (`.exe`). Při spuštění přes `dotnet run` dialog zobrazí varování.

### Linux (systemd timer)

Na Linuxu se vytvoří systemd user timer:

```
~/.config/systemd/user/pc-cleaner.service
~/.config/systemd/user/pc-cleaner.timer
```

- Spouští se každou **neděli v 9:00** v rámci uživatelské systemd session.
- Vyžaduje běžící systemd user session (GNOME, KDE).
- Zkontroluj stav: `systemctl --user status pc-cleaner.timer`

---

## 18. Automatická aktualizace

Při každém startu aplikace **na pozadí tiše zkontroluje** GitHub Releases. Pokud je dostupná nová verze:

1. Ve výstupním panelu se zobrazí: `★  Update available: v1.0.3  —  File › Check for Updates`
2. Status bar změní `Freed: —` na `★ v1.0.3 ready`
3. Otevři **File › Check for Updates** a klikni **Download & Install**

### Průběh aktualizace

**Windows:**
```
Updating to v1.0.3...
Downloading...
Preparing update script...
```
Po stažení se aplikace ukončí a helper batch skript nahradí `.exe` novou verzí. Znovu ji otevři ručně.

**Linux:**
```
Updating to v1.0.3...
Downloading...
Applying update...
```
Binárka se přepíše přímo, nová verze se spustí okamžitě.

---

## 19. Tipy a doporučení

### Bezpečný první postup

1. Zaškrtni cleanery které chceš použít — přesuň focus na checkbox pro zobrazení popisu v info panelu.
2. Klikni **Scan (preview)** — nic se nesmaže, jen uvidíš co by se smazalo.
3. Zkontroluj výstup. Pokud vidíš `✗ N errors`, klikni na checkbox daného cleaneru pro zobrazení jeho popisu a zvaž přeskočení.
4. Pokud vypadá výstup v pořádku, klikni **Run clean** a potvrd.

### Zavři prohlížeče před čistěním

Browser cleaner funguje lépe když jsou Chrome/Firefox/Edge zavřené. Pokud běží, aplikace zobrazí varování:
```
⚠  Browser process(es) are running: chrome. Close browsers first.
```
Čistění sice proběhne, ale část souborů může být zamčena a přeskočena.

### Admin cleanery

Cleanery označené `admin!` jsou zašedlé a nelze je zaškrtnout bez správcovských práv. Pokud je potřebuješ, použij **File › Restart as Administrator**. Windows zobrazí UAC dialog.

### Duplicate files cleaner

Tento cleaner (**manual**) prohledává složku `Documents` a maže soubory kde se shoduje **velikost i SHA-256 hash**. Zachová vždy první nalezený soubor ze skupiny duplikátů. **Vždy nejdřív použij Scan (preview)** — osobní soubory se špatně obnovují.

### Steam cache

Po smazání Steam shader cache očekávej **sekání a koktání v hrách** při prvním spuštění — shadery se znovu kompilují. Je to dočasné, při dalším startu hry bude vše normálně.

### Věkový filtr pro každodenní použití

Pokud appku používáš každý den, nastav Age na `1` nebo `2` hodiny — budeš mazat jen skutečně starý odpad, ne soubory které ses teprve před chvílí vytvořil.

### Headless čistění ručně

Pokud nechceš naplánovanou úlohu ale občas chceš spustit čistění bez klikání:
```powershell
PCCleaner.exe --auto-clean --age-hours 72
```
Uloží report, nevyžaduje žádnou interakci.

---

*PC Cleaner · soda144p · github.com/Sodicek/PC-Cleaner-ghetto*
