#!/usr/bin/env node

/**
 * SessionStart hook: feeds /CONTRIBUTING.md into agent context so Flowthru's
 * fail-fast philosophy and the three error phases are loaded before any work.
 */

const fs = require('fs');
const path = require('path');

const repoRoot = path.resolve(__dirname, '../../../..');
const contributingPath = path.join(repoRoot, 'CONTRIBUTING.md');

let contributing;
try {
  contributing = fs.readFileSync(contributingPath, 'utf-8');
} catch (err) {
  process.stderr.write(`on-start/load-contributing: could not read CONTRIBUTING.md: ${err.message}\n`);
  process.exit(0);
}

const output = {
  hookSpecificOutput: {
    hookEventName: 'SessionStart',
    additionalContext: [
      'The following is CONTRIBUTING.md for this repository.',
      'Read and internalize it before proceeding — pay particular attention to',
      'the three error phases and where validations belong.',
      '',
      contributing,
    ].join('\n'),
  },
};

process.stdout.write(JSON.stringify(output));
