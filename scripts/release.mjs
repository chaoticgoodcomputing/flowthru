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

const __dirname = dirname(fileURLToPath(import.meta.url));
const ROOT = resolve(__dirname, '..');
const DRY_RUN = process.argv.includes('--dry-run');
const FROM_ARG = process.argv.find(a => a.startsWith('--from='));
const FROM_TAG = FROM_ARG ? FROM_ARG.slice('--from='.length) : undefined;

// ── 1. Version determination via NX Release ──────────────────────────────────

const { workspaceVersion, projectsVersionData } = await releaseVersion({
  dryRun: DRY_RUN,
  verbose: false,
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
