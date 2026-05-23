# Contributing to Flowthru Tools

This document is for **Tool Developers** — building processes that consume Flowthru from outside a Flow Dev's project. Tools include the shared Inspector backbone, editor frontends like the Flowthru VSCode extension, agent frontends like an MCP server, and CLI utilities. Where Extensions extend what a Flow Dev *can write*, Tools extend what a Flow Dev *can do with what they've already written*.

**Audience scope:** assumes familiarity with [examples/CONTRIBUTING.md](/examples/CONTRIBUTING.md) (Flow / Catalog Developer vocabulary) and [src/extensions/CONTRIBUTING.md](/src/extensions/CONTRIBUTING.md) (Extension surfaces a Tool may introspect). Tools may be authored in any language matched to their host — TypeScript for VSCode frontends, MCP-supported languages for agent frontends — though the Inspector itself is .NET because it must load a Flow Dev's compiled Flowthru assembly.

See [/CONTRIBUTING.md](/CONTRIBUTING.md) for cross-cutting design rules (the three error phases, decision rules for where validation belongs). See [ADR-0007](/.claude/docs/adr/0007-tools-as-development-context.md) for why Tools exist as a context distinct from Extensions.

## Tool vs. Extension

The two contexts are distinct in two structural ways:

1. **Tools are *processes*; Extensions are *libraries*.** A Flow Dev references an Extension via NuGet and links it into their Flowthru project. A Flow Dev invokes a Tool from outside their project — either directly (CLI) or through an editor / agent host that owns the Tool's lifecycle.

2. **Tools may be polyglot; Extensions are .NET-only.** Because Tools live outside a Flow Dev's project, they're free to be authored in whatever language their host demands. A VSCode Editor Frontend is TypeScript; an Agent Frontend is whatever language the agent host (MCP, etc.) supports; the Inspector is .NET because it loads a Flow Dev's compiled Flowthru assembly.

Tools that need to introspect a Flow Dev's live `IFlowthruService` rely on the [[Inspector]] — they don't reload the assembly themselves. The Inspector is the canonical introspection backbone; other Tools are frontends or peers over its RPC.

## Honoring Fail-Fast in Tool UX

Tools surface errors visually or programmatically; they must not invert Flowthru's three error phases. Concretely:

- A **design-time** error (a Roslyn analyzer diagnostic) should appear in the Tool's UI as soon as the editor surfaces it. Tools may *render* design-time errors more richly (e.g., projecting an analyzer diagnostic onto a DAG node), but never delay them to a later phase.
- A **pre-flight** error (a `--dry-run` snapshot finding that requires a built assembly + DI container to detect) should be surfaced from the Inspector's snapshot endpoint, distinguishable from design-time errors so the user can tell which phase caught what.
- A **runtime** error (a step failure during a real run) should be presented post-run, with enough detail (logs, input/output state) to diagnose without re-running.

A Tool that surfaces a runtime error in a way that *looks like* a design-time error is a regression to the very thing Flowthru exists to reject.

## Glossary

### Roles

**Tool Developer**: A contributor who builds processes that consume Flowthru from outside a Flow Dev's project — editor frontends, agent frontends, CLI utilities, or the shared Inspector backbone other Tools rely on.

**Responsibilities:**
- Define and version a Tool's external surface (RPC, CLI flags, MCP tools/resources) for backward compatibility with the artifacts that depend on it.
- Keep the [[Inspector]]'s RPC the canonical introspection backbone — frontends are thin clients, not parallel introspection engines.
- Honor Flowthru's fail-fast posture in Tool UX — surface each of the three error phases distinguishably; never present a later-phase error as if it were an earlier one.
- Choose a host language matched to the Tool's audience (TypeScript for VSCode, .NET for the Inspector, MCP-host language for Agent Frontends).

_Avoid_: "Plugin Developer" (Tools are not plugins; they live outside a Flow Dev's project), "Frontend Developer" (the role spans frontends and the Inspector backbone, not just UIs).

### Tools Vocabulary

**Inspector**: The shared long-running .NET Tool that loads a Flow Dev's compiled Flowthru assembly, holds an `IFlowthruService`, and exposes a stable read + scoped-dispatch [[RPC]] consumed by Editor Frontends, Agent Frontends, and other tooling. The canonical introspection backbone for any Tool that needs live access to a Flow Dev's Flow, Catalog, or run dispatch — other Tools are frontends or peers over its RPC, not parallel engines.
_Avoid_: "Sidecar" (Kubernetes deployment-pattern baggage; "Inspector" carries the introspect-this-Flow framing without the deployment connotation), "Server" (the Inspector is launched per-Flow-Dev-project, not deployed centrally).

**Editor Frontend**: A Tool that presents a visual UI in a developer's editor; the canonical example is the Flowthru VSCode extension. Editor Frontends combine the editor's existing language services (Roslyn LSP for design-time diagnostics) with the [[Inspector]]'s RPC (for pre-flight snapshots, run dispatch, Catalog Item previews) — neither feed alone is sufficient.
_Avoid_: "IDE Plugin" (Tools are not plugins; "Editor Frontend" preserves the Tool framing).

**Agent Frontend**: A Tool that presents Flowthru's introspection surface to an LLM agent; the canonical example is a Flowthru MCP server. Agent Frontends consume the [[Inspector]]'s RPC and translate it into the agent host's protocol (MCP tools, resources, prompts). Real-run dispatch from an Agent Frontend is always gated by the host's elicitation surface — the Inspector dispatches; the Agent Frontend prompts for explicit user consent.
_Avoid_: "MCP Plugin" (Tools are not plugins; "Agent Frontend" leaves room for agent protocols beyond MCP).

**RPC**: The protocol the [[Inspector]] exposes for other Tools. Carries read endpoints (DAG projection, manifest snapshot, Catalog Item metadata + preview) and dispatch endpoints (dry-run refresh, run-slice, cancel). Dispatch is scoped: dry-run is unscoped; real-run dispatch requires explicit frontend consent (button click for Editor Frontends, agent elicitation for Agent Frontends).
_Avoid_: "API" (too generic — Flowthru already uses "API surface" elsewhere; "RPC" specifically denotes the Tool-to-Tool process protocol).
