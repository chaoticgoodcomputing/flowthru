#!/usr/bin/env bash
# Runs Flowthru.Tests.Examples with a NUnit filter scoped to affected examples.
#
# In CI, NX_AFFECTED_PROJECTS is exported by the "Compute affected projects" step
# before `nx affected -t test` runs. Locally, the script computes it fresh from
# the last git tag.
#
# Logic:
#   1. Intersect affected projects with known example project names.
#   2. If intersection is empty → skip (exit 0).
#   3. Otherwise → run dotnet test filtered to those examples + Category=FUnit.
#
# All extra args ($@) are forwarded to `dotnet test` (coverage flags, loggers, etc.)
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
WORKSPACE_ROOT="$(cd "$PROJECT_DIR/../.." && pwd)"
EXAMPLES_DIR="$WORKSPACE_ROOT/examples"

EXTRA_ARGS=("$@")

echo "=== Example Test Runner ==="
echo "Project:        $PROJECT_DIR"
echo "Workspace root: $WORKSPACE_ROOT"

# ── 1. Get affected project list ──────────────────────────────────────────────

if [[ -n "${NX_AFFECTED_PROJECTS:-}" ]]; then
  echo "Source: NX_AFFECTED_PROJECTS env var (CI)"
  read -ra AFFECTED <<< "$NX_AFFECTED_PROJECTS"
else
  echo "Source: computing from last git tag (local)"
  NX_BASE=$(cd "$WORKSPACE_ROOT" && \
    git describe --tags --abbrev=0 2>/dev/null || git rev-list --max-parents=0 HEAD)
  echo "Base: $NX_BASE"
  AFFECTED_JSON=$(cd "$WORKSPACE_ROOT" && \
    pnpm nx show projects --affected --base="$NX_BASE" --json 2>/dev/null)
  read -ra AFFECTED <<< "$(node -e \
    "process.stdout.write(JSON.parse(require('fs').readFileSync('/dev/stdin','utf8')).join(' '))" \
    <<< "$AFFECTED_JSON")"
fi

echo "Affected (${#AFFECTED[@]}): ${AFFECTED[*]:-none}"

# ── 2. Discover example project names from disk ────────────────────────────────

declare -A EXAMPLE_SET
while IFS= read -r csproj; do
  name="$(basename "$(dirname "$csproj")")"
  EXAMPLE_SET["$name"]=1
done < <(find "$EXAMPLES_DIR" -name "*.csproj" \
           -not -path "*/archived/*" \
           -not -path "*/obj/*" \
           -not -path "*/bin/*")

echo "Discovered ${#EXAMPLE_SET[@]} example projects"

# ── 3. Intersect affected with examples ───────────────────────────────────────

AFFECTED_EXAMPLES=()
for proj in "${AFFECTED[@]}"; do
  if [[ -n "${EXAMPLE_SET[$proj]:-}" ]]; then
    AFFECTED_EXAMPLES+=("$proj")
  fi
done

echo "Affected examples (${#AFFECTED_EXAMPLES[@]}): ${AFFECTED_EXAMPLES[*]:-none}"

if [[ ${#AFFECTED_EXAMPLES[@]} -eq 0 ]]; then
  echo "No example projects affected — skipping example integration tests."
  exit 0
fi

# ── 4. Run dotnet test filtered to affected examples ──────────────────────────
# FUnit auto-discovery tests (Category=FUnit) are always included — they are
# fast and verify source generator output independently of which examples changed.

FILTER_PARTS=("Category=FUnit")
for example in "${AFFECTED_EXAMPLES[@]}"; do
  FILTER_PARTS+=("FullyQualifiedName~${example}")
done

FILTER=$(IFS='|'; echo "${FILTER_PARTS[*]}")
echo "NUnit filter: $FILTER"

dotnet test "$PROJECT_DIR" --no-build --no-restore --filter "$FILTER" "${EXTRA_ARGS[@]}"


SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
WORKSPACE_ROOT="$(cd "$PROJECT_DIR/../.." && pwd)"
EXAMPLES_DIR="$WORKSPACE_ROOT/examples"

EXTRA_ARGS=("$@")

echo "=== Example Test Runner ==="
echo "Project:        $PROJECT_DIR"
echo "Workspace root: $WORKSPACE_ROOT"

# ── 1. Get affected project list ──────────────────────────────────────────────

if [[ -n "${NX_AFFECTED_PROJECTS:-}" ]]; then
  echo "Source: NX_AFFECTED_PROJECTS env var (CI)"
  read -ra AFFECTED <<< "$NX_AFFECTED_PROJECTS"
else
  echo "Source: computing from last git tag (local)"
  NX_BASE=$(cd "$WORKSPACE_ROOT" && \
    git describe --tags --abbrev=0 2>/dev/null || git rev-list --max-parents=0 HEAD)
  echo "Base: $NX_BASE"
  AFFECTED_JSON=$(cd "$WORKSPACE_ROOT" && \
    pnpm nx show projects --affected --base="$NX_BASE" --json 2>/dev/null)
  read -ra AFFECTED <<< "$(node -e \
    "process.stdout.write(JSON.parse(require('fs').readFileSync('/dev/stdin','utf8')).join(' '))" \
    <<< "$AFFECTED_JSON")"
fi

echo "Affected (${#AFFECTED[@]}): ${AFFECTED[*]:-none}"

# ── 2. Discover example project names from disk ────────────────────────────────

declare -A EXAMPLE_SET
while IFS= read -r csproj; do
  name="$(basename "$(dirname "$csproj")")"
  EXAMPLE_SET["$name"]=1
done < <(find "$EXAMPLES_DIR" -name "*.csproj" \
           -not -path "*/archived/*" \
           -not -path "*/obj/*" \
           -not -path "*/bin/*")

echo "Discovered ${#EXAMPLE_SET[@]} example projects"

# ── 3. Determine if any framework (src/) library is affected ──────────────────
# A project is a "framework library" if its root directory is under src/.
# This check is structural — no manual exclusion list to maintain.

FRAMEWORK_AFFECTED=false
for proj in "${AFFECTED[@]}"; do
  # Skip known example and test projects
  [[ -n "${EXAMPLE_SET[$proj]:-}" ]] && continue
  # Ask NX for the project root
  PROJ_ROOT=$(cd "$WORKSPACE_ROOT" && \
    pnpm nx show project "$proj" --json 2>/dev/null | \
    node -e "try{const d=JSON.parse(require('fs').readFileSync('/dev/stdin','utf8'));process.stdout.write(d.root||'')}catch{}" 2>/dev/null || true)
  if [[ "$PROJ_ROOT" == src/* ]]; then
    FRAMEWORK_AFFECTED=true
    echo "Framework project affected: $proj ($PROJ_ROOT) → running all examples"
    break
  fi
done

# ── 4. Run tests ───────────────────────────────────────────────────────────────

if [[ "$FRAMEWORK_AFFECTED" == "true" ]]; then
  echo "Running all example integration tests."
  dotnet test "$PROJECT_DIR" --no-build --no-restore "${EXTRA_ARGS[@]}"
  exit $?
fi

# Intersect affected with known example names
AFFECTED_EXAMPLES=()
for proj in "${AFFECTED[@]}"; do
  if [[ -n "${EXAMPLE_SET[$proj]:-}" ]]; then
    AFFECTED_EXAMPLES+=("$proj")
  fi
done

echo "Affected examples (${#AFFECTED_EXAMPLES[@]}): ${AFFECTED_EXAMPLES[*]:-none}"

if [[ ${#AFFECTED_EXAMPLES[@]} -eq 0 ]]; then
  echo "No example projects affected — skipping example integration tests."
  exit 0
fi

# FUnit discovery tests are always included (fast, assertion-only).
# Integration tests scoped to affected examples via FullyQualifiedName.
FILTER_PARTS=("Category=FUnit")
for example in "${AFFECTED_EXAMPLES[@]}"; do
  FILTER_PARTS+=("FullyQualifiedName~${example}")
done

# NUnit OR operator is |
FILTER=$(IFS='|'; echo "${FILTER_PARTS[*]}")
echo "NUnit filter: $FILTER"

dotnet test "$PROJECT_DIR" --no-build --no-restore --filter "$FILTER" "${EXTRA_ARGS[@]}"
