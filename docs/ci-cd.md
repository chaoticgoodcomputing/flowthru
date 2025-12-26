# CI/CD Pipeline Documentation

This document describes Flowthru's continuous integration and deployment pipeline.

## Overview

The CI/CD pipeline follows industry best practices using:

- **[Conventional Commits](https://www.conventionalcommits.org/)** - Structured commit messages for automated versioning
- **[commit-and-tag-version](https://github.com/absolute-version/commit-and-tag-version)** - Automated version bumping and changelog generation
- **[NX](https://nx.dev/)** - Build orchestration and task management
- **GitHub Actions** - CI/CD automation
- **ReportGenerator** - Code coverage reports and badges

## Pipeline Stages

### 1. Continuous Integration (CI)

**Trigger:** Push to `main` or pull requests

**Workflow:** `.github/workflows/ci-test.yml`

**Steps:**
1. Run all tests with code coverage
2. Generate coverage report with badges
3. Archive coverage report as artifact

### 2. Release and Publish

**Trigger:** Push to `main` (excluding markdown and docs changes)

**Workflow:** `.github/workflows/release.yml`

**Steps:**
1. Calculate next version using conventional commits
2. Update version in:
   - `package.json`
   - `src/Flowthru/Flowthru.csproj`
   - `src/Flowthru/README.md`
3. Generate/update `CHANGELOG.md`
4. Commit changes and create git tag
5. Build and pack NuGet package
6. Generate coverage badge
7. Create GitHub Release with changelog, NuGet packages, and coverage badge
8. Publish to NuGet.org

## Semantic Versioning

Flowthru uses [Semantic Versioning 2.0.0](https://semver.org/):

- **MAJOR** (X.0.0): Breaking changes
- **MINOR** (0.X.0): New features (backward compatible)
- **PATCH** (0.0.X): Bug fixes (backward compatible)

Version bumps are determined automatically from commit messages:

| Commit Type                    | Version Bump | Example                         |
| ------------------------------ | ------------ | ------------------------------- |
| `feat!:` or `BREAKING CHANGE:` | Major        | `feat!: redesign catalog API`   |
| `feat:`                        | Minor        | `feat: add Excel support`       |
| `fix:`, `perf:`                | Patch        | `fix: null reference in parser` |
| Other types                    | None         | `docs:`, `chore:`, `style:`     |

## Conventional Commit Format

```
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
```

### Commit Types

| Type       | Description                   | Appears in Changelog |
| ---------- | ----------------------------- | -------------------- |
| `feat`     | New feature                   | ✅ Yes                |
| `fix`      | Bug fix                       | ✅ Yes                |
| `perf`     | Performance improvement       | ✅ Yes                |
| `docs`     | Documentation only            | ✅ Yes                |
| `style`    | Code style (formatting, etc.) | ❌ No                 |
| `refactor` | Code refactoring              | ❌ No                 |
| `test`     | Adding/updating tests         | ❌ No                 |
| `build`    | Build system changes          | ❌ No                 |
| `ci`       | CI configuration changes      | ❌ No                 |
| `chore`    | Other changes                 | ❌ No                 |

### Examples

```bash
# Minor release (new feature)
git commit -m "feat: add JSON catalog entry support"

# Patch release (bug fix)
git commit -m "fix: resolve CSV parsing for quoted fields"

# Major release (breaking change)
git commit -m "feat!: redesign catalog API

BREAKING CHANGE: ICatalogEntry interface now requires async Load/Save methods"

# With scope
git commit -m "feat(csv): add support for custom delimiters"

# No release
git commit -m "docs: update README with examples"
```

## Local Development

### Running Tests

```bash
# Run tests with coverage
nx run ft:test

# Generate and view coverage report
nx run ft:test:report
```

### Creating a Release (Manual)

```bash
# Preview what would be released
nx run ft:release --dry-run

# Create release (for maintainers)
nx run ft:release
git push --follow-tags
```

The manual release process:
1. Analyzes commits since last tag
2. Bumps version in all files
3. Updates CHANGELOG.md
4. Does NOT commit or tag (handled by CI)

## GitHub Actions Secrets

The following secrets must be configured in GitHub repository settings:

| Secret          | Description       | Required For        |
| --------------- | ----------------- | ------------------- |
| `NUGET_API_KEY` | NuGet.org API key | Publishing packages |

## Configuration Files

### `.versionrc.json`

Configures `commit-and-tag-version` behavior:
- Changelog structure and formatting
- Files to bump (package.json, .csproj, README.md)
- Custom updaters for non-JSON files

### `scripts/csproj-updater.js`

Custom updater for bumping version in `.csproj` XML files.

### `scripts/readme-updater.js`

Custom updater for bumping version in README.md badges.

## Best Practices

### For Contributors

1. **Use conventional commits** - Ensures proper versioning
2. **Write descriptive commit messages** - Appears in changelog
3. **One logical change per commit** - Easier to review and revert
4. **Test before committing** - CI will catch failures

### For Maintainers

1. **Review PR commit messages** - Ensure conventional format
2. **Squash merge PRs** - Clean history with proper commit message
3. **Monitor releases** - Verify automation succeeded
4. **Review coverage reports** - Check artifacts from CI runs

## Troubleshooting

### Release didn't trigger

- Check if commit message contains `[skip ci]` or `chore(release)`
- Verify push was to `main` branch
- Check if changes were only in ignored paths (docs, markdown)

### Version didn't bump

- Verify commits follow conventional format
- Check if commits are version-affecting types (`feat`, `fix`, `perf`)
- Review `commit-and-tag-version` output in Actions logs

### NuGet publish failed

- Verify `NUGET_API_KEY` secret is set correctly
- Check if package version already exists (must be unique)
- Review NuGet.org API status

## References

- [Conventional Commits](https://www.conventionalcommits.org/)
- [Semantic Versioning](https://semver.org/)
- [commit-and-tag-version](https://github.com/absolute-version/commit-and-tag-version)
- [Conventional Changelog](https://github.com/conventional-changelog/conventional-changelog)
