#!/usr/bin/env bash
#
# Installs the published exe somewhere Windows will let you pin it, from WSL.
#
# Windows will not pin a program that lives on a network path, and the WSL share
# (\\wsl.localhost\...) is one — run dist/WindowOpacityTuner.exe over that share and
# the taskbar right-click menu offers nothing but "Close window". This copies the exe
# to %LOCALAPPDATA%\Programs and adds a Start menu shortcut.
#
# Re-run it after every build to update the installed copy.
set -euo pipefail

cd "$(dirname "$0")"

SOURCE="${1:-dist/WindowOpacityTuner.exe}"

if [[ ! -f "$SOURCE" ]]; then
  echo "not found: $SOURCE — run ./build.sh first" >&2
  exit 1
fi

WIN_USER="$(cmd.exe /c 'echo %USERNAME%' 2>/dev/null | tr -d '\r\n')"
DEST="/mnt/c/Users/${WIN_USER}/AppData/Local/Programs/WindowOpacityTuner"

mkdir -p "$DEST"
cp "$SOURCE" "$DEST/WindowOpacityTuner.exe"
echo "installed: C:\\Users\\${WIN_USER}\\AppData\\Local\\Programs\\WindowOpacityTuner\\WindowOpacityTuner.exe"

# The shortcut has to be made by Windows itself; WScript.Shell is the only thing
# that writes a .lnk, so this hops over to PowerShell for that one step.
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command '
$exe = Join-Path $env:LOCALAPPDATA "Programs\WindowOpacityTuner\WindowOpacityTuner.exe"
$lnk = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs\Window Opacity Tuner.lnk"
$shell = New-Object -ComObject WScript.Shell
$link = $shell.CreateShortcut($lnk)
$link.TargetPath = $exe
$link.WorkingDirectory = (Split-Path $exe)
$link.IconLocation = "$exe,0"
$link.Description = "Adjust the opacity of any window"
$link.Save()
Write-Output "shortcut: $lnk"
' 2>/dev/null | tr -d '\r'

echo
echo 'Pin it: Start menu -> "Window Opacity Tuner" -> right-click -> Pin to taskbar.'
