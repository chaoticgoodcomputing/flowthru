#!/usr/bin/env node
/**
 * `_test:row-features-inventory` — every flag declared on `FormatRowFeatures` has a
 * corresponding row in the row-shape feature surface table maintained at
 * `docs/scratch/data-extension-contract.md` §2 (or its successor at
 * `src/extensions/CONTRIBUTING.md` after Phase E).
 *
 * Catches drift between the type and the documentation: a contributor adds a new flag
 * to `FormatRowFeatures` and the surface table goes stale, or a flag is removed but
 * the table still references it. The meta-test prompts a paired update.
 *
 * Detection strategy:
 *   1. Parse `FormatRowFeatures.cs` — extract every `Supports*` property name.
 *   2. Read the surface-table source document — extract every flag-shaped reference.
 *   3. Flags-without-table-rows fail; table-rows-without-flags emit a warning (the
 *      surface table can legitimately mention features the type doesn't yet expose).
 *
 * Usage:
 *   node scripts/_test/row-features-inventory.mjs
 */

import { existsSync, readFileSync } from 'node:fs';
import { join } from 'node:path';
import { rel, ROOT, SRC_DIR } from './_lib.mjs';

const FEATURES_TYPE_PATH = join(
  SRC_DIR,
  'core',
  'Flowthru.Core',
  'Data',
  'Capabilities',
  'FormatRowFeatures.cs'
);

// Source-of-truth surface table during Phase B/C. Migrates to
// src/extensions/CONTRIBUTING.md once Phase E lands.
const SURFACE_TABLE_PATH = join(
  ROOT,
  'docs',
  'scratch',
  'data-extension-contract.md'
);

if (!existsSync(FEATURES_TYPE_PATH)) {
  console.error(
    `\nFormatRowFeatures.cs not found at ${FEATURES_TYPE_PATH}.\n`
      + 'The row-features inventory check cannot run without the type definition.\n'
  );
  process.exit(1);
}

if (!existsSync(SURFACE_TABLE_PATH)) {
  console.error(
    `\nSurface-table source document not found at ${SURFACE_TABLE_PATH}.\n`
      + 'The row-features inventory check cannot run without it.\n'
  );
  process.exit(1);
}

// ── Extract Supports* property names from FormatRowFeatures.cs ──────────────

const featuresText = readFileSync(FEATURES_TYPE_PATH, 'utf8');
const propRe = /public\s+bool\s+(Supports[A-Z][A-Za-z0-9_]*)\s*\{[^}]*\}/g;
const declaredFlags = new Set();
let m;
while ((m = propRe.exec(featuresText)) !== null) {
  declaredFlags.add(m[1]);
}

// ── Extract flag-shaped references from the surface table document ──────────

// The row-shape feature surface table appears in §2 of data-extension-contract.md as
// a Markdown table. We don't try to parse the table structure; we just look for
// `Supports*` mentions anywhere in the document, which covers both the table itself
// and its prose context.
const tableText = readFileSync(SURFACE_TABLE_PATH, 'utf8');
const referencedFlags = new Set();
const refRe = /Supports[A-Z][A-Za-z0-9_]*/g;
let r;
while ((r = refRe.exec(tableText)) !== null) {
  referencedFlags.add(r[0]);
}

// ── Compare ─────────────────────────────────────────────────────────────────

const flagsWithoutTableRow = [...declaredFlags].filter((f) => !referencedFlags.has(f));
const tableRowsWithoutFlag = [...referencedFlags].filter((f) => !declaredFlags.has(f));

let exitCode = 0;
if (flagsWithoutTableRow.length > 0) {
  exitCode = 1;
  console.error(
    `\n${flagsWithoutTableRow.length} flag(s) declared on FormatRowFeatures lack a reference in the surface table:\n`
  );
  for (const flag of flagsWithoutTableRow) {
    console.error(`  ${flag}`);
  }
  console.error(`\nUpdate ${rel(SURFACE_TABLE_PATH)} to document the flag(s).`);
  console.error('Each flag should map to a row in the row-shape feature surface table.\n');
}

if (tableRowsWithoutFlag.length > 0) {
  // Warning, not failure. The surface table can mention features that aren't yet
  // implemented as flags — the table is the design surface; the type is the
  // implemented surface.
  console.warn(
    `\nWarning: ${tableRowsWithoutFlag.length} surface-table reference(s) have no matching FormatRowFeatures flag:\n`
  );
  for (const flag of tableRowsWithoutFlag) {
    console.warn(`  ${flag}`);
  }
  console.warn(
    '\nThis is allowed (the surface table can document forthcoming features), but if these'
  );
  console.warn(
    'are no longer planned, consider removing them from the table to avoid drift.\n'
  );
}

if (exitCode === 0) {
  console.log(
    `_test:row-features-inventory — all ${declaredFlags.size} FormatRowFeatures flag(s) are referenced in the surface table.`
  );
}

process.exit(exitCode);
