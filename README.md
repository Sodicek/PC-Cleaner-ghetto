# PC Cleaner Ghetto

A console-based Windows PC cleaner. Deletes temp files, browser caches, crash dumps, thumbnail databases, recent file history, and more — with a confirmation step before touching anything.

Made by **soda144p**.

## Requirements

- .NET 8 SDK

## Run

```
dotnet run --project "PC Cleaner ghetto"
```

Or build and run the exe directly after `dotnet build`.

## Cleaners

| # | Cleaner | Admin needed |
|---|---------|-------------|
| 1 | User temp files | No |
| 2 | Windows temp files | Yes |
| 3 | Crash dumps | No |
| 4 | Windows log files | Yes |
| 5 | Browser caches (Chrome, Edge, Brave, Opera, Firefox) | No |
| 6 | Thumbnail cache | No |
| 7 | Recent items | No |
| 8 | Recycle Bin | No |
| 9 | App startup entry | No |
| 10 | Windows component cleanup task | Yes |
| 11 | Duplicate files in Documents | No |

## Options

```
--preview, --dry-run, -n     Preview only — nothing is deleted
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

74 tests covering core logic, age filter, preview mode, file deletion, and pattern matching.
