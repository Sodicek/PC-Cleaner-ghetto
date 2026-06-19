[CmdletBinding()]
param(
    [string]$Runtime = "linux-x64",
    [string]$ArtifactName = "",
    [string]$Configuration = "Release",
    [switch]$SelfContained
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($ArtifactName)) {
    $ArtifactName = "$Runtime-smoke"
}

$artifactsRoot = Join-Path $repoRoot "artifacts/$ArtifactName"
$consoleProject = Join-Path $repoRoot "PC Cleaner ghetto/PC Cleaner ghetto.csproj"
$desktopProject = Join-Path $repoRoot "PCCleaner.Desktop/PCCleaner.Desktop.csproj"
$testProject = Join-Path $repoRoot "PCCleanerTests/PCCleanerTests.csproj"
$consoleOutput = Join-Path $artifactsRoot "console"
$desktopOutput = Join-Path $artifactsRoot "desktop"
$selfContainedValue = if ($SelfContained) { "true" } else { "false" }

function Invoke-DotNet {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments -join ' ') failed with exit code $LASTEXITCODE"
    }
}

Write-Host "PC Cleaner cross-platform smoke test"
Write-Host "Runtime: $Runtime"
Write-Host "Configuration: $Configuration"
Write-Host "Self-contained: $selfContainedValue"
Write-Host ""

Write-Host "1/3 Running unit tests with simulated Windows/Linux/macOS coverage..."
Invoke-DotNet @("test", $testProject, "-c", $Configuration)

Write-Host ""
Write-Host "2/3 Cross-publishing console app for $Runtime..."
Invoke-DotNet @("publish", $consoleProject, "-c", $Configuration, "-r", $Runtime, "--self-contained", $selfContainedValue, "-o", $consoleOutput)

Write-Host ""
Write-Host "3/3 Cross-publishing TUI desktop app for $Runtime..."
Invoke-DotNet @("publish", $desktopProject, "-c", $Configuration, "-r", $Runtime, "--self-contained", $selfContainedValue, "-o", $desktopOutput)

$requiredFiles = @(
    (Join-Path $consoleOutput "PC Cleaner ghetto.dll"),
    (Join-Path $consoleOutput "PC Cleaner ghetto.runtimeconfig.json"),
    (Join-Path $desktopOutput "PCCleaner.Desktop.dll"),
    (Join-Path $desktopOutput "PCCleaner.Desktop.runtimeconfig.json")
)

foreach ($file in $requiredFiles) {
    if (-not (Test-Path -LiteralPath $file)) {
        throw "Expected publish output was not created: $file"
    }
}

Write-Host ""
Write-Host "Smoke test passed."
Write-Host "Console output: $consoleOutput"
Write-Host "Desktop output: $desktopOutput"
