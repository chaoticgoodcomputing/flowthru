#!/usr/bin/env node

/**
 * SessionStart hook: injects CONTRIBUTING.md into agent context at the start
 * of each new session so the agent has Flowthru's architecture and conventions
 * loaded before any work begins.
 */

const fs = require('fs');
const path = require('path');

const repoRoot = path.resolve(__dirname, '../../..');
const contributingPath = path.join(repoRoot, 'CONTRIBUTING.md');

let contributing;
try {
  contributing = fs.readFileSync(contributingPath, 'utf-8');
} catch (err) {
  process.stderr.write(`session-start: could not read CONTRIBUTING.md: ${err.message}\n`);
  process.exit(0);
}

const output = {
  systemMessage: [
    'The following is CONTRIBUTING.md for this repository.',
    'Read and internalize it before proceeding — pay particular attention to',
    'the three error phases and where validations belong.',
    '',
    contributing,
  ].join('\n'),
};

process.stdout.write(JSON.stringify(output));
