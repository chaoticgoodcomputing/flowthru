#!/usr/bin/env node
/**
 * `_test:row-features-claims` — every `Supports* = true` flag declared by an
 * `IFormatSerializer<TRow>` implementation has at least one corresponding kit fixture
 * AND a conformance subclass that exercises it via the matching
 * `RequiredFeatures` predicate.
 *
 * The check inverts on the conformance side: a format claiming `SupportsIScalar = true`
 * should have at least one conformance subclass whose `RequiredFeatures` predicate
 * is satisfied by the IScalar feature flag (and whose fixture path corresponds to an
 * IScalar fixture). When no such subclass exists, the format's claim is unverified —
 * the meta-test fails and prompts the author to either drop the claim or add a
 * conformance subclass.
 *
 * Detection strategy:
 *   1. Walk source under `src/extensions/**\/*.cs` and `src/core/**\/*.cs` for
 *      `IFormatSerializer<TRow>` implementors. For each, parse the `RowFeatures`
 *      property body for `Supports* = true` flag assignments.
 *   2. Walk `tests/extensions/**\/Conformance/*.cs` and `tests/core/**\/Conformance/*.cs`
 *      for `*Conformance<...>` classes that override `RequiredFeatures` referencing each
 *      `Supports*` flag.
 *   3. For each (format, claimed-true-flag) pair, verify a conformance subclass exists
 *      that (a) targets a serializer of that format and (b) overrides RequiredFeatures
 *      against that flag.
 *
 * Caveats:
 *   - Textual scan, not Roslyn-level. A format that declares its features via a static
 *     factory or indirection may evade detection. The current first-party formats all
 *     use object-initializer syntax inside the property body, which the regex matches.
 *   - The claim/verification mapping is per-flag. A format claiming three features
 *     needs three matching conformance subclasses (one per flag).
 *
 * Usage:
 *   node scripts/_test/row-features-claims.mjs
 */

import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { findCs, rel, SRC_DIR, TESTS_DIR } from './_lib.mjs';

const SRC_SCAN_DIRS = [join(SRC_DIR, 'extensions'), join(SRC_DIR, 'core')];
const TEST_SCAN_DIRS = [join(TESTS_DIR, 'extensions'), join(TESTS_DIR, 'core')];

// ── Find IFormatSerializer<TRow> implementors and their declared features ───

function findFormatSerializerSources() {
  const results = [];
  for (const dir of SRC_SCAN_DIRS) {
    for (const file of findCs(dir)) {
      const text = readFileSync(file, 'utf8');
      const collapsed = text.replace(/\s+/g, ' ');

      // Match `class FooFormatSerializer<...> : ... IFormatSerializer<...>`.
      const classRe = /\b(class|record)\s+([A-Z][A-Za-z0-9_]*)(?:<[^>]+>)?\s*:[^{]*\bIFormatSerializer\s*</g;
      let m;
      while ((m = classRe.exec(collapsed)) !== null) {
        const className = m[2];
        const claims = extractClaimedFeatures(text);
        if (claims.length > 0) {
          results.push({ className, file, claims });
        }
      }
    }
  }
  return results;
}

function extractClaimedFeatures(text) {
  // Find the RowFeatures property's object-initializer body and extract `Supports* = true`
  // assignments. Anchored by the property declaration; tolerates whitespace and trailing
  // commas.
  const propRe = /public\s+FormatRowFeatures\s+RowFeatures\s*=>\s*new\s*\(\s*\)\s*\{([^}]*)\}/m;
  const match = text.match(propRe);
  if (!match) {
    return [];
  }
  const body = match[1];
  const claims = [];
  const flagRe = /(Supports[A-Z][A-Za-z0-9_]*)\s*=\s*(true|false)/g;
  let m;
  while ((m = flagRe.exec(body)) !== null) {
    if (m[2] === 'true') {
      claims.push(m[1]);
    }
  }
  return claims;
}

// ── Find conformance subclasses with their RequiredFeatures ────────────────

function findConformanceVerifications() {
  // map: format-class-name (best-effort textual association) → set of verified flags
  const verifiedByFormat = new Map();

  for (const dir of TEST_SCAN_DIRS) {
    for (const file of findCs(dir)) {
      const text = readFileSync(file, 'utf8');

      // Find each conformance subclass and the format-serializer type it constructs.
      // Pattern: `new <FormatType><...>(...)` inside CreateSerializer. We extract the
      // open-generic name (e.g., `CsvFormatSerializer`) as the format identifier.
      const classRe = /class\s+([A-Z][A-Za-z0-9_]*)\s*:\s*FormatSerializerConformance\s*<[^>]+>/g;
      let classMatch;
      while ((classMatch = classRe.exec(text)) !== null) {
        const className = classMatch[1];

        // Find the CreateSerializer body for THIS class. Limit search scope to the
        // text after the class declaration, up to the next class or end of file.
        const afterClass = text.slice(classMatch.index);
        const nextClass = afterClass.search(/\n\s*(?:public|internal|\[TestFixtureSource)\b.*class\s+/);
        const classBody = nextClass > 0 ? afterClass.slice(0, nextClass) : afterClass;

        const createMatch = classBody.match(/CreateSerializer\s*\(\s*\)\s*=>\s*new\s+([A-Z][A-Za-z0-9_]*)\s*</);
        if (!createMatch) {
          continue;
        }
        const formatTypeName = createMatch[1];

        // Find the RequiredFeatures override (if any) and extract the flags it references.
        const requiredMatch = classBody.match(
          /RequiredFeatures\s*=>\s*([^;]+);/
        );
        if (!requiredMatch) {
          continue;
        }
        const requiredBody = requiredMatch[1];
        const flagRefs = [...requiredBody.matchAll(/f\.(Supports[A-Z][A-Za-z0-9_]*)/g)].map(
          (m) => m[1]
        );
        if (flagRefs.length === 0) {
          continue;
        }

        if (!verifiedByFormat.has(formatTypeName)) {
          verifiedByFormat.set(formatTypeName, new Set());
        }
        for (const flag of flagRefs) {
          verifiedByFormat.get(formatTypeName).add(flag);
        }
      }
    }
  }

  return verifiedByFormat;
}

const formats = findFormatSerializerSources();
const verifiedByFormat = findConformanceVerifications();

const violations = [];
for (const { className, file, claims } of formats) {
  const verified = verifiedByFormat.get(className) ?? new Set();
  for (const claim of claims) {
    if (!verified.has(claim)) {
      violations.push({ className, file: rel(file), claim });
    }
  }
}

let exitCode = 0;
if (violations.length > 0) {
  exitCode = 1;
  console.error(
    `\n${violations.length} RowFeatures claim(s) lack a verifying conformance subclass:\n`
  );
  for (const { className, file, claim } of violations) {
    console.error(`  ${className} declares ${claim} = true  (${file})`);
    console.error(
      `    no *Conformance<...> subclass that constructs ${className} overrides RequiredFeatures = f => f.${claim}`
    );
  }
  console.error(
    '\nEither add a conformance subclass that exercises the claimed feature (a kit fixture'
  );
  console.error(
    'whose RequiredFeatures predicate matches the flag), or drop the claim from the format'
  );
  console.error("declaration if the feature isn't actually supported.\n");
}

if (exitCode === 0) {
  console.log(
    `_test:row-features-claims — all RowFeatures = true claims are exercised by at least one conformance subclass.`
  );
}

process.exit(exitCode);
