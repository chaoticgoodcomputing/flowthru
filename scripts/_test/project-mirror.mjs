#!/usr/bin/env node
/**
 * `_test:project-mirror` — every `src/{domain}/{Project}` has a matching
 * `tests/{domain}/{Project}.Tests` at the mirrored path.
 *
 *   src/{domain}/{ProjectName}/{ProjectName}.csproj
 *     → tests/{domain}/{ProjectName}.Tests/{ProjectName}.Tests.csproj
 *
 * Failures fall into two categories:
 *   - "wrong path"  — a test project with the right name exists somewhere else.
 *   - "missing"     — no test project with that name exists at all.
 *
 * Exits with non-zero on any failure. Extracted from the previous monolithic
 * `verify-test-coverage.mjs` Pass 1.
 *
 * Usage:
 *   node scripts/_test/project-mirror.mjs
 */

import { existsSync } from 'node:fs';
import { join, basename, sep } from 'node:path';
import { findCsproj, rel, ROOT, SRC_DIR, TESTS_DIR } from './_lib.mjs';

const allTestCsproj = findCsproj(TESTS_DIR);
const testProjectsByName = new Map();
for (const p of allTestCsproj) {
  testProjectsByName.set(basename(p, '.csproj'), p);
}

const srcCsproj = findCsproj(SRC_DIR);
const wrongPath = [];
const missing = [];

for (const srcPath of srcCsproj) {
  const relativeSrc = srcPath.slice(SRC_DIR.length + 1);
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
    '_test:project-mirror — all src/ projects have a corresponding test project at the correct mirrored path.'
  );
}

process.exit(exitCode);
