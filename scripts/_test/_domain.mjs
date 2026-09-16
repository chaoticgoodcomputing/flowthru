/**
 * Shared domain vocabulary for the `_test:*` meta-tests that enforce Flowthru's
 * context / glossary / ADR conventions (issue #158, epic #154).
 *
 * `_lib.mjs` answers "where are the files?"; this module answers "what do they
 * MEAN" — what counts as a context, a glossary term, an ADR. Four gates share
 * these definitions (`wikilink-terms`, `adr-citations`, `adr-frontmatter`, and
 * the context-map generator), so a change to the convention is one edit here
 * rather than four divergent reimplementations.
 *
 * The load-bearing definition, from #154: **a directory is a context iff it
 * DIRECTLY contains a `CONTRIBUTING.md`.** Everything else — which glossaries
 * exist, which directories may own ADRs, what an ADR's `contexts:` may name —
 * derives from that one rule.
 */

import { execFileSync } from 'node:child_process';
import { existsSync, readdirSync, readFileSync, statSync } from 'node:fs';
import { join, dirname } from 'node:path';
import { ROOT, rel } from './_lib.mjs';

/**
 * Directory names never walked. `node_modules` and build output are obvious;
 * the rest are trees the repo does not author and cannot fix:
 *   - `.agents/`, `.claude/skills/` — VENDORED skill sources, refreshed wholesale
 *     on update. Their prose is upstream's; flagging it would produce a failure
 *     no contributor here can resolve. (The `CONTEXT.md` guard deliberately
 *     still walks them — see `isVendored` below.)
 *   - `repo` under each `docs/reference/misc/external/<src>/` — checked-out third-party
 *     repositories pulled by `nx run xdocs:pull`.
 */
const SKIP_DIRS = new Set([
  'node_modules',
  'bin',
  'obj',
  'dist',
  'TestResults',
  'coverage',
  '.git',
  '.nx',
  '.vs',
]);

/**
 * Repo-relative path with forward slashes, extending `_lib.mjs`'s `rel()` with the
 * one case it does not cover: ROOT itself, which these gates address as `.`.
 */
export function relPath(absPath) {
  return absPath === ROOT ? '.' : rel(absPath);
}

/** True for trees the repo vendors rather than authors (see SKIP_DIRS commentary). */
export function isVendored(relative) {
  return (
    relative.startsWith('.agents/') ||
    relative.startsWith('.claude/skills/') ||
    /(^|\/)docs\/reference\/misc\/external\/[^/]+\/repo(\/|$)/.test(relative)
  );
}

/**
 * Walk the repo, yielding repo-relative paths of files matching `predicate`.
 *
 * `includeVendored` exists for exactly one caller: the `CONTEXT.md` guard, which
 * must see a stray file ANYWHERE — including inside a vendored skill dir, since
 * that is one of the places a confused agent could drop one.
 */
export function walkFiles(predicate, { includeVendored = false } = {}) {
  const found = [];
  const visit = (dir) => {
    let entries;
    try {
      entries = readdirSync(dir, { withFileTypes: true });
    } catch {
      return; // unreadable dir — not this test's concern
    }
    for (const entry of entries) {
      const full = join(dir, entry.name);
      const relative = relPath(full);
      if (entry.isDirectory()) {
        if (SKIP_DIRS.has(entry.name)) continue;
        if (!includeVendored && isVendored(`${relative}/`)) continue;
        visit(full);
      } else if (entry.isFile() && predicate(entry.name, relative)) {
        found.push(relative);
      }
    }
  };
  visit(ROOT);
  return found.sort();
}

/**
 * Files a contributor actually authors: tracked, plus untracked-but-not-ignored.
 *
 * Asking git rather than re-deriving the rules is what keeps this honest. The repo
 * carries several large GENERATED trees — docfx output under `docs/reference/src/`,
 * the 783-file Starlight mirror under `src/website/src/content/docs/docs/`,
 * `docs/scratch/` — that are all gitignored. A hand-maintained skip list would
 * drift from `.gitignore` the moment a new generated tree appears; this cannot.
 */
function authoredFiles(extension) {
  let out;
  try {
    out = execFileSync('git', ['ls-files', '--cached', '--others', '--exclude-standard', '-z'], {
      cwd: ROOT,
      encoding: 'utf8',
      maxBuffer: 64 * 1024 * 1024,
    });
  } catch {
    // Not a git checkout (or git unavailable) — fall back to the filesystem walk.
    return walkFiles((name) => name.endsWith(extension));
  }
  return out
    .split('\0')
    .filter((p) => p !== '' && p.endsWith(extension) && !isVendored(p))
    .sort();
}

/**
 * Repo-authored Markdown.
 *
 * Committed docfx output under `docs/reference/src/` is excluded even where it is
 * still tracked: it is regenerated from `src/` XML doc comments, so a defect there
 * is a defect in the source comment and is reported at the source.
 */
export function authoredMarkdown() {
  return authoredFiles('.md').filter((p) => !p.startsWith('docs/reference/src/'));
}

/** Repo-authored C# sources. */
export function authoredCSharp() {
  return authoredFiles('.cs');
}

// ---------------------------------------------------------------------------
// Contexts
// ---------------------------------------------------------------------------

/**
 * Every context in the repo: a directory directly containing a `CONTRIBUTING.md`.
 *
 * Returns `{ id, dir, contributing, adrDir, ownsAdrs }` sorted with the root
 * context first, then lexically. `id` is the repo-relative directory path, with
 * the root context identified as `/`.
 *
 * `ownsAdrs` encodes #154's single exclusion: `docs/` may not own ADRs
 * (documentation decisions are repo-wide, and `docs/docs/adr/` is absurd) —
 * every other context may.
 */
export function contexts() {
  const files = walkFiles((name) => name === 'CONTRIBUTING.md');
  const list = files.map((file) => {
    const dir = file === 'CONTRIBUTING.md' ? '.' : dirname(file);
    const id = dir === '.' ? '/' : dir;
    const adrDir = dir === '.' ? 'docs/adr' : `${dir}/docs/adr`;
    return {
      id,
      dir,
      contributing: file,
      adrDir,
      ownsAdrs: dir !== 'docs',
    };
  });
  return list.sort((a, b) => (a.id === '/' ? -1 : b.id === '/' ? 1 : a.id.localeCompare(b.id)));
}

/**
 * Resolve a value from an ADR's `contexts:` array to a context, tolerantly.
 *
 * Accepts the canonical id (`src/core`, `/`) and the directory form without the
 * leading-slash root convention. Returns the context, or `undefined`.
 */
export function findContext(value, all = contexts()) {
  const raw = String(value).trim().replace(/^\.\//, '').replace(/\/+$/, '');
  if (raw === '' || raw === '/' || raw === '.') return all.find((c) => c.id === '/');
  // `/src/core` is the root-anchored spelling used by every other path in the repo;
  // `src/core` is the bare id. Accept both so `contexts:` reads like a link target.
  const needle = raw.replace(/^\//, '');
  return all.find((c) => c.id === needle || c.dir === needle);
}

/**
 * Best-effort fix hint for a `contexts:` value that names no context.
 *
 * If a directory with that basename exists somewhere sensible, point at the
 * `CONTRIBUTING.md` that would mint it as a context — that is the actionable
 * form #158 asks for ("`Flowthru.Extensions.Python` is not a context — add
 * `src/extensions/Flowthru.Extensions.Python/CONTRIBUTING.md`"). Otherwise fall
 * back to enumerating what IS a context.
 */
export function contextFixHint(value, all = contexts()) {
  const needle = String(value).trim();
  const candidate = findDirectoryNamed(needle);
  if (candidate) return `add \`${candidate}/CONTRIBUTING.md\``;
  if (needle.includes('/') && existsSync(join(ROOT, needle))) {
    return `add \`${needle}/CONTRIBUTING.md\``;
  }
  return `valid contexts: ${all.map((c) => c.id).join(', ')}`;
}

/** Find a directory anywhere under the conventional roots whose basename is `name`. */
function findDirectoryNamed(name) {
  if (!name || name.includes('/')) return null;
  const roots = ['src/core', 'src/extensions', 'src/tools', 'tests/core', 'tests/extensions', 'examples', 'src', 'tests', 'docs'];
  for (const root of roots) {
    const candidate = join(ROOT, root, name);
    try {
      if (statSync(candidate).isDirectory()) return `${root}/${name}`;
    } catch {
      // not there — try the next root
    }
  }
  return null;
}

// ---------------------------------------------------------------------------
// Glossary terms
// ---------------------------------------------------------------------------

/** A glossary entry: `**Term**: definition`, under a `## Glossary` heading. */
const TERM_RE = /^\*\*(.+?)\*\*\s*:/;

/**
 * Every glossary term defined across every context's `CONTRIBUTING.md`.
 *
 * Returns `Map<term, string[]>` — term to the contexts' `CONTRIBUTING.md` paths
 * that define it (a term may legitimately be defined in more than one).
 *
 * Only lines under a `## Glossary` heading count, and only where the colon falls
 * OUTSIDE the bold — that discriminator is what keeps sub-headings like
 * `**Responsibilities:**` (colon inside) out of the term set.
 */
export function glossaryTerms(all = contexts()) {
  const terms = new Map();
  for (const ctx of all) {
    const path = join(ROOT, ctx.contributing);
    if (!existsSync(path)) continue;
    let inGlossary = false;
    for (const line of readFileSync(path, 'utf8').split('\n')) {
      if (/^##\s+Glossary\b/i.test(line)) {
        inGlossary = true;
        continue;
      }
      if (inGlossary && /^##\s/.test(line)) inGlossary = false;
      if (!inGlossary) continue;
      const m = TERM_RE.exec(line);
      if (!m) continue;
      const term = normalizeTerm(m[1]);
      if (!terms.has(term)) terms.set(term, []);
      if (!terms.get(term).includes(ctx.contributing)) terms.get(term).push(ctx.contributing);
    }
  }
  return terms;
}

/** Strip code ticks and collapse whitespace so `**`Anchor.None` sentinel**` matches its references. */
export function normalizeTerm(raw) {
  return raw.replace(/`/g, '').replace(/\s+/g, ' ').trim();
}

/**
 * Split a role-parenthetical term into its base and role:
 * `Wide transform (Flow Developer)` → `{ base: 'Wide transform', role: 'Flow Developer' }`.
 * A term with no trailing parenthetical yields `role: null`.
 */
export function splitRoleParenthetical(term) {
  const m = /^(.*?)\s*\(([^()]+)\)$/.exec(term);
  if (!m) return { base: term, role: null };
  return { base: m[1].trim(), role: m[2].trim() };
}

// ---------------------------------------------------------------------------
// ADRs
// ---------------------------------------------------------------------------

/** Filename of a numbered ADR: `NNNN-slug.md`. */
const ADR_FILE_RE = /^(\d{4})-[\w.-]+\.md$/;

/**
 * Every ADR in the repo, across the root `docs/adr/` and any context-owned
 * `<context>/docs/adr/` (the layout #157 migrates toward — enumerating both
 * now means this gate keeps working across that move without an edit).
 *
 * Returns `{ number, id, file, dir }[]`, where `id` is the `ADR-NNNN` citation form.
 */
export function adrs(all = contexts()) {
  const found = [];
  const dirs = new Set(all.filter((c) => c.ownsAdrs).map((c) => c.adrDir));
  dirs.add('docs/adr'); // the root ADR directory exists whether or not `/` is enumerated
  for (const dir of [...dirs].sort()) {
    const abs = join(ROOT, dir);
    if (!existsSync(abs)) continue;
    for (const name of readdirSync(abs).sort()) {
      const m = ADR_FILE_RE.exec(name);
      if (!m) continue;
      found.push({
        number: m[1],
        id: `ADR-${m[1]}`,
        file: `${dir}/${name}`,
        dir,
      });
    }
  }
  return found;
}

/**
 * Parse a Markdown file's leading `--- … ---` YAML frontmatter.
 *
 * Returns `null` when there is no frontmatter block at all, and throws on
 * malformed YAML so the caller can attribute the failure to a file.
 */
export function frontmatter(text) {
  const m = /^---\r?\n([\s\S]*?)\r?\n---\s*(\r?\n|$)/.exec(text);
  if (!m) return null;
  return parseSimpleYaml(m[1]);
}

/**
 * Minimal YAML reader for the ADR frontmatter contract: scalars, inline `[a, b]`
 * lists, and `- item` block lists. Deliberately NOT a general YAML parser — the
 * frontmatter contract is three keys (`status`, `contexts`, `exemplars`), and a
 * hand-rolled reader keeps this gate free of a runtime dependency that is not
 * declared in `package.json`.
 */
function parseSimpleYaml(body) {
  const out = {};
  const lines = body.split(/\r?\n/);
  let currentKey = null;
  for (const raw of lines) {
    if (!raw.trim() || raw.trim().startsWith('#')) continue;
    const blockItem = /^\s*-\s*(.*)$/.exec(raw);
    if (blockItem && currentKey) {
      if (!Array.isArray(out[currentKey])) out[currentKey] = [];
      out[currentKey].push(unquote(blockItem[1]));
      continue;
    }
    const kv = /^([A-Za-z_][\w-]*)\s*:\s*(.*)$/.exec(raw);
    if (!kv) continue;
    const [, key, rest] = kv;
    currentKey = key;
    const value = stripTrailingComment(rest.trim());
    if (value === '') {
      out[key] = []; // a block list is expected to follow
    } else if (value.startsWith('[')) {
      // Take through the LAST `]` so a trailing comment can't swallow the list
      // into the scalar branch and garble it into one bogus context name.
      const close = value.lastIndexOf(']');
      const inner = close === -1 ? value.slice(1) : value.slice(1, close);
      out[key] = inner
        .split(',')
        .map((s) => unquote(s))
        .filter((s) => s !== '');
    } else {
      out[key] = unquote(value);
      currentKey = null; // a scalar closes the key; no block list may follow
    }
  }
  return out;
}

/**
 * Drop a trailing `# comment` from a value, respecting quotes. A bare `#` only
 * opens a comment when preceded by whitespace, so `status: a#b` stays intact.
 */
function stripTrailingComment(value) {
  let quote = null;
  for (let i = 0; i < value.length; i++) {
    const ch = value[i];
    if (quote) {
      if (ch === quote) quote = null;
    } else if (ch === '"' || ch === "'") {
      quote = ch;
    } else if (ch === '#' && (i === 0 || /\s/.test(value[i - 1]))) {
      return value.slice(0, i).trim();
    }
  }
  return value;
}

function unquote(s) {
  const t = s.trim();
  if ((t.startsWith('"') && t.endsWith('"')) || (t.startsWith("'") && t.endsWith("'"))) {
    return t.slice(1, -1);
  }
  return t;
}

/** Shared failure reporter: prints a titled list of violations and exits 1. */
export function reportFailures(target, headline, violations, ...hints) {
  console.error(`\n${target} — ${headline}\n`);
  for (const v of violations) console.error(`  ${v}`);
  for (const hint of hints) console.error(`\n  ${hint}`);
  console.error('');
  process.exit(1);
}

/** Shared success reporter. */
export function reportOk(target, message) {
  console.log(`${target} — ${message} ✓`);
  process.exit(0);
}
