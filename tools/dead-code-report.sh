#!/usr/bin/env bash
set -euo pipefail

root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
game="${1:-${PROBABLY_STOLEN_DIR:-$HOME/.local/share/Steam/steamapps/common/Probably Stolen}}"
managed="$game/Probably Stolen_Data/Managed"

if ! command -v dotnet >/dev/null 2>&1; then
    echo 'Missing .NET SDK (dotnet).' >&2
    exit 1
fi
if [[ ! -f "$managed/Assembly-CSharp.dll" ]]; then
    echo "Missing game assembly in: $managed" >&2
    exit 1
fi

# Debug skips the Release-only ILRepack target and focuses on C# diagnostics.
# This is a candidate list, not a proof that public/internal API members are dead.
log="$root/dead-code-report.log"
dotnet build "$root/ProbablyLua.csproj" -c Debug     "-p:GameManagedDir=$managed"     -p:RunAnalyzers=true     -p:RunAnalyzersDuringBuild=true     -p:EnforceCodeStyleInBuild=true     -p:EnableNETAnalyzers=true     -p:AnalysisLevel=latest     -v:normal 2>&1 | tee "$log"

printf '\n=== Suspected unused PRIVATE members ===\n'
if command -v rg >/dev/null 2>&1; then
    rg 'IDE0051|IDE0052|CS0169|CS0414' "$log" || true
else
    grep -E 'IDE0051|IDE0052|CS0169|CS0414' "$log" || true
fi
printf 'Full build report: %s\n' "$log"
