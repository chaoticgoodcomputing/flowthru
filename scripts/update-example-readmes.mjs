#!/usr/bin/env node
/**
 * Syncs each example project's README auto-managed blocks with the
 * current state of the project. Two block types are managed:
 *
 *   - mermaid block (`<!-- flowthru:mermaid:start --> … <!-- flowthru:mermaid:end -->`):
 *     contents are replaced with `Metadata/dag-merged.md`, which is
 *     emitted by the Flowthru Mermaid metadata extension during a
 *     `dotnet run -- --dry-run` invocation.
 *
 *   - filetree block (`<!-- flowthru:filetree:start --> … <!-- flowthru:filetree:end -->`):
 *     contents are replaced with a bare ASCII tree of the example
 *     directory. The tree is a breadcrumb that lets a reader map the
 *     mermaid diagram onto the filesystem; no inline annotations.
 *     Layout rules:
 *       · Walk root is the example dir itself (so multi-project examples
 *         like SpaceflightsDistributed show their sibling-project topology).
 *       · `Data/` directories elide their `_NN_<name>` category subdirs
 *         to first + last only, with a `...` placeholder between them.
 *         The kept categories show full contents (Datasets/, Schemas/, …).
 *       · `Flows/` directories show full depth (per the user's intent:
 *         "Flows -> SpecificFlow -> Steps").
 *       · Build artefacts (bin/, obj/, TestResults/), the sync target's
 *         own output (Metadata/), and dotfiles are excluded.
 *
 * Per example under `examples/{starter,advanced}/<name>/`:
 *   1. Run `dotnet run -- --dry-run` from the project directory (the dir
 *      containing `Program.cs`) so the Mermaid extension emits
 *      `<project-dir>/Metadata/dag-merged.md`.
 *   2. Build the filetree text by walking `<example-dir>/`.
 *   3. Splice both payloads into `<example-dir>/README.md`.
 *
 * Per-block splice rules (identical for both block types):
 *   · README absent: scaffolded as `# {Name}\n\n{mermaid}\n\n{filetree}\n`.
 *   · Markers present: contents between them are replaced.
 *   · No markers but a `## File Structure` / `### File Structure` heading
 *     immediately followed by a plain code fence: the fence is converted
 *     to an auto-managed filetree block in place (one-time migration of
 *     hand-authored trees; annotations are dropped per design).
 *   · Otherwise: the block is appended at EOF.
 *
 * `dotnet run --dry-run` failures (e.g. live-infra dependencies) and
 * missing `dag-merged.md` produce a warning and skip the mermaid splice;
 * the filetree splice still runs.
 *
 * Discovery: examples are directories under `examples/starter/` or
 * `examples/advanced/` containing a `Program.cs` within three levels.
 * `examples/archived/` is excluded. The "project directory" is wherever
 * `Program.cs` lives (may be the example dir itself or a nested one).
 */

import { spawnSync } from 'node:child_process';
import {
  existsSync,
  readFileSync,
  readdirSync,
  statSync,
  writeFileSync,
} from 'node:fs';
import { dirname, join, relative, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const ROOT = resolve(__dirname, '..');
const EXAMPLE_GROUPS = ['starter', 'advanced'];

const MERMAID_START = '<!-- flowthru:mermaid:start -->';
const MERMAID_END = '<!-- flowthru:mermaid:end -->';
const FILETREE_START = '<!-- flowthru:filetree:start -->';
const FILETREE_END = '<!-- flowthru:filetree:end -->';

// Migration heuristic: a hand-authored "File Structure" or "Project
// Structure" block is a heading at level 2 or 3 followed by a plain
// code fence. Captures both styles seen in the existing READMEs.
const FILE_STRUCTURE_HEADING_RE = /^(#{2,3})\s+(?:File|Project)\s+Structure\s*$/m;

// ─── Discovery ────────────────────────────────────────────────────────

function findProgramCs(dir, maxDepth = 3) {
  if (maxDepth < 0) return null;
  let entries;
  try {
    entries = readdirSync(dir, { withFileTypes: true });
  } catch {
    return null;
  }
  for (const entry of entries) {
    if (entry.isFile() && entry.name === 'Program.cs') {
      return join(dir, entry.name);
    }
  }
  for (const entry of entries) {
    if (
      entry.isDirectory()
      && !entry.name.startsWith('.')
      && entry.name !== 'bin'
      && entry.name !== 'obj'
    ) {
      const hit = findProgramCs(join(dir, entry.name), maxDepth - 1);
      if (hit) return hit;
    }
  }
  return null;
}

function discoverExamples() {
  const examples = [];
  for (const group of EXAMPLE_GROUPS) {
    const groupDir = join(ROOT, 'examples', group);
    if (!existsSync(groupDir)) continue;
    for (const name of readdirSync(groupDir).sort()) {
      const exampleDir = join(groupDir, name);
      if (!statSync(exampleDir).isDirectory()) continue;
      const programCs = findProgramCs(exampleDir);
      if (!programCs) continue;
      examples.push({
        name,
        group,
        exampleDir,
        projectDir: dirname(programCs),
      });
    }
  }
  return examples;
}

// ─── Dry-run / mermaid emit ──────────────────────────────────────────

function runDryRun(projectDir) {
  const result = spawnSync('dotnet', ['run', '--', '--dry-run'], {
    cwd: projectDir,
    stdio: ['ignore', 'pipe', 'pipe'],
    encoding: 'utf8',
  });
  return {
    ok: result.status === 0,
    code: result.status,
    stderrTail: (result.stderr ?? '').split('\n').slice(-6).join('\n'),
  };
}

// ─── Filetree walker ─────────────────────────────────────────────────

const EXCLUDED_DIRS = new Set([
  'bin',
  'obj',
  'TestResults',
  'Metadata',
  'node_modules',
  '_generated',
]);
const EXCLUDED_FILES = new Set(['.gitignore', '.DS_Store']);
const EXCLUDED_FILE_SUFFIXES = ['.lscache', '.csproj.user', '.user'];
const DATA_CATEGORY_RE = /^_(\d+)_/;
// Catalog plumbing: literal `Catalog.cs` (root) and dotted-segment
// per-category variants (`Catalog.Raw.cs`, `Catalog.Intermediate.Production.cs`).
// Custom-named catalogs (e.g. `DataProcessingCatalog.cs`,
// `ProductionCatalog.cs`) are *not* matched — those carry per-example signal.
const CATALOG_FILE_RE = /^Catalog(?:\.[A-Za-z0-9]+)*\.cs$/;
const PROGRAM_CS_ANNOTATION = '# entry point';

// "Project boundary" dirs are pruned to keep only `Program.cs`. The example
// root is always a boundary; nested dirs that contain a `.csproj` are too
// (so each sub-project of a multi-project example like SpaceflightsDistributed
// has its own boundary).
function isProjectBoundary(dir, exampleDir) {
  if (dir === exampleDir) return true;
  try {
    return readdirSync(dir, { withFileTypes: true }).some(
      (e) => e.isFile() && e.name.endsWith('.csproj'),
    );
  } catch {
    return false;
  }
}

function shouldExclude(name, isDir, ctx) {
  if (name.startsWith('.')) return true;
  if (isDir) return EXCLUDED_DIRS.has(name);
  if (EXCLUDED_FILES.has(name)) return true;
  if (EXCLUDED_FILE_SUFFIXES.some((suffix) => name.endsWith(suffix))) return true;
  // Project-boundary dir: only Program.cs survives.
  if (ctx.atProjectBoundary && name !== 'Program.cs') return true;
  // Inside Data/: drop Catalog plumbing (root + per-category partials).
  if (ctx.insideData && CATALOG_FILE_RE.test(name)) return true;
  // Inside Flows/<FlowName>/: drop the flow-registration file.
  if (ctx.flowName && name === `${ctx.flowName}Flow.cs`) return true;
  return false;
}

// Files first, then directories; alphabetical within each group. Matches the
// "entry point → data → flows" reading order used by the existing READMEs.
function listChildren(dir, ctx) {
  let entries;
  try {
    entries = readdirSync(dir, { withFileTypes: true });
  } catch {
    return [];
  }
  return entries
    .filter((e) => !shouldExclude(e.name, e.isDirectory(), ctx))
    .sort((a, b) => {
      const aDir = a.isDirectory();
      const bDir = b.isDirectory();
      if (aDir !== bDir) return aDir ? 1 : -1;
      return a.name.localeCompare(b.name);
    });
}

// Inside a `Data/` directory, collapse the run of `_NN_<name>` category
// dirs down to the first and last, with a `...` placeholder between.
// Non-category children (e.g. `Data/Catalog.cs`) pass through unchanged.
function applyDataCategoryElision(entries) {
  const categories = entries.filter(
    (e) => e.isDirectory() && DATA_CATEGORY_RE.test(e.name),
  );
  if (categories.length <= 2) {
    return entries.map((entry) => ({ kind: 'entry', entry }));
  }
  const numOf = (name) => parseInt(name.match(DATA_CATEGORY_RE)[1], 10);
  const sorted = [...categories].sort((a, b) => numOf(a.name) - numOf(b.name));
  const firstName = sorted[0].name;
  const lastName = sorted[sorted.length - 1].name;
  const result = [];
  let ellipsisInserted = false;
  for (const entry of entries) {
    const isCategory = entry.isDirectory() && DATA_CATEGORY_RE.test(entry.name);
    if (!isCategory) {
      result.push({ kind: 'entry', entry });
      continue;
    }
    if (entry.name === firstName) {
      result.push({ kind: 'entry', entry });
      if (!ellipsisInserted) {
        result.push({ kind: 'ellipsis' });
        ellipsisInserted = true;
      }
    } else if (entry.name === lastName) {
      result.push({ kind: 'entry', entry });
    }
    // Other categories are elided.
  }
  return result;
}

function renderTree(exampleDir, displayName) {
  const lines = [`${displayName}/`];

  function walk(dir, prefix, parentCtx) {
    const ctx = {
      atProjectBoundary: isProjectBoundary(dir, exampleDir),
      insideData: parentCtx.insideData,
      flowName: parentCtx.flowName,
    };
    const children = listChildren(dir, ctx);
    const items = ctx.insideData && parentCtx.parentName === 'Data'
      ? applyDataCategoryElision(children)
      : children.map((entry) => ({ kind: 'entry', entry }));

    for (let i = 0; i < items.length; i++) {
      const isLast = i === items.length - 1;
      const branch = isLast ? '└── ' : '├── ';
      const continuation = isLast ? '    ' : '│   ';

      if (items[i].kind === 'ellipsis') {
        lines.push(`${prefix}${branch}...`);
        continue;
      }
      const entry = items[i].entry;
      if (entry.isDirectory()) {
        lines.push(`${prefix}${branch}${entry.name}/`);
        const childCtx = {
          insideData: ctx.insideData || entry.name === 'Data',
          // Track the flow name when we descend into a Flows/<FlowName>/ dir.
          // Reset on any other directory so Steps/ doesn't inherit.
          flowName:
            parentCtx.parentName === 'Flows' ? entry.name : null,
          parentName: entry.name,
        };
        walk(join(dir, entry.name), prefix + continuation, childCtx);
      } else if (entry.name === 'Program.cs' && ctx.atProjectBoundary) {
        lines.push(`${prefix}${branch}${entry.name}  ${PROGRAM_CS_ANNOTATION}`);
      } else {
        lines.push(`${prefix}${branch}${entry.name}`);
      }
    }
  }

  walk(exampleDir, '', { insideData: false, flowName: null, parentName: null });
  return lines.join('\n');
}

// ─── README splice helpers ───────────────────────────────────────────

function wrapBlock(startMarker, endMarker, payload) {
  return `${startMarker}\n${payload.trimEnd()}\n${endMarker}`;
}

function spliceBetweenMarkers(content, startMarker, endMarker, payload) {
  const startIdx = content.indexOf(startMarker);
  if (startIdx === -1) return null;
  const endIdx = content.indexOf(endMarker, startIdx + startMarker.length);
  if (endIdx === -1) return null;
  const before = content.slice(0, startIdx);
  const after = content.slice(endIdx + endMarker.length);
  return `${before}${wrapBlock(startMarker, endMarker, payload)}${after}`;
}

// Migrate a hand-authored `## File Structure` / `## Project Structure`
// block (heading + plain code fence) into a marker-wrapped filetree
// block in the same position. The plain fence must be the *immediately
// next* non-blank content after the heading — if a sub-heading or any
// other element appears between the heading and the fence, the heading
// is not treated as a file-structure section (e.g. `## Project
// Structure` followed by `### Flow Structure` and a mermaid fence is
// just a structural heading for the mermaid block; no migration).
// Annotations inside the migrated fence are dropped — by design.
function migrateHandAuthoredFiletree(content, treePayload) {
  const headingMatch = content.match(FILE_STRUCTURE_HEADING_RE);
  if (!headingMatch) return null;
  const headingStart = headingMatch.index;
  const headingEnd = headingStart + headingMatch[0].length;
  // Immediately-next plain fence: after the heading, allow only blank
  // lines, then require ` ``` ` (no language tag) on its own line.
  const after = content.slice(headingEnd);
  const adjacentFenceRe = /^(?:[ \t]*\n)+```[ \t]*\n/;
  const openMatch = after.match(adjacentFenceRe);
  if (!openMatch) return null;
  const fenceOpenAbs = headingEnd + openMatch[0].indexOf('```');
  const fenceContentStart = headingEnd + openMatch[0].length;
  const closeRel = content.slice(fenceContentStart).indexOf('\n```');
  if (closeRel === -1) return null;
  const fenceCloseEnd = fenceContentStart + closeRel + '\n```'.length;
  const before = content.slice(0, fenceOpenAbs);
  const tail = content.slice(fenceCloseEnd);
  return `${before}${wrapBlock(FILETREE_START, FILETREE_END, treePayload)}${tail}`;
}

function appendBlock(content, startMarker, endMarker, payload) {
  const trailingNewline = content.endsWith('\n') ? '' : '\n';
  return `${content}${trailingNewline}\n${wrapBlock(startMarker, endMarker, payload)}\n`;
}

// ─── Top-level per-README processing ─────────────────────────────────

function scaffoldReadme(name, mermaidPayload, treePayload) {
  const parts = [`# ${name}`, ''];
  if (mermaidPayload !== null) {
    parts.push(wrapBlock(MERMAID_START, MERMAID_END, mermaidPayload), '');
  }
  parts.push(wrapBlock(FILETREE_START, FILETREE_END, treePayload), '');
  return parts.join('\n');
}

function applyMermaid(content, mermaidPayload) {
  if (mermaidPayload === null) return { content, action: 'mermaid:skipped' };
  const replaced = spliceBetweenMarkers(
    content,
    MERMAID_START,
    MERMAID_END,
    mermaidPayload,
  );
  if (replaced !== null) {
    return {
      content: replaced,
      action: replaced === content ? 'mermaid:unchanged' : 'mermaid:updated',
    };
  }
  return {
    content: appendBlock(content, MERMAID_START, MERMAID_END, mermaidPayload),
    action: 'mermaid:appended',
  };
}

function applyFiletree(content, treePayload) {
  const replaced = spliceBetweenMarkers(
    content,
    FILETREE_START,
    FILETREE_END,
    treePayload,
  );
  if (replaced !== null) {
    return {
      content: replaced,
      action: replaced === content ? 'filetree:unchanged' : 'filetree:updated',
    };
  }
  const migrated = migrateHandAuthoredFiletree(content, treePayload);
  if (migrated !== null) {
    return { content: migrated, action: 'filetree:migrated' };
  }
  return {
    content: appendBlock(content, FILETREE_START, FILETREE_END, treePayload),
    action: 'filetree:appended',
  };
}

function processReadme(readmePath, name, mermaidPayload, treePayload) {
  if (!existsSync(readmePath)) {
    writeFileSync(readmePath, scaffoldReadme(name, mermaidPayload, treePayload));
    return ['scaffolded'];
  }
  const original = readFileSync(readmePath, 'utf8');
  const mermaidStep = applyMermaid(original, mermaidPayload);
  const filetreeStep = applyFiletree(mermaidStep.content, treePayload);
  if (filetreeStep.content !== original) {
    writeFileSync(readmePath, filetreeStep.content);
  }
  return [mermaidStep.action, filetreeStep.action];
}

// ─── Main ────────────────────────────────────────────────────────────

function main() {
  const examples = discoverExamples();
  const tally = {};
  const bump = (key) => {
    tally[key] = (tally[key] ?? 0) + 1;
  };

  for (const ex of examples) {
    const rel = relative(ROOT, ex.exampleDir);
    process.stdout.write(`→ ${rel}\n`);

    let mermaidPayload = null;
    const dry = runDryRun(ex.projectDir);
    if (!dry.ok) {
      process.stdout.write(
        `  ⚠ dotnet run --dry-run failed (exit ${dry.code}); mermaid skipped.\n`
          + `    ${dry.stderrTail.replace(/\n/g, '\n    ')}\n`,
      );
      bump('skipped:dry-run-failed');
    } else {
      const dagPath = join(ex.projectDir, 'Metadata', 'dag-merged.md');
      if (existsSync(dagPath)) {
        mermaidPayload = readFileSync(dagPath, 'utf8');
      } else {
        process.stdout.write(
          `  ⚠ dag-merged.md not produced at ${relative(ROOT, dagPath)}; mermaid skipped.\n`,
        );
        bump('skipped:no-metadata');
      }
    }

    // Wrap the tree in a plain code fence so markdown renders it
    // verbatim — without the fence, the `├──` lines look like a
    // malformed pipe table to most renderers.
    const treePayload = '```\n' + renderTree(ex.exampleDir, ex.name) + '\n```';
    const readmePath = join(ex.exampleDir, 'README.md');
    const actions = processReadme(
      readmePath,
      ex.name,
      mermaidPayload,
      treePayload,
    );
    for (const action of actions) {
      bump(action);
      process.stdout.write(`  ${action}\n`);
    }
  }

  process.stdout.write('\nSummary:\n');
  for (const key of Object.keys(tally).sort()) {
    process.stdout.write(`  ${key}: ${tally[key]}\n`);
  }

  // Warnings only — CI signals drift via `git diff --exit-code examples/`.
  process.exit(0);
}

main();
