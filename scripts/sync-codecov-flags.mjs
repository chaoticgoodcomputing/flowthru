#!/usr/bin/env node
/**
 * Synchronizes the `flags` section of codecov.yml with the set of NX
 * projects that have a `test` target.
 *
 * Flag naming convention: basename of the project's root directory.
 *   e.g. tests/Flowthru.Tests → Flowthru.Tests
 *
 * Intended to be run before `scripts/release.mjs` so the updated
 * codecov.yml is included in the release commit.
 *
 * Usage:
 *   node scripts/sync-codecov-flags.mjs           # update in place + git add
 *   node scripts/sync-codecov-flags.mjs --dry-run # preview without side effects
 */

import { execSync } from 'node:child_process';
import { mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { basename, resolve, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import yaml from 'js-yaml';

const __dirname = dirname(fileURLToPath(import.meta.url));
const ROOT = resolve(__dirname, '..');
const DRY_RUN = process.argv.includes('--dry-run');
const CODECOV_YML = resolve(ROOT, 'codecov.yml');

// ── 1. Discover all projects with a test target ───────────────────────────────

const projectsJson = execSync('pnpm nx show projects --with-target=test --json', {
  cwd: ROOT,
  encoding: 'utf8',
});
const projectNames = JSON.parse(projectsJson);

// ── 2. Map each project to a flag name (basename of root directory) ───────────
//
// Projects whose root is outside the repo's source tree (e.g. the NX
// github-actions project rooted at .github/) cannot produce .NET coverage
// files and are excluded.

const EXCLUDED_ROOTS = ['.github'];

const flagsData = [];

for (const name of projectNames) {
  let projectJson;
  try {
    projectJson = JSON.parse(
      execSync(`pnpm nx show project "${name}" --json`, {
        cwd: ROOT,
        encoding: 'utf8',
        stdio: ['pipe', 'pipe', 'ignore'],
      })
    );
  } catch {
    console.warn(`Warning: could not inspect project "${name}" — skipping.`);
    continue;
  }

  const root = projectJson.root;
  if (!root) {
    console.warn(`Warning: project "${name}" has no root — skipping.`);
    continue;
  }

  if (EXCLUDED_ROOTS.includes(root)) {
    continue;
  }

  flagsData.push({ flag: basename(root), root });
}

flagsData.sort((a, b) => a.flag.localeCompare(b.flag));

console.log(`Derived ${flagsData.length} flags:`);
for (const { flag } of flagsData) {
  console.log(`  ${flag}`);
}

// ── 3. Write dist/codecov-flags.json for use by CI upload step ───────────────

const DIST_DIR = resolve(ROOT, 'dist');
const FLAGS_JSON = resolve(DIST_DIR, 'codecov-flags.json');

mkdirSync(DIST_DIR, { recursive: true });
writeFileSync(FLAGS_JSON, JSON.stringify(flagsData, null, 2) + '\n', 'utf8');
console.log(`\nWrote ${FLAGS_JSON}`);

// ── 4. Update flags in codecov.yml ───────────────────────────────────────────

const raw = readFileSync(CODECOV_YML, 'utf8');
const doc = yaml.load(raw);

const newFlags = Object.fromEntries(
  flagsData.map(({ flag }) => [flag, { carryforward: true }])
);

doc.flags = newFlags;

const updated = yaml.dump(doc, { lineWidth: -1 });

if (updated === raw) {
  console.log('\ncodecov.yml flags already up to date — no changes needed.');
  process.exit(0);
}

if (!DRY_RUN) {
  writeFileSync(CODECOV_YML, updated, 'utf8');
  execSync(`git add "${CODECOV_YML}"`);
  console.log('\n✓ Updated codecov.yml and staged for commit.');
} else {
  console.log('\n[dry-run] Would write the following to codecov.yml:');
  console.log(updated);
}
