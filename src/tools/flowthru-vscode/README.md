# Flowthru — VSCode Extension

The canonical **Editor Frontend** for [Flowthru](https://github.com/chaoticgoodcomputing/flowthru), the type-safe data engineering framework for .NET.

## Status

**Scaffold only.** This package establishes the build / install / test loop. Feature implementation ships in subsequent work — see the v1 milestone definition for the five-step golden path (F2 → F3 → F4 → F5 → F6) and [ADR-0012](../../../.claude/docs/adr/0012-inspector-rpc-protocol-and-surface.md) for the Inspector RPC contract this extension consumes.

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

This extension is a [[Tool]] in Flowthru's contributor model — a process that consumes a Flow Developer's compiled Flowthru project from outside it. See [src/tools/CONTRIBUTING.md](../CONTRIBUTING.md) for Tool Developer conventions and the [Inspector](../CONTRIBUTING.md#tools-vocabulary) introspection backbone this extension talks to over JSON-RPC.

Related decisions:

- [ADR-0007](../../../.claude/docs/adr/0007-tools-as-development-context.md) — Tools as a development context
- [ADR-0011](../../../.claude/docs/adr/0011-diagnostic-anchor-contract.md) — diagnostic anchor contract (F2's data source)
- [ADR-0012](../../../.claude/docs/adr/0012-inspector-rpc-protocol-and-surface.md) — Inspector RPC protocol and surface
- [ADR-0015](../../../.claude/docs/adr/0015-snapshot-lifecycle.md) — `.flowthru/manifests/` snapshot lifecycle (F6's data source)
