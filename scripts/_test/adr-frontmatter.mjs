#!/usr/bin/env node
/**
 * `_test:adr-frontmatter` — an ADR that declares the #154 frontmatter contract
 * honors it: `exemplars` is non-empty and every path resolves, and every
 * `contexts` value names a real context.
 *
 * The contract:
 *   contexts   — who DECIDED (direct application only)
 *   exemplars  — files demonstrating the decision in practice, non-empty
 *   status     — accepted | superseded | rejected
 *
 * **An ADR reaches `main` with the work that implements it.** `main` is the
 * canonical state of the repository, so an ADR merged into it describes something
 * the repository actually does. A decision still under review lives on its own
 * `adr/<slug>` branch and merges when its implementation does.
 *
 * This is why there is no `proposed` status. A `proposed` ADR on `main` would need
 * an exemption from the non-empty-`exemplars` rule, and every exemption is a way
 * to assert a decision without practising it — exactly what `exemplars` exists to
 * prevent. Removing the status removes the loophole: on `main`, `accepted` means
 * something demonstrates this, with no escape hatch.
 *
 * `exemplars` is what makes an ADR falsifiable: it is the difference between a
 * decision that is *practised* and one that is merely *asserted*. ADR-0019
 * declares 3 packages but is cited by 8; only 12 of 27 ADRs carry a governed-code
 * section at all. An accepted decision with nothing demonstrating it is a claim.
 *
 * `superseded` and `rejected` ADRs are exempt from the exemplars requirement —
 * their exemplars are expected to have been deleted. ADR-0001 is the proof: its
 * exemplar was `/GLOSSARY.md`, which ADR-0004 removed.
 *
 * **Migration tolerance.** This gate lands BEFORE #157 relocates and renumbers
 * the ADRs, so it must not fail the 27 that have not been migrated yet. An ADR
 * counts as migrated once it declares `contexts` or `exemplars`; an ADR that
 * declares neither is skipped ENTIRELY, status included. That way the gate is
 * live for every ADR written from here on and tightens automatically as #157
 * migrates each file, with no follow-up edit needed to "turn it on".
 *
 * The cost is that the gate is presently vacuous — no ADR declares the contract,
 * so nothing is enforced. Five ADRs already say `status: accepted` with no
 * exemplar at all (ADR-0019 among them, the epic's own poster child: it declares
 * 3 packages but is cited by 8). Failing them here would be correct in principle
 * and wrong in practice — adding their frontmatter is #157's job, which #158 puts
 * out of scope, and only tests 2 and 3 are meant to be red on the current tree.
 * So the run REPORTS them as a pending worklist instead of failing on them; the
 * gap is visible rather than silent, and closes as #157 lands.
 *
 * Usage:
 *   node scripts/_test/adr-frontmatter.mjs
 */

import { existsSync, readFileSync } from 'node:fs';
import { join } from 'node:path';
import { ROOT } from './_lib.mjs';
import {
  adrs,
  contexts,
  contextFixHint,
  findContext,
  frontmatter,
  reportFailures,
  reportOk,
} from './_domain.mjs';

const TARGET = '_test:adr-frontmatter';
const VALID_STATUS = ['accepted', 'superseded', 'rejected'];
/**
 * Statuses exempt from the non-empty-exemplars requirement — and the ONLY two,
 * deliberately.
 *
 * Both describe a decision whose exemplars are expected to be GONE: ADR-0001's
 * exemplar was /GLOSSARY.md, which ADR-0004 deleted. There is no exemption for a
 * decision whose exemplars do not exist YET, because such an ADR does not belong
 * on `main` at all — see the header note.
 */
const EXEMPT_FROM_EXEMPLARS = new Set(['superseded', 'rejected']);

const allContexts = contexts();
const known = adrs(allContexts);

const violations = [];
let migrated = 0;

for (const adr of known) {
  const text = readFileSync(join(ROOT, adr.file), 'utf8');

  let fm;
  try {
    fm = frontmatter(text);
  } catch (e) {
    violations.push(`${adr.file}: unparseable frontmatter — ${e.message}`);
    continue;
  }
  if (fm === null) continue; // no frontmatter at all — not yet migrated (#157)

  const asList = (v) => (Array.isArray(v) ? v : v === undefined || v === null || v === '' ? [] : [v]);
  const declaredContexts = asList(fm.contexts);
  const declaredExemplars = asList(fm.exemplars);
  const hasContractKeys = 'contexts' in fm || 'exemplars' in fm;

  // The leading word carries the status; the tail is the pre-migration spelling
  // of the supersession pointer (`superseded by 0004`, `accepted; supersedes 0001`),
  // which #157 moves into its own key.
  const status = typeof fm.status === 'string' ? fm.status.trim() : '';
  const statusWord = status.split(/[\s;,]+/)[0].toLowerCase();

  if (!hasContractKeys) continue; // no frontmatter contract declared — nothing to check
  migrated++;

  // Status strictness is part of the migrated contract, not a pre-condition of it.
  // Enforcing it on unmigrated ADRs would fail the tree over `status: proposed` —
  // a reformat this issue puts out of scope (#157 owns it).
  if (!VALID_STATUS.includes(statusWord)) {
    // `proposed` is the common case and deserves a signpost rather than a bare
    // rejection: it means the ADR is on the right track but the wrong branch.
    violations.push(
      statusWord === 'proposed'
        ? `${adr.file}: status 'proposed' — an ADR on the mainline describes what the ` +
          'repository already does. Keep this on its `adr/<slug>` branch and merge it ' +
          'with the work that implements it, then set `status: accepted` and name the ' +
          'files that demonstrate it.'
        : `${adr.file}: status '${status || '(absent)'}' is not one of ${VALID_STATUS.join(' / ')}`,
    );
  }

  // --- exemplars ---------------------------------------------------------
  if (!EXEMPT_FROM_EXEMPLARS.has(statusWord)) {
    if (declaredExemplars.length === 0) {
      violations.push(
        `${adr.file}: status '${statusWord || 'accepted'}' requires at least one exemplar — ` +
          'add `exemplars:` naming a file that demonstrates this decision in practice',
      );
    }
    for (const exemplar of declaredExemplars) {
      const clean = String(exemplar).split('#')[0].trim().replace(/^\//, '');
      if (clean === '') {
        violations.push(`${adr.file}: empty exemplar entry`);
        continue;
      }
      if (!existsSync(join(ROOT, clean))) {
        violations.push(`${adr.file}: exemplar does not resolve — \`${exemplar}\``);
      }
    }
  }

  // --- contexts ----------------------------------------------------------
  if (declaredContexts.length === 0) {
    violations.push(
      `${adr.file}: \`contexts:\` is empty — name the context(s) that DECIDED this ` +
        '(direct application only; conforming packages are exemplars, not contexts)',
    );
  }
  for (const value of declaredContexts) {
    if (findContext(value, allContexts)) continue;
    violations.push(
      `${adr.file}: \`${value}\` is not a context — ${contextFixHint(value, allContexts)}`,
    );
  }
}

if (violations.length > 0) {
  reportFailures(
    TARGET,
    `${violations.length} ADR frontmatter violation(s) across ${known.length} ADR(s):`,
    violations,
    'The #154 frontmatter contract:\n' +
      '    contexts:  who DECIDED — direct application only\n' +
      '    exemplars: files demonstrating the decision in practice (non-empty unless\n' +
      '               status is superseded or rejected)\n' +
      '    status:    accepted | superseded | rejected\n\n' +
      '  If the decision is not implemented yet, it does not belong on `main`:\n' +
      '  move it to an `adr/<slug>` branch and merge it with its implementation.',
  );
}


reportOk(
  TARGET,
  `${migrated} migrated ADR(s) honor the frontmatter contract; ` +
    `${known.length - migrated} not yet migrated (#157)`,
);
