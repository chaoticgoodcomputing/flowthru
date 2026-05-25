# Phase 0 Spikes

Throwaway de-risking experiments for the Editor Frontend v1 plan. Each spike validates one architectural assumption from the ADRs before the implementation work commits to it.

| Spike | Validates | ADR |
|---|---|---|
| [`diagnostic-property-bag/`](./diagnostic-property-bag/) | Custom `Diagnostic.Properties` keys reach a VSCode extension via `vscode.languages.getDiagnostics()` with the bag intact | [ADR-0011](/.claude/docs/adr/0011-diagnostic-anchor-contract.md) |
| [`jsonrpc-stdio/`](./jsonrpc-stdio/) | `StreamJsonRpc` (.NET) ↔ `vscode-jsonrpc` (TS) over Content-Length-framed stdio, including round-trip + notifications | [ADR-0012](/.claude/docs/adr/0012-inspector-rpc-protocol-and-surface.md) |

## Lifecycle

These directories exist to be deleted. Once Phase 0 closes — both spikes validated and conclusions captured in issue comments — this whole `spikes/` directory should be removed and the conclusions referenced from the Phase 0 issues.
