[CmdletBinding()]
param(
    [string]$Runtime = "osx-x64",
    [string]$Configuration = "Release",
    [switch]$SelfContained
)

$script = Join-Path $PSScriptRoot "smoke-publish.ps1"
& $script -Runtime $Runtime -ArtifactName "macos-smoke" -Configuration $Configuration -SelfContained:$SelfContained
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
