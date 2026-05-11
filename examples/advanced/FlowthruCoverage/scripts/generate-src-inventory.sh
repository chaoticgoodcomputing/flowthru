#!/usr/bin/env bash
# generate-src-inventory.sh
# Walks src/**/*.cs and emits a CSV listing every source file with its raw
# line count, scoped by the owning assembly. Run from repo root.
#
# Used by BuildProvenanceIcicleStep as a fallback denominator: when a src
# project's test project produces no Cobertura output (because it has zero
# .cs files / no tests to run), the icicle step synthesises 0%-coverage
# nodes from these rows so the project still appears in the report.
#
# TotalLines is `wc -l` (raw lines, including blanks/comments) — coverlet's
# "instrumentable lines" is a stricter subset. The two never mix within a
# project: coverlet's numbers are used when at least one test ran for the
# project, inventory numbers otherwise.
set -euo pipefail

DEST="examples/advanced/FlowthruCoverage/Data/_01_Raw/Datasets/src_inventory.csv"
mkdir -p "$(dirname "$DEST")"

echo "AssemblyName,RelativePath,TotalLines" > "$DEST"

count=0
while IFS= read -r csproj; do
  project_dir=$(dirname "$csproj")
  assembly_name=$(basename "$csproj" .csproj)

  while IFS= read -r cs_file; do
    rel_path=${cs_file#"$project_dir/"}
    lines=$(wc -l < "$cs_file")
    printf '%s,%s,%s\n' "$assembly_name" "$rel_path" "$lines"
    count=$((count + 1))
  done < <(find "$project_dir" -name '*.cs' \
             -not -path '*/bin/*' \
             -not -path '*/obj/*')
done < <(find src -name '*.csproj' -not -path '*/bin/*' -not -path '*/obj/*') >> "$DEST"

echo "Generated $count src files → $DEST"
