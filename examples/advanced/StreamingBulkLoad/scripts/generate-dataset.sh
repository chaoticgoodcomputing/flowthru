#!/usr/bin/env bash
# Regenerate the synthetic multi-row-group Parquet dataset this example reads.
#
# Row count knob (larger = wider eager-vs-streaming memory gap, longer runtime):
#   ./scripts/generate-dataset.sh              # default (200,000 rows)
#   ./scripts/generate-dataset.sh 2000000      # crank it up to 2,000,000
#   STREAMINGBULKLOAD_ROWS=500000 ./scripts/generate-dataset.sh
#
# The dataset lands at Data/_01_Raw/Datasets/transactions.parquet with small
# (10,000-row) row groups so even a modest dataset spans many groups — that is
# what lets the streaming reader keep peak memory to one row group.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"

ROWS="${1:-${STREAMINGBULKLOAD_ROWS:-}}"

cd "$PROJECT_DIR"
if [[ -n "$ROWS" ]]; then
  dotnet run -- --generate --rows "$ROWS"
else
  dotnet run -- --generate
fi
