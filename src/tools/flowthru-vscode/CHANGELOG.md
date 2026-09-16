# Changelog

All notable changes to the Flowthru VSCode extension will be documented in this file.

## 0.0.1 — Initial scaffold

- Project structure under `src/tools/flowthru-vscode/`.
- NX targets: `build` (esbuild bundle), `install` (vsce package + `code --install-extension`), `test` (vitest).
- Placeholder activation that registers `flowthru-vscode.hello` as a sanity-check command.
- No features implemented; the Inspector RPC and snapshot-lifecycle contracts are not yet consumed.
