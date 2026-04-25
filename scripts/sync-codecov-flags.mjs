#!/usr/bin/env node
/**
 * Synchronizes the `flags` and `component_management` sections of codecov.yml.
 *
 * Flags fall into two groups:
 *
 *   Standard test projects (all NX projects with a `test` target except
 *   Flowthru.Tests.Examples):
 *     Flag name = basename of the project's root directory.
 *     e.g. tests/core/Flowthru.Core.Tests → Flowthru.Core.Tests
 *
 *   Example projects (one per .csproj under examples/, excluding archived/ and
 *   item-templates/):
 *     Flag name = csproj basename without extension.
 *     Root      = tests/integration/Flowthru.Tests.Examples/TestResults/{Name}
 *     Plus one "FUnit" flag for the FUnit auto-discovery tests that run
 *     alongside examples.
 *
 * Components: derived from src/ projects (csproj files).
 *   Component id  = slugified project directory basename.
 *   Component name = project directory basename.
 *   Component path = src/<domain>/<Project>/**
 *   Used for per-library aggregate coverage badges and status checks.
 *
 * Intended to be run before `scripts/release.mjs` so the updated
 * codecov.yml is included in the release commit.
 *
 * Usage:
 *   node scripts/sync-codecov-flags.mjs           # update in place + git add
 *   node scripts/sync-codecov-flags.mjs --dry-run # preview without side effects
 */

import { execSync } from 'node:child_process';
import { mkdirSync, readFileSync, writeFileSync, globSync } from 'node:fs';
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
//
// "tests" is the root structural mirror-check project (root: "tests/"). Its
// root directory is a parent of every test project, so including it would
// cause the upload script to re-upload all coverage files under a single
// umbrella flag, duplicating every report.
//
// "tests/integration/Flowthru.Tests.Examples" is handled separately below —
// it produces per-example coverage files in named subdirectories, not a single
// combined report. Including it here would upload all those files under one flag.
//
// Projects rooted under "examples/" are also handled by the per-example glob
// below — exclude them here to avoid double-counting.
const EXCLUDED_ROOTS = [
  '.github',
  'tests',
  'tests/integration/Flowthru.Tests.Examples',
];

const EXCLUDED_ROOT_PREFIXES = ['examples/'];

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

  if (EXCLUDED_ROOT_PREFIXES.some((prefix) => root.startsWith(prefix))) {
    continue;
  }

  flagsData.push({ flag: basename(root), root });
}

// ── 2b. Add per-example flags ─────────────────────────────────────────────────
//
// Flowthru.Tests.Examples runs each example project in its own dotnet test
// invocation, writing coverage to TestResults/{ExampleName}/coverage.cobertura.xml.
// We add one flag per example and one for the FUnit auto-discovery suite.

const EXAMPLES_RESULTS_ROOT =
  'tests/integration/Flowthru.Tests.Examples/TestResults';

const exampleCsprojPaths = globSync(
  'examples/**/*.csproj',
  { cwd: ROOT }
).filter(
  (p) =>
    !p.includes('/archived/') &&
    !p.includes('/item-templates/') &&
    !p.includes('/obj/')
);

for (const csproj of exampleCsprojPaths) {
  const exampleName = basename(csproj, '.csproj');
  flagsData.push({
    flag: exampleName,
    root: `${EXAMPLES_RESULTS_ROOT}/${exampleName}`,
  });
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

// ── 5. Derive components from src/ projects ───────────────────────────────────
//
// Each .csproj under src/ becomes a component whose path covers all files under
// that project's directory. This lets Codecov aggregate coverage across all test
// suites (unit, integration, example) for each library, enabling per-library
// badges and status checks.
//
// component_id: lowercase slug  e.g. "flowthru_core"
// name:         display name    e.g. "Flowthru.Core"
// paths:        [ "src/core/Flowthru.Core/**" ]

const csprojPaths = globSync('src/**/*.csproj', { cwd: ROOT });

const components = csprojPaths
  .map((csproj) => {
    // csproj = "src/core/Flowthru.Core/Flowthru.Core.csproj"
    const projectDir = csproj.replace(/\/[^/]+\.csproj$/, '');
    const name = basename(projectDir);
    const component_id = name.toLowerCase().replace(/\./g, '_');
    return { component_id, name, paths: [`${projectDir}/**`] };
  })
  .sort((a, b) => a.name.localeCompare(b.name));

console.log(`\nDerived ${components.length} components:`);
for (const { name } of components) {
  console.log(`  ${name}`);
}

doc.component_management = {
  individual_components: components,
};

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
