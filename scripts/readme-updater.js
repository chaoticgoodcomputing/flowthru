/**
 * Custom updater for README.md version badges
 * Used by commit-and-tag-version to bump version in README
 */

const VERSION_REGEX = /\*\*Version:\*\* \d+\.\d+\.\d+(?:-[\da-z\-]+(?:\.[\da-z\-]+)*)?(?:\+[\da-z\-]+(?:\.[\da-z\-]+)*)?\s*\(/i;

module.exports.readVersion = function (contents) {
  const match = contents.match(VERSION_REGEX);
  if (!match) return null;

  // Extract just the version number from "**Version:** X.Y.Z (..."
  const versionMatch = match[0].match(/\d+\.\d+\.\d+(?:-[\da-z\-]+(?:\.[\da-z\-]+)*)?(?:\+[\da-z\-]+(?:\.[\da-z\-]+)*)*/);
  return versionMatch ? versionMatch[0] : null;
};

module.exports.writeVersion = function (contents, version) {
  return contents.replace(VERSION_REGEX, `**Version:** ${version} (`);
};
