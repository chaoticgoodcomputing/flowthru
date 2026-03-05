#!/usr/bin/env bash
# Script to inject current Flowthru version into template.json before packing

set -e

# Extract version from Directory.Build.props (macOS-compatible)
VERSION=$(grep '<Version>' Directory.Build.props | sed -n 's/.*<Version>\(.*\)<\/Version>.*/\1/p' | head -n 1)

if [ -z "$VERSION" ]; then
  echo "Error: Could not extract version from Directory.Build.props"
  exit 1
fi

echo "Injecting version $VERSION into template.json..."

# Path to template.json
TEMPLATE_JSON="examples/starter/.template.config/template.json"

if [ ! -f "$TEMPLATE_JSON" ]; then
  echo "Error: Template file not found at $TEMPLATE_JSON"
  exit 1
fi

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

echo "✓ Version $VERSION injected into template.json"
