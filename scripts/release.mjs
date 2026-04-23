#!/usr/bin/env node
/**
 * NX Release orchestration script for Flowthru.
 *
 * Replaces commit-and-tag-version. Responsibilities:
 *  1. Determine next version via NX Release conventional commits
 *  2. Sync new version to Directory.Build.props
 *  3. Generate CHANGELOG.md and create GitHub Release
 *
 * Publishing (dotnet nuget push) is handled separately in CI after this script
 * succeeds, ensuring NuGet receives packages before the release is finalized.
 *
 * Usage:
 *   node scripts/release.mjs           # version + changelog only (local)
 *   node scripts/release.mjs --dry-run # preview without side effects
 */

import { releaseVersion, releaseChangelog } from 'nx/release/index.js';
import { readFileSync, writeFileSync } from 'node:fs';
import { execSync } from 'node:child_process';
import { resolve, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import semver from 'semver';

const __dirname = dirname(fileURLToPath(import.meta.url));
const ROOT = resolve(__dirname, '..');
const DRY_RUN = process.argv.includes('--dry-run');
const FROM_ARG = process.argv.find(a => a.startsWith('--from='));
const FROM_TAG = FROM_ARG ? FROM_ARG.slice('--from='.length) : undefined;
const FORCE_BUMP_ARG = process.argv.find(a => a.startsWith('--force-bump='));
const FORCE_BUMP = FORCE_BUMP_ARG ? FORCE_BUMP_ARG.slice('--force-bump='.length) : undefined;

// ── 1. Version determination via NX Release ──────────────────────────────────
//
// NX's conventional-commits specifier detection filters commits by project-file
// ownership — only commits that touch files belonging to a project in the release
// group are considered. Since the release group only contains the workspace root
// project (`flowthru`), commits that exclusively touch src/ library projects are
// silently discarded by NX before it can compute a specifier.
//
// To compensate, we pre-scan the git log ourselves and derive the specifier from
// commit types, then hand it to NX as an explicit override. NX still handles
// version writing, changelog generation, and tagging — we're only fixing the
// commit-detection blind spot.
//
// Commit types and their bumps mirror nx.json release.conventionalCommits.types:
//   feat         → minor
//   fix/perf/revert → patch
//   breaking (! or BREAKING CHANGE) → major
//   everything else → not releasable
function deriveSpecifierFromGitLog(fromRef) {
  const range = fromRef ? `${fromRef}..HEAD` : 'HEAD';
  const raw = execSync(`git log ${range} --format=%s`).toString().trim();
  if (!raw) return null;
  const lines = raw.split('\n').filter(Boolean);
  let hasFeat = false;
  let hasPatch = false;
  for (const msg of lines) {
    if (/^[a-z]+(\([^)]+\))?!:/.test(msg) || /^BREAKING CHANGE/.test(msg)) return 'major';
    if (/^feat(\([^)]+\))?:/.test(msg)) hasFeat = true;
    if (/^(fix|perf|revert)(\([^)]+\))?:/.test(msg)) hasPatch = true;
  }
  if (hasFeat) return 'minor';
  if (hasPatch) return 'patch';
  return null;
}

const specifier = (() => {
  if (FORCE_BUMP) {
    // For manual dispatch, compute the new version directly from package.json
    // rather than letting NX scan git tags. This is resilient to orphaned or
    // missing tags — the committed package.json version is always authoritative.
    const pkg = JSON.parse(readFileSync(resolve(ROOT, 'package.json'), 'utf8'));
    const next = semver.inc(pkg.version, FORCE_BUMP);
    if (!next) throw new Error(`Invalid semver bump type: ${FORCE_BUMP}`);
    console.log(`Manual dispatch: bumping ${pkg.version} → ${next} (${FORCE_BUMP})`);
    return next; // absolute version, bypasses NX tag resolution
  }
  return deriveSpecifierFromGitLog(FROM_TAG);
})();

if (!specifier) {
  console.log('No version bump required — no releasable commits since last tag.');
  process.exit(0);
}

const { workspaceVersion, projectsVersionData } = await releaseVersion({
  dryRun: DRY_RUN,
  verbose: false,
  specifier,
  ...(FROM_TAG ? { from: FROM_TAG } : {}),
});

if (workspaceVersion === null || workspaceVersion === undefined) {
  console.log('No version bump required — no releasable commits since last tag.');
  process.exit(0);
}

console.log(`\nNew version: ${workspaceVersion}`);

// ── 2. Sync version to Directory.Build.props ─────────────────────────────────

const buildPropsPath = resolve(ROOT, 'Directory.Build.props');
const buildPropsContent = readFileSync(buildPropsPath, 'utf8');
const updatedBuildProps = buildPropsContent.replace(
  /<Version>.*?<\/Version>/,
  `<Version>${workspaceVersion}</Version>`
);

if (updatedBuildProps === buildPropsContent) {
  console.warn('Warning: <Version> tag not found in Directory.Build.props — skipping sync.');
} else if (!DRY_RUN) {
  writeFileSync(buildPropsPath, updatedBuildProps, 'utf8');
  execSync(`git add "${buildPropsPath}"`);
  console.log(`✓ Updated Directory.Build.props to ${workspaceVersion}`);
} else {
  console.log(`[dry-run] Would update Directory.Build.props to ${workspaceVersion}`);
}

// ── 3. Changelog + commit + tag (no push, no GitHub Release yet) ─────────────
//
// git push and GitHub Release are kicked off in CI after NuGet publish succeeds.
// This ensures NuGet packages are available before the release is publicly visible.

await releaseChangelog({
  dryRun: DRY_RUN,
  verbose: false,
  version: workspaceVersion,
  versionData: projectsVersionData,
  gitPush: false,
  createRelease: false,
  ...(FROM_TAG ? { from: FROM_TAG } : {}),
});

console.log('\n✓ Release preparation complete.');
if (DRY_RUN) {
  console.log('  (dry-run — no files were modified, no tags created)');
} else {
  console.log('  Next: pack NuGet packages, push packages, then git push --follow-tags.');
}
