#!/usr/bin/env node
/**
 * Non-blocking documentation warnings for user-facing markdown. Emits to
 * stdout and, when running under GitHub Actions, to the step summary
 * ($GITHUB_STEP_SUMMARY) so warnings surface in the PR Checks UI WITHOUT
 * failing the check. Always exits 0 — these are pre-flight warnings, not gates.
 *
 * Two warning families share this one surface (one harness, not two):
 *
 *   1. Terminology — uses of a glossary `_Avoid_:` term where canonical
 *      Flowthru vocabulary is expected. The {avoid → canonical} map is parsed
 *      mechanically from examples/CONTRIBUTING.md (the Flow/Catalog Developer
 *      glossary — the most end-user-facing view). Case-insensitive, whole-word,
 *      skips fenced code blocks and inline code, and never flags the glossary's
 *      own `_Avoid_:` lines.
 *
 *   2. Review state — pages whose `review:` frontmatter is not `reviewed`
 *      (absent counts as `draft`). Flags content a human has not yet signed off
 *      on. See the docs review-provenance decision.
 *
 * MVP note: a STOPLIST suppresses avoid-terms that are common English words
 * (`type`, `model`, `node`, …). Flagging those mechanically buries the
 * high-signal hits (`pipeline` → Flow). The stoplist is logged on every run —
 * never a silent cap — and is the obvious curation knob for promoting this
 * lint toward a gate once the corpus is cleaned.
 */

import { readdirSync, readFileSync, existsSync, appendFileSync } from 'node:fs';
import { join, relative, resolve } from 'node:path';
import { dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const GLOSSARY = join(ROOT, 'examples', 'CONTRIBUTING.md');
const DOC_SECTIONS = ['docs/tutorials', 'docs/guides', 'docs/explanation'];
// reference/src is generated from C# XML docs; reference/misc/external is
// vendored third-party source. Neither is human-authored Flowthru prose.
const SKIP_PATH = /\/reference\/(src|misc\/external)\//;

// Avoid-terms too generic to flag mechanically at MVP. Logged on every run.
const STOPLIST = new Set([
  'type', 'model', 'node', 'task', 'job', 'config', 'settings', 'slot',
  'zone', 'tier', 'operator', 'dto', 'repository', 'data source',
]);

// ── Parse {avoid → canonical} from the glossary's `_Avoid_:` lines ──
function parseAvoidMap() {
  const lines = readFileSync(GLOSSARY, 'utf8').split('\n');
  const map = new Map(); // avoidTerm(lower) → canonical
  let canonical = null;
  const TERM_RE = /^\*\*(.+?)\*\*:/; // **Flow**: …
  const AVOID_RE = /^_Avoid_:\s*(.+)$/;
  for (const line of lines) {
    const t = TERM_RE.exec(line.trim());
    if (t) { canonical = t[1].trim(); continue; }
    const a = AVOID_RE.exec(line.trim());
    if (a && canonical) {
      for (let term of a[1].split(',')) {
        term = term.replace(/\(.*$/, '').trim().toLowerCase(); // drop parenthetical
        if (!term) continue;
        if (STOPLIST.has(term)) continue;
        if (!map.has(term)) map.set(term, canonical);
      }
    }
  }
  return map;
}

function walk(dir, out) {
  let entries;
  try { entries = readdirSync(dir, { withFileTypes: true }); } catch { return out; }
  for (const e of entries) {
    const full = join(dir, e.name);
    if (SKIP_PATH.test(full.split('\\').join('/'))) continue;
    if (e.isDirectory()) walk(full, out);
    else if (e.isFile() && e.name.endsWith('.md')) out.push(full);
  }
  return out;
}

// Blank out fenced code blocks and inline code so matches inside them are
// ignored, while preserving line numbers (replace with spaces, keep newlines).
function maskCode(content) {
  let masked = content.replace(/```[\s\S]*?```/g, (m) =>
    m.replace(/[^\n]/g, ' '),
  );
  masked = masked.replace(/`[^`\n]*`/g, (m) => m.replace(/./g, ' '));
  return masked;
}

function frontmatterReview(content) {
  const m = /^---\r?\n([\s\S]*?)\r?\n---/.exec(content);
  if (!m) return 'draft'; // no frontmatter → treat as draft
  const r = /^review:\s*(\S+)/m.exec(m[1]);
  return r ? r[1].trim() : 'draft';
}

function main() {
  const avoidMap = parseAvoidMap();
  const files = [];
  for (const s of DOC_SECTIONS) walk(join(ROOT, s), files);
  files.sort();

  const termHits = []; // { file, line, term, canonical }
  const drafts = []; // file
  const termTotals = new Map();

  for (const file of files) {
    const raw = readFileSync(file, 'utf8');
    const rel = relative(ROOT, file);
    if (frontmatterReview(raw) !== 'reviewed') drafts.push(rel);

    const masked = maskCode(raw).split('\n');
    for (let i = 0; i < masked.length; i++) {
      const lineText = masked[i];
      for (const [term, canonical] of avoidMap) {
        const re = new RegExp(`\\b${term.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')}\\b`, 'gi');
        if (re.test(lineText)) {
          termHits.push({ file: rel, line: i + 1, term, canonical });
          termTotals.set(term, (termTotals.get(term) ?? 0) + 1);
        }
      }
    }
  }

  // ── Render ──
  const out = [];
  out.push('# Documentation warnings\n');
  out.push(`_Non-blocking. ${files.length} page(s) scanned. `
    + `Stoplisted generic avoid-terms (not flagged): ${[...STOPLIST].sort().join(', ')}._\n`);

  out.push(`## Terminology — ${termHits.length} hit(s)\n`);
  if (termHits.length) {
    out.push('| File | Line | Avoid | Use instead |');
    out.push('| --- | --- | --- | --- |');
    for (const h of termHits) {
      out.push(`| ${h.file} | ${h.line} | \`${h.term}\` | **${h.canonical}** |`);
    }
    out.push('');
  } else {
    out.push('None. ✓\n');
  }

  out.push(`## Review state — ${drafts.length} page(s) not \`reviewed\`\n`);
  if (drafts.length) {
    for (const f of drafts) out.push(`- ${f}`);
    out.push('');
  } else {
    out.push('All scanned pages are `reviewed`. ✓\n');
  }

  const report = out.join('\n');
  process.stdout.write(report + '\n');
  if (process.env.GITHUB_STEP_SUMMARY) {
    appendFileSync(process.env.GITHUB_STEP_SUMMARY, report + '\n');
  }
  // Pre-flight warning surface — never fails the check.
  process.exit(0);
}

main();
