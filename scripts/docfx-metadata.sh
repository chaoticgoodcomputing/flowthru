#!/usr/bin/env bash
# Generates markdown API reference docs into docs/reference/{src,examples}/ mirroring
# the repo structure. Discovers .csproj files in src/ and/or examples/, writes a
# temporary per-project docfx config, runs `docfx metadata`, and removes the temp config.
#
# Output layout example:
#   docs/reference/src/core/Flowthru.Core/
#   docs/reference/src/extensions/Flowthru.Extensions.Csv/
#
# Usage: ./scripts/docfx-metadata.sh [--projects src|examples|all]
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SCOPE="${1:---projects}"
FILTER="${2:-src}"

# Parse arguments
if [[ "$SCOPE" == "--projects" ]]; then
  SCOPE="$FILTER"
elif [[ "$SCOPE" != "src" && "$SCOPE" != "examples" && "$SCOPE" != "all" ]]; then
  SCOPE="src"
fi

# Build find paths based on scope
FIND_PATHS=()
case "$SCOPE" in
  src)      FIND_PATHS=("$REPO_ROOT/src") ;;
  examples) FIND_PATHS=("$REPO_ROOT/examples") ;;
  all)      FIND_PATHS=("$REPO_ROOT/src" "$REPO_ROOT/examples") ;;
esac

# Discover projects, excluding source generators, archived, and template projects
PROJECTS=()
while IFS= read -r line; do
  PROJECTS+=("$line")
done < <(
  find "${FIND_PATHS[@]}" \
    -name '*.csproj' \
    -not -path '*/archived/*' \
    -not -path '*/item-templates/*' \
    -not -path '*/bin/*' \
    -not -path '*/obj/*' \
    -not -path '*/dist/*' \
    -not -name '*SourceGenerators*' \
    | sort
)

if [[ ${#PROJECTS[@]} -eq 0 ]]; then
  echo "No projects found for scope: $SCOPE"
  exit 0
fi

echo "Generating API reference docs for ${#PROJECTS[@]} projects (scope: $SCOPE)"
echo "---"

FAILURES=()

for csproj in "${PROJECTS[@]}"; do
  project_dir="$(dirname "$csproj")"
  csproj_name="$(basename "$csproj")"
  project_name="${csproj_name%.csproj}"
  relative_dir="${project_dir#"$REPO_ROOT"/}"
  config_path="$project_dir/.docfx.metadata.json"

  # Output goes to docs/reference/{relative_dir}/, computed as a path relative
  # to the config file location (which is in the project directory).
  output_dir="$REPO_ROOT/docs/reference/$relative_dir"
  # Make dest relative to config file (project_dir)
  dest_rel="$(python3 -c "import os; print(os.path.relpath('$output_dir', '$project_dir'))")"

  echo "[$project_name] Generating metadata..."

  # Write temporary docfx config in the project directory so .csproj paths resolve naturally.
  # shouldSkipMarkup: workaround for Markdig crash in DocFX 2.78.5 (ArgumentOutOfRangeException
  # in XmlComment.cs). HTML tags from XML docs (<p>, <strong>, etc.) pass through as valid
  # CommonMark inline HTML, which renders correctly on GitHub and VS Code.
  cat > "$config_path" << EOF
{
  "metadata": [{
    "src": [{ "files": ["$csproj_name"] }],
    "dest": "$dest_rel",
    "outputFormat": "markdown",
    "disableGitFeatures": true,
    "shouldSkipMarkup": true
  }]
}
EOF

  if dotnet docfx metadata "$config_path" --noRestore 2>&1 | sed 's/^/  /'; then
    echo "  ✓ docs/reference/$relative_dir/"
  else
    echo "  ✗ Failed for $project_name"
    FAILURES+=("$project_name")
  fi

  rm -f "$config_path"
done

echo "---"
if [[ ${#FAILURES[@]} -gt 0 ]]; then
  echo "Completed with ${#FAILURES[@]} failure(s): ${FAILURES[*]}"
  exit 1
else
  echo "✓ All ${#PROJECTS[@]} projects processed successfully"
fi
