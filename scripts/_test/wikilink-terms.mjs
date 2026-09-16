#!/usr/bin/env node
/**
 * `_test:wikilink-terms` — every `[[Term]]` in repo-authored Markdown resolves to
 * a glossary entry defined in some context's `CONTRIBUTING.md`.
 *
 * Before this gate there was NO wikilink resolution anywhere in the repo:
 * `scripts/_test/skill-links.mjs` parses standard Markdown links only, so a
 * `[[Term]]` pointing at nothing was invisible. Three had already rotted unnoticed:
 * two in `tests/extensions/CONTRIBUTING.md` and one in
 * `src/tools/flowthru-vscode/README.md`.
 *
 * Two distinct failures:
 *
 *   1. UNRESOLVED — the target names no glossary term. Either the term was
 *      renamed and references weren't updated, or the reference is aspirational
 *      (the term was never defined).
 *
 *   2. AMBIGUOUS — the target is a BARE term whose glossary carries role
 *      parenthetical variants. Per #154, where a term genuinely means different
 *      things to different roles it is disambiguated in the entry NAME
 *      (`Wide transform (Flow Developer)` vs `Wide transform (Extension
 *      Developer)`) rather than by reciprocal prose notes — so every reference
 *      must carry the full form. A bare `[[Wide transform]]` silently picks no
 *      side; that is the ambiguity the naming convention exists to remove.
 *
 * Alias form `[[Target|display text]]` is supported — only the target resolves.
 * Trailing plurals stay outside the brackets (`[[Law]]s`), so they need no
 * special handling.
 *
 * Code is not prose: wikilinks inside an inline code span or a fenced block are
 * ignored. Documentation ABOUT this convention has to quote the syntax, and a
 * checker that fails on its own examples would be self-defeating.
 *
 * Scope: repo-authored Markdown. Vendored skill sources under `.agents/` and
 * `.claude/skills/` are excluded — their prose is upstream's and is replaced
 * wholesale on update, so a failure there is one no contributor here can fix.
 *
 * Usage:
 *   node scripts/_test/wikilink-terms.mjs
 */

import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { ROOT } from './_lib.mjs';
import {
  authoredMarkdown,
  contexts,
  glossaryTerms,
  normalizeTerm,
  splitRoleParenthetical,
  reportFailures,
  reportOk,
} from './_domain.mjs';

const TARGET = '_test:wikilink-terms';

// [[Target]] or [[Target|display]]. An embedded `#` would be a heading anchor —
// not part of this convention, and excluded so it reads as unresolved rather
// than silently matching a term containing one.
const WIKILINK_RE = /\[\[([^\][|#]+)(?:\|([^\][]*))?\]\]/g;

const allContexts = contexts();
const terms = glossaryTerms(allContexts);

if (terms.size === 0) {
  reportFailures(
    TARGET,
    'no glossary terms found in any context CONTRIBUTING.md — the resolver has nothing to resolve against:',
    allContexts.map((c) => `${c.contributing}  (no '## Glossary' section?)`),
    'A glossary entry is a line `**Term**: definition` under a `## Glossary` heading.',
  );
}

/**
 * Bare term → the role-parenthetical variants that exist for it. Populated only
 * where the glossary actually defines variants, so the ambiguity rule costs
 * nothing until #156 introduces the first one.
 */
const variantsOf = new Map();
for (const term of terms.keys()) {
  const { base, role } = splitRoleParenthetical(term);
  if (!role) continue;
  if (!variantsOf.has(base)) variantsOf.set(base, []);
  variantsOf.get(base).push(term);
}

const unresolved = [];
const ambiguous = [];

/** Blank out inline code spans so a quoted `[[Term]]` reads as code, not a reference. */
function stripInlineCode(line) {
  return line.replace(/(`+)(?:(?!\1).)*\1/g, (m) => ' '.repeat(m.length));
}

for (const file of authoredMarkdown()) {
  const lines = readFileSync(join(ROOT, file), 'utf8').split('\n');
  // CommonMark: a fence closes only on the same marker character, at least as
  // long as the opener — so a ``` inside a ~~~ block can't flip the state.
  let fence = null;
  lines.forEach((rawLine, i) => {
    const f = /^\s*(`{3,}|~{3,})/.exec(rawLine);
    if (f) {
      const marker = f[1];
      if (fence === null) {
        fence = marker;
        return;
      }
      if (marker[0] === fence[0] && marker.length >= fence.length) {
        fence = null;
        return;
      }
    }
    if (fence !== null) return;
    const line = stripInlineCode(rawLine);
    for (const m of line.matchAll(WIKILINK_RE)) {
      const target = normalizeTerm(m[1]);
      const where = `${file}:${i + 1}`;
      const shown = m[2] !== undefined ? `[[${m[1]}|${m[2]}]]` : `[[${m[1]}]]`;

      if (terms.has(target)) continue;

      const variants = variantsOf.get(target);
      if (variants && variants.length > 0) {
        ambiguous.push(
          `${where}  ${shown}\n      → use the full form: ${variants.map((v) => `[[${v}]]`).join(' or ')}`,
        );
        continue;
      }

      unresolved.push(`${where}  ${shown}`);
    }
  });
}

if (unresolved.length > 0 || ambiguous.length > 0) {
  const violations = [];
  if (unresolved.length > 0) {
    violations.push(`UNRESOLVED (${unresolved.length}) — target names no glossary term:`);
    violations.push(...unresolved.map((v) => `  ${v}`));
  }
  if (ambiguous.length > 0) {
    if (violations.length > 0) violations.push('');
    violations.push(`AMBIGUOUS (${ambiguous.length}) — bare term has role-parenthetical variants:`);
    violations.push(...ambiguous.map((v) => `  ${v}`));
  }

  reportFailures(
    TARGET,
    `${unresolved.length + ambiguous.length} unresolvable wikilink(s) across ${terms.size} glossary term(s):`,
    violations,
    'Fix: either correct the reference to an existing term, or define the term as\n' +
      '  `**Term**: definition` under the `## Glossary` heading of the context that owns it:\n' +
      `    ${allContexts.map((c) => c.contributing).join('\n    ')}`,
  );
}

reportOk(
  TARGET,
  `all wikilinks resolve against ${terms.size} glossary term(s) from ${allContexts.length} context(s)`,
);
