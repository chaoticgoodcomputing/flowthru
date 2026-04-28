# Sourced by ../post-install.sh.
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
