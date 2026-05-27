# Engine internals log via `ILogger` directly; the `FlowthruActivityLogger` bridge is retired

`FlowthruService`, `ParallelFlowScheduler`, and other engine components take a non-generic `ILogger` dependency — the shared singleton `AddFlowthru` registers under category `"Flowthru"` per [ADR-0005](./0005-step-logging-via-shared-ilogger.md) — and emit human-readable lifecycle and validation logs directly. This replaces the previous pattern where `FlowthruActivitySource` emitted `Activity` events and `FlowthruActivityLogger` (in `Flowthru.Cli`) bridged them into `ILogger.Log*` calls. `ActivitySource` emissions remain in place for distributed-tracing consumers — their actual purpose — but they are no longer the channel for human logs. The bridge existed only as a workaround for the constraint "core cannot depend on `ILogger`," which the step-logging convention eliminates; keeping the bridge alongside direct logging would be a backwards-compatibility shim with no real consumer. Hosts that want logs suppressed register `NullLoggerFactory` — the standard .NET escape hatch — rather than relying on the absence of a logging bridge.

## Governed code

- `src/core/Flowthru.Core/Hosting/ServiceCollectionExtensions.cs` — shared `ILogger` registration (also governed by ADR-0005)
- `src/core/Flowthru.Core/Hosting/FlowthruService.cs` — direct lifecycle logging (run-start, cache-uncacheable decisions)
- `src/core/Flowthru.Core/Diagnostics/FlowthruActivitySource.cs` — activity source scoped to OTel tracing only; human-readable logging excluded
- `src/core/Flowthru.Core/Flow/ParallelFlowScheduler.cs` — per-step logging via shared `ILogger` (also governed by ADR-0005)
- `tests/core/Flowthru.Core.Tests/Hosting/FlowthruServiceLoggingTests.cs` — regression tests for direct engine logging
- `tests/core/Flowthru.Core.Tests/Flow/ParallelFlowSchedulerLoggingTests.cs` — regression tests for direct per-step logging
- `tests/core/Flowthru.Core.Tests/Diagnostics/ActivitySourceTests.cs` — verifies activity spans exist for tracing consumers only
