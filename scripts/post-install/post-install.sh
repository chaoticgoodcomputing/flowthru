#!/usr/bin/env bash
# Post-install hook for the Flowthru repo.
# Sources each dependency check in ./dependencies/ in the order listed below.
# Each subscript is sourced (not executed) so it inherits ROOT_DIR and the
# ok/warn helpers defined here; subscripts are not intended to run standalone.
set -euo pipefail

POST_INSTALL_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$POST_INSTALL_DIR/../.." && pwd)"
DEPENDENCIES_DIR="$POST_INSTALL_DIR/dependencies"

ok()   { echo "  [OK]  $*"; }
warn() { echo "  [!!]  $*" >&2; }

# Order is intentional: `dotnet-9` inspects runtimes only when `dotnet`
# itself was found, so it must run after the base dotnet check.
DEPENDENCIES=(
  dotnet
  dotnet-9
  uv
  python
  java
  spark
  chromium
)

echo ""
echo "Checking non-Node dependencies..."
echo ""

for dep in "${DEPENDENCIES[@]}"; do
  # shellcheck source=/dev/null
  source "$DEPENDENCIES_DIR/$dep.sh"
  echo ""
done
