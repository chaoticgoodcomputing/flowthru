#!/usr/bin/env node

/**
 * Calculates the next semantic version based on conventional commits
 * Updates version in package.json, Flowthru.csproj, and README.md
 * 
 * Uses:
 * - conventional-recommended-bump for analyzing commit history
 * - semver for version manipulation
 */

const { execSync } = require('child_process');
const fs = require('fs');
const path = require('path');
const semver = require('semver');
const xml2js = require('xml2js');

const CSPROJ_PATH = path.join(__dirname, '../src/core/Flowthru/Flowthru.csproj');
const PACKAGE_JSON_PATH = path.join(__dirname, '../package.json');
const README_PATH = path.join(__dirname, '../src/core/Flowthru/README.md');

async function getCurrentVersion() {
  const csprojContent = fs.readFileSync(CSPROJ_PATH, 'utf8');
  const parser = new xml2js.Parser();
  const result = await parser.parseStringPromise(csprojContent);

  const version = result.Project.PropertyGroup[0].Version[0];
  if (!semver.valid(version)) {
    throw new Error(`Invalid version in Flowthru.csproj: ${version}`);
  }
  return version;
}

function getRecommendedBump() {
  try {
    // Use conventional-recommended-bump to analyze commits
    const result = execSync(
      'conventional-recommended-bump -p angular',
      { encoding: 'utf8', stdio: ['pipe', 'pipe', 'ignore'] }
    ).trim();

    // Result is one of: major, minor, patch
    return result || null;
  } catch (error) {
    // If no tags exist or no conventional commits, default to patch
    console.log('Could not determine version bump, defaulting to patch');
    return 'patch';
  }
}

async function updateCsProj(newVersion) {
  const content = fs.readFileSync(CSPROJ_PATH, 'utf8');
  const parser = new xml2js.Parser();
  const builder = new xml2js.Builder();

  const result = await parser.parseStringPromise(content);
  result.Project.PropertyGroup[0].Version[0] = newVersion;

  const xml = builder.buildObject(result);
  fs.writeFileSync(CSPROJ_PATH, xml, 'utf8');
  console.log(`✓ Updated Flowthru.csproj to ${newVersion}`);
}

function updatePackageJson(newVersion) {
  const pkg = JSON.parse(fs.readFileSync(PACKAGE_JSON_PATH, 'utf8'));
  pkg.version = newVersion;
  fs.writeFileSync(PACKAGE_JSON_PATH, JSON.stringify(pkg, null, 2) + '\n', 'utf8');
  console.log(`✓ Updated package.json to ${newVersion}`);
}

function updateReadme(newVersion) {
  let content = fs.readFileSync(README_PATH, 'utf8');
  // Match semver pattern: https://semver.org/#is-there-a-suggested-regular-expression-regex-to-check-a-semver-string
  content = content.replace(
    /\*\*Version:\*\* \d+\.\d+\.\d+(?:-[\da-z\-]+(?:\.[\da-z\-]+)*)?(?:\+[\da-z\-]+(?:\.[\da-z\-]+)*)?/i,
    `**Version:** ${newVersion}`
  );
  fs.writeFileSync(README_PATH, content, 'utf8');
  console.log(`✓ Updated README.md to ${newVersion}`);
}

async function main() {
  const currentVersion = await getCurrentVersion();
  console.log(`Current version: ${currentVersion}`);

  const bump = getRecommendedBump();
  if (!bump) {
    console.log('No version bump required');
    process.exit(0);
  }

  const newVersion = semver.inc(currentVersion, bump);
  if (!newVersion) {
    throw new Error(`Failed to calculate new version from ${currentVersion} with bump ${bump}`);
  }

  console.log(`Recommended bump: ${bump}`);
  console.log(`New version: ${newVersion}`);

  await updateCsProj(newVersion);
  updatePackageJson(newVersion);
  updateReadme(newVersion);

  // Write version to file for GitHub Actions to read
  fs.writeFileSync(path.join(__dirname, '../.version'), newVersion, 'utf8');
  console.log(`✓ Wrote version to .version file`);

  console.log(`\n✨ Version bumped from ${currentVersion} to ${newVersion}`);
}

main().catch(error => {
  console.error('Error:', error.message);
  process.exit(1);
});
