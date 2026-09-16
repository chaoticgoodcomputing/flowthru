# Flowthru — VSCode Extension

The canonical **Editor Frontend** for [Flowthru](https://github.com/chaoticgoodcomputing/flowthru), the type-safe data engineering framework for .NET.

## Status

**Scaffold only.** This package establishes the build / install / test loop. Feature implementation ships in subsequent work — see the v1 milestone definition for the five-step golden path (F2 → F3 → F4 → F5 → F6). The Inspector RPC contract this extension consumes is documented in [src/tools/CONTRIBUTING.md](../CONTRIBUTING.md#tools-vocabulary).

## Development

From the repository root:

```bash
# compile + bundle to out/extension.js
nx run flowthru-vscode:build

# package as .vsix and install into your local VSCode
nx run flowthru-vscode:install

# run unit tests
nx run flowthru-vscode:test
```

Watch mode (rebuild on change):

```bash
pnpm --filter flowthru-vscode run watch
```

## Architecture

This extension is a Tool in Flowthru's contributor model — a process that consumes a Flow Developer's compiled Flowthru project from outside it. See [src/tools/CONTRIBUTING.md](../CONTRIBUTING.md) for Tool Developer conventions and the [Inspector](../CONTRIBUTING.md#tools-vocabulary) introspection backbone this extension talks to over JSON-RPC.

The decisions this extension is built on — Tools as a development context, the
diagnostic anchor contract behind F2, the Inspector RPC protocol, and the
`.flowthru/manifests/` snapshot lifecycle behind F6 — are recorded in the
repository's architecture decision records (`docs/adr/`).
