#!/usr/bin/env node
/**
 * `_test:conformance-presence` — every first-party Flowthru extension that implements a
 * Core extension surface (`IStorageAdapter<T>`, `IFormatSerializer<TRow>`, `IStorageMedium`,
 * `IStorageMediumProvider`, `IMetadataProvider`) has at least one corresponding `*Conformance`
 * subclass in its sibling `tests/extensions/<Ext>.Tests/` project.
 *
 * The check is at the *extension level*, not per implementor: an extension passes if any
 * conformance subclass for the relevant surface kind exists, regardless of which specific
 * implementor it covers. This admits patterns like Gql, where `GqlQueryStorageAdapter`
 * doesn't fit the kit's contract (read-only deferred handle) but its sibling
 * `GqlStorageAdapter` does and has a conformance subclass. The extension-level
 * aggregation also lets multi-shape conformance (e.g., EFCore's flat + nested entities)
 * cover multiple implementors with one parameterized base.
 *
 * Container-adapter conformance (`IContainerAdapter<TContainer, TRow>`) is intentionally
 * excluded: the kit does not yet codify a `ContainerAdapterConformance` base, and the only
 * first-party container adapter outside Core (MLNet's `DataViewContainerAdapter`) was
 * descoped from the conformance initiative because ONNX is structurally different from
 * the row-oriented surfaces the kit targets. Restore it to the SURFACES list when
 * container conformance is added.
 *
 * Exits with non-zero on any failure. Extracted from the previous monolithic
 * `verify-test-coverage.mjs` Pass 2.
 *
 * Usage:
 *   node scripts/_test/conformance-presence.mjs
 */

import { readFileSync, existsSync, readdirSync } from 'node:fs';
import { join } from 'node:path';
import { findCs, rel, SRC_DIR, TESTS_DIR } from './_lib.mjs';

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

let exitCode = 0;

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
    '_test:conformance-presence — every first-party extension surface implementor has a conformance subclass.'
  );
}

process.exit(exitCode);
