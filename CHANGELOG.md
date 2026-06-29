# Changelog

## Unreleased

## v1.0.2

### Stability & polish

- **Per-file error log** — instead of a bare "3 errors" count, each failure now records the filename and OS error reason in the output (e.g. `✗ cache.db: Access to the path is denied`). Capped at 20 notes per cleaner to keep output readable.
- **Locked file retry** — when a file delete is blocked (`UnauthorizedAccessException` or `IOException` sharing violation), the cleaner waits 500 ms and retries once before giving up. Catches most transient antivirus/indexer locks without failing the whole run.
- **Keyboard shortcuts overlay** — press `F1` or `?` at any time to open a popup listing all keyboard shortcuts. `F1` also appears in the status bar as a permanent hint.
- **Changelog viewer** — `File › Changelog...` opens a scrollable view of `CHANGELOG.md` so you can review what changed without leaving the app.
- **Error count in status bar** — after a run that produced failures, the status bar shows `✗ N errors` so problems are visible at a glance even if the output has scrolled.
- **Symlink safety** — symlinks and reparse points are skipped in `FileDeleteHelper` for both files and directories (already in v1.0.1 but now documented here).

### Admin warning (Windows)

- **One-time admin prompt on first Run Clean** — if you click "Run clean" without administrator rights on Windows, a dialog lists which cleaners are disabled and offers [Restart as Admin] / [Continue anyway] / [Cancel]. Only shown once per session so it never nags.

### New cleaners (Linux / macOS)

- **Trash / Bin** (`UnixTrashCleaner`) — empties `~/.local/share/Trash/` (Linux XDG) or `~/.Trash/` (macOS). Recommended. Equivalent to the Windows Recycle Bin cleaner.
- **Linux recent files list** (`LinuxRecentFilesCleaner`) — deletes `~/.local/share/recently-used.xbel`, which clears the recent-file history in GNOME Files, GIMP, Inkscape, and other GTK apps. Opt-in (not recommended by default).

### UI polish

- **Selected cleaner count in status bar** — the status bar now shows `4 / 18 selected` and updates live as you check or uncheck cleaners, so you always know what's about to run without counting manually.
- **Drive free space after clean** — the summary line at the end of a real clean now appends `Drive free: 47.3 GB` so you can see the before/after impact without leaving the app.
- **Cleaner info panel** — focusing any cleaner checkbox in the list updates a small panel at the bottom of the Controls column with the cleaner's name, description, and risk level. No more guessing what "manual" means.

### Fixes

- **Admin warning dialog** — previously listed all admin-requiring cleaners regardless of the current OS; now filters by `IsSupported(c.Platform)` so Linux and macOS users are never shown Windows-only cleaners in the dialog.

### Settings persistence fixes

- **Age hours field** — changing the value in the Age (hours) input now saves immediately on every keystroke, not only when a scan/clean is launched. Previously quitting after editing the field would lose the change.
- **Include recent files checkbox** — toggling this checkbox now saves immediately. Same bug as above.

### Security

- **Updater: batch script injection guard** — the asset name from the GitHub API response is now validated against an allowlist (`[a-zA-Z0-9._-]` only) before the Windows update batch script is written. A compromised asset name containing `"`, `&`, `|`, or other cmd metacharacters is rejected with an error before any file is downloaded.
- **Updater: batch `%` expansion** — percent signs in the executable and staged file paths are now escaped as `%%` in the generated `.cmd` file so cmd.exe does not expand environment variable references (e.g. `%WINDIR%`).
- **systemd unit file `%` specifier escape** — the executable path written to the `ExecStart=` line in the systemd service unit is now escaped (`%` → `%%`) to prevent systemd from interpreting specifiers such as `%h` (home directory) or `%u` (username) that could silently redirect execution to a different path.

### Tests

- 189 unit tests (up from 180), covering: updater asset name validation (known safe names pass; injection strings fail), systemd unit file path escaping (`%`-containing paths pass the newline guard; newline-containing paths are still rejected), all-cleaner metadata completeness (name/description/risk non-empty on all platforms), admin cleaner platform filter correctness, and drive snapshot returning valid free space.

### Project housekeeping

- Renamed solution from `PC Cleaner ghetto.sln` → `PCCleaner.sln`
- Renamed project `PCCleaner.Desktop` → `PCCleaner.App` (it's a TUI app, not a desktop GUI) — folder, `.csproj`, and namespace updated
- Renamed project `PCCleanerTests` → `PCCleaner.Tests` — folder, `.csproj`, and namespace updated to match dot-notation convention
- Fixed `InternalsVisibleTo` in `PCCleaner.Core.csproj` to reference new assembly names (`PCCleaner.App`, `PCCleaner.Tests`)
## v1.0.1

### New features
- **Windows Prefetch cache cleaner** — removes `.pf` files from `C:\Windows\Prefetch`; Windows rebuilds them on next app launch. Requires administrator rights.
- **Progressive Disk Overview** — `File › Disk Overview` now scans all home subfolders in parallel and updates the list in real time as each folder completes, instead of waiting for the full scan to finish.

### Fixes & improvements
- Cancel button now works correctly mid-run — pressing it during a scan or clean stops between cleaners without leaving the UI in a broken state.
- Output panel auto-scrolls to the latest line during long runs.
- Admin note moved into the output panel on startup instead of floating over the UI.
- Starfield and output panel no longer overlap on narrow terminals.

### Tests
- 140 unit tests (up from 94 in v1.0.0), covering Prefetch cleaner, progressive disk scan, cancel behaviour, and extended platform-selection edge cases.
