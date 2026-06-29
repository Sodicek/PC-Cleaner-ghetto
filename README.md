# PC Cleaner

[![Latest Release](https://img.shields.io/github/v/release/Sodicek/PC-Cleaner?style=flat-square&color=blue)](https://github.com/Sodicek/PC-Cleaner/releases/latest)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-purple?style=flat-square)](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
[![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20Linux%20%7C%20macOS-lightgrey?style=flat-square)](https://github.com/Sodicek/PC-Cleaner/releases/latest)
[![Tests](https://img.shields.io/badge/tests-189%20passing-brightgreen?style=flat-square)](https://github.com/Sodicek/PC-Cleaner)

A cross-platform terminal PC cleaner with an animated TUI. Cleans temp files, browser caches, developer tool caches, duplicate files, and more — with a preview mode and confirmation step before touching anything real.

## Download

Grab the latest binary from [Releases](https://github.com/Sodicek/PC-Cleaner/releases/latest) — no .NET SDK required:

| Platform  | File |
|-----------|------|
| Windows   | `PCCleaner-win-x64.exe` |
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
- **Disk overview** — `File › Disk Overview` shows top home folders by size
- **Keyboard shortcuts overlay** — press `F1` or `?` to see all shortcuts
- **Changelog viewer** — `File › Changelog...` shows release notes inside the app
- **Auto-update** — checks GitHub Releases on startup, downloads and applies in one click
- **Cleaner info panel** — shows name, description and risk level for each cleaner
- **One-time admin prompt** — lists disabled cleaners and offers Restart as Admin on Windows
- Settings persistence (language, age filter, selected cleaners)
- Czech / English language toggle (`File › Language`)
- Preview (dry-run) mode — scans without deleting anything
- `--auto-clean` headless mode for scheduled / scripted use
- Scheduled auto-clean via Windows Task Scheduler or Linux systemd user timer
- Clean run reports saved locally

## Cleaners

| # | Cleaner | Platform | Recommended |
|---|---------|----------|-------------|
| 1 | User temporary files | All | Yes |
| 2 | Safe Linux/macOS cache buckets (thumbnails, fontconfig, shaders, pip) | Linux/macOS | Yes |
| 3 | Windows temporary files | Windows | Yes (admin) |
| 4 | Crash dumps | Windows | Yes |
| 5 | Windows log files | Windows | Yes |
| 6 | Browser caches (Chrome, Edge, Brave, Opera GX, Chromium, Vivaldi, Firefox — native + Flatpak) | All | Yes |
| 7 | macOS user logs | macOS | Yes |
| 8 | Thumbnail cache | Windows | Yes |
| 9 | Recent items | Windows | Yes |
| 10 | Recycle Bin | Windows | Yes |
| 11 | Trash / Bin (`~/.local/share/Trash/` or `~/.Trash/`) | Linux/macOS | Yes |
| 12 | Linux recent files list (`recently-used.xbel`) | Linux | No |
| 13 | App startup entry | Windows | Yes |
| 14 | Windows component store cleanup | Windows | No (admin) |
| 15 | Duplicate files in Documents | All | No |
| 16 | PC Cleaner scheduled task | Windows | No |
| 17 | Windows Prefetch cache | Windows | No (admin) |
| 18 | Steam shader & download cache | All | No |
| 19 | Developer tool caches (npm, pip, yarn, cargo, Gradle) | All | Yes |
| 20 | Flatpak unused runtimes | Linux | Yes |
| 21 | VS Code orphaned workspace storage | All | No |

Only cleaners supported on the current OS are shown. Admin-only cleaners are disabled unless running as Administrator / root.

## CLI options

```
--preview, --dry-run, -n        Preview only — nothing is deleted
--age-hours <hours>             Skip files newer than N hours (default: 24)
--include-recent                Disable the age safety filter
--lang <en|cs>                  Set language (English or Czech)
--auto-clean                    Headless mode: run recommended cleaners silently and exit
```

## Scheduled auto-clean

`File › Schedule Auto-Clean` creates a recurring job that runs `--auto-clean` every Sunday at 09:00.

- **Windows** — Windows Task Scheduler (`schtasks`), requires the compiled `.exe`
- **Linux** — systemd user timer (`~/.config/systemd/user/pc-cleaner.timer`)

## Requirements

- Windows, Linux, or macOS
- A proper terminal emulator (Windows Terminal recommended; Konsole / GNOME Terminal on Linux)
- .NET 8 SDK only needed if building from source

## Building from source

```bash
# Run
dotnet run --project PCCleaner.App

# Run tests
dotnet test PCCleaner.Tests

# Self-contained single-file binary (Linux example)
dotnet publish PCCleaner.App -c Release -r linux-x64 \
  --self-contained true -p:PublishSingleFile=true -o out/linux

chmod +x out/linux/PCCleaner.App
./out/linux/PCCleaner.App
```

## Architecture

```
PCCleaner.Core/    Cleaner engine, OS detection, file helpers, all cleaner implementations
PCCleaner.App/     Terminal.Gui TUI frontend (animated, cross-platform)
PCCleaner.Tests/   189 unit tests covering core logic and platform selection
```

## Auto-update

On startup the app silently checks GitHub Releases for a newer version. If one is found, a banner appears in the output panel and `File › Check for Updates` opens a download dialog.

- **Windows** — downloads new `.exe`, applies via helper batch script after exit
- **Linux** — overwrites binary in-place and relaunches immediately
