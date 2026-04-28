# Sourced by ../post-install.sh.
# ── uv ────────────────────────────────────────────────────────────────────────
# Required for Python-backed flows and the Flowthru.Extensions.Python test suite.

if command -v uv &>/dev/null; then
  UV_VERSION=$(uv --version 2>/dev/null | awk '{print $2}')
  ok "uv $UV_VERSION"
else
  warn "uv not found. uv is required for Python-backed Flowthru flows and tests."
  warn "Install: https://docs.astral.sh/uv/getting-started/installation/"
fi
