#!/usr/bin/env bash
set -euo pipefail

# Upload all coverage.cobertura.xml files to Codecov, tagged with the flags
# written by sync-codecov-flags.mjs into dist/codecov-flags.json.
#
# Uses codecov upload-coverage, which:
#   - Automatically creates commit and report server-side
#   - Collects network files (git ls-files) to convey source tree state to Codecov
#   - Properly removes deleted files from carryforward tracking
#   - Runs uploads concurrently, one per flag
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

# ── Spawn concurrent uploads (one per flag) ──────────────────────────────────

pids=()
failed=0

while IFS= read -r entry; do
  flag=$(echo "$entry" | jq -r '.flag')
  root=$(echo "$entry" | jq -r '.root')
  while IFS= read -r -d '' coverage_file; do
    echo "Queuing upload: $coverage_file (flag: $flag)"
    codecov upload-coverage \
      --token "$CODECOV_TOKEN" \
      --flag "$flag" \
      --file "$coverage_file" \
      --disable-search &
    pids+=($!)
  done < <(find "$ROOT/$root" -name "coverage.cobertura.xml" -print0 2>/dev/null || true)
done < <(jq -c '.[]' "$FLAGS_JSON")

# ── Wait for all uploads, report failures ────────────────────────────────────

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
