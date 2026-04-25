#!/usr/bin/env bash
# Runs Flowthru.Tests.Examples with a NUnit filter scoped to affected examples.
#
# In CI, NX_AFFECTED_PROJECTS is exported by the "Compute affected projects" step
# before `nx affected -t test` runs. Locally, the script uses --base=HEAD so only
# uncommitted working-tree changes are considered.
#
# Flags:
#   --run-all   Skip the affected-project filter and run every discovered example
#               individually (one dotnet test invocation per example, separate
#               TestResults directory per example — required for per-example
#               coverage XML output).
#
# All other args are forwarded to each `dotnet test` invocation.
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
  # Skip library sub-projects (those without OutputType=Exe are supporting
  # libraries of a multi-project example, not runnable harness entry points).
  grep -q '<OutputType>Exe</OutputType>' "$csproj" || continue
  name="$(basename "$(dirname "$csproj")")"
  EXAMPLE_SET["$name"]=1
done < <(find "$EXAMPLES_DIR" -name "*.csproj" \
           -not -path "*/archived/*" \
           -not -path "*/item-templates/*" \
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
    echo "Source: computing from HEAD (local)"
    NX_BASE="HEAD"
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
# Pre-warming here ensures the venv is materialized before the first test
# process runs, avoiding redundant uv sync calls on cold runs.

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

# ── 4. Run tests serially ─────────────────────────────────────────────────────
#
# Each example gets its own dotnet test invocation so coverlet produces a
# separate TestResults/{Name}/coverage.cobertura.xml per example.
#
# Serial execution is required for coverage correctness. Coverlet's DataCollector
# instruments DLLs into temp copies identified by a per-process GUID. If multiple
# dotnet test processes run in parallel against the same output directory, each
# process's test host loads whichever DLL copy it finds first — which may have
# been instrumented by a different process. The resulting hits file GUID then
# doesn't match the DataCollector's instrumented copy, and all hits are lost.

failed=0

for example in "${TARGET_EXAMPLES[@]}"; do
  echo "--- Running example: $example ---"
  if ! (cd "$PROJECT_DIR" && dotnet test "$PROJECT_DIR" --no-build --no-restore \
    --filter "FullyQualifiedName~${example}" \
    --results-directory "$PROJECT_DIR/TestResults/${example}" \
    "${EXTRA_ARGS[@]}"); then
    echo "Test run failed for: $example" >&2
    failed=1
  fi
done

if [ "$failed" -ne 0 ]; then
  echo "One or more example test runs failed." >&2
  exit 1
fi

echo "All example test runs passed."
