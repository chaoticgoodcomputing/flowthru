#!/usr/bin/env bash
# generate-project-manifest.sh
# Scans src/, tests/, and examples/ from the repo root and writes a CSV manifest
# mapping each assembly name to its ProjectType and Subgroup. Run from repo root.
set -euo pipefail

MANIFEST="examples/advanced/FlowthruCoverage/Data/_01_Raw/Datasets/project_manifest.csv"
mkdir -p "$(dirname "$MANIFEST")"

echo "AssemblyName,ProjectType,Subgroup" > "$MANIFEST"

# Packages excluded from coverage measurement (kept in sync with [Flowthru.Misc.ML]*
# in coverlet.runsettings <Exclude>). These produce no Cobertura data; keeping their
# entries here would create permanent ghost rows in the heatmap.
EXCLUDED_PACKAGES="Flowthru.Misc.ML Flowthru.Misc.ML.Tests"

is_excluded() {
  for ex in $EXCLUDED_PACKAGES; do
    [ "$1" = "$ex" ] && return 0
  done
  return 1
}

for proj in src/core/*/;       do name=$(basename "$proj"); is_excluded "$name" || echo "$name,Library,Core";       done >> "$MANIFEST"
for proj in src/extensions/*/; do name=$(basename "$proj"); is_excluded "$name" || echo "$name,Library,Extensions"; done >> "$MANIFEST"
for proj in src/misc/*/;       do name=$(basename "$proj"); is_excluded "$name" || echo "$name,Library,Misc";       done >> "$MANIFEST"

for proj in tests/core/*/;       do name=$(basename "$proj"); is_excluded "$name" || echo "$name,LibraryTest,Core";       done >> "$MANIFEST"
for proj in tests/extensions/*/; do name=$(basename "$proj"); is_excluded "$name" || echo "$name,LibraryTest,Extensions"; done >> "$MANIFEST"
for proj in tests/misc/*/;       do name=$(basename "$proj"); is_excluded "$name" || echo "$name,LibraryTest,Misc";       done >> "$MANIFEST"

for proj in tests/helpers/*/;     do echo "$(basename "$proj"),IntegrationTest,"; done >> "$MANIFEST"
for proj in tests/integration/*/; do echo "$(basename "$proj"),IntegrationTest,"; done >> "$MANIFEST"

find examples/starter examples/advanced -name "*.csproj" | while read -r f; do
  echo "$(basename "$f" .csproj),Example,"
done >> "$MANIFEST"

echo "Generated $(wc -l < "$MANIFEST") entries → $MANIFEST"
