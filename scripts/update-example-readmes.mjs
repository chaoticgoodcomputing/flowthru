#!/usr/bin/env node
/**
 * Syncs each example project's README mermaid block with the latest
 * `Metadata/dag-merged.md` produced by the Flowthru Mermaid metadata
 * extension.
 *
 * Per example under `examples/{starter,advanced}/<name>/`:
 *   1. Run `dotnet run --project <project-dir> -- --dry-run` from the
 *      project directory so the Mermaid extension emits
 *      `<project-dir>/Metadata/dag-merged.md`.
 *   2. Splice the contents of `dag-merged.md` between the
 *      `<!-- flowthru:mermaid:start -->` / `<!-- flowthru:mermaid:end -->`
 *      markers in `<name>/README.md`.
 *
 * README handling:
 *   - Missing entirely → scaffold `# {Name}` + marker block + diagram.
 *   - Present but missing markers → append marker block + diagram at EOF.
 *   - Markers present → replace the content between them.
 *
 * Skip + warn when `dotnet run --dry-run` fails (some examples need live
 * infra like Testcontainers) or `dag-merged.md` is absent.
 *
 * Discovery: examples are any directory under `examples/starter/` or
 * `examples/advanced/` that contains a `Program.cs` within three
 * levels. `examples/archived/` is excluded. The project directory
 * (where `dotnet run` is invoked and where `Metadata/` lands) is the
 * directory containing `Program.cs`; the README lives at the top-level
 * example dir, which may be the same dir or a parent (e.g.
 * `SpaceflightsDistributed/` whose runnable host is one level deeper).
 */

import { spawnSync } from 'node:child_process';
import {
  existsSync,
  readFileSync,
  readdirSync,
  statSync,
  writeFileSync,
} from 'node:fs';
import { basename, dirname, join, relative, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const ROOT = resolve(__dirname, '..');
const EXAMPLE_GROUPS = ['starter', 'advanced'];
const MARKER_START = '<!-- flowthru:mermaid:start -->';
const MARKER_END = '<!-- flowthru:mermaid:end -->';

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
    if (entry.isDirectory() && !entry.name.startsWith('.') && entry.name !== 'bin' && entry.name !== 'obj') {
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

function spliceMermaid(readmePath, exampleName, mermaidContent) {
  const block = `${MARKER_START}\n${mermaidContent.trim()}\n${MARKER_END}`;

  if (!existsSync(readmePath)) {
    const scaffold = `# ${exampleName}\n\n${block}\n`;
    writeFileSync(readmePath, scaffold);
    return 'scaffolded';
  }

  const original = readFileSync(readmePath, 'utf8');
  const startIdx = original.indexOf(MARKER_START);
  const endIdx = original.indexOf(MARKER_END);

  if (startIdx === -1 || endIdx === -1 || endIdx < startIdx) {
    const trailingNewline = original.endsWith('\n') ? '' : '\n';
    const appended = `${original}${trailingNewline}\n${block}\n`;
    writeFileSync(readmePath, appended);
    return 'appended';
  }

  const before = original.slice(0, startIdx);
  const after = original.slice(endIdx + MARKER_END.length);
  const updated = `${before}${block}${after}`;
  if (updated === original) return 'unchanged';
  writeFileSync(readmePath, updated);
  return 'updated';
}

function main() {
  const examples = discoverExamples();
  const summary = {
    scaffolded: [],
    appended: [],
    updated: [],
    unchanged: [],
    'skipped:dry-run-failed': [],
    'skipped:no-metadata': [],
  };

  for (const ex of examples) {
    const rel = relative(ROOT, ex.exampleDir);
    process.stdout.write(`→ ${rel}\n`);

    const dry = runDryRun(ex.projectDir);
    if (!dry.ok) {
      process.stdout.write(
        `  ⚠ dotnet run --dry-run failed (exit ${dry.code}); skipping.\n` +
          `    ${dry.stderrTail.replace(/\n/g, '\n    ')}\n`,
      );
      summary['skipped:dry-run-failed'].push(rel);
      continue;
    }

    const dagPath = join(ex.projectDir, 'Metadata', 'dag-merged.md');
    if (!existsSync(dagPath)) {
      process.stdout.write(
        `  ⚠ dag-merged.md not produced at ${relative(ROOT, dagPath)}; skipping.\n`,
      );
      summary['skipped:no-metadata'].push(rel);
      continue;
    }

    const mermaidContent = readFileSync(dagPath, 'utf8');
    const readmePath = join(ex.exampleDir, 'README.md');
    const action = spliceMermaid(readmePath, ex.name, mermaidContent);
    summary[action].push(rel);
    process.stdout.write(`  ${action}\n`);
  }

  process.stdout.write('\nSummary:\n');
  for (const [key, items] of Object.entries(summary)) {
    process.stdout.write(`  ${key}: ${items.length}\n`);
  }

  // Warnings only — CI signals drift via `git diff --exit-code examples/`.
  process.exit(0);
}

main();
