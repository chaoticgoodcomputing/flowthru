#!/usr/bin/env node

/**
 * Stop hook: runs `dotnet build` and reports only warnings/errors.
 * - No C# changes in working tree: skips the build entirely.
 * - Clean build: brief success message via systemMessage.
 * - Warnings present: systemMessage with filtered warning lines.
 * - Build failure / errors: blocks the agent via hookSpecificOutput so it can
 *   address errors before concluding.
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

// Short-circuit if no C#-related files are dirty (staged or unstaged vs HEAD).
const CSHARP_PATTERN = /\.(cs|csproj|fsproj|slnx|sln|props|targets)$|^(global\.json|NuGet\.Config)$/i;

const gitStatus = spawnSync('git', ['status', '--porcelain'], {
  cwd: repoRoot,
  encoding: 'utf-8',
});
const dirtyFiles = (gitStatus.stdout || '').trim().split('\n').filter(Boolean);
const hasCSharpChanges = dirtyFiles.some(line => {
  // Each line is "XY filename" or "XY old -> new"; grab the last path segment.
  const filePath = line.slice(3).trim().split(' -> ').pop();
  return CSHARP_PATTERN.test(path.basename(filePath));
});

if (!hasCSharpChanges) {
  process.stdout.write(JSON.stringify({
    systemMessage: 'dotnet build: skipped (no C# changes detected in working tree).',
  }));
  process.exit(0);
}

const result = spawnSync('dotnet', ['build', 'Flowthru.slnx'], {
  cwd: repoRoot,
  encoding: 'utf-8',
  timeout: 180000,
});

const stdout = (result.stdout || '').trim();
const stderr = (result.stderr || '').trim();
const combined = [stdout, stderr].filter(Boolean).join('\n');

// Extract compiler diagnostics: lines containing ': warning XXXX' or ': error XXXX'.
const diagnosticLines = combined
  .split('\n')
  .filter(line => /:\s*(warning|error)\s+[A-Za-z]*\d+/i.test(line));

const hasErrors = result.status !== 0 || diagnosticLines.some(l => /:\s*error\s+/i.test(l));
const hasWarnings = diagnosticLines.some(l => /:\s*warning\s+/i.test(l));

if (hasErrors) {
  // Block the agent so it addresses errors before concluding.
  const diagnosticSummary = diagnosticLines.length > 0
    ? diagnosticLines.join('\n')
    : combined; // fall back to full output if pattern didn't match anything
  process.stdout.write(JSON.stringify({
    hookSpecificOutput: {
      hookEventName: 'Stop',
      decision: 'block',
      reason: [
        'dotnet build FAILED — address these errors before concluding.',
        '',
        diagnosticSummary,
      ].join('\n'),
    },
  }));
} else if (hasWarnings) {
  process.stdout.write(JSON.stringify({
    systemMessage: [
      'dotnet build succeeded with warnings:',
      '',
      diagnosticLines.join('\n'),
    ].join('\n'),
  }));
} else {
  process.stdout.write(JSON.stringify({
    systemMessage: 'dotnet build: succeeded with no warnings or errors.',
  }));
}
