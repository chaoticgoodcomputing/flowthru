#!/bin/bash

# This script creates a new external source directory with proper structure
# Usage: add-script.sh <directory-name> <source-url>

SOURCE_DIR="$1"
SOURCE_URL="$2"

# Validate arguments
if [ -z "$SOURCE_DIR" ] || [ -z "$SOURCE_URL" ]; then
  echo "Error: Both directory name and source URL are required"
  echo "Usage: add-script.sh <directory-name> <source-url>"
  exit 1
fi

# Check if directory name starts with underscore (reserved for utilities)
if [[ "$SOURCE_DIR" == _* ]]; then
  echo "Error: Directory names starting with underscore are reserved for utilities"
  exit 1
fi

# Check if directory already exists
if [ -d "$SOURCE_DIR" ]; then
  echo "Error: Directory '$SOURCE_DIR' already exists"
  exit 1
fi

# Normalize GitHub URLs by appending .git if not present
if [[ "$SOURCE_URL" == *"github.com"* ]] && [[ "$SOURCE_URL" != *".git" ]]; then
  SOURCE_URL="${SOURCE_URL}.git"
  echo "Normalized GitHub URL to: $SOURCE_URL"
fi

echo "Creating external source directory: $SOURCE_DIR"
echo "Source URL: $SOURCE_URL"

# Create directory structure
mkdir -p "$SOURCE_DIR"

# Create .source.env file
cat > "$SOURCE_DIR/.source.env" << EOF
SOURCE_TYPE=repo
SOURCE_ADDRESS=$SOURCE_URL
EOF

# Create template README.md
cat > "$SOURCE_DIR/README.md" << 'EOF'
# External Source Notes

<!-- Brief summary of the source itself, including the category of the source -->

## Key Takeaways

<!-- An ordered list of key aspects of the source. In an external code source, this may be the overall structure of the project or the technical stack it uses. In external tool documentation, it may be use cases of the tool or key features the tool offers. In philosophy and design docs, it may be key tenants of the philosophy being analyzed -->

## Applications of Takeaways

### Takeaway 1

<!-- Begin the section expanding on details from the source that relate to this key takeaway -->

#### Connections

<!-- Using other external source notes, answer the question: How does this connect to other external documentation? -->

#### Applications

<!-- Using context into the main project, answer the question: How does this help us develop the main project better? -->
EOF

echo "Directory structure created successfully"

# Automatically pull the repository
echo "Pulling repository..."
./_utils/pull-script.sh "$SOURCE_DIR"

if [ $? -eq 0 ]; then
  echo "External source '$SOURCE_DIR' created and pulled successfully!"
  echo "You can now edit the README.md file to add your notes."
else
  echo "Warning: Directory created but repository pull failed. Check the URL and try running:"
  echo "pnpm nx run xdocs:pull $SOURCE_DIR"
fi
