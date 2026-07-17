#!/usr/bin/env node
/**
 * Extracts `#region docs:<label> … #endregion` ranges from example source
 * into standalone fenced-markdown snippet files under
 * `dist/examples/docs/snippets/`. The `examples` half of the doc-snippet
 * pipeline: the `docs:sync-snippets` target consumes this `dist` output and
 * splices each snippet into the tutorials. The two projects communicate ONLY
 * through this artifact — `docs` never reaches into `examples/` source
 * directly.
 *
 * Contract:
 *   - A doc-region is any `#region docs:<label>` (C#) or `# region docs:<label>`
 *     (Python comment form) paired with a matching `#endregion`. Region
 *     nesting is tracked by depth, so an inner `#region Constructors` inside
 *     a doc-region closes correctly.
 *   - `<label>` is GLOBALLY UNIQUE across all examples. Two regions sharing a
 *     label collide on the same output filename — this script fails with both
 *     source locations, which IS the uniqueness lint (no separate check).
 *   - The emitted snippet is the region body with ALL `#region`/`#endregion`
 *     lines removed (fold markers are editor noise, not tutorial code),
 *     dedented to the common minimum indentation, and blank-trimmed.
 *   - The fence language is chosen from the source extension (.cs → csharp,
 *     .py → python) — `examples` owns the language; the docs side stays
 *     ignorant of it.
 *
 * Output: `dist/examples/docs/snippets/docs-<label>.md`, one fenced block
 * each. The `:` in the `docs:<label>` token is sanitized to `-` for the
 * filename; the token itself stays `docs:<label>` in source and sentinels.
 *
 * `#region` is a compile-time no-op, so nothing here affects whether the
 * examples build or run.
 */

import {
  mkdirSync,
  readdirSync,
  readFileSync,
  rmSync,
  statSync,
  writeFileSync,
} from 'node:fs';
import { dirname, join, relative, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const ROOT = resolve(__dirname, '..');
const OUT_DIR = join(ROOT, 'dist', 'examples', 'docs', 'snippets');

// Scan roots: the two non-archived example groups.
const SCAN_GROUPS = ['examples/starter', 'examples/advanced'];
const EXCLUDE_DIR = new Set(['bin', 'obj', 'Metadata', 'node_modules']);
const LANG_BY_EXT = { '.cs': 'csharp', '.py': 'python' };

// `#region docs:<label>` / `# region docs:<label>` — tolerates the optional
// space after `#` so the Python comment form works too.
const REGION_OPEN_RE = /^\s*#\s*region\s+(docs:\S+)\s*$/;
const REGION_ANY_OPEN_RE = /^\s*#\s*region\b/;
const REGION_CLOSE_RE = /^\s*#\s*endregion\b/;

function walk(dir, out) {
  let entries;
  try {
    entries = readdirSync(dir, { withFileTypes: true });
  } catch {
    return out;
  }
  for (const entry of entries) {
    if (entry.name.startsWith('.')) continue;
    const full = join(dir, entry.name);
    if (entry.isDirectory()) {
      if (EXCLUDE_DIR.has(entry.name)) continue;
      walk(full, out);
    } else if (entry.isFile()) {
      const ext = entry.name.slice(entry.name.lastIndexOf('.'));
      if (LANG_BY_EXT[ext]) out.push(full);
    }
  }
  return out;
}

function extractRegions(file) {
  const lines = readFileSync(file, 'utf8').replace(/\r\n/g, '\n').split('\n');
  const regions = [];
  // Stack of ALL open regions (doc and non-doc). Every region marker line is
  // dropped; every content line is appended to ALL currently-open DOC region
  // bodies — so nested doc-regions each accumulate their own snippet (the
  // inner gets its lines; the outer gets the inner's lines too, minus markers).
  const stack = [];
  for (let n = 0; n < lines.length; n++) {
    const line = lines[n];
    const open = REGION_OPEN_RE.exec(line);
    if (open) {
      stack.push({ doc: true, label: open[1], startLine: n + 1, body: [] });
      continue; // drop the marker line
    }
    if (REGION_ANY_OPEN_RE.test(line)) {
      stack.push({ doc: false }); // e.g. `#region Constructors`
      continue;
    }
    if (REGION_CLOSE_RE.test(line)) {
      const closed = stack.pop();
      if (!closed) {
        throw new Error(
          `Unmatched '#endregion' in ${relative(ROOT, file)}:${n + 1}`,
        );
      }
      if (closed.doc) {
        regions.push({
          label: closed.label,
          file,
          startLine: closed.startLine,
          endLine: n + 1,
          body: closed.body,
        });
      }
      continue; // drop the marker line
    }
    for (const region of stack) {
      if (region.doc) region.body.push(line);
    }
  }
  if (stack.length) {
    const open = stack.find((r) => r.doc) ?? stack[0];
    throw new Error(
      `Unterminated '#region${open.label ? ' ' + open.label : ''}' in `
      + `${relative(ROOT, file)}:${open.startLine ?? '?'}`,
    );
  }
  return regions;
}

// Strip the common leading whitespace from a block of lines, ignoring blank
// lines when computing the minimum, and trim leading/trailing blank lines.
function dedent(lines) {
  const trimmed = [...lines];
  while (trimmed.length && trimmed[0].trim() === '') trimmed.shift();
  while (trimmed.length && trimmed[trimmed.length - 1].trim() === '') {
    trimmed.pop();
  }
  let min = Infinity;
  for (const line of trimmed) {
    if (line.trim() === '') continue;
    const indent = line.length - line.trimStart().length;
    if (indent < min) min = indent;
  }
  if (!Number.isFinite(min) || min === 0) return trimmed;
  return trimmed.map((line) => (line.trim() === '' ? '' : line.slice(min)));
}

// Provenance line appended to every snippet artifact, so each consumer (doc or
// skill) carries a machine-generated "real source" link inside the managed
// block — never hand-authored, so it can't lie. The link is an absolute GitHub
// URL (clickable for humans, fetchable for agents — the skill-links precedent);
// deliberately file-level, no #L line anchor: line numbers shift on unrelated
// edits above the region and would churn every committed consumer.
function attribution(file) {
  const rel = relative(ROOT, file).split('\\').join('/');
  const m = /^examples\/(?:starter|advanced)\/([^/]+)\//.exec(rel);
  const display = m ? `${m[1]}/${rel.split('/').pop()}` : rel;
  return `_(source: [\`${display}\`](https://github.com/chaoticgoodcomputing/flowthru/blob/main/${rel}))_`;
}

function main() {
  const files = [];
  for (const group of SCAN_GROUPS) walk(join(ROOT, group), files);
  files.sort();

  // label → region. Collision is the global-uniqueness lint.
  const byLabel = new Map();
  for (const file of files) {
    for (const region of extractRegions(file)) {
      const prior = byLabel.get(region.label);
      if (prior) {
        console.error(
          `[generate-doc-snippets] duplicate region '${region.label}':\n`
          + `  ${relative(ROOT, prior.file)}:${prior.startLine}\n`
          + `  ${relative(ROOT, region.file)}:${region.startLine}\n`
          + `Region labels must be globally unique.`,
        );
        process.exit(1);
      }
      byLabel.set(region.label, region);
    }
  }

  rmSync(OUT_DIR, { recursive: true, force: true });
  mkdirSync(OUT_DIR, { recursive: true });

  for (const [label, region] of [...byLabel].sort()) {
    const ext = region.file.slice(region.file.lastIndexOf('.'));
    const lang = LANG_BY_EXT[ext];
    const code = dedent(region.body).join('\n');
    const fenced = `\`\`\`${lang}\n${code}\n\`\`\`\n${attribution(region.file)}\n`;
    const fileName = `${label.replace(/:/g, '-')}.md`;
    writeFileSync(join(OUT_DIR, fileName), fenced, 'utf8');
  }

  console.log(
    `[generate-doc-snippets] ${byLabel.size} snippet(s) → ${relative(ROOT, OUT_DIR)}`,
  );
}

main();
