#!/usr/bin/env node

/**
 * SessionStart hook: feeds /examples/CONTRIBUTING.md into agent context so
 * the Flow Developer + Catalog Developer vocabulary (Flow, Step, Schema,
 * Catalog, Catalog Item, Data category, Configuration Item, DAG, Dry-run
 * mode, plus the Diátaxis split for starter/advanced and the per-example
 * requirements) is available from turn one.
 *
 * Rationale: the examples context is the most necessary portion of the
 * multi-context vocabulary — Core and Extension developers need to know
 * what the product is and how it's used by Flow/Catalog Devs before they
 * can reason about extending or curating it. Other per-context CONTRIBUTING
 * files (src/extensions/, src/core/, tests/*) are read on demand by the
 * agent when working in those areas.
 */

const fs = require('fs');
const path = require('path');

const repoRoot = path.resolve(__dirname, '../../../..');
const examplesContributingPath = path.join(repoRoot, 'examples/CONTRIBUTING.md');

let examplesContributing;
try {
  examplesContributing = fs.readFileSync(examplesContributingPath, 'utf-8');
} catch (err) {
  process.stderr.write(`on-start/load-examples-contributing: could not read examples/CONTRIBUTING.md: ${err.message}\n`);
  process.exit(0);
}

const output = {
  hookSpecificOutput: {
    hookEventName: 'SessionStart',
    additionalContext: [
      'The following is /examples/CONTRIBUTING.md — Flowthru\'s Flow Developer',
      'and Catalog Developer context: vocabulary, example structure conventions,',
      'and per-example requirements. This is the audience-scoped view of what',
      'Flowthru looks like to downstream users. Other per-context CONTRIBUTING',
      'files (src/extensions/, src/core/, tests/*) are read on demand when',
      'working in those areas.',
      '',
      examplesContributing,
    ].join('\n'),
  },
};

process.stdout.write(JSON.stringify(output));
