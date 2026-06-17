#!/usr/bin/env node
/**
 * Create a *verified* commit on a branch from the current working-tree diff,
 * using GitHub's GraphQL `createCommitOnBranch` mutation.
 *
 * Why this exists: the org requires verified (signed) signatures on commits to
 * main. A commit made locally by the CI bot and `git push`ed is unsigned and is
 * rejected. Commits created through the GitHub API are signed by GitHub
 * server-side and land as "Verified" — so the release flow writes its changes
 * into the working tree (scripts/release.mjs, with nx's commit/tag disabled) and
 * this script commits exactly that diff via the API.
 *
 * Auth: GITHUB_TOKEN (or GH_TOKEN) with `contents: write`. The committer/author
 * is the token's identity (github-actions[bot]); no custom author/committer or
 * signature is sent, which is what makes GitHub mark the commit Verified.
 *
 * Inputs (env):
 *   GITHUB_TOKEN / GH_TOKEN  required — token used for the mutation
 *   GITHUB_REPOSITORY        required — "owner/name" (set by Actions)
 *   COMMIT_MESSAGE           required — commit headline
 *   COMMIT_BRANCH            optional — branch to commit to (default: main)
 *
 * Output: writes `sha=<oid>` to $GITHUB_OUTPUT (and prints it).
 */

import { execFileSync } from 'node:child_process';
import { readFileSync, appendFileSync } from 'node:fs';

const token = process.env.GITHUB_TOKEN || process.env.GH_TOKEN;
const repo = process.env.GITHUB_REPOSITORY;
const message = process.env.COMMIT_MESSAGE;
const branch = process.env.COMMIT_BRANCH || 'main';

if (!token) throw new Error('GITHUB_TOKEN (or GH_TOKEN) is required.');
if (!repo) throw new Error('GITHUB_REPOSITORY is required.');
if (!message) throw new Error('COMMIT_MESSAGE is required.');

const git = (args) => execFileSync('git', args, { encoding: 'utf8' }).trim();

// Parent of the new commit = the branch tip we have checked out. If main has
// advanced since checkout, the mutation rejects with a stale-OID error — same
// non-fast-forward semantics as the previous `git push`; the workflow's
// cancel-in-progress concurrency guard makes this rare.
const expectedHeadOid = git(['rev-parse', 'HEAD']);

const toList = (out) => out.split('\n').map((s) => s.trim()).filter(Boolean);

// `--no-renames` so a rename surfaces as delete(old)+add(new) rather than a
// rename we'd have to special-case.
const changed = toList(git(['diff', '--name-only', '--no-renames', '--diff-filter=ACM', 'HEAD']));
const untracked = toList(git(['ls-files', '--others', '--exclude-standard']));
const deletions = toList(git(['diff', '--name-only', '--no-renames', '--diff-filter=D', 'HEAD']));

const additionPaths = [...new Set([...changed, ...untracked])];
const additions = additionPaths.map((path) => ({
  path,
  contents: readFileSync(path).toString('base64'),
}));

if (additions.length === 0 && deletions.length === 0) {
  console.log('No working-tree changes to commit — nothing to do.');
  process.exit(0);
}

console.log(`Committing to ${repo}@${branch} on ${expectedHeadOid}`);
console.log(`  additions: ${additions.length}  deletions: ${deletions.length}`);
for (const a of additions) console.log(`    + ${a.path}`);
for (const path of deletions) console.log(`    - ${path}`);

const query = `
  mutation ($input: CreateCommitOnBranchInput!) {
    createCommitOnBranch(input: $input) {
      commit { oid }
    }
  }`;

const input = {
  branch: { repositoryNameWithOwner: repo, branchName: branch },
  message: { headline: message },
  expectedHeadOid,
  fileChanges: {
    additions,
    deletions: deletions.map((path) => ({ path })),
  },
};

const res = await fetch('https://api.github.com/graphql', {
  method: 'POST',
  headers: {
    Authorization: `bearer ${token}`,
    'Content-Type': 'application/json',
    'User-Agent': 'flowthru-release',
  },
  body: JSON.stringify({ query, variables: { input } }),
});

const body = await res.json();
if (!res.ok || body.errors) {
  console.error('createCommitOnBranch failed:');
  console.error(JSON.stringify(body.errors ?? body, null, 2));
  process.exit(1);
}

const oid = body.data.createCommitOnBranch.commit.oid;
console.log(`\n✓ Created verified commit ${oid}`);

if (process.env.GITHUB_OUTPUT) {
  appendFileSync(process.env.GITHUB_OUTPUT, `sha=${oid}\n`);
}
