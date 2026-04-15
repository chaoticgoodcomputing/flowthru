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

/**
 * Parse dotnet test output to extract test counts.
 * Aggregates counts across all test runs.
 * Returns: { passed, failed, skipped, inconclusive, total }
 */
function parseTestCounts(output) {
  // Match patterns like: "Failed:     6, Passed:   156, Skipped:     0, Total:   162"
  const regex = /Failed:\s*(\d+),\s*Passed:\s*(\d+),\s*Skipped:\s*(\d+),\s*Total:\s*(\d+)/g;
  const counts = { passed: 0, failed: 0, skipped: 0, total: 0 };

  let match;
  while ((match = regex.exec(output)) !== null) {
    counts.failed += parseInt(match[1], 10);
    counts.passed += parseInt(match[2], 10);
    counts.skipped += parseInt(match[3], 10);
    counts.total += parseInt(match[4], 10);
  }

  // Inconclusive = Total - Passed - Failed - Skipped
  counts.inconclusive = counts.total - counts.passed - counts.failed - counts.skipped;

  return counts;
}

/**
 * Format test counts for display.
 */
function formatTestCounts(counts) {
  return [
    `Passed:       ${counts.passed}`,
    `Failed:       ${counts.failed}`,
    `Skipped:      ${counts.skipped}`,
    `Inconclusive: ${counts.inconclusive}`,
    `Total:        ${counts.total}`,
  ].join('\n');
}

/**
 * Extract full failure blocks from dotnet test output.
 * A block starts with an indented "Failed <TestName>" line and continues
 * until the next dotnet test summary line or end of output.
 */
function extractFailureBlocks(output) {
  const lines = output.split('\n');
  const resultLines = [];
  let capturing = false;

  for (const line of lines) {
    if (/^\s+Failed\s+\S/.test(line)) {
      // Start of a new failure block — blank separator between blocks.
      if (resultLines.length > 0) resultLines.push('');
      capturing = true;
      resultLines.push(line);
    } else if (capturing) {
      // Stop at the dotnet test run summary line (e.g. "Failed!  - Failed: 6, Passed: ...")
      if (/^(Failed!|Passed!)\s+-\s+Failed:/.test(line.trim())) {
        capturing = false;
      } else {
        resultLines.push(line);
      }
    }
  }

  return resultLines.join('\n').trim();
}

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

// Parse test counts from output.
const testCounts = parseTestCounts(combined);
const countsDisplay = formatTestCounts(testCounts);

// NX does not always propagate the dotnet exit code — fall back to parsed counts.
if (result.status !== 0 || testCounts.failed > 0) {
  const failureBlocks = extractFailureBlocks(combined);
  const summary = failureBlocks.length > 0 ? failureBlocks : combined;

  process.stdout.write(JSON.stringify({
    hookSpecificOutput: {
      hookEventName: 'Stop',
      decision: 'block',
      reason: [
        `nx affected test FAILED (affected: ${affectedProjects.join(', ')}) — address these failures before concluding.`,
        '',
        'Test Summary:',
        countsDisplay,
        '',
        summary,
      ].join('\n'),
    },
  }));
  process.exit(1);
} else {
  process.stdout.write(JSON.stringify({
    systemMessage: [
      `nx affected test: passed (${affectedProjects.length} project(s): ${affectedProjects.join(', ')}).`,
      '',
      'Test Summary:',
      countsDisplay,
    ].join('\n'),
  }));
}
