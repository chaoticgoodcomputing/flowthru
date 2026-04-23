#!/usr/bin/env node
/**
 * Verifies that every project under src/ has a corresponding *.Tests project
 * under tests/, mirroring the same subdirectory structure.
 *
 * Expected mapping:
 *   src/{domain}/{ProjectName}/{ProjectName}.csproj
 *     → tests/{domain}/{ProjectName}.Tests/{ProjectName}.Tests.csproj
 *
 * Failures are bucketed into two categories:
 *   - Wrong path: the *.Tests project exists somewhere in tests/ but not at
 *     the correct mirrored location.
 *   - Missing: no *.Tests project exists anywhere in tests/.
 *
 * Exits 1 if any failures are found.
 *
 * Usage:
 *   node scripts/verify-test-coverage.mjs
 */

import { readdirSync, existsSync } from 'node:fs';
import { join, basename, sep } from 'node:path';
import { fileURLToPath } from 'node:url';
import { resolve, dirname } from 'node:path';

const __dirname = dirname(fileURLToPath(import.meta.url));
const ROOT = resolve(__dirname, '..');

const SRC_DIR = join(ROOT, 'src');
const TESTS_DIR = join(ROOT, 'tests');

// ── Helpers ───────────────────────────────────────────────────────────────────

/** Recursively collect all *.csproj paths under a directory. */
function findCsproj(dir) {
  const results = [];
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    if (entry.name.startsWith('.')) continue;
    const fullPath = join(dir, entry.name);
    if (entry.isDirectory()) {
      results.push(...findCsproj(fullPath));
    } else if (entry.name.endsWith('.csproj')) {
      results.push(fullPath);
    }
  }
  return results;
}

/** Relative path from ROOT, using forward slashes for legible output. */
function rel(absPath) {
  return absPath.slice(ROOT.length + 1).replaceAll(sep, '/');
}

// ── Build a lookup of existing test project names → their actual paths ─────────

const allTestCsproj = findCsproj(TESTS_DIR);
const testProjectsByName = new Map();
for (const p of allTestCsproj) {
  testProjectsByName.set(basename(p, '.csproj'), p);
}

// ── Check every src/ project ──────────────────────────────────────────────────

const srcCsproj = findCsproj(SRC_DIR);

const wrongPath = [];
const missing = [];

for (const srcPath of srcCsproj) {
  // srcPath layout: {ROOT}/src/{domain}/{ProjectName}/{ProjectName}.csproj
  const relativeSrc = srcPath.slice(SRC_DIR.length + 1); // {domain}/...
  const domain = relativeSrc.split(sep)[0];
  const projectName = basename(srcPath, '.csproj');
  const testProjectName = `${projectName}.Tests`;

  const expectedTestPath = join(
    TESTS_DIR,
    domain,
    testProjectName,
    `${testProjectName}.csproj`
  );

  if (existsSync(expectedTestPath)) continue;

  if (testProjectsByName.has(testProjectName)) {
    wrongPath.push({
      src: rel(srcPath),
      expected: rel(expectedTestPath),
      actual: rel(testProjectsByName.get(testProjectName)),
    });
  } else {
    missing.push({
      src: rel(srcPath),
      expected: rel(expectedTestPath),
    });
  }
}

// ── Report ────────────────────────────────────────────────────────────────────

let exitCode = 0;

if (wrongPath.length > 0) {
  exitCode = 1;
  console.error(
    `\n${wrongPath.length} test project(s) exist but are not at the correct mirrored path:\n`
  );
  for (const { src, expected, actual } of wrongPath) {
    console.error(`  source:   ${src}`);
    console.error(`  expected: ${expected}`);
    console.error(`  actual:   ${actual}`);
    console.error('');
  }
}

if (missing.length > 0) {
  exitCode = 1;
  console.error(`\n${missing.length} test project(s) are missing entirely:\n`);
  for (const { src, expected } of missing) {
    console.error(`  source:   ${src}`);
    console.error(`  expected: ${expected}`);
    console.error('');
  }
}

if (exitCode === 0) {
  console.log(
    'All src/ projects have a corresponding test project at the correct mirrored path.'
  );
}

process.exit(exitCode);
