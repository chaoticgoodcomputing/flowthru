#!/usr/bin/env node
/**
 * `_test:package-versions` — every `<PackageVersion>` entry in
 * `Directory.Packages.props` either declares a major-only floor or carries a
 * same-line justification comment.
 *
 * Why this matters: every `<PackageVersion>` floor in this file becomes the
 * minimum version stamped into the `.nuspec` of any in-repo package that
 * depends on it. A floor of `10.0.4` excludes downstream consumers on
 * `10.0.0`–`10.0.3` from using the published Flowthru package, even though
 * they are otherwise on a compatible major. Major-only-by-default keeps that
 * floor as wide as possible; the comment escape hatch makes any narrower
 * floor a deliberate, reviewed decision.
 *
 * Policy:
 *   • Default form: `<PackageVersion Include="X" Version="N.0.0" />` —
 *     major-only. `N`, `N.0`, and `N.0.0` are accepted as equivalent canonical
 *     forms.
 *   • For 0-major packages (`0.x.y` is "anything goes" per semver, with the
 *     minor functioning as the practical major), `0.M` and `0.M.0` are
 *     canonical.
 *   • Escape hatch: any higher floor must carry a same-line `<!-- ... -->`
 *     comment explaining why the higher floor is required, e.g.:
 *       `<PackageVersion Include="Foo" Version="10.0.4" /> <!-- needs API X added in 10.0.4 -->`
 *   • Hard prohibitions (no escape hatch):
 *       - Bracketed versions (`[X]`, `[X, Y)`, etc.) — exact pins force
 *         consumers onto one version; ranges with upper bounds cause
 *         diamond-dependency conflicts.
 *       - Floating wildcards (`*`) — non-reproducible consumer builds.
 *
 * Usage:
 *   node scripts/_test/package-versions.mjs
 */

import { existsSync, readFileSync } from 'node:fs';
import { join } from 'node:path';
import { rel, ROOT } from './_lib.mjs';

const PACKAGES_PROPS = join(ROOT, 'Directory.Packages.props');

if (!existsSync(PACKAGES_PROPS)) {
  console.error(`\nDirectory.Packages.props not found at ${PACKAGES_PROPS}.\n`);
  process.exit(1);
}

const text = readFileSync(PACKAGES_PROPS, 'utf8');
const lines = text.split('\n');

// Match a single-line `<PackageVersion ... Version="V" />` entry. Captures the
// package name, the version literal, and any trailing content after the
// self-closing tag — that trailing content is where a same-line justification
// comment lives.
const PKG_RE = /<PackageVersion\s+Include="([^"]+)"\s+Version="([^"]+)"\s*\/>(.*)$/;

const violations = [];
let matchedEntries = 0;

for (let i = 0; i < lines.length; i++) {
  const line = lines[i];

  // Skip lines that are entirely inside an XML comment block, e.g. a
  // commented-out PackageVersion left as documentation.
  if (line.trimStart().startsWith('<!--')) continue;

  const m = line.match(PKG_RE);
  if (!m) continue;
  matchedEntries++;

  const [, name, version, trailing] = m;
  const lineNo = i + 1;
  const where = `${rel(PACKAGES_PROPS)}:${lineNo}`;

  // ── Hard prohibitions ────────────────────────────────────────────────────
  if (version.startsWith('[')) {
    violations.push({
      where,
      name,
      reason:
        `bracketed version "${version}" not allowed.\n`
        + `      Exact pins force consumers onto one specific version; ranges with upper bounds\n`
        + `      cause diamond-dependency conflicts when transitive deps move. Drop the brackets.`,
    });
    continue;
  }
  if (version.includes('*')) {
    violations.push({
      where,
      name,
      reason:
        `floating wildcard "${version}" not allowed.\n`
        + `      Wildcard versions resolve at the consumer's restore time, making builds\n`
        + `      non-reproducible and shifting your testing burden onto downstream users.`,
    });
    continue;
  }

  // ── Canonical major-only forms ───────────────────────────────────────────
  // Major >= 1: `N`, `N.0`, or `N.0.0`.
  // Major == 0: `0.M` or `0.M.0` (semver treats 0.x as unstable; the minor is
  // the practical major).
  const majorOnly = /^[1-9]\d*(\.0(\.0)?)?$/.test(version);
  const zeroMajorMinorOnly = /^0\.\d+(\.0)?$/.test(version);
  if (majorOnly || zeroMajorMinorOnly) continue;

  // ── Escape hatch: same-line trailing `<!-- ... -->` comment ──────────────
  const hasJustification = /<!--[\s\S]*?-->/.test(trailing);
  if (hasJustification) continue;

  // Suggest the canonical floor the contributor could drop to.
  const suggestion = version.startsWith('0.')
    ? `${version.split('.').slice(0, 2).join('.')}.0`
    : `${version.match(/^\d+/)[0]}.0.0`;

  violations.push({
    where,
    name,
    reason:
      `version "${version}" pins above the major-only floor and lacks a same-line justification.\n`
      + `      Either lower the floor to "${suggestion}" (preferred — widest downstream compatibility),\n`
      + `      or add a same-line comment explaining the required floor, e.g.:\n`
      + `        <PackageVersion Include="${name}" Version="${version}" /> <!-- needs API X added in ${version} -->`,
  });
}

// Sanity check: total `<PackageVersion ` occurrences vs. matched entries. A
// mismatch means the file has multi-line entries this script doesn't parse;
// surface that explicitly so violations aren't silently skipped.
const totalEntries = (text.match(/<PackageVersion\s/g) || []).length;
if (totalEntries !== matchedEntries) {
  console.error(
    `\nFormat check failed in ${rel(PACKAGES_PROPS)}: found ${totalEntries} <PackageVersion> entries\n`
      + `but the line-by-line parser matched only ${matchedEntries}. This script assumes each\n`
      + `entry is on a single line; reformat any multi-line entries to single-line.\n`
  );
  process.exit(1);
}

if (violations.length === 0) {
  console.log(
    `_test:package-versions — all ${matchedEntries} <PackageVersion> entr${matchedEntries === 1 ? 'y' : 'ies'} in ${rel(PACKAGES_PROPS)} comply with the major-only-by-default floor policy.`
  );
  process.exit(0);
}

console.error(
  `\n${violations.length} <PackageVersion> entr${violations.length === 1 ? 'y' : 'ies'} in ${rel(PACKAGES_PROPS)} violate${violations.length === 1 ? 's' : ''} the floor policy:\n`
);
for (const v of violations) {
  console.error(`  ${v.where}  ${v.name}`);
  console.error(`    ${v.reason}\n`);
}
console.error(
  'Policy: declare floors as major-only by default (N.0.0 for 1+ majors, 0.M.0 for 0-majors).\n'
    + 'A higher floor is a downstream-consumer constraint, so it must be a deliberate decision —\n'
    + 'add a same-line <!-- ... --> comment justifying it. Bracketed and wildcard versions are\n'
    + 'forbidden outright (they break consumer flexibility).\n'
);
process.exit(1);
