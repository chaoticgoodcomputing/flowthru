#!/usr/bin/env bash
# stage-coverage-xml.sh
# Copies the most-recent coverage.cobertura.xml from each test project into the
# raw datasets directory, named {ProjectName}.xml. Run from repo root.
set -euo pipefail

DEST="examples/advanced/FlowthruCoverage/Data/_01_Raw/Datasets"
mkdir -p "$DEST"

# Standard library test projects: tests/core, tests/extensions, tests/misc, tests/core
for dir in tests/core tests/extensions tests/misc tests/core; do
  for proj in "$dir"/*/; do
    name=$(basename "$proj")
    latest=$(ls -t "$proj"TestResults/*/coverage.cobertura.xml 2>/dev/null | head -1 || true)
    if [ -n "$latest" ]; then
      cp "$latest" "$DEST/${name}.xml"
      echo "Staged: ${name}.xml"
    fi
  done
done

# Per-example runs produced by Flowthru.Tests.Examples (TestResults/{ExampleName}/{guid}/)
for entry in tests/integration/Flowthru.Tests.Examples/TestResults/*/; do
  name=$(basename "$entry")
  latest=$(ls -t "$entry"*/coverage.cobertura.xml 2>/dev/null | head -1 || true)
  if [ -n "$latest" ]; then
    cp "$latest" "$DEST/${name}.xml"
    echo "Staged (example): ${name}.xml"
  fi
done
