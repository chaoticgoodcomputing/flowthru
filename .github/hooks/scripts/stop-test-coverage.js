#!/usr/bin/env node

/**
 * Stop hook: checks that every changed `src/` project also has changes in its
 * mirror `tests/` project.
 *
 * Mapping convention:
 *   src/<category>/<ProjectName>  →  tests/<category>/<ProjectName>.Tests
 *
 * Rules:
 * - Only fires when src/ files have changed vs HEAD.
 * - Skipped for any src project whose tests/ mirror directory does not exist
 *   (no test project created yet — not our concern here).
 * - tests/ directories with no src/ counterpart (helpers/, integration/) are
 *   ignored entirely — this is a one-way check from src to tests.
 * - Blocks with a per-project breakdown so the agent can justify or add tests.
 */

const { spawnSync } = require('child_process');
const fs = require('fs');
const path = require('path');

const repoRoot = path.resolve(__dirname, '../../..');

// ---------------------------------------------------------------------------
// Collect changed files vs HEAD (staged + unstaged + untracked new files)
// ---------------------------------------------------------------------------

function gitLines(args) {
  const result = spawnSync('git', args, { cwd: repoRoot, encoding: 'utf-8', timeout: 15000 });
  return (result.stdout || '').split('\n').map((l) => l.trim()).filter(Boolean);
}

const trackedChanges = gitLines(['diff', '--name-only', 'HEAD']);
const untrackedNew = gitLines(['ls-files', '--others', '--exclude-standard']);
const changedFiles = [...new Set([...trackedChanges, ...untrackedNew])];

if (changedFiles.length === 0) {
  process.exit(0);
}

// ---------------------------------------------------------------------------
// Partition changed files by src project
// ---------------------------------------------------------------------------

// Map<"category/ProjectName", string[]>  (relative paths of changed files)
const srcProjectFiles = new Map();

for (const file of changedFiles) {
  // Must be under src/<category>/<ProjectName>/...
  const match = /^src\/([^/]+)\/([^/]+)\//.exec(file);
  if (!match) continue;

  const key = `${match[1]}/${match[2]}`;
  if (!srcProjectFiles.has(key)) srcProjectFiles.set(key, []);
  srcProjectFiles.get(key).push(file);
}

if (srcProjectFiles.size === 0) {
  process.stdout.write(JSON.stringify({
    systemMessage: 'test-coverage check: skipped (no src/ changes in working tree).',
  }));
  process.exit(0);
}

// ---------------------------------------------------------------------------
// For each changed src project, check for corresponding tests/ changes
// ---------------------------------------------------------------------------

// Projects whose tests/ mirror directory does not exist at all.
const missingSuite = []; // [{ srcProject, testProject, changedSrcFiles }]

// Projects whose mirror exists but has no corresponding changes.
const noTestChanges = []; // [{ srcProject, testProject, changedSrcFiles }]

for (const [srcProject, files] of srcProjectFiles) {
  const [category, projectName] = srcProject.split('/');
  const testProject = `${category}/${projectName}.Tests`;
  const testDir = path.join(repoRoot, 'tests', testProject);

  if (!fs.existsSync(testDir)) {
    missingSuite.push({ srcProject, testProject, changedSrcFiles: files });
    continue;
  }

  const testPrefix = `tests/${testProject}/`;
  const hasTestChanges = changedFiles.some((f) => f.startsWith(testPrefix));

  if (!hasTestChanges) {
    noTestChanges.push({ srcProject, testProject, changedSrcFiles: files });
  }
}

// ---------------------------------------------------------------------------
// Emit result
// ---------------------------------------------------------------------------

if (missingSuite.length === 0 && noTestChanges.length === 0) {
  const covered = [...srcProjectFiles.keys()]
    .map((p) => `  ✓ src/${p}`)
    .join('\n');
  process.stdout.write(JSON.stringify({
    systemMessage: [
      `test-coverage check: passed (${srcProjectFiles.size} src project(s) have corresponding test changes).`,
      '',
      covered,
    ].join('\n'),
  }));
  process.exit(0);
}

const lines = [];

if (missingSuite.length > 0) {
  lines.push(
    `${missingSuite.length} src project(s) have changes but no test suite exists — a tests/ mirror must be created.`,
    '',
  );
  for (const { srcProject, testProject, changedSrcFiles } of missingSuite) {
    lines.push(`  ✗ src/${srcProject}  →  tests/${testProject}  (MISSING)`);
    for (const f of changedSrcFiles) lines.push(`      ${f}`);
    lines.push('');
  }
}

if (noTestChanges.length > 0) {
  lines.push(
    `${noTestChanges.length} src project(s) have changes with no corresponding test changes.`,
    'For each project below, either add tests or explain why none are needed.',
    '',
  );
  for (const { srcProject, testProject, changedSrcFiles } of noTestChanges) {
    lines.push(`  ○ src/${srcProject}  →  tests/${testProject}`);
    for (const f of changedSrcFiles) lines.push(`      ${f}`);
    lines.push('');
  }
}

process.stdout.write(JSON.stringify({
  hookSpecificOutput: {
    hookEventName: 'Stop',
    decision: 'block',
    reason: lines.join('\n').trim(),
  },
}));
process.exit(1);
