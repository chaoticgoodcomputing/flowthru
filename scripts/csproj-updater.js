/**
 * Custom updater for .csproj files
 * Used by commit-and-tag-version to bump version in Flowthru.csproj
 */

const VERSION_REGEX = /<Version>(.*?)<\/Version>/;

module.exports.readVersion = function (contents) {
  const match = contents.match(VERSION_REGEX);
  return match ? match[1] : null;
};

module.exports.writeVersion = function (contents, version) {
  return contents.replace(VERSION_REGEX, `<Version>${version}</Version>`);
};
