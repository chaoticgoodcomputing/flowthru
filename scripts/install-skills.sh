#!/usr/bin/env bash
# Dogfood: install this repo's own public Flowthru skills into .claude/skills/
# the same way a downstream user would — via `npx skills`, reading the generated
# .claude-plugin/marketplace.json and copying each isolated src/<Pkg>/skill/ dir.
#
# The installed skills are gitignored (see .claude/skills/.gitignore) — this is
# provisioning, not a commit. Run it after a fresh clone (or after editing a
# skill source) to refresh the loaded copies. flowthru-contributing is left
# untouched (its source == its install location; the installer skips the overlap).
#
# Usage:  bash scripts/install-skills.sh
set -euo pipefail
cd "$(dirname "$0")/.."

echo "Regenerating the extension index + manifest from skill sources…"
node scripts/generate-skill-extensions.mjs

echo "Installing public Flowthru skills into .claude/skills/ from local sources…"
DISABLE_TELEMETRY=1 DO_NOT_TRACK=1 npx -y skills add . --skill '*' -a claude-code --copy -y

echo "Done. Installed skills are gitignored; edit sources under src/*/skill/ and re-run."
