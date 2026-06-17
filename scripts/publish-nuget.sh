#!/usr/bin/env bash
set -euo pipefail

# Manual, API-key NuGet publish — run from a maintainer's machine, NOT CI.
#
# Routine publishing is keyless in CI via NuGet Trusted Publishing (OIDC); see
# .github/workflows/release.yml (preview) and publish.yml (release). nuget.org no
# longer recommends storing long-lived API keys in CI secrets.
#
# This path is retained for the case Trusted Publishing can't cover on its own:
# bootstrapping a BRAND-NEW package ID that nuget.org doesn't yet associate with
# the org (no reserved-prefix ownership), or any out-of-band push. Once the ID
# exists under the org (or is covered by a reserved prefix), Trusted Publishing
# handles it in CI and this script is no longer needed for it.
#
# Usage:
#   NUGET_API_KEY=<key> nx run flowthru:publish:nuget
#   NUGET_API_KEY=<key> bash scripts/publish-nuget.sh [extra `dotnet nuget push` args]
#
# Prerequisites:
#   - Packages packed into dist/packages/  (run `nx run-many -t pack` first).
#   - NUGET_API_KEY: a nuget.org API key with the "Push new packages and package
#     version" scope for the target package(s)/prefix.
#   - Optional NUGET_SOURCE override (defaults to nuget.org).

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
PACKAGES_DIR="$ROOT/dist/packages"
SOURCE="${NUGET_SOURCE:-https://api.nuget.org/v3/index.json}"

if [ -z "${NUGET_API_KEY:-}" ]; then
  echo "Error: NUGET_API_KEY is not set." >&2
  echo "  NUGET_API_KEY=<key> nx run flowthru:publish:nuget" >&2
  exit 1
fi

shopt -s nullglob
nupkgs=("$PACKAGES_DIR"/*.nupkg)
if [ ${#nupkgs[@]} -eq 0 ]; then
  echo "Error: no .nupkg files in $PACKAGES_DIR." >&2
  echo "  Run 'nx run-many -t pack' first." >&2
  exit 1
fi

echo "Publishing ${#nupkgs[@]} package(s) from $PACKAGES_DIR to $SOURCE"
dotnet nuget push "$PACKAGES_DIR/*.nupkg" \
  --api-key "$NUGET_API_KEY" \
  --source "$SOURCE" \
  --skip-duplicate \
  "$@"
