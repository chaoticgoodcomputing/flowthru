# Inspector RPC: protocol, consent, schema, v1 surface

The [[Inspector]]'s RPC binds Editor Frontends (the planned VSCode extension) and Agent Frontends (an MCP server) to the same Flowthru introspection backbone. Four decisions define the v1 contract.

## Wire protocol, transport, lifecycle

JSON-RPC 2.0 over stdio, frontend-launched per consumer. This is what MCP itself uses and what VSCode language clients are idiomatic for (Roslyn LSP, OmniSharp, C# DevKit). Each frontend spawns its own Inspector child process; lifecycle is tied to the consumer that owns it. Cost accepted for v1: double compilation when both frontends are active on the same project. An `--attach` flag is **reserved** in the Inspector CLI surface for a future named-pipe shared-instance mode if multi-consumer-shared-state ever becomes a real need; v1 ships without it.

## Consent model

The Inspector is consent-blind. Real-run dispatch carries no consent field, no two-step prepare/confirm flow, no identity-based gating in the protocol — because the Inspector is a local process and cannot meaningfully validate any consent payload (any field a frontend can set, a frontend can fabricate). Consent is a *frontend* concern: VSCode collects it via button click; MCP via host elicitation. The Inspector logs the caller's `initialize`-declared identity with every dispatch as the audit trail. The trust boundary lives at the frontend's user-interaction surface, not in the protocol. This is a contract on Agent Frontend authors (they MUST elicit before dispatching real runs) and a structural property of Editor Frontends (no path to dispatch without a UI gesture). See [[Frontend trust boundary]] in [src/tools/CONTRIBUTING.md](/src/tools/CONTRIBUTING.md).

## Schema source

`[RpcMethod]`-decorated C# interfaces are the source of truth; a `Flowthru.Tools.Inspector.SourceGenerators` package emits typed TypeScript client bindings. Same architectural pattern Flowthru already uses for `FlowBuilderGenerator`, `CatalogPropertyGenerator`, `ColumnNewTypeGenerator`. Polyglot Tool authors (a hypothetical Python Agent Frontend) derive their client from the generated TS or the C# source — second-class but acceptable. The closed-sum-to-TS-discriminated-union mapping is a **non-negotiable** feature of the generator: it preserves Flowthru's structured-error story (`RuntimeError`, `PreFlightError`) across the RPC boundary without flattening to strings.

## v1 method surface

Twelve methods, mapped one-to-one to F2–F6's needs.

**Lifecycle:**
- `flowthru/initialize` — handshake. Client declares identity, project root, protocol version. Inspector returns capabilities and loaded Flowthru version.
- `flowthru/shutdown` — graceful stop request.
- `exit` — terminate process (standard JSON-RPC convention).

**Reads:**
- `flowthru/dag/get` — live `DagMetadataProjection` from the registered `IFlowthruService`.
- `flowthru/dag/snapshot` — last successful manifest snapshot read from disk.
- `flowthru/catalog/item/preview` — row preview for a specific item.

**Dispatches:**
- `flowthru/dryrun` — execute pre-flight only; emit fresh snapshot.
- `flowthru/run/start` — execute a slice. Returns `runId`. Inspector logs caller identity.
- `flowthru/run/cancel` — cancel an in-flight run by `runId`.

**Notifications (Inspector → frontend, correlated by `runId`):**
- `flowthru/run/log`, `flowthru/run/step`, `flowthru/run/end`.

Two design rules: (a) **no catalog writes ever** — even via dispatch; Flows are the only producers of catalog state. (b) **`run/start` is refused without a prior `initialize`** — protocol-level expression of the audit-trail story; every dispatched run is provably attributable to a declared client identity.

Explicit v2-or-later deferrals: `flows/list`, `catalog/list`, `cache/plan`, `step/diagnostics`. Each gets added on demand when frontend code demonstrates need rather than pre-emptively.
