# Roadmap — PCCleaner v1.0.2 → v1.0.9

## Přehled

| Verze | Téma | Klíčové featury | Odhad |
|-------|------|-----------------|-------|
| **1.0.2** | Stability | Symlink fix, per-file error log, retry, shortcuts overlay | ~2–3 hod |
| **1.0.3** | App Cleaners | Discord, Teams, Spotify, Zoom, Arc/Thorium | ~2 hod |
| **1.0.4** | Dev Tools | JetBrains, Java/Maven, Docker, Duplicate finder+ | ~3–4 hod |
| **1.0.5** | Windows System | Delivery Opt., Event Logs, Installer patches, GPU shader cache | ~3 hod |
| **1.0.6** | CLI | `--cleaner`, `--output json`, `--min-age-days`, exit kódy, `--list-cleaners` | ~3 hod |
| **1.0.7** | Reports & History | JSON/CSV export, 30denní součet, Report viewer, History dialog | ~3–4 hod |
| **1.0.8** | Profiles & Whitelist | Quick/Standard/Deep profily, whitelist, per-cleaner age, progress bar | ~4–5 hod |
| **1.0.9** | Insights | Health score, disk trend graf, granulární browser, Startup Manager | ~5 hod |

**Celkem: ~25–30 hodin práce**

---

## v1.0.2 — "Stability" ✅ HOTOVO

**Téma:** Opravit drobné problémy a dobrušovat co tam už je — žádné nové featury, jen solidnější základ.  
**Odhad:** ~2–3 hod  **Skutečnost:** ~4 hod

- ✅ **Symlink detekce** — přeskočit symlinky v `FileDeleteHelper` (bylo již v 1.0.1, zdokumentováno)
- ✅ **Per-file error log** — místo "3 errors" zobrazit přesně které soubory selhaly a proč (max 20 per cleaner)
- ✅ **Locked file retry** — při `Access Denied` nebo `IOException` (sharing violation) zkusit ještě jednou po 500 ms
- ✅ **Keyboard shortcuts overlay** — `F1` nebo `?` otevře popup se všemi zkratkami; F1 taky v status baru
- ✅ **Changelog viewer** — `File › Changelog...` zobrazí CHANGELOG.md přímo v aplikaci
- ✅ **Error count v status baru** — po runu s chybami status bar zobrazí `✗ N errors`
- ✅ **Build fix** — opraveno `InternalsVisibleTo` po přejmenování projektů (PCCleaner.App, PCCleaner.Tests)
- ✅ **Admin warning dialog** — při prvním "Run clean" bez práv správce se zobrazí dialog se seznamem zakázaných cleanerů a nabídkou [Restart as Admin] / [Continue] / [Cancel]; jednou za session
- ✅ **Trash / Bin cleaner** (Linux/macOS) — čistí `~/.local/share/Trash/` (Linux) nebo `~/.Trash/` (macOS)
- ✅ **Linux recent files cleaner** — maže `~/.local/share/recently-used.xbel` (opt-in)
- ✅ **Selected count v status baru** — `4 / 18 selected`, aktualizuje se živě při každém kliknutí na checkbox
- ✅ **Disk free po čistění** — za souhrnným řádkem se zobrazí `Drive free: 47.3 GB` (jen při reálném čistění, ne preview)
- ✅ **Info panel čistič** — při přechodu na checkbox se v Controls panelu zobrazí název, popis a míra rizika daného čistice
- ✅ **Fix: admin dialog filtruje podle platformy** — zobrazuje jen admin čistice podporované na aktuálním OS; Linux/macOS neuvidí Windows-only položky
- ✅ **Fix: Age hours a Include recent se ukládají okamžitě** — TextChanged / Toggled handler přidán pro obě pole; dříve se uložilo jen při scan/clean nebo togglenutí checkboxu čistice
- ✅ **Security: validace asset name v updateru** — název souboru z GitHub API prochází allowlist (`[a-zA-Z0-9._-]`) před zápisem do `.cmd` skriptu; zamezuje cmd injection při kompromitaci GitHub účtu
- ✅ **Security: escapování `%` v batch skriptu** — procenta v cestě binárky escapována jako `%%` aby cmd.exe nevyhodnocoval proměnné prostředí
- ✅ **Security: escapování `%` v systemd unit souboru** — cesta v `ExecStart=` má `%` → `%%` aby systemd nevyhodnocoval specifikátory jako `%h` nebo `%u`

---

## v1.0.3 — "App Cleaners"

**Téma:** Čtyři populární Windows appky + dva nové prohlížeče.  
**Odhad:** ~2 hod

### Nové cleanery

| Cleaner | Cesta | Platforma |
|---------|-------|-----------|
| **Discord cache** | `%AppData%\discord\Cache`, `Code Cache` | Windows |
| **Microsoft Teams cache** | `%AppData%\Microsoft\Teams\Cache` | Windows |
| **Spotify cache** | `%LocalAppData%\Spotify\Storage` | Windows |
| **Zoom cache** | `%AppData%\Zoom\data`, `logs` | Windows |

### Vylepšení stávajících

- **Browser cleaner** — přidat Arc (`%LocalAppData%\Packages\company.thebrowser.browser*`) a Thorium

---

## v1.0.4 — "Dev Tools"

**Téma:** Cache vývojářských nástrojů + rozšíření Duplicate finderu.  
**Odhad:** ~3–4 hod

### Nové cleanery

| Cleaner | Cesta | Platforma | Obtížnost |
|---------|-------|-----------|-----------|
| **JetBrains IDE caches** | `%APPDATA%\JetBrains\*\caches`, `system\`, `log\` (IntelliJ, Rider, PyCharm…) | All | Střední — dynamické cesty |
| **Java / Maven cache** | `~/.m2/repository`, JVM temp soubory | All | Nízká |
| **Docker cleanup** | nepoužívané images, zastavené kontejnery, dangling volumes (`docker system prune`) | All | Vysoká — process spawn |

### Vylepšení stávajících

- **Duplicate file finder** — rozšířit z `Documents` na `Desktop`, `Downloads`, `Pictures`, `Videos`; přidat volbu ve settings

---

## v1.0.5 — "Windows System"

**Téma:** Systémové Windows cleanery které uvolní nejvíc místa.  
**Odhad:** ~3 hod

### Nové cleanery

| Cleaner | Cesta | Poznámka |
|---------|-------|----------|
| **Delivery Optimization cache** | `%SystemRoot%\SoftwareDistribution\DeliveryOptimization` | Admin, typicky 500 MB–2 GB |
| **Windows Event Logs** | starší záznamy z Application/System/Security logů | Admin, `EventLog` API |
| **Windows Installer patches** | zastaralé `.msp` soubory v `%WINDIR%\Installer` | Nízká riziko |
| **GPU Shader cache** | NVIDIA `%LocalAppData%\NVIDIA\GLCache`, AMD `%LocalAppData%\AMD\GLCache` | Přegenerují se automaticky |

---

## v1.0.6 — "CLI"

**Téma:** Výkonné přepínače pro power usery a scripty.  
**Odhad:** ~3 hod

- `--cleaner <id,id,...>` — spustit jen konkrétní cleanery podle ID
- `--output json` — strojově čitelný výstup (pro skripty, CI)
- `--min-age-days <n>` — přívětivější alias pro `--age-hours` (24 h = 1 den)
- `--list-cleaners` — vypíše všechny cleanery jako JSON (id, name, platform, recommended)
- `--exclude <id,id,...>` — spustit vše KROMĚ zadaných
- Standardizované **exit kódy**: `0` = OK, `1` = nic nenalezeno, `2` = chyba

---

## v1.0.7 — "Reports & History"

**Téma:** Přehled o tom co bylo vyčištěno — export, součty, zobrazení v TUI.  
**Odhad:** ~3–4 hod

- **JSON / CSV export** — `--report-format json` nebo `csv`; soubor vedle textového reportu
- **Součtový řádek** — "Celkem uvolněno za posledních 30 dní: X GB" v reportu i TUI
- **Report viewer** — File › View Reports: seznam posledních reportů, otevření v TUI
- **History dialog** — File › Clean History: tabulka posledních N runů (datum, uvolněno, cleanery)

---

## v1.0.8 — "Profiles & Whitelist"

**Téma:** Čistění podle profilu + ochrana složek před smazáním.  
**Odhad:** ~4–5 hod

### Cleaner profily

| Profil | Co se čistí |
|--------|-------------|
| **Quick** | temp soubory, browser cache, thumbnails — hotovo za <30 s |
| **Standard** | doporučené cleanery (dnešní výchozí stav) |
| **Deep** | vše včetně dev cache, duplikáty, component store |
| **Custom** | ruční výběr jako dnes |

- Profil se uloží do `settings.json`
- `--profile quick/standard/deep` pro headless/auto-clean mode
- Rychlý přepínač v TUI nad checkboxy

### Whitelist

- File › Whitelist — přidat složky nebo soubory co se **nikdy** nemažou
- Kontrola před každým smazáním v `FileDeleteHelper`
- Uloženo v `settings.json`: `"whitelist": ["C:\\Projects\\...", ...]`
- V preview módu: "2 soubory přeskočeny (whitelist)"

### Další vylepšení

- **Per-cleaner age override** — `settings.json`: `"cleanerAgeOverrides": { "BrowserCacheCleaner": 1, "TempCleaner": 72 }`
- **Progress bar per cleaner** — vlastní progress místo jednoho globálního (vyžaduje změny v `CleanerRunner`)
- **Post-scan řazení** — cleanery seřazené od největší úspory po nejmenší

---

## v1.0.9 — "Insights"

**Téma:** Vizualizace stavu systému a inteligentní ovládání.  
**Odhad:** ~5 hod

- **System Health Score** — agregované skóre 0–100 v status baru (`Health: 74/100`); výpočet z odpadků, stáří posledního čistění, stavu Recycle Bin, počtu startup položek; tlačítko "Why?" s rozpisem
- **Disk Space Trend** — File › Disk Trend: ASCII line chart volného místa za posledních 30 dní; data v `%AppData%\pc-cleaner\disk-trend.json`
- **Granulární browser cleaning** — místo "vše nebo nic" výběr per browser: Cache (ON), Cookies (OFF), History (OFF), Session data (OFF)
- **Startup Manager** — File › Startup Manager: tabulka startup položek (jméno, cesta, stav); lze zakázat/povolit bez smazání; Windows: `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`
- **Odhad času per cleaner** — "~12s" vedle každého cleaneru dle průměru z předchozích runů; průměry ukládány do `settings.json`

---

## Budoucnost — v1.1.0+

Větší featury které nevejdou do 1.0.x:

| Téma | Co |
|------|----|
| **macOS & Linux cleaners** | Xcode, Homebrew, APT/dnf/Pacman, Snap, Journal |
| **Privacy Shield** | Clipboard history, Windows Timeline, shell history, Cortana |
| **Cloud cleaners** | OneDrive, Google Drive, Dropbox, iCloud, Nextcloud |
| **Localization** | DE, SK, PL jazyky; plain-text mód; barevná témata |
| **Windows Registry** | Registry cleaner, Windows.old, hiberfil.sys |
| **Automation** | Plugin API, daemon mode, system tray, web dashboard |
| **Distributions** | WinGet, Chocolatey, Flatpak, Homebrew tap, AUR |
| **Remote** | SSH remote clean, GitHub Actions integrace |
