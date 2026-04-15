#!/usr/bin/env bash
# Post-pnpm-install hook.
# Checks for required non-Node dependencies and runs any associated setup actions.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"

# ── Helpers ───────────────────────────────────────────────────────────────────

ok()   { echo "  [OK]  $*"; }
warn() { echo "  [!!]  $*" >&2; }

echo ""
echo "Checking non-Node dependencies..."
echo ""

# ── dotnet ────────────────────────────────────────────────────────────────────
# Required to build and test all Flowthru projects.
# On success, runs 'dotnet restore' to prime the NuGet cache.

REQUIRED_DOTNET_MAJOR=$(grep -o '"version":[[:space:]]*"[^"]*"' "$ROOT_DIR/global.json" \
  | grep -o '[0-9][0-9]*' | head -1)

if command -v dotnet &>/dev/null; then
  DOTNET_VERSION=$(dotnet --version 2>/dev/null)
  DOTNET_MAJOR=$(echo "$DOTNET_VERSION" | cut -d. -f1)

  if [ "$DOTNET_MAJOR" -ge "$REQUIRED_DOTNET_MAJOR" ]; then
    ok "dotnet $DOTNET_VERSION  (global.json requires .NET $REQUIRED_DOTNET_MAJOR+)"
    echo ""
    echo "  Running dotnet restore..."
    dotnet restore "$ROOT_DIR"
    ok "dotnet restore complete"
  else
    warn "dotnet $DOTNET_VERSION found, but .NET $REQUIRED_DOTNET_MAJOR+ is required (see global.json)."
    warn "Download: https://dotnet.microsoft.com/download"
  fi
else
  warn "dotnet not found. .NET $REQUIRED_DOTNET_MAJOR+ is required to build and test Flowthru."
  warn "Download: https://dotnet.microsoft.com/download"
fi

echo ""

# ── uv ────────────────────────────────────────────────────────────────────────
# Required for Python-backed flows and the Flowthru.Extensions.Python test suite.

if command -v uv &>/dev/null; then
  UV_VERSION=$(uv --version 2>/dev/null | awk '{print $2}')
  ok "uv $UV_VERSION"
else
  warn "uv not found. uv is required for Python-backed Flowthru flows and tests."
  warn "Install: https://docs.astral.sh/uv/getting-started/installation/"
fi

echo ""

# ── python ────────────────────────────────────────────────────────────────────
# Required for the Flowthru.Extensions.Python extension and example projects.

if command -v python3 &>/dev/null; then
  PYTHON_VERSION=$(python3 --version 2>/dev/null | awk '{print $2}')
  ok "python $PYTHON_VERSION"
elif command -v python &>/dev/null; then
  PYTHON_VERSION=$(python --version 2>/dev/null | awk '{print $2}')
  ok "python $PYTHON_VERSION"
else
  warn "python not found. Python 3.10+ is required for the Flowthru Python extension and examples."
  warn "Install via uv: https://docs.astral.sh/uv/  or  https://www.python.org/downloads/"
fi

echo ""

# ── java ──────────────────────────────────────────────────────────────────────
# Required to run Spark-backed tests. Spark 4.1.1 requires JDK 17+.

if command -v java &>/dev/null; then
  JAVA_VERSION=$(java -version 2>&1 | head -1)
  ok "java: $JAVA_VERSION"
else
  warn "java not found. JDK 17+ is required to run Spark-backed Flowthru tests."
fi

echo ""

# ── SPARK_HOME ────────────────────────────────────────────────────────────────
# Required to run Spark-backed tests (Flowthru.Extensions.Spark.Tests).
# Tests guard themselves with Assume checks and skip gracefully if unset.

if [ -n "${SPARK_HOME:-}" ] && [ -d "$SPARK_HOME" ]; then
  if [ -f "$SPARK_HOME/RELEASE" ]; then
    SPARK_RELEASE=$(head -1 "$SPARK_HOME/RELEASE")
    ok "SPARK_HOME=$SPARK_HOME  ($SPARK_RELEASE)"
  else
    ok "SPARK_HOME=$SPARK_HOME"
  fi
else
  warn "SPARK_HOME is not set or does not point to a valid directory."
  warn "Apache Spark 4.1.1 is required to run Spark-backed Flowthru tests."
  warn "Download: https://spark.apache.org/downloads.html"
fi

echo ""
