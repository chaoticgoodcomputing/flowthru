#!/usr/bin/env node
/**
 * `_test:solution-membership` — every first-party C# project under src/, tests/,
 * and examples/ is registered in Flowthru.slnx, and every solution entry resolves
 * to a project on disk.
 *
 * Why this is load-bearing: `flowthru:restore` (and the `docs:_build-reference`
 * docfx metadata build, which runs `docfx metadata --noRestore`) restore the
 * *solution*, not the nx project graph. nx auto-discovers projects from .csproj,
 * so a project missing from Flowthru.slnx still builds and tests locally — but on
 * a fresh CI checkout its NuGet packages are never restored, and the docs
 * reference build fails to compile it (`CS0246: type ... could not be found`).
 * This invariant turns that CI-only break into a fast workspace meta-test failure.
 *
 * Usage:
 *   node scripts/_test/solution-membership.mjs
 */

import { readFileSync, existsSync } from 'node:fs';
import { join } from 'node:path';
import { findCsproj, rel, ROOT, SRC_DIR, TESTS_DIR } from './_lib.mjs';

const SOLUTION = 'Flowthru.slnx';
const EXAMPLES_DIR = join(ROOT, 'examples');

// First-party projects deliberately kept out of the solution. Each entry must
// carry a reason. If you are adding a normal project, register it in
// Flowthru.slnx instead of adding it here.
const KNOWN_NOT_IN_SOLUTION = new Set([
  // Heavy ML/ONNX dependency footprint; excluded from the solution build.
  'examples/advanced/MnistDistributed/MnistDistributed.csproj',
  // WIP example, intentionally outside the solution.
  'examples/advanced/SpaceflightsNewTypes/SpaceflightsNewTypes.csproj',
]);

// Parse <Project Path="...csproj" /> entries from the solution.
const slnxText = readFileSync(join(ROOT, SOLUTION), 'utf8');
const solutionPaths = new Set(
  [...slnxText.matchAll(/Path="([^"]+\.csproj)"/g)].map((m) =>
    m[1].replaceAll('\\', '/')
  )
);

// First-party projects, skipping build output and archived sources.
const discovered = [
  ...findCsproj(SRC_DIR),
  ...findCsproj(TESTS_DIR),
  ...findCsproj(EXAMPLES_DIR),
]
  .map(rel)
  .filter((p) => !/(^|\/)(bin|obj|dist|archived)\//.test(p));

const missing = discovered
  .filter((p) => !solutionPaths.has(p) && !KNOWN_NOT_IN_SOLUTION.has(p))
  .sort();

const stale = [...solutionPaths]
  .filter((p) => !existsSync(join(ROOT, p)))
  .sort();

let exitCode = 0;

if (missing.length > 0) {
  exitCode = 1;
  console.error(
    `\n${missing.length} C# project(s) are not registered in ${SOLUTION}:\n`
  );
  for (const p of missing) console.error(`  ${p}`);
  console.error(
    `\nAdd a <Project Path="..." /> entry for each. Without it, a fresh CI checkout`
  );
  console.error(
    `never restores the project, and the docs reference build fails to compile it.`
  );
  console.error(
    `(If a project is intentionally excluded, add it to KNOWN_NOT_IN_SOLUTION with a reason.)\n`
  );
}

if (stale.length > 0) {
  exitCode = 1;
  console.error(
    `\n${stale.length} ${SOLUTION} entr(y/ies) reference a project missing on disk (renamed or deleted?):\n`
  );
  for (const p of stale) console.error(`  ${p}`);
  console.error('');
}

if (exitCode === 0) {
  console.log(
    `✓ All ${discovered.length} first-party projects are registered in ${SOLUTION}.`
  );
}

process.exit(exitCode);
