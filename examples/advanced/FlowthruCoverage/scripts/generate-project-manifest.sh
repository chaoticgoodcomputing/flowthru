#!/usr/bin/env bash
# generate-project-manifest.sh
# Scans src/, tests/, and examples/ from the repo root and writes a CSV manifest
# mapping each assembly name to its ProjectType and Subgroup. Run from repo root.
set -euo pipefail

MANIFEST="examples/advanced/FlowthruCoverage/Data/_01_Raw/Datasets/project_manifest.csv"
mkdir -p "$(dirname "$MANIFEST")"

echo "AssemblyName,ProjectType,Subgroup" > "$MANIFEST"

# Every src project is in scope, including Roslyn analyzer / source-generator /
# code-fix projects (*.SourceGenerators, *.CodeFixes). When the corresponding
# *.Tests project references them with ReferenceOutputAssembly="true", coverlet
# instruments them at test runtime — see coverlet.runsettings for the rationale.
# Test projects with no .cs files (and therefore no coverage data) are surfaced
# via the src-inventory fallback in BuildProvenanceIcicleStep.

for proj in src/core/*/;       do echo "$(basename "$proj"),Library,Core";       done >> "$MANIFEST"
for proj in src/extensions/*/; do echo "$(basename "$proj"),Library,Extensions"; done >> "$MANIFEST"
for proj in src/misc/*/;       do echo "$(basename "$proj"),Library,Misc";       done >> "$MANIFEST"

for proj in tests/core/*/;       do echo "$(basename "$proj"),LibraryTest,Core";       done >> "$MANIFEST"
for proj in tests/extensions/*/; do echo "$(basename "$proj"),LibraryTest,Extensions"; done >> "$MANIFEST"
for proj in tests/misc/*/;       do echo "$(basename "$proj"),LibraryTest,Misc";       done >> "$MANIFEST"

for proj in tests/helpers/*/;     do echo "$(basename "$proj"),IntegrationTest,"; done >> "$MANIFEST"
for proj in tests/integration/*/; do echo "$(basename "$proj"),IntegrationTest,"; done >> "$MANIFEST"

find examples/starter examples/advanced -name "*.csproj" | while read -r f; do
  echo "$(basename "$f" .csproj),Example,"
done >> "$MANIFEST"

echo "Generated $(wc -l < "$MANIFEST") entries → $MANIFEST"
