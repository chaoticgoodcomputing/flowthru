#!/usr/bin/env node
/**
 * Verifies test-coverage discipline at two levels:
 *
 * 1. **Project-mirror check.** Every project under `src/` has a corresponding `*.Tests`
 *    project under `tests/`, at the mirrored subdirectory.
 *
 *      src/{domain}/{ProjectName}/{ProjectName}.csproj
 *        → tests/{domain}/{ProjectName}.Tests/{ProjectName}.Tests.csproj
 *
 * 2. **Conformance-presence check (extension surfaces).** Every first-party Flowthru
 *    extension (`src/extensions/Flowthru.Extensions.*`) that implements one of the Core
 *    extension surfaces — `IStorageAdapter<T>`, `IFormatSerializer<TRow>`, `IStorageMedium`,
 *    `IStorageMediumProvider`, `IMetadataProvider` — must have at least one corresponding
 *    `*Conformance` subclass in its sibling `tests/extensions/<Ext>.Tests/` project.
 *
 *    The check is at the *extension level*, not per implementor: an extension passes if
 *    any conformance subclass for the relevant surface kind exists, regardless of which
 *    specific implementor it covers. This admits patterns like Gql, where `GqlQuery-
 *    StorageAdapter` doesn't fit the kit's contract (read-only deferred handle) but its
 *    sibling `GqlStorageAdapter` does and has a conformance subclass. The extension-level
 *    aggregation also lets multi-shape conformance (e.g., EFCore's flat + nested entities)
 *    cover multiple implementors with one parameterized base.
 *
 *    Container-adapter conformance (`IContainerAdapter<TContainer, TRow>`) is intentionally
 *    excluded from the check: the kit does not yet codify a `ContainerAdapterConformance`
 *    base, and the only first-party container adapter outside Core (MLNet's
 *    `DataViewContainerAdapter`) was descoped from the conformance initiative because ONNX
 *    is structurally different from the row-oriented surfaces the kit targets. When
 *    container conformance is added, restore it to the SURFACES list below.
 *
 * Failures in either pass exit 1.
 *
 * Usage:
 *   node scripts/verify-test-coverage.mjs
 */

import { readdirSync, readFileSync, existsSync } from 'node:fs';
import { join, basename, sep } from 'node:path';
import { fileURLToPath } from 'node:url';
import { resolve, dirname } from 'node:path';

const __dirname = dirname(fileURLToPath(import.meta.url));
const ROOT = resolve(__dirname, '..');

const SRC_DIR = join(ROOT, 'src');
const TESTS_DIR = join(ROOT, 'tests');

// ── Surfaces in scope for conformance enforcement ────────────────────────────
//
// Each surface maps from the Core interface name (matched on the impl side) to the
// kit base class name (matched on the test side).
//
const SURFACES = [
  { iface: 'IStorageAdapter', base: 'StorageAdapterConformance' },
  { iface: 'IFormatSerializer', base: 'FormatSerializerConformance' },
  { iface: 'IStorageMedium', base: 'StorageMediumConformance' },
  { iface: 'IStorageMediumProvider', base: 'StorageMediumProviderConformance' },
  { iface: 'IMetadataProvider', base: 'MetadataProviderConformance' },
];

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

/** Recursively collect all *.cs files under a directory, skipping obj/bin/TestResults. */
function findCs(dir) {
  const results = [];
  if (!existsSync(dir)) return results;
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    if (entry.name.startsWith('.')) continue;
    if (
      entry.name === 'obj' ||
      entry.name === 'bin' ||
      entry.name === 'TestResults'
    ) {
      continue;
    }
    const fullPath = join(dir, entry.name);
    if (entry.isDirectory()) {
      results.push(...findCs(fullPath));
    } else if (entry.name.endsWith('.cs')) {
      results.push(fullPath);
    }
  }
  return results;
}

/** Relative path from ROOT, using forward slashes for legible output. */
function rel(absPath) {
  return absPath.slice(ROOT.length + 1).replaceAll(sep, '/');
}

/**
 * Returns the set of surface keys (e.g., 'IStorageAdapter') the file declares as base
 * types or implemented interfaces. Matches `: IStorageAdapter<` and similar across multi-
 * line declarations by collapsing whitespace before regex matching.
 */
function detectSurfaceImpls(filePath) {
  const text = readFileSync(filePath, 'utf8');
  const collapsed = text.replace(/\s+/g, ' ');
  const found = new Set();
  for (const { iface } of SURFACES) {
    // Match `: IStorageAdapter<` (with or without surrounding whitespace) anywhere a base
    // list could appear. Also match `, IStorageAdapter<` (additional interfaces).
    const re = new RegExp(`[:,]\\s*${iface}(?:<|\\s)`, 'g');
    if (re.test(collapsed)) {
      found.add(iface);
    }
  }
  return found;
}

/**
 * Returns the set of conformance-base keys (e.g., 'StorageAdapterConformance') the file
 * declares as a base class.
 */
function detectConformanceBases(filePath) {
  const text = readFileSync(filePath, 'utf8');
  const collapsed = text.replace(/\s+/g, ' ');
  const found = new Set();
  for (const { base } of SURFACES) {
    // Match `: StorageAdapterConformance<` or `: StorageAdapterConformance ` (no generics).
    const re = new RegExp(`[:,]\\s*${base}(?:<|\\s|$)`, 'g');
    if (re.test(collapsed)) {
      found.add(base);
    }
  }
  return found;
}

// ── Pass 1: build a lookup of existing test project names → actual paths ──────

const allTestCsproj = findCsproj(TESTS_DIR);
const testProjectsByName = new Map();
for (const p of allTestCsproj) {
  testProjectsByName.set(basename(p, '.csproj'), p);
}

// ── Pass 1: project-mirror check ──────────────────────────────────────────────

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

// ── Pass 2: conformance-presence check ────────────────────────────────────────

const EXTENSIONS_SRC = join(SRC_DIR, 'extensions');
const EXTENSIONS_TESTS = join(TESTS_DIR, 'extensions');

const conformanceFailures = [];

if (existsSync(EXTENSIONS_SRC)) {
  for (const entry of readdirSync(EXTENSIONS_SRC, { withFileTypes: true })) {
    if (!entry.isDirectory()) continue;
    if (!entry.name.startsWith('Flowthru.Extensions.')) continue;

    const extName = entry.name;
    const extSrcDir = join(EXTENSIONS_SRC, extName);
    const extTestsDir = join(EXTENSIONS_TESTS, `${extName}.Tests`);

    // Collect all surface kinds this extension implements.
    const implementedSurfaces = new Set();
    for (const csFile of findCs(extSrcDir)) {
      const impls = detectSurfaceImpls(csFile);
      for (const surface of impls) {
        implementedSurfaces.add(surface);
      }
    }

    if (implementedSurfaces.size === 0) {
      // Extension implements no kit-tracked surface (e.g., EFCore.Bulk supplies saveFunc
      // delegates only). Nothing to enforce.
      continue;
    }

    // Collect all conformance bases the test project covers.
    const coveredBases = new Set();
    if (existsSync(extTestsDir)) {
      for (const csFile of findCs(extTestsDir)) {
        const bases = detectConformanceBases(csFile);
        for (const base of bases) {
          coveredBases.add(base);
        }
      }
    }

    // Map each implemented surface to its expected conformance base, then check coverage.
    const missingBases = [];
    for (const surface of implementedSurfaces) {
      const entry = SURFACES.find((s) => s.iface === surface);
      if (!entry) continue;
      if (!coveredBases.has(entry.base)) {
        missingBases.push({ surface, base: entry.base });
      }
    }

    if (missingBases.length > 0) {
      conformanceFailures.push({
        extension: extName,
        testsDir: existsSync(extTestsDir) ? rel(extTestsDir) : '<missing>',
        missing: missingBases,
      });
    }
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

if (conformanceFailures.length > 0) {
  exitCode = 1;
  console.error(
    `\n${conformanceFailures.length} extension(s) implement Core surfaces but lack conformance coverage:\n`
  );
  for (const { extension, testsDir, missing } of conformanceFailures) {
    console.error(`  extension: ${extension}`);
    console.error(`  tests:     ${testsDir}`);
    for (const { surface, base } of missing) {
      console.error(`    - implements ${surface} but no ${base} subclass found`);
    }
    console.error('');
  }
  console.error(
    'See `tests/README.md` (Extension Conformance Kits) and ' +
      '`docs/scratch/extension-conformance-kits.md` for the kit pattern.\n'
  );
}

if (exitCode === 0) {
  console.log(
    'All src/ projects have a corresponding test project at the correct mirrored path,\n' +
      'and every first-party extension surface implementor has a conformance subclass.'
  );
}

process.exit(exitCode);
