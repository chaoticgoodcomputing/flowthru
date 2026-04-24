#!/usr/bin/env node

/**
 * Stop hook: runs `nx affected -t test --base="HEAD"` and blocks on failures.
 * - No affected projects: skips entirely.
 * - All tests pass: brief success message via systemMessage.
 * - Any test failures: blocks the agent via hookSpecificOutput so it can
 *   address failures before concluding.
 * - Timeout: kills the child and emits a partial-progress block report so
 *   the agent knows tests did not complete.
 *
 * Reads stdin to check stop_hook_active and avoid infinite loops.
 * Reads its own timeout from hooks.json and self-manages a deadline so a
 * partial report is always returned before the host kills the process.
 */

const { spawnSync, spawn } = require('child_process');
const path = require('path');
const fs = require('fs');
const readline = require('readline');

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

/**
 * Format aggregated test counts for display.
 */
function formatTestCounts(counts) {
  return [
    `Passed:       ${counts.passed}`,
    `Failed:       ${counts.failed}`,
    `Skipped:      ${counts.skipped}`,
    `Inconclusive: ${counts.inconclusive}`,
    `Total:        ${counts.total}`,
  ].join('\n');
}

/**
 * Extract full failure blocks from collected output lines.
 * NX stream mode prefixes lines with "ProjectName: " — strip that before matching.
 * A block starts with an indented "Failed <TestName>" line and continues until
 * the next dotnet summary line.
 */
function extractFailureBlocks(lines) {
  const resultLines = [];
  let capturing = false;

  for (const line of lines) {
    // Strip the "ProjectName: " prefix that NX adds in stream mode.
    const stripped = line.replace(/^[\w.-]+:\s+/, '');

    if (/^\s+Failed\s+\S/.test(stripped)) {
      if (resultLines.length > 0) resultLines.push('');
      capturing = true;
      resultLines.push(stripped);
    } else if (capturing) {
      if (/^(Failed!|Passed!)\s+-\s+Failed:/.test(stripped.trim())) {
        capturing = false;
      } else {
        resultLines.push(stripped);
      }
    }
  }

  return resultLines.join('\n').trim();
}

/**
 * Read the timeout (in seconds) for this script from hooks.json.
 * Searches every event array for an entry whose `command` contains scriptBasename.
 * Returns null if the file is missing or no matching entry is found.
 */
function readHookTimeout(hooksJsonPath, scriptBasename) {
  try {
    const config = JSON.parse(fs.readFileSync(hooksJsonPath, 'utf-8'));
    for (const entries of Object.values(config.hooks || {})) {
      if (!Array.isArray(entries)) continue;
      for (const entry of entries) {
        if (
          typeof entry.command === 'string' &&
          entry.command.includes(scriptBasename) &&
          entry.timeout != null
        ) {
          return Number(entry.timeout);
        }
      }
    }
  } catch (_) {
    // File unreadable or JSON invalid — fall back to default budget.
  }
  return null;
}

// ---------------------------------------------------------------------------
// Bootstrap
// ---------------------------------------------------------------------------

// Read and parse stdin to detect re-entry.
let hookInput = {};
try {
  const raw = fs.readFileSync('/dev/stdin', 'utf-8').trim();
  if (raw) hookInput = JSON.parse(raw);
} catch (_) {
  // stdin unavailable or empty — treat as first invocation.
}

// Prevent infinite loop: if we are already in a stop-hook continuation, exit cleanly.
if (hookInput.stop_hook_active) {
  process.exit(0);
}

const repoRoot = path.resolve(__dirname, '../../..');
const scriptBasename = path.basename(__filename);
const hooksJsonPath = path.resolve(__dirname, '../hooks.json');

// Budget = hook timeout minus a safety buffer so we always return before the
// host hard-kills the process. Falls back to 5 minutes if hooks.json is missing.
const TIMEOUT_BUFFER_MS = 20_000;
const hookTimeoutSec = readHookTimeout(hooksJsonPath, scriptBasename);
const budgetMs =
  hookTimeoutSec != null
    ? hookTimeoutSec * 1000 - TIMEOUT_BUFFER_MS
    : 5 * 60 * 1000;

// ---------------------------------------------------------------------------
// Determine affected projects (those that have a `test` target)
// ---------------------------------------------------------------------------

const affectedResult = spawnSync(
  'pnpm',
  ['nx', 'show', 'projects', '--affected', '--base=HEAD', '--withTarget=test', '--json'],
  { cwd: repoRoot, encoding: 'utf-8', timeout: 30000 },
);

let affectedProjects = [];
try {
  affectedProjects = JSON.parse(affectedResult.stdout || '[]');
} catch (_) {
  // Parse failure — treat as no affected projects.
}

if (affectedProjects.length === 0) {
  process.stdout.write(JSON.stringify({
    systemMessage: 'nx affected test: skipped (no affected projects with test targets in working tree).',
  }));
  process.exit(0);
}

// ---------------------------------------------------------------------------
// Per-project progress tracking
// ---------------------------------------------------------------------------

// Set of project names whose `test` task has started executing.
const projectsStarted = new Set();

// Map<projectName, { status, passed, failed, skipped, total }>
const projectsCompleted = new Map();

// All output lines (for failure block extraction).
const outputLines = [];

const startTime = Date.now();

// "> nx run ProjectName:test [--logger ...] [[existing outputs match the cache]]"
// Matches all :test task lines regardless of trailing flags or cache marker.
const TASK_START_RE = /^>\s+nx run ([\w.-]+):test/;

// "Passed!  - Failed: 0, Passed: 30, Skipped: 0, Total: 30, Duration: 39 ms - ProjectName.dll (net10.0)"
// With --logger console;verbosity=minimal, NX strips the "ProjectName: " prefix from summary
// lines (both live and cached). The project name is recoverable from the dll filename at the end.
const SUMMARY_RE =
  /^(Passed!|Failed!)\s+-\s+Failed:\s*(\d+),\s*Passed:\s*(\d+),\s*Skipped:\s*(\d+),\s*Total:\s*(\d+).*-\s+([\w.-]+)\.dll\b/;

function processLine(line) {
  outputLines.push(line);

  const taskStart = TASK_START_RE.exec(line);
  if (taskStart) {
    projectsStarted.add(taskStart[1]);
    return;
  }

  const summary = SUMMARY_RE.exec(line);
  if (summary) {
    const [, verdict, failed, passed, skipped, total, dllName] = summary;
    projectsCompleted.set(dllName, {
      status: verdict === 'Passed!' ? 'passed' : 'failed',
      failed: parseInt(failed, 10),
      passed: parseInt(passed, 10),
      skipped: parseInt(skipped, 10),
      total: parseInt(total, 10),
    });
  }
}

function buildProjectStatusTable() {
  return affectedProjects
    .map((proj) => {
      if (projectsCompleted.has(proj)) {
        const r = projectsCompleted.get(proj);
        const icon = r.failed > 0 ? '✗' : '✓';
        return `  ${icon} ${proj.padEnd(48)} ${r.status} (${r.passed}/${r.total})`;
      }
      if (projectsStarted.has(proj)) {
        // Started but no dotnet summary: timed out mid-run, or a non-dotnet test target.
        return timedOut
          ? `  ○ ${proj.padEnd(48)} running (interrupted)`
          : `  ✓ ${proj.padEnd(48)} passed (no test summary)`;
      }
      return `  - ${proj.padEnd(48)} not started`;
    })
    .join('\n');
}

function aggregateCounts() {
  const counts = { passed: 0, failed: 0, skipped: 0, total: 0, inconclusive: 0 };
  for (const r of projectsCompleted.values()) {
    counts.passed += r.passed;
    counts.failed += r.failed;
    counts.skipped += r.skipped;
    counts.total += r.total;
  }
  counts.inconclusive = counts.total - counts.passed - counts.failed - counts.skipped;
  return counts;
}

// ---------------------------------------------------------------------------
// Result emission
// ---------------------------------------------------------------------------

function emitResult(timedOut, exitCode) {
  const elapsed = Math.round((Date.now() - startTime) / 1000);
  const counts = aggregateCounts();
  const countsDisplay = formatTestCounts(counts);
  const completedCount = projectsCompleted.size;
  const totalCount = affectedProjects.length;
  const statusTable = buildProjectStatusTable();

  if (timedOut) {
    process.stdout.write(JSON.stringify({
      hookSpecificOutput: {
        hookEventName: 'Stop',
        decision: 'block',
        reason: [
          `nx affected test TIMED OUT after ${elapsed}s (budget: ${Math.round(budgetMs / 1000)}s).`,
          `Completed ${completedCount}/${totalCount} test project(s) before deadline.`,
          '',
          'Project Status:',
          statusTable,
          '',
          'Partial Test Summary (completed projects only):',
          countsDisplay,
        ].join('\n'),
      },
    }));
    process.exit(1);
    return;
  }

  if (exitCode !== 0 || counts.failed > 0) {
    const failureBlocks = extractFailureBlocks(outputLines);
    const summary = failureBlocks.length > 0 ? failureBlocks : outputLines.slice(-50).join('\n');

    process.stdout.write(JSON.stringify({
      hookSpecificOutput: {
        hookEventName: 'Stop',
        decision: 'block',
        reason: [
          `nx affected test FAILED (${totalCount} project(s): ${affectedProjects.join(', ')}) — address these failures before concluding.`,
          '',
          'Project Status:',
          statusTable,
          '',
          'Test Summary:',
          countsDisplay,
          '',
          summary,
        ].join('\n'),
      },
    }));
    process.exit(1);
  } else {
    process.stdout.write(JSON.stringify({
      systemMessage: [
        `nx affected test: passed (${totalCount} project(s): ${affectedProjects.join(', ')}).`,
        '',
        'Project Status:',
        statusTable,
        '',
        'Test Summary:',
        countsDisplay,
      ].join('\n'),
    }));
    process.exit(0);
  }
}

// ---------------------------------------------------------------------------
// Launch async test run
// ---------------------------------------------------------------------------

const child = spawn(
  'pnpm',
  [
    'nx', 'affected', '-t', 'test',
    '--base=HEAD',
    '--output-style=stream',
    '--logger', 'console;verbosity=minimal',
  ],
  { cwd: repoRoot },
);

const rlOut = readline.createInterface({ input: child.stdout });
const rlErr = readline.createInterface({ input: child.stderr });
rlOut.on('line', processLine);
rlErr.on('line', processLine);

let timedOut = false;
const deadlineTimer = setTimeout(() => {
  timedOut = true;
  child.kill('SIGTERM');
}, budgetMs);

child.on('close', (exitCode) => {
  clearTimeout(deadlineTimer);
  emitResult(timedOut, exitCode);
});
