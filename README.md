# PC Cleaner

A cross-platform PC cleaner with an animated terminal UI (TUI). Cleans temp files, browser caches, crash dumps, duplicate files, and more — with a confirmation step before touching anything real.

Made by **soda144p**.

## Features

- Animated ASCII splash screen + two-layer parallax starfield
- Live streaming output while cleaning
- Disk freed counter in the status bar
- Settings persistence (language, age filter, selected cleaners)
- Admin indicator — admin-only cleaners are hidden when not elevated
- Localized confirm dialog (type `CLEAN` / `CISTIT` to confirm)
- Scheduled auto-clean via Windows Task Scheduler
- Czech / English language toggle
- Preview (dry-run) mode — scans without deleting anything
- `--auto-clean` headless mode for scheduled/scripted use

## Requirements

- .NET 8 SDK (or Runtime for the compiled binary)
- Windows, Linux, or macOS
- A proper terminal emulator (Windows Terminal recommended on Windows)

## Quick start

**Windows** — build once, then double-click or run from Windows Terminal:

```powershell
dotnet build PCCleaner.Desktop -c Release
.\start.ps1
```

**Linux / macOS:**

```bash
dotnet build PCCleaner.Desktop -c Release
chmod +x start.sh && ./start.sh
```

Or run directly without building:

```bash
dotnet run --project PCCleaner.Desktop
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

| # | Cleaner | Platform | Admin |
|---|---------|----------|-------|
| 1 | User temp files | All | No |
| 2 | Unix/macOS user cache | Linux/macOS | No |
| 3 | Windows temp files | Windows | Yes |
| 4 | Crash dumps | Windows | No |
| 5 | Windows log files | Windows | Yes |
| 6 | Browser caches (Chrome, Edge, Brave, Opera GX, Chromium, Vivaldi, Firefox) | All | No |
| 7 | macOS user logs | macOS | No |
| 8 | Thumbnail cache | Windows | No |
| 9 | Recent items | Windows | No |
| 10 | Recycle Bin | Windows | No |
| 11 | App startup entries | Windows | No |
| 12 | Windows component store cleanup | Windows | Yes |
| 13 | Duplicate files in Documents | All | No |
| 14 | PC Cleaner scheduled task | Windows | No |

Only cleaners supported on the current OS are shown. Admin-only cleaners are disabled unless running as Administrator / root.

## Architecture

```
PCCleaner.Core/        Cleaner engine, OS detection, file helpers, all cleaner implementations
PCCleaner.Desktop/     Terminal.Gui TUI frontend (animated, cross-platform)
PCCleanerTests/        Unit tests for core logic and platform selection
```

Settings are saved to `%AppData%\PC Cleaner\settings.json` (Windows) or `~/.config/PC Cleaner/settings.json` (Linux/macOS).

Clean reports are saved to `%AppData%\PC Cleaner\` / `~/.config/PC Cleaner/`.

## Scheduled auto-clean (Windows)

From the TUI: **File › Schedule Auto-Clean** — creates a Windows Task Scheduler entry that runs `--auto-clean` every Sunday at 09:00. Requires the compiled `.exe` (not `dotnet run`).

Manual headless run:

```
PCCleaner.Desktop.exe --auto-clean
```

## Tests

```
dotnet test PCCleanerTests
```

Cross-platform smoke publish (Windows):

```powershell
powershell -ExecutionPolicy Bypass -File scripts/smoke-linux.ps1
powershell -ExecutionPolicy Bypass -File scripts/smoke-macos.ps1
```

Tests cover core logic, age filter, preview mode, file deletion, pattern matching, localization, and platform support.
