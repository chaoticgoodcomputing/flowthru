#!/usr/bin/env bash
# Script to inject current Flowthru version into all per-starter template.json files before packing

set -e

# Extract version from Directory.Build.props (macOS-compatible)
VERSION=$(grep '<Version>' Directory.Build.props | sed -n 's/.*<Version>\(.*\)<\/Version>.*/\1/p' | head -n 1)

if [ -z "$VERSION" ]; then
  echo "Error: Could not extract version from Directory.Build.props"
  exit 1
fi

echo "Injecting version $VERSION into starter template.json files..."

# Glob all per-starter template.json files
TEMPLATE_FILES=$(find examples/starter -name "template.json" -path "*/.template.config/template.json")

if [ -z "$TEMPLATE_FILES" ]; then
  echo "Error: No template.json files found under examples/starter/"
  exit 1
fi

for TEMPLATE_JSON in $TEMPLATE_FILES; do
  echo "  Patching $TEMPLATE_JSON"

  # Create backup
  cp "$TEMPLATE_JSON" "$TEMPLATE_JSON.bak"

  # Use jq to update the FlowthruVersion defaultValue
  if command -v jq &> /dev/null; then
    jq --arg version "$VERSION" '.symbols.FlowthruVersion.defaultValue = $version' "$TEMPLATE_JSON" > "$TEMPLATE_JSON.tmp"
    mv "$TEMPLATE_JSON.tmp" "$TEMPLATE_JSON"
  else
    # Fallback to sed if jq is not available
    sed -i.tmp "s/\"defaultValue\": \"[^\"]*\"/\"defaultValue\": \"$VERSION\"/g" "$TEMPLATE_JSON"
    rm -f "$TEMPLATE_JSON.tmp"
  fi

  # Clean up backup
  rm -f "$TEMPLATE_JSON.bak"
done

echo "✓ Version $VERSION injected into $(echo "$TEMPLATE_FILES" | wc -l | tr -d ' ') template.json files"
