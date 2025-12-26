#!/usr/bin/env node

/**
 * Commit message validator for Conventional Commits
 * Used by Husky to validate commit messages before they're created
 */

const fs = require('fs');
const path = require('path');

const commitMsgFile = process.argv[2];
const commitMsg = fs.readFileSync(commitMsgFile, 'utf-8').trim();

// Conventional Commits regex
// Format: type(scope?): subject
const conventionalCommitRegex = /^(feat|fix|docs|style|refactor|perf|test|build|ci|chore|revert)(\(.+\))?!?: .{1,}$/;

// Allow merge commits, revert commits, and release commits
if (
  commitMsg.startsWith('Merge') ||
  commitMsg.startsWith('Revert') ||
  commitMsg.startsWith('chore(release)')
) {
  process.exit(0);
}

if (!conventionalCommitRegex.test(commitMsg.split('\n')[0])) {
  console.error(`
❌ Invalid commit message format!

Your commit message:
  ${commitMsg.split('\n')[0]}

Required format:
  <type>[optional scope]: <description>

Examples:
  feat: add JSON catalog support
  fix: resolve CSV parsing error
  feat!: redesign catalog API (breaking change)
  docs(readme): update installation instructions

Valid types:
  feat     - New feature
  fix      - Bug fix
  docs     - Documentation only
  style    - Code style (formatting, etc.)
  refactor - Code refactoring
  perf     - Performance improvement
  test     - Adding/updating tests
  build    - Build system changes
  ci       - CI configuration changes
  chore    - Other changes
  revert   - Revert a previous commit

For more info: https://www.conventionalcommits.org/
`);
  process.exit(1);
}

process.exit(0);
