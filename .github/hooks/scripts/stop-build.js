#!/usr/bin/env node

/**
 * Stop hook: runs `dotnet build` before the agent yields back to the user and
 * injects the result as a system message. Build errors surface immediately so
 * the agent can address them before the next prompt rather than discovering
 * them later.
 */

const { spawnSync } = require('child_process');
const path = require('path');

const repoRoot = path.resolve(__dirname, '../../..');

const result = spawnSync('dotnet', ['build', 'Flowthru.slnx'], {
  cwd: repoRoot,
  encoding: 'utf-8',
  timeout: 180000,
});

const stdout = (result.stdout || '').trim();
const stderr = (result.stderr || '').trim();
const combined = [stdout, stderr].filter(Boolean).join('\n');

const succeeded = result.status === 0;
const headline = succeeded
  ? 'dotnet build succeeded.'
  : 'dotnet build FAILED — address these errors before concluding.';

const output = {
  systemMessage: [
    `## Build Check (dotnet build)`,
    '',
    headline,
    '',
    '```',
    combined,
    '```',
  ].join('\n'),
};

process.stdout.write(JSON.stringify(output));
