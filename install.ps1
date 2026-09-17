<#
.SYNOPSIS
    Installs Window Opacity Tuner to a local folder so Windows will let you pin it.

.DESCRIPTION
    Windows refuses to pin a program that lives on a network path, and the WSL share
    (\\wsl.localhost\...) is a network path as far as Explorer is concerned — which is
    why right-clicking the taskbar button only offers "Close window".

    This copies the published exe to %LOCALAPPDATA%\Programs\WindowOpacityTuner and
    adds a Start menu shortcut. Launch it from there and "Pin to taskbar" appears.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .\install.ps1
#>
[CmdletBinding()]
param(
    [string]$Source = (Join-Path $PSScriptRoot 'dist\WindowOpacityTuner.exe'),
    [string]$Destination = (Join-Path $env:LOCALAPPDATA 'Programs\WindowOpacityTuner')
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $Source)) {
    throw "Not found: $Source`nBuild it first (build.ps1 or build.sh), then run this again."
}

New-Item -ItemType Directory -Path $Destination -Force | Out-Null

$exe = Join-Path $Destination 'WindowOpacityTuner.exe'
Copy-Item -LiteralPath $Source -Destination $exe -Force
Write-Host "Installed to $exe"

$startMenu = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs'
$shortcut = Join-Path $startMenu 'Window Opacity Tuner.lnk'

$shell = New-Object -ComObject WScript.Shell
$link = $shell.CreateShortcut($shortcut)
$link.TargetPath = $exe
$link.WorkingDirectory = $Destination
$link.IconLocation = "$exe,0"
$link.Description = 'Adjust the opacity of any window'
$link.Save()

Write-Host "Start menu shortcut: $shortcut"
Write-Host ''
Write-Host 'To pin it: open Start, find "Window Opacity Tuner", right-click it and choose'
Write-Host 'Pin to taskbar. Or launch it from there and right-click its taskbar button.'
