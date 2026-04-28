# Sourced by ../post-install.sh.
# ── .NET 9 runtime ────────────────────────────────────────────────────────────
# Optional but recommended. StrawberryShake.Server's MSBuild codegen invokes
# a tool that ships only net8 and net9 binaries; on hosts running newer SDKs
# (e.g. .NET 10), MSBuild still selects the net9 tool, which then requires the
# .NET 9 *shared framework* on disk to launch. SDK-only installs of newer
# .NET versions are not sufficient.
#
# Without this runtime, Flowthru.Extensions.GQL example projects
# (e.g. KedroSpaceflightsGQL) will fail at build time with MSB3073 / exit 150.

if command -v dotnet &>/dev/null; then
  if dotnet --list-runtimes 2>/dev/null \
       | grep -qE '^Microsoft\.NETCore\.App 9\.'; then
    DOTNET9_VERSION=$(dotnet --list-runtimes 2>/dev/null \
      | grep -E '^Microsoft\.NETCore\.App 9\.' \
      | head -1 \
      | awk '{print $2}')
    ok "Microsoft.NETCore.App $DOTNET9_VERSION  (required for Flowthru.Extensions.GQL codegen)"
  else
    warn ".NET 9 runtime not found. The Flowthru.Extensions.GQL example projects"
    warn "(e.g. KedroSpaceflightsGQL) will fail to build because StrawberryShake's"
    warn "codegen tool ships only net8/net9 binaries and needs the .NET 9 shared"
    warn "framework at build time."
    warn "Download: https://dotnet.microsoft.com/download/dotnet/9.0"
  fi
fi
# If dotnet itself is missing, the dotnet check has already warned.
