# PC Cleaner Ghetto

A cross-platform PC cleaner with an animated terminal UI. Cleans temp files, browser caches, crash dumps, developer tool caches, Steam shader cache, duplicate files, and more — with a preview mode and confirmation step before touching anything real.

Made by **soda144p**.

## Download

Grab the latest binary from [Releases](https://github.com/Sodicek/PC-Cleaner-ghetto/releases/latest) — no .NET SDK required:

| Platform | File |
|----------|------|
| Windows  | `PCCleaner-win-x64.exe` |
| Linux x64 | `PCCleaner-linux-x64` |

```bash
# Linux: make executable and run
chmod +x PCCleaner-linux-x64
./PCCleaner-linux-x64
```

## Features

- Animated ASCII splash screen with two-layer parallax starfield
- Live streaming output while cleaning
- **Disk freed counter** in status bar — current run and all-time total
- **Disk overview dialog** — File › Disk Overview, shows top home folders by size
- **Auto-update** — checks GitHub Releases on startup, downloads and applies in one click
- Settings persistence (language, age filter, selected cleaners)
- Admin indicator — admin-only cleaners are disabled when not elevated
- Localized confirm dialog — type `CLEAN` / `CISTIT` to confirm real deletion
- **Scheduled auto-clean** — Windows Task Scheduler or Linux systemd user timer
- Czech / English language toggle (File › Language)
- Preview (dry-run) mode — scans without deleting anything
- `--auto-clean` headless mode for scheduled/scripted use
- Clean run reports saved to `%LocalAppData%\pc-cleaner-ghetto\reports\`

## Requirements

- Windows, Linux, or macOS
- A proper terminal emulator (Windows Terminal recommended; Konsole / GNOME Terminal on Linux)
- .NET 8 SDK only needed if building from source

## Running from source

```powershell
# Windows
dotnet run --project PCCleaner.Desktop

# Linux / macOS
dotnet run --project PCCleaner.Desktop
```

Or build a Release binary:

```bash
# Linux self-contained single-file
dotnet publish PCCleaner.Desktop -c Release -r linux-x64 \
  --self-contained true -p:PublishSingleFile=true -o out/linux

chmod +x out/linux/PCCleaner.Desktop
./out/linux/PCCleaner.Desktop
```

## CLI options

```
--preview, --dry-run, -n        Preview only — nothing is deleted
--age-hours <hours>             Skip files newer than N hours (default: 24)
--include-recent                Disable the age safety filter
--lang <en|cs>                  Set language (English or Czech)
--auto-clean                    Headless mode: run recommended cleaners silently and exit
```

## Cleaners

| # | Cleaner | Platform | Recommended |
|---|---------|----------|-------------|
| 1 | User temporary files | All | Yes |
| 2 | Safe Linux/macOS cache buckets (thumbnails, fontconfig, shaders, pip) | Linux/macOS | Yes |
| 3 | Windows temporary files | Windows | Yes |
| 4 | Crash dumps | Windows | Yes |
| 5 | Windows log files | Windows | Yes |
| 6 | Browser caches (Chrome, Edge, Brave, Opera GX, Chromium, Vivaldi, Firefox — native + Flatpak) | All | Yes |
| 7 | macOS user logs | macOS | Yes |
| 8 | Thumbnail cache | Windows | Yes |
| 9 | Recent items | Windows | Yes |
| 10 | Recycle Bin | Windows | Yes |
| 11 | App startup entry | Windows | Yes |
| 12 | Windows component store cleanup | Windows | No |
| 13 | Duplicate files in Documents | All | No |
| 14 | PC Cleaner scheduled task | Windows | No |
| 15 | Steam shader & download cache | All | No |
| 16 | Developer tool caches (npm, pip, yarn, cargo, Gradle) | All | Yes |
| 17 | Flatpak unused runtimes | Linux | Yes |
| 18 | VS Code orphaned workspace storage | All | No |

Only cleaners supported on the current OS are shown. Admin-only cleaners are disabled unless running as Administrator / root.

## Scheduled auto-clean

**File › Schedule Auto-Clean** in the TUI creates a recurring job that runs `--auto-clean` every Sunday at 09:00.

- **Windows** — Windows Task Scheduler (`schtasks`), requires the compiled `.exe`
- **Linux** — systemd user timer (`~/.config/systemd/user/pc-cleaner.timer`), requires a running systemd user session

## Architecture

```
PCCleaner.Core/        Cleaner engine, OS detection, file helpers, all cleaner implementations
PCCleaner.Desktop/     Terminal.Gui TUI frontend (animated, cross-platform)
PCCleanerTests/        94 unit tests covering core logic and platform selection
```

Settings: `%AppData%\pc-cleaner-ghetto\settings.json` / `~/.config/pc-cleaner-ghetto/settings.json`
Reports: `%LocalAppData%\pc-cleaner-ghetto\reports\`

## Tests

```bash
dotnet test PCCleanerTests
```

## Auto-update

On startup the app silently checks `github.com/Sodicek/PC-Cleaner-ghetto` for a newer release. If one is found, a banner appears in the output panel and **File › Check for Updates** opens a download dialog.

- **Windows** — downloads new `.exe`, applies via helper batch script after exit
- **Linux** — overwrites binary in-place, relaunches the new version immediately
