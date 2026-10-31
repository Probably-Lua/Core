#!/usr/bin/env bash
set -euo pipefail

root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
game="${1:-${PROBABLY_STOLEN_DIR:-$HOME/.local/share/Steam/steamapps/common/Probably Stolen}}"

if ! command -v dotnet >/dev/null 2>&1; then
    echo 'Missing .NET SDK (dotnet).' >&2
    exit 1
fi

if [[ ! -f "$game/Probably Stolen_Data/Managed/Assembly-CSharp.dll" ]]; then
    echo "Game assembly not found under: $game" >&2
    echo "Usage: $0 '/path/to/Probably Stolen'" >&2
    exit 1
fi

game="$(cd -- "$game" && pwd)"
dotnet build "$root/ProbablyLua.csproj" -c Release "-p:GameManagedDir=$game/Probably Stolen_Data/Managed"

merged="$root/bin/Release/netstandard2.1/standalone/ProbablyLua.dll"
if [[ ! -s "$merged" ]]; then
    echo 'Standalone DLL missing; check ILRepack target output.' >&2
    exit 1
fi

patched="$root/bin/Release/netstandard2.1/standalone/ProbablyLua.patched.dll"
dotnet run --project "$root/tools/PatchMoonSharp/PatchMoonSharp.csproj" \
    -c Release -- \
    "$merged" \
    "$patched" \
    "$game/Probably Stolen_Data/Managed"

if [[ ! -s "$patched" ]]; then
    echo 'Patched standalone DLL missing.' >&2
    exit 1
fi

package="$root/dist/ProbablyLua"
rm -rf -- "$package"
mkdir -p -- "$package"

cp -- "$patched" "$package/ProbablyLua.dll"
cp -- "$root/manifest.xml" "$package/manifest.xml"

cp -- "$root/LICENSE" "$package/LICENSE"
cp -- "$root/THIRD_PARTY_LICENSES/MoonSharp.LICENSE" "$package/MoonSharp.LICENSE"

cp -- "$root/logo.jpg" "$package/logo.jpg"

printf 'Packaged: %s\n' "$package"
