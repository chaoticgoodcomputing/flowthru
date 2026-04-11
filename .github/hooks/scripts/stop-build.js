#!/usr/bin/env node

/**
 * Stop hook: runs `nx affected -t test --base="HEAD"` and blocks on failures.
 * - No affected projects: skips entirely.
 * - All tests pass: brief success message via systemMessage.
 * - Any test failures: blocks the agent via hookSpecificOutput so it can
 *   address failures before concluding.
 *
 * Reads stdin to check stop_hook_active and avoid infinite loops.
 */

const { spawnSync } = require('child_process');
const path = require('path');

// Read and parse stdin to detect re-entry.
let hookInput = {};
try {
  const raw = require('fs').readFileSync('/dev/stdin', 'utf-8').trim();
  if (raw) hookInput = JSON.parse(raw);
} catch (_) {
  // stdin unavailable or empty — treat as first invocation.
}

// Prevent infinite loop: if we are already in a stop-hook continuation, exit cleanly.
if (hookInput.stop_hook_active) {
  process.exit(0);
}

const repoRoot = path.resolve(__dirname, '../../..');

// Determine which projects are affected by uncommitted working tree changes.
const affectedResult = spawnSync(
  'pnpm',
  ['nx', 'show', 'projects', '--affected', '--base=HEAD', '--json'],
  { cwd: repoRoot, encoding: 'utf-8', timeout: 30000 },
);

let affectedProjects = [];
try {
  affectedProjects = JSON.parse(affectedResult.stdout || '[]');
} catch (_) {
  // Parse failure — treat as no affected projects.
}

if (affectedProjects.length === 0) {
  process.stdout.write(JSON.stringify({
    systemMessage: 'nx affected test: skipped (no affected projects in working tree).',
  }));
  process.exit(0);
}

// Run tests for all affected projects.
const result = spawnSync(
  'pnpm',
  [
    'nx', 'affected', '-t', 'test',
    '--base=HEAD',
    '--output-style=stream',
    '--logger', 'console;verbosity=minimal',
  ],
  { cwd: repoRoot, encoding: 'utf-8', timeout: 300000 },
);

const stdout = (result.stdout || '').trim();
const stderr = (result.stderr || '').trim();
const combined = [stdout, stderr].filter(Boolean).join('\n');

if (result.status !== 0) {
  // Extract failed test lines for a focused summary.
  const failureLines = combined
    .split('\n')
    .filter(line => /failed|error|FAILED|ERROR/i.test(line))
    .slice(0, 40); // cap at 40 lines to avoid overwhelming the agent

  const summary = failureLines.length > 0 ? failureLines.join('\n') : combined;

  process.stdout.write(JSON.stringify({
    hookSpecificOutput: {
      hookEventName: 'Stop',
      decision: 'block',
      reason: [
        `nx affected test FAILED (affected: ${affectedProjects.join(', ')}) — address these failures before concluding.`,
        '',
        summary,
      ].join('\n'),
    },
  }));
} else {
  process.stdout.write(JSON.stringify({
    systemMessage: `nx affected test: passed (${affectedProjects.length} project(s): ${affectedProjects.join(', ')}).`,
  }));
}
