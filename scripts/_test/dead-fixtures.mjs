#!/usr/bin/env node
/**
 * `_test:dead-fixtures` — every fixture file under
 * `tests/helpers/Flowthru.Tests.Kits/Fixtures/` must be referenced by at least one
 * conformance subclass (typically as an entry in a `static IEnumerable<string> Fixtures`
 * member, surfaced via `[TestFixtureSource(nameof(Fixtures))]`).
 *
 * A "dead fixture" is one that exists in the kit's data catalog but isn't loaded by any
 * conformance suite. Like dead schemas, it's a false-confidence trap.
 *
 * Detection strategy:
 *   1. Enumerate `.json` fixture files under `Fixtures/`.
 *   2. For each fixture, compute its "kit-relative" path (e.g., `Flat/Simple/rows.json`).
 *   3. Search `tests/extensions/**\/*.cs` and `tests/core/**\/*.cs` for that string. If
 *      it appears literally in any source file, the fixture is alive.
 *
 * Caveats:
 *   - The literal-string match is intentional: `[TestFixtureSource]` resolves at test-
 *     run time via NUnit reflection, so a static analyzer can't follow `nameof(Fixtures)`
 *     back to the array. The kit's authoring convention is to inline fixture paths as
 *     string literals, so the match is straightforward.
 *   - A fixture referenced via dynamic concatenation (e.g., `$"{Prefix}/rows.json"`)
 *     would evade detection. None of the existing kit subclasses use that pattern, but
 *     callers of this kit who do should be aware.
 *
 * Usage:
 *   node scripts/_test/dead-fixtures.mjs
 */

import { readFileSync } from 'node:fs';
import { join, sep } from 'node:path';
import { findJson, findCs, rel, KITS_DIR, TESTS_DIR } from './_lib.mjs';

const FIXTURES_DIR = join(KITS_DIR, 'Fixtures');
const REFERENCE_SEARCH_DIRS = [
  join(TESTS_DIR, 'extensions'),
  join(TESTS_DIR, 'core'),
];

// ── Enumerate fixtures and compute their kit-relative paths ──────────────────

const fixtures = []; // { absPath, kitRelPath }
for (const fixturePath of findJson(FIXTURES_DIR)) {
  const kitRelPath = fixturePath.slice(FIXTURES_DIR.length + 1).replaceAll(sep, '/');
  fixtures.push({ absPath: fixturePath, kitRelPath });
}

// ── Build reference index: every .cs source under conformance dirs ───────────

const sourceTexts = [];
for (const dir of REFERENCE_SEARCH_DIRS) {
  for (const csFile of findCs(dir)) {
    sourceTexts.push(readFileSync(csFile, 'utf8'));
  }
}

// ── Match each fixture path against the reference index ─────────────────────

const orphans = [];
for (const { absPath, kitRelPath } of fixtures) {
  const found = sourceTexts.some((text) => text.includes(kitRelPath));
  if (!found) {
    orphans.push({ path: kitRelPath, file: rel(absPath) });
  }
}

let exitCode = 0;
if (orphans.length > 0) {
  exitCode = 1;
  console.error(
    `\n${orphans.length} kit fixture(s) exist but are not referenced by any conformance subclass:\n`
  );
  for (const { path, file } of orphans) {
    console.error(`  ${path}  (${file})`);
  }
  console.error(
    '\nA fixture is "alive" iff at least one conformance subclass references its kit-relative path'
  );
  console.error(
    '(typically in a `static IEnumerable<string> Fixtures` array on a `[TestFixtureSource]`-decorated subclass).'
  );
  console.error(
    'Either wire up a conformance subclass that exercises this fixture, or remove the fixture.\n'
  );
}

if (exitCode === 0) {
  console.log(
    `_test:dead-fixtures — all ${fixtures.length} kit fixtures are referenced by at least one conformance subclass.`
  );
}

process.exit(exitCode);
