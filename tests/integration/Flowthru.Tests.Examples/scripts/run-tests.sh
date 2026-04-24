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

# ── 3. Run tests ──────────────────────────────────────────────────────────────

if [[ "$RUN_ALL" == true ]]; then
  # One dotnet test invocation per example → separate TestResults/{Name}/ directory
  # → separate coverage.cobertura.xml per example. FUnit tests run once up front.
  echo "Running FUnit auto-discovery tests..."
  dotnet test "$PROJECT_DIR" --no-build --no-restore \
    --filter "Category=FUnit" \
    --results-directory "$PROJECT_DIR/TestResults/FUnit" \
    "${EXTRA_ARGS[@]}"

  for example in "${TARGET_EXAMPLES[@]}"; do
    echo "--- Running example: $example ---"
    dotnet test "$PROJECT_DIR" --no-build --no-restore \
      --filter "FullyQualifiedName~${example}" \
      --results-directory "$PROJECT_DIR/TestResults/${example}" \
      "${EXTRA_ARGS[@]}"
  done
else
  # Original behaviour: single dotnet test invocation, combined results.
  # FUnit auto-discovery tests (Category=FUnit) are always included.
  FILTER_PARTS=("Category=FUnit")
  for example in "${TARGET_EXAMPLES[@]}"; do
    FILTER_PARTS+=("FullyQualifiedName~${example}")
  done

  FILTER=$(IFS='|'; echo "${FILTER_PARTS[*]}")
  echo "NUnit filter: $FILTER"

  dotnet test "$PROJECT_DIR" --no-build --no-restore --filter "$FILTER" "${EXTRA_ARGS[@]}"
fi
