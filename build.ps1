<#
.SYNOPSIS
    Builds WindowOpacityTuner.exe.

.EXAMPLE
    .\build.ps1
    Framework-dependent build: ~1 MB exe, needs the .NET 8 Desktop Runtime installed.

.EXAMPLE
    .\build.ps1 -Standalone
    Self-contained build: ~70 MB exe, runs on any Windows 10/11 machine as-is.
#>
[CmdletBinding()]
param(
    [switch]$Standalone,
    [string]$Output = 'dist'
)

$ErrorActionPreference = 'Stop'
Set-Location -Path $PSScriptRoot

$project = 'src/WindowOpacityTuner/WindowOpacityTuner.csproj'

$args = @(
    'publish', $project,
    '-c', 'Release',
    '-r', 'win-x64',
    '-p:PublishSingleFile=true',
    '-p:EnableCompressionInSingleFile=true',
    '-o', $Output,
    '--self-contained', $(if ($Standalone) { 'true' } else { 'false' })
)

if ($Standalone) {
    Write-Host '==> self-contained build (no .NET runtime required on the target machine)'
} else {
    Write-Host '==> framework-dependent build (requires the .NET 8 Desktop Runtime)'
}

& dotnet @args
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }

Write-Host ''
Get-Item (Join-Path $Output 'WindowOpacityTuner.exe') | Format-List Name, Length, FullName
