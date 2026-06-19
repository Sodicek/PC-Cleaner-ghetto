#!/usr/bin/env bash
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
EXE="$SCRIPT_DIR/PCCleaner.Desktop/bin/Release/net8.0/PCCleaner.Desktop"

if [ -f "$EXE" ]; then
    "$EXE"
else
    dotnet run --project "$SCRIPT_DIR/PCCleaner.Desktop" -c Release
fi
