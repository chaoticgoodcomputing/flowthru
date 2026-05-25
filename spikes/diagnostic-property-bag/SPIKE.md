# Spike: diagnostic property bag survives VSCode's diagnostic API

## What we're testing

Whether a Roslyn diagnostic emitted with `Diagnostic.Properties` populated (the `Flowthru.Anchor.*` schema from [ADR-0011](/.claude/docs/adr/0011-diagnostic-anchor-contract.md)) actually reaches a VSCode extension that calls `vscode.languages.getDiagnostics(uri)` *with the property bag intact*.

This is the load-bearing assumption underneath F2-as-renderer. If the property bag is stripped by VSCode's diagnostic adapter or by the Roslyn LSP layer in between, the renderer-over-LSP model in ADR-0011 doesn't work as written and needs a follow-up.

## Pieces

- `SpikeAnalyzer/` — a one-rule Roslyn analyzer that, whenever it sees a source file containing the marker comment `// FLOWTHRU_SPIKE_TRIGGER`, emits a warning carrying a populated `Flowthru.Anchor.*` property bag.
- `SpikeTestProject/` — a one-file C# project that contains the marker comment and references the analyzer.
- `spike-extension/` — a tiny VSCode extension that subscribes to `vscode.languages.onDidChangeDiagnostics`, finds the spike's diagnostic by code (`FLSPIKE001`), and logs its properties (and the LSP-`data` field, which is where the property bag *might* land) to an output channel.

## How to run

```bash
# Build the analyzer + test project
dotnet build SpikeTestProject

# Build + install the spike extension
cd spike-extension
pnpm install
pnpm run build
pnpm run package
code --install-extension spike-property-bag.vsix --force
cd ..

# Open the test project in VSCode and watch the diagnostic light up
code SpikeTestProject/
```

Open `SpikeTestProject/Program.cs` in VSCode. The analyzer should run, emit the diagnostic on the trigger line. Open the **Output** panel → channel **Spike: Property Bag**.

## What success looks like

The output channel shows something like:

```
[Spike] Diagnostic detected at .../Program.cs:5
  code: FLSPIKE001
  message: SPIKE diagnostic for property-bag validation
  data (LSP extension): {
    "Flowthru.Anchor.Step.0.Label": "spike_step",
    "Flowthru.Anchor.Step.0.Flow": "spike_flow",
    "Flowthru.Anchor.Item.0.Label": "spike_item"
  }
```

The presence of populated `data` (or any equivalent surface that carries the keys) means F2's renderer can read the anchor block.

## What failure looks like

The output channel shows:

```
[Spike] Diagnostic detected at .../Program.cs:5
  code: FLSPIKE001
  message: SPIKE diagnostic for property-bag validation
  data (LSP extension): undefined
```

That means VSCode's diagnostic adapter is stripping the property bag somewhere between the C# language server and the extension API. Possible follow-ons:
- Use Microsoft's Roslyn LSP instead of OmniSharp (one of them may pass it; the other may not)
- Have the extension call the C# language server directly via `vscode-languageclient` instead of relying on the unified diagnostic stream
- Re-architect F2 to source diagnostics from the Inspector instead of LSP (the Inspector loads the user's assembly anyway, so it can re-run analyzers and produce the property bag itself)

## Cleanup

Delete `spikes/diagnostic-property-bag/` once the conclusion is captured in the Phase 0 issue.
