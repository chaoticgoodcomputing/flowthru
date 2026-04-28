# Sourced by ../post-install.sh.
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
