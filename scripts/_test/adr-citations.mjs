#!/usr/bin/env node
/**
 * `_test:adr-citations` — every `ADR-NNNN` citation names an ADR that exists, and
 * every Markdown link into an ADR directory resolves on disk.
 *
 * **This is the gate that makes renumbering safe.** Per #154, ADRs are renumbered
 * when they relocate, AND code comments are explicitly permitted to cite them —
 * two rules that collide silently without this test. There are 61 bare-text
 * `ADR-00NN` citations in `src/**\/*.cs`; they are comments, not Markdown links,
 * so `scripts/lint-doc-links.mjs` cannot see them. A renumber invalidates every
 * one with no signal at all.
 *
 * Two checks, because a citation can rot in two independent ways:
 *
 *   1. UNKNOWN NUMBER — `ADR-0042` where no ADR 0042 exists. Catches a renumber
 *      that left citations behind, and a citation invented from memory.
 *
 *   2. BROKEN LINK PATH — `[ADR-0012](/some/path/adr/0012-….md)` whose target is
 *      not on disk. A number can resolve while its link does not: 19 references
 *      point into `.claude/docs/adr/`, a directory that has never existed in this
 *      repo. The number check alone would pass all 19.
 *
 * ADRs are discovered across the root `docs/adr/` and every context-owned
 * `<context>/docs/adr/`, so this keeps working across #157's relocation without
 * an edit here.
 *
 * Scope: repo-authored Markdown and C#. Committed docfx output under
 * `docs/reference/src/` is excluded — it is regenerated from `src/` XML doc
 * comments, so a stale citation there is a defect in the source comment and is
 * reported at the source.
 *
 * Usage:
 *   node scripts/_test/adr-citations.mjs
 */

import { existsSync, readFileSync } from 'node:fs';
import { join, resolve, dirname } from 'node:path';
import { ROOT } from './_lib.mjs';
import {
  adrs,
  authoredCSharp,
  authoredMarkdown,
  contexts,
  reportFailures,
  reportOk,
} from './_domain.mjs';

const TARGET = '_test:adr-citations';

/** A bare citation anywhere — prose, a Markdown link label, or a `//` comment. */
const CITATION_RE = /\bADR-(\d{4})\b/g;
/**
 * A Markdown link whose target reaches an ADR directory or a file inside one.
 * The trailing slash is optional so a link to the bare directory — `](/docs/adr)`,
 * which `scripts/generate-context-map.mjs` emits for its "Owns ADRs" column — is
 * validated too, rather than silently skipped.
 */
const ADR_LINK_RE = /\[([^\]]*)\]\(([^)\s]*(?:\/|^)adr(?:\/[^)\s]*)?)\)/gi;

const allContexts = contexts();
const known = adrs(allContexts);
const byNumber = new Map(known.map((a) => [a.number, a]));

if (known.length === 0) {
  reportFailures(
    TARGET,
    'no ADRs found — nothing to validate citations against:',
    [`searched: docs/adr/ and ${allContexts.filter((c) => c.ownsAdrs).length} context ADR director(ies)`],
    'An ADR file is named `NNNN-slug.md`.',
  );
}

const unknownNumbers = [];
const brokenLinks = [];

/** Resolve a Markdown link target to an absolute path, or null if not repo-local. */
function resolveTarget(target, fromFile) {
  const clean = target.split('#')[0].trim();
  if (clean === '' || /^(?:https?:|mailto:)/i.test(clean)) return null;
  // A leading `/` is root-anchored (the repo's convention), not filesystem-absolute.
  if (clean.startsWith('/')) return join(ROOT, clean.slice(1));
  return resolve(ROOT, dirname(fromFile), clean);
}

for (const file of [...authoredMarkdown(), ...authoredCSharp()]) {
  const text = readFileSync(join(ROOT, file), 'utf8');
  const lines = text.split('\n');

  lines.forEach((line, i) => {
    const where = `${file}:${i + 1}`;

    for (const m of line.matchAll(CITATION_RE)) {
      if (!byNumber.has(m[1])) {
        unknownNumbers.push(`${where}  ADR-${m[1]}  (no such ADR)`);
      }
    }

    for (const m of line.matchAll(ADR_LINK_RE)) {
      const [, label, target] = m;
      const abs = resolveTarget(target, file);
      if (abs === null || existsSync(abs)) continue;
      brokenLinks.push(`${where}  [${label}](${target})`);
    }
  });
}

if (unknownNumbers.length > 0 || brokenLinks.length > 0) {
  const violations = [];
  if (unknownNumbers.length > 0) {
    violations.push(`UNKNOWN ADR NUMBER (${unknownNumbers.length}):`);
    violations.push(...unknownNumbers.map((v) => `  ${v}`));
  }
  if (brokenLinks.length > 0) {
    if (violations.length > 0) violations.push('');
    violations.push(`BROKEN ADR LINK PATH (${brokenLinks.length}) — target not on disk:`);
    violations.push(...brokenLinks.map((v) => `  ${v}`));
  }

  const dirs = [...new Set(known.map((a) => a.dir))].sort();
  reportFailures(
    TARGET,
    `${unknownNumbers.length + brokenLinks.length} bad ADR citation(s) against ${known.length} known ADR(s):`,
    violations,
    `ADR directories on disk: ${dirs.join(', ')}`,
    'Fix: point the citation at a real ADR. If an ADR was renumbered on relocation\n' +
      '  (#154), update every citation in the same change — including bare-text ones in\n' +
      '  `.cs` comments, which the Markdown link linter cannot see.',
  );
}

reportOk(TARGET, `all ADR citations resolve against ${known.length} ADR(s)`);
