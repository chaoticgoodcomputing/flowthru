#!/usr/bin/env bash
# Runs Flowthru example integration tests with per-example shard isolation.
#
# Each affected example is run as its own `dotnet test` process against a private
# copy of the test project's publish output. Per-shard isolation is required for
# correctness: Coverlet's collector instruments DLLs in-place (rewriting the file
# on disk with hits-file paths baked into IL via Ldstr). Two processes targeting
# the same on-disk DLLs race on instrumentation and silently produce zero hits.
# Per-shard copies give each process its own DLL inodes — no contention, real
# coverage, and trivially parallel runs.
#
# Inputs:
#   NX_AFFECTED_PROJECTS env var (CI) or `nx show projects --affected --base=HEAD`
#     determines which examples to run.
#   FLOWTHRU_TEST_PARALLEL env var caps concurrent shards (default: nproc).
#
# Flags:
#   --run-all   Skip the affected-project filter and run every discovered example.
#
# All other args are forwarded to each `dotnet test` invocation.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
WORKSPACE_ROOT="$(cd "$PROJECT_DIR/../../.." && pwd)"
EXAMPLES_DIR="$WORKSPACE_ROOT/examples"
PUBLISH_DIR="$WORKSPACE_ROOT/dist/tests/integration/Flowthru.Tests.Examples/publish"
SHARDS_ROOT="$WORKSPACE_ROOT/dist/tests/integration/Flowthru.Tests.Examples/shards"
RUNSETTINGS="$PROJECT_DIR/coverlet.runsettings"
RESULTS_ROOT="$PROJECT_DIR/TestResults"
MAX_PARALLEL="${FLOWTHRU_TEST_PARALLEL:-$(nproc)}"

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
echo "Publish source: $PUBLISH_DIR"
echo "Max parallel:   $MAX_PARALLEL"

# Verify publish output exists. The `publish` Nx target is an upstream dependency,
# so this should already be there — fail loudly if not.
if [ ! -f "$PUBLISH_DIR/Flowthru.Tests.Examples.dll" ]; then
  echo "ERROR: Publish output missing at $PUBLISH_DIR" >&2
  echo "Run: nx run Flowthru.Tests.Examples:publish" >&2
  exit 1
fi

# ── 1. Discover example project names from disk ───────────────────────────────

declare -A EXAMPLE_SET
while IFS= read -r csproj; do
  # Skip library sub-projects (no OutputType=Exe) — they're supporting libraries
  # of multi-project examples, not runnable harness entry points.
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
  echo "Mode: --run-all (all examples)"
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

# ── 3. Pre-warm Python venv in publish output (once, before shard cloning) ────
#
# The publish output contains the test project's pyproject.toml. Materializing
# the venv here means each shard inherits .venv via cp -r, avoiding N redundant
# uv syncs at run time.

if [ -f "$PUBLISH_DIR/pyproject.toml" ] && [ ! -f "$PUBLISH_DIR/.venv/pyvenv.cfg" ]; then
  echo "Pre-warming Python venv in $PUBLISH_DIR..."
  (cd "$PUBLISH_DIR" && uv sync --frozen --python-preference only-managed) \
    || echo "Warning: venv pre-warm failed; per-shard runs may retry."
fi

# ── 4. Run each affected example in its own shard, in parallel ────────────────
#
# xargs -P bounds concurrency to $MAX_PARALLEL. Each invocation is independent —
# shards can't collide because each owns its own DLL set, and per-example
# --results-directory keeps coverage XMLs separate.

mkdir -p "$SHARDS_ROOT" "$RESULTS_ROOT"

run_one_example() {
  local example="$1"
  local shard="$SHARDS_ROOT/$example"
  local results="$RESULTS_ROOT/$example"
  local log="$results/run.log"

  rm -rf "$shard" "$results"
  mkdir -p "$results"

  cp -r "$PUBLISH_DIR" "$shard"

  echo "→ $example (shard: $shard)"

  # Notes on flag choices when invoking against a DLL (not a csproj):
  #   - --no-build / --no-restore are csproj-only; vstest rejects them.
  #   - --filter "FullyQualifiedName~..." silently runs ALL parametric cases.
  #     The Test platform's TestCaseFilter sees only the underlying method
  #     (Example_ExecutesSuccessfully) for parameterized tests, so a substring
  #     match against the example name returns no hits and vstest falls back
  #     to running everything.
  #   - --Tests:<name> matches against the FULL parametric Name, including the
  #     parameter inside parens. The trailing ")" anchors the boundary so e.g.
  #     --Tests:...(KedroSpaceflights) matches only that exact case, not the
  #     KedroSpaceflightsCustom / FUnit / GQL / Python siblings. The Windows-
  #     style /Tests: spelling is mis-parsed as a file path on Linux when run
  #     under a non-interactive shell (e.g. the xargs subshell), so use the
  #     --Tests: form for portability.
  if dotnet test "$shard/Flowthru.Tests.Examples.dll" \
    "--Tests:Example_ExecutesSuccessfully(${example})" \
    --settings "$RUNSETTINGS" \
    --testadapterpath "$shard" \
    --collect "XPlat Code Coverage" \
    --results-directory "$results" \
    "${EXTRA_ARGS[@]}" \
    > "$log" 2>&1; then
    echo "✓ $example"
  else
    echo "✗ $example  — see $log" >&2
    cat "$log" >&2
    return 1
  fi
}

export -f run_one_example
export SHARDS_ROOT RESULTS_ROOT PUBLISH_DIR RUNSETTINGS
# Cannot export bash arrays via env. Serialize to a single env var; the xargs
# subshell rehydrates via eval. Guard the printf — calling it with zero args
# produces a single empty quoted arg ("''"), which would then leak into every
# dotnet test invocation as a stray empty argument and trip vstest.
if [ ${#EXTRA_ARGS[@]} -gt 0 ]; then
  EXTRA_ARGS_SERIALIZED="$(printf '%q ' "${EXTRA_ARGS[@]}")"
else
  EXTRA_ARGS_SERIALIZED=""
fi
export EXTRA_ARGS_SERIALIZED

if printf '%s\n' "${TARGET_EXAMPLES[@]}" | \
    xargs -P "$MAX_PARALLEL" -I{} bash -c '
      set -euo pipefail
      eval "EXTRA_ARGS=( $EXTRA_ARGS_SERIALIZED )"
      run_one_example "$@"
    ' _ {}; then
  echo "All ${#TARGET_EXAMPLES[@]} example test runs passed."
else
  echo "One or more example test runs failed." >&2
  exit 1
fi
