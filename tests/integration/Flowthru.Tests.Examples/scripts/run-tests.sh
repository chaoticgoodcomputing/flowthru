#!/usr/bin/env bash
# Runs Flowthru.Tests.Examples with a NUnit filter scoped to affected examples.
#
# In CI, NX_AFFECTED_PROJECTS is exported by the "Compute affected projects" step
# before `nx affected -t test` runs. Locally, the script computes it fresh from
# the last git tag.
#
# Flags:
#   --run-all   Skip the affected-project filter and run every discovered example
#               individually (one dotnet test invocation per example, separate
#               TestResults directory per example — required for per-example
#               coverage XML output).
#
# All other args are forwarded to each `dotnet test` invocation.
#
# Parallelism:
#   All dotnet test invocations are launched as background jobs and waited on
#   collectively. PythonEngine is process-global, so Python examples that share
#   an interpreter would conflict — but each invocation here is a separate
#   process, so there is no shared state.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
WORKSPACE_ROOT="$(cd "$PROJECT_DIR/../../.." && pwd)"
EXAMPLES_DIR="$WORKSPACE_ROOT/examples"

# ── Parse flags ───────────────────────────────────────────────────────────────

RUN_ALL=false
EXTRA_ARGS=()
for arg in "$@"; do
  if [[ "$arg" == "--run-all" ]]; then
    RUN_ALL=true
  else
    EXTRA_ARGS+=("$arg")
  fi
done

echo "=== Example Test Runner ==="
echo "Project:        $PROJECT_DIR"
echo "Workspace root: $WORKSPACE_ROOT"

# ── 1. Discover example project names from disk ───────────────────────────────

declare -A EXAMPLE_SET
while IFS= read -r csproj; do
  name="$(basename "$(dirname "$csproj")")"  
  EXAMPLE_SET["$name"]=1
done < <(find "$EXAMPLES_DIR" -name "*.csproj" \
           -not -path "*/archived/*" \
           -not -path "*/obj/*" \
           -not -path "*/bin/*")

echo "Discovered ${#EXAMPLE_SET[@]} example projects"

# ── 2. Determine which examples to run ────────────────────────────────────────

if [[ "$RUN_ALL" == true ]]; then
  echo "Mode: --run-all (all examples, one invocation each)"
  TARGET_EXAMPLES=("${!EXAMPLE_SET[@]}")
else
  echo "Mode: affected only"

  if [[ -n "${NX_AFFECTED_PROJECTS:-}" ]]; then
    echo "Source: NX_AFFECTED_PROJECTS env var (CI)"
    read -ra AFFECTED <<< "$NX_AFFECTED_PROJECTS"
  else
    echo "Source: computing from last git tag (local)"
    if NX_BASE=$(cd "$WORKSPACE_ROOT" && git describe --tags --abbrev=0 2>/dev/null); then
      true
    else
      NX_BASE=$(cd "$WORKSPACE_ROOT" && git rev-list --max-parents=0 HEAD)
    fi
    echo "Base: $NX_BASE"
    AFFECTED_JSON=$(cd "$WORKSPACE_ROOT" && \
      pnpm nx show projects --affected --base="$NX_BASE" --json 2>/dev/null)
    read -ra AFFECTED <<< "$(node -e \
      "process.stdout.write(JSON.parse(require('fs').readFileSync('/dev/stdin','utf8')).join(' '))" \
      <<< "$AFFECTED_JSON")"
  fi

  echo "Affected (${#AFFECTED[@]}): ${AFFECTED[*]:-none}"

  TARGET_EXAMPLES=()
  for proj in "${AFFECTED[@]}"; do
    if [[ -n "${EXAMPLE_SET[$proj]:-}" ]]; then
      TARGET_EXAMPLES+=("$proj")
    fi
  done

  echo "Affected examples (${#TARGET_EXAMPLES[@]}): ${TARGET_EXAMPLES[*]:-none}"

  if [[ ${#TARGET_EXAMPLES[@]} -eq 0 ]]; then
    echo "No example projects affected — skipping example integration tests."
    exit 0
  fi
fi

# ── 3. Pre-warm Python venv ───────────────────────────────────────────────────
#
# Each parallel dotnet test process would independently call `uv sync --frozen`
# on first run. Pre-warming here serializes that once so the parallel test
# processes find the venv already materialized.

OUTPUT_DIR="$WORKSPACE_ROOT/dist/tests/integration/Flowthru.Tests.Examples/net10.0"
if [ -d "$OUTPUT_DIR" ] && [ -f "$OUTPUT_DIR/pyproject.toml" ]; then
  if [ ! -f "$OUTPUT_DIR/.venv/pyvenv.cfg" ]; then
    echo "Pre-warming Python venv in $OUTPUT_DIR..."
    (cd "$OUTPUT_DIR" && uv sync --frozen --python-preference only-managed) \
      || echo "Warning: venv pre-warm failed; individual test processes will retry."
  else
    echo "Python venv already materialized."
  fi
fi

# ── 4. Run tests in parallel ──────────────────────────────────────────────────
#
# Each invocation is a background job. Wrapping in a subshell with an explicit
# `cd "$PROJECT_DIR"` ensures the dotnet test host always inherits a valid,
# known cwd — guarding against the caller having a stale working directory
# (e.g. from a renamed parent directory in the same shell session).

pids=()
failed=0

for example in "${TARGET_EXAMPLES[@]}"; do
  echo "--- Queuing example: $example ---"
  (cd "$PROJECT_DIR" && dotnet test "$PROJECT_DIR" --no-build --no-restore \
    --filter "FullyQualifiedName~${example}" \
    --results-directory "$PROJECT_DIR/TestResults/${example}" \
    "${EXTRA_ARGS[@]}") &
  pids+=($!)
done

echo "Waiting for ${#pids[@]} test run(s) to complete..."
for pid in "${pids[@]}"; do
  if ! wait "$pid"; then
    echo "Test run failed (pid $pid)" >&2
    failed=1
  fi
done

if [ "$failed" -ne 0 ]; then
  echo "One or more example test runs failed." >&2
  exit 1
fi

echo "All example test runs passed."
