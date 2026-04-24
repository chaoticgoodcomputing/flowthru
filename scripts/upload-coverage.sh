#!/usr/bin/env bash
set -euo pipefail

# Upload all coverage.cobertura.xml files to Codecov, tagged with the flags
# written by sync-codecov-flags.mjs into dist/codecov-flags.json.
#
# Structure:
#   1. create-commit and create-report once (registers the commit server-side).
#   2. Spawn one background do-upload per coverage file, each with its flag.
#   3. Wait for all uploads to complete, collecting any failures.
#
# This avoids the N redundant create-commit/create-report round-trips that
# upload-process would make, and runs all uploads concurrently.
#
# Prerequisites:
#   - codecov CLI installed and on PATH
#   - CODECOV_TOKEN environment variable set
#   - dist/codecov-flags.json produced by sync-codecov-flags.mjs

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
FLAGS_JSON="$ROOT/dist/codecov-flags.json"

if [ ! -f "$FLAGS_JSON" ]; then
  echo "Error: $FLAGS_JSON not found. Run sync-codecov-flags.mjs first." >&2
  exit 1
fi

# ── 1. Register commit and report once ───────────────────────────────────────

echo "Creating commit..."
codecov create-commit --token "$CODECOV_TOKEN"

echo "Creating report..."
codecov create-report --token "$CODECOV_TOKEN"

# ── 2. Spawn concurrent uploads ───────────────────────────────────────────────

pids=()
failed=0

while IFS= read -r entry; do
  flag=$(echo "$entry" | jq -r '.flag')
  root=$(echo "$entry" | jq -r '.root')
  while IFS= read -r -d '' coverage_file; do
    echo "Queuing upload: $coverage_file (flag: $flag)"
    codecov do-upload \
      --token "$CODECOV_TOKEN" \
      --flag "$flag" \
      --file "$coverage_file" \
      --disable-search &
    pids+=($!)
  done < <(find "$ROOT/$root" -name "coverage.cobertura.xml" -print0 2>/dev/null || true)
done < <(jq -c '.[]' "$FLAGS_JSON")

# ── 3. Wait for all uploads, report failures ─────────────────────────────────

for pid in "${pids[@]}"; do
  if ! wait "$pid"; then
    echo "Upload failed (pid $pid)" >&2
    failed=1
  fi
done

if [ "$failed" -ne 0 ]; then
  echo "One or more uploads failed." >&2
  exit 1
fi

echo "All uploads complete."
