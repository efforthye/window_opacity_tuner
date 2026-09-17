#!/usr/bin/env bash
#
# Builds a Windows .exe. Runs from WSL or Linux as well as from Windows —
# `dotnet publish -r win-x64` cross-compiles, it does not need a Windows host.
#
#   ./build.sh                 framework-dependent  (~1 MB exe, needs .NET 8 Desktop Runtime)
#   ./build.sh --standalone    self-contained       (~70 MB exe, no prerequisites)
#
set -euo pipefail

cd "$(dirname "$0")"

PROJECT="src/WindowOpacityTuner/WindowOpacityTuner.csproj"
STANDALONE=0
OUT="dist"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --standalone|--self-contained) STANDALONE=1; shift ;;
    -o|--output) OUT="$2"; shift 2 ;;
    -h|--help) sed -n '2,10p' "$0"; exit 0 ;;
    *) echo "unknown option: $1" >&2; exit 2 ;;
  esac
done

COMMON=(
  -c Release
  -r win-x64
  -p:PublishSingleFile=true
  -o "$OUT"
)

if [[ $STANDALONE -eq 1 ]]; then
  echo "==> self-contained build (no .NET runtime required on the target machine)"
  # Compression is only supported in self-contained single-file bundles.
  dotnet publish "$PROJECT" "${COMMON[@]}" --self-contained true -p:EnableCompressionInSingleFile=true
else
  echo "==> framework-dependent build (requires the .NET 8 Desktop Runtime)"
  dotnet publish "$PROJECT" "${COMMON[@]}" --self-contained false
fi

echo
echo "Done:"
ls -lh "$OUT"/WindowOpacityTuner.exe
