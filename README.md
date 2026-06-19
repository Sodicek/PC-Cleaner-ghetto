# PC Cleaner Ghetto

A console-based PC cleaner for Windows, Linux. Deletes temp files, browser caches, Linux/macOS user caches, Windows cleanup targets, duplicate files, and more - with a confirmation step before touching anything.

Made by **soda144p**.

## Requirements

- .NET 8 SDK
- Windows, Linux, or macOS

Nobara Linux works like other Fedora-based Linux systems. The app uses the current user's temp folder plus targeted rebuildable cache buckets under `$XDG_CACHE_HOME` when set, otherwise `~/.cache`.

At startup, both the desktop GUI and console app detect the current operating system and load only cleaners that are supported on that platform. Windows-only cleaners are not offered on Linux/macOS, and Linux/macOS cache cleaners are not offered on Windows.

## Run

Desktop GUI:

```
dotnet run --project PCCleaner.Desktop
```

Console app:

```
dotnet run --project "PC Cleaner ghetto"
```

Or build and run the binary directly after `dotnet build`.

## Architecture

- `PCCleaner.Core` contains the shared cleaner engine, OS detection, reports, file deletion helpers, and all cleaner implementations.
- `PC Cleaner ghetto` is the console frontend.
- `PCCleaner.Desktop` is the Avalonia desktop frontend.
- `PCCleanerTests` tests the shared cleaner engine and platform selection logic.

## Cleaners

| # | Cleaner | Platform | Admin needed |
|---|---------|----------|-------------|
| 1 | User temp files | Windows/Linux/macOS | No |
| 2 | Safe Linux/macOS cache buckets | Linux/macOS | No |
| 3 | Windows temp files | Windows | Yes |
| 4 | Crash dumps | Windows | No |
| 5 | Windows log files | Windows | Yes |
| 6 | Browser caches (Chrome, Edge, Brave, Opera, Chromium, Vivaldi, Firefox) | Windows/Linux/macOS | No |
| 7 | macOS user logs | macOS | No |
| 8 | Thumbnail cache | Windows | No |
| 9 | Recent items | Windows | No |
| 10 | Recycle Bin | Windows | No |
| 11 | App startup entry | Windows | No |
| 12 | Windows component cleanup task | Windows | Yes |
| 13 | Duplicate files in Documents | Windows/Linux/macOS | No |

## Options

```
--preview, --dry-run, -n     Preview only - nothing is deleted
--age-hours <hours>          Skip files newer than N hours (default: 24)
--include-recent             Disable the age safety filter
--lang <en|cs>               Set language (English or Czech)
--skip-intro                 Skip the intro animation
--help, -h                   Show help
```

## Tests

```
dotnet test PCCleanerTests
```

Windows-side Linux smoke test:

```
powershell -ExecutionPolicy Bypass -File scripts/smoke-linux.ps1
```

Windows-side macOS smoke test:

```
powershell -ExecutionPolicy Bypass -File scripts/smoke-macos.ps1
```

Generic smoke publish for another runtime:

```
powershell -ExecutionPolicy Bypass -File scripts/smoke-publish.ps1 -Runtime linux-arm64
```

These run the tests, then cross-publish the console and Avalonia desktop apps into `artifacts/`. Add `-SelfContained` if you want the publish output to include the .NET runtime for the target OS.

Cross-publishing proves restore/build/publish for the target runtime from this machine. Before distributing, still launch the output on a real Linux/macOS machine or VM to catch native desktop dependencies, display-server differences, and file-permission behavior.

Run one smoke script at a time. Parallel cross-publishes can race while updating .NET restore files under `obj/`.

Tests cover core logic, age filter, preview mode, file deletion, pattern matching, localization, and platform support.
