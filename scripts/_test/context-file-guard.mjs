#!/usr/bin/env node
/**
 * `_test:context-file-guard` — no `CONTEXT.md` or `CONTEXT-MAP.md` exists anywhere
 * in the repo. Ever.
 *
 * Why a TEST and not a prose rule: four INSTALLED skills actively instruct agents
 * to create one, and they are vendored under `.agents/skills/` — they will keep
 * saying it through every upstream update:
 *
 *   - `domain-modeling`  — "If no CONTEXT.md exists, create one when the first term is resolved"
 *   - `improve-codebase-architecture` — "Add the term to CONTEXT.md. Create the file lazily…"
 *   - `triage`           — "updating CONTEXT.md/ADRs inline"
 *   - `wait-what`        — "follow CONTEXT-MAP.md to the right one"
 *
 * `docs/agents/domain.md` says to ignore those instructions here, but a prose rule
 * only works if the agent read it — and `domain-modeling`'s instruction fires
 * mid-task, long after any session-start context. This fails loudly at a fixed
 * time instead. Flowthru's equivalents are per-context `CONTRIBUTING.md`
 * glossaries and the generated context map in root `/CONTRIBUTING.md` (ADR-0004).
 *
 * Scans EVERYTHING, vendored trees included — a stray file inside `.agents/` is
 * exactly the kind of accident this guards against.
 *
 * Usage:
 *   node scripts/_test/context-file-guard.mjs
 */

import { walkFiles, reportFailures, reportOk } from './_domain.mjs';

const TARGET = '_test:context-file-guard';
const BANNED = new Set(['CONTEXT.md', 'CONTEXT-MAP.md']);

const offenders = walkFiles((name) => BANNED.has(name), { includeVendored: true });

if (offenders.length > 0) {
  reportFailures(
    TARGET,
    `${offenders.length} banned context file(s) — Flowthru does not use CONTEXT.md or CONTEXT-MAP.md:`,
    offenders,
    'Fix: delete the file and move its content to the right home —\n' +
      '    a context glossary  → that directory\'s CONTRIBUTING.md (## Glossary)\n' +
      '    a list of contexts  → the generated context map in /CONTRIBUTING.md\n' +
      '                          (run `node scripts/generate-context-map.mjs`)',
    'An installed skill (domain-modeling, improve-codebase-architecture, triage,\n' +
      '  wait-what) told you to create this. Those instructions are generic; ignore\n' +
      '  them in this repo. See docs/agents/domain.md § "Never create CONTEXT.md".',
  );
}

reportOk(TARGET, 'no CONTEXT.md / CONTEXT-MAP.md in the repo');
