#!/usr/bin/env bash
set -euo pipefail

# Re-uploads the canonical, in-repo coverage XMLs from
# examples/advanced/FlowthruCoverage/Data/_01_Raw/Datasets/ to Codecov, one
# upload per flag in dist/codecov-flags.json. A complete-set upload on a single
# commit re-anchors Codecov's carryforward chain — every flag carries forward
# from these numbers afterward.
#
# Prerequisites:
#   - codecov CLI on PATH
#   - CODECOV_TOKEN environment variable set
#   - dist/codecov-flags.json produced by sync-codecov-flags.mjs

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
FLAGS_JSON="$ROOT/dist/codecov-flags.json"
DATASETS="$ROOT/examples/advanced/FlowthruCoverage/Data/_01_Raw/Datasets"

if [ ! -f "$FLAGS_JSON" ]; then
  echo "Error: $FLAGS_JSON not found. Run sync-codecov-flags.mjs first." >&2
  exit 1
fi

if [ ! -d "$DATASETS" ]; then
  echo "Error: $DATASETS not found." >&2
  exit 1
fi

# ── Cross-check flags ↔ dataset files; warn on either-side drift ─────────────

declare -A flag_seen=()
missing_files=()

while IFS= read -r flag; do
  flag_seen["$flag"]=1
  if [ ! -f "$DATASETS/${flag}.xml" ]; then
    missing_files+=("$flag")
  fi
done < <(jq -r '.[].flag' "$FLAGS_JSON")

orphan_files=()
while IFS= read -r -d '' xml; do
  base=$(basename "$xml" .xml)
  if [ -z "${flag_seen[$base]:-}" ]; then
    orphan_files+=("$base")
  fi
done < <(find "$DATASETS" -maxdepth 1 -name "*.xml" -print0)

if [ "${#missing_files[@]}" -gt 0 ]; then
  echo "Warning: ${#missing_files[@]} flag(s) have no matching XML in Datasets/ — they will be skipped:" >&2
  printf '  %s\n' "${missing_files[@]}" >&2
fi

if [ "${#orphan_files[@]}" -gt 0 ]; then
  echo "Warning: ${#orphan_files[@]} XML file(s) in Datasets/ do not match any current flag — they will be skipped:" >&2
  printf '  %s.xml\n' "${orphan_files[@]}" >&2
fi

# ── Spawn concurrent uploads (one per flag with a matching dataset file) ─────

pids=()
failed=0
queued=0

while IFS= read -r flag; do
  file="$DATASETS/${flag}.xml"
  if [ ! -f "$file" ]; then
    continue
  fi
  echo "Queuing upload: $file (flag: $flag)"
  codecov upload-coverage \
    --token "$CODECOV_TOKEN" \
    --flag "$flag" \
    --file "$file" \
    --disable-search &
  pids+=($!)
  queued=$((queued + 1))
done < <(jq -r '.[].flag' "$FLAGS_JSON")

# ── Wait for all uploads, report failures ────────────────────────────────────

for pid in "${pids[@]}"; do
  if ! wait "$pid"; then
    echo "Upload failed (pid $pid)" >&2
    failed=1
  fi
done

if [ "$failed" -ne 0 ]; then
  echo "One or more uploads failed ($queued queued)." >&2
  exit 1
fi

echo "Refresh complete: $queued upload(s) succeeded."
