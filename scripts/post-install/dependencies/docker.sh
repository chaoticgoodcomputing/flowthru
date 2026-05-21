# Sourced by ../post-install.sh.
# ── docker ────────────────────────────────────────────────────────────────────
# Optional. Required by Flowthru tests that opt into Testcontainers-managed
# backends (e.g., PostgresContainerBackend in Flowthru.Extensions.EFCore.Tests).
# Tests guard themselves via TestCapabilities.Docker; missing Docker yields
# an Inconclusive verdict rather than a failure.

if command -v docker &>/dev/null; then
  DOCKER_VERSION=$(docker --version 2>/dev/null | head -1)
  ok "$DOCKER_VERSION"
else
  warn "docker not found. Docker is required for any backend matrix tier that"
  warn "spins up a real database/broker via Testcontainers (e.g. the EF Core"
  warn "PostgresContainerBackend). Tests gate themselves and skip cleanly when"
  warn "Docker is absent, so this is optional unless you're touching those tests."
  warn "Install: https://docs.docker.com/get-docker/"
fi
