# Phase 7 — CLI Cache Surface (`--no-cache`, `flowthru cache invalidate`)

> **Created:** 2026-05-13
> **Status:** Pending
> **Depends on:** Phase 2 (`--exclude`, `flows:` prefix), Phase 6 (cache manifest + plan).
> **Unblocks:** —

## Motivation

The cache machinery is invisible to users without a CLI surface to control it. This phase adds the minimal set of user-facing controls:

- **Force re-run** for one invocation without erasing the cache.
- **Targeted invalidation** so a user can say "rebuild from this point onward" without nuking unrelated state.

The invariant: the slice algebra from Phase 2 governs **both** what runs and what gets invalidated. A user's selectors mean the same thing whether they're running a slice or invalidating a slice.

## Scope

**In scope:**
- New flag `--no-cache` on the existing `flowthru run` invocation.
- New subcommand `flowthru cache invalidate [<slice flags>]` that mirrors `run`'s slice surface.
- Updated help text.
- Tests for the CLI surface.

**Out of scope:**
- `flowthru cache purge` (output deletion). Per the locked design, deletion is the user's responsibility outside Flowthru. Re-runs after invalidation use each item's existing save behavior (drop+write for JSON, upsert for EFCore, etc.).
- A "diff against last run" flag like dbt's `--state` — deferred.
- Manifest inspection commands (`flowthru cache ls`, `flowthru cache show <step>`) — useful, but not blocking; defer to a follow-up.

## Current State

[ArgumentParser.cs](../../../src/core/Flowthru.Cli/ArgumentParser.cs):
- Single entry point; produces a `CliArguments` record.
- After Phase 2, supports `--from`, `--to`, `--only`, `--flow`, `--exclude`, plus the `flows:` prefix in patterns.
- After Phase 6, the CLI has the framework wiring it needs — pre-flight produces a cache plan that the scheduler honors.

[FlowthruCli.cs](../../../src/core/Flowthru.Cli/FlowthruCli.cs):
- Hosts the DI container, dispatches to `IFlowthruService.RunAsync`.
- Renders `FlowResult` outcomes to console.

## Design

### `--no-cache` flag

A new flag on `flowthru run`. Semantics:

- Suppresses cache reads — the scheduler treats every step as a cache miss.
- Cache writes still happen — successful steps update the manifest as usual.
- Effect bounded to the slice — manifest entries for nodes outside the slice are untouched.

This makes `--no-cache` useful for "I want to rebuild this run-through fresh, but next time should benefit from this run's results."

Implementation: a new `ExecutionOptions.BypassCacheReads` boolean flag. The pre-flight cache-plan walk (Phase 6) sees this flag and short-circuits to marking every step as `StaleStepLabels` for plan purposes; the post-step manifest upsert path is unaffected.

### `flowthru cache invalidate` subcommand

The CLI gains its first subcommand. Today, `flowthru run` is the only verb. Adding `cache` as a sibling verb requires a small parser restructure:

```
flowthru run [...]            # existing
flowthru cache invalidate [...]   # new
```

The slice flags after `cache invalidate` are the same as those after `run`:
- `--to`, `--from`, `--only`, `--exclude`, `--flow`
- Same prefix support (`flows:`, etc.)

Semantics:
- Resolves the slice against the merged flow (same code path as `run`).
- Loads the manifest.
- Removes manifest entries whose labels match nodes in the resolved slice (both step labels and item labels).
- Writes the manifest back atomically.
- Reports `N entries removed` and lists the affected node labels.

**Crucially**: it does *not* delete any output data. The next `run` invocation that touches those steps will be a cache miss and the steps' regular save logic takes over.

If no slice flags are provided, default is "invalidate every entry in the manifest" — with a confirmation prompt unless `--yes` is supplied. (Matches the spirit of "the user must affirm a global invalidation.")

### Examples

```bash
# Re-run the whole pipeline once without consuming cache; still populate it.
flowthru run --no-cache

# Re-run from a specific step downstream, ignoring cache for that path.
flowthru run --from BuildTrainingPairs --no-cache

# Invalidate cache for everything downstream of a specific item.
flowthru cache invalidate --from CardEmbeddings

# Invalidate cache for everything in the Ingest flow only.
flowthru cache invalidate --flow DataIngest

# Invalidate everything (with confirmation).
flowthru cache invalidate

# Invalidate everything (no prompt).
flowthru cache invalidate --yes
```

## Tasks

1. **`src/core/Flowthru.Cli/ArgumentParser.cs`** — Add `--no-cache` flag parsing. Maps to `ExecutionOptions.BypassCacheReads = true`.

2. **`src/core/Flowthru.Cli/ArgumentParser.cs`** — Restructure to dispatch on first non-flag arg. If first arg is `cache`, route to subcommand parsing; otherwise default to `run`-compatible parsing.

3. **`src/core/Flowthru.Cli/CacheCommands.cs`** — New file. Implements `cache invalidate` execution: resolves the slice, removes manifest entries, reports results.

4. **`src/core/Flowthru.Core/Flow/ExecutionOptions.cs`** — Add `bool BypassCacheReads { get; init; } = false`.

5. **`src/core/Flowthru.Core/Caching/CachePlanBuilder.cs`** (from Phase 6) — Honor `BypassCacheReads`: when set, mark every step as stale regardless of fingerprint matches.

6. **`src/core/Flowthru.Cli/FlowthruCli.cs`** — Route the parsed subcommand to either `IFlowthruService.RunAsync` or the new cache-command handler.

7. **Help text update** in [ArgumentParser.cs:198-214](../../../src/core/Flowthru.Cli/ArgumentParser.cs#L198-L214) — Document `--no-cache` and `cache invalidate`.

8. **Tests:**
   - `--no-cache` causes a cache hit scenario to re-execute the step; manifest is still updated.
   - `cache invalidate --to X` removes only matching entries; out-of-slice entries are untouched.
   - `cache invalidate` (no flags, no `--yes`) prompts; `--yes` skips prompt.
   - Default behavior (no `--no-cache`) is identical to today's `run` for non-cacheable flows.
   - Help text snapshot covers new flags and subcommand.

## Public Surface Changes

Additive:
- `flowthru run --no-cache` flag.
- `flowthru cache invalidate [slice flags] [--yes]` subcommand.
- `ExecutionOptions.BypassCacheReads` property.

No breaking changes. Existing CLI invocations continue to work identically.

## Phase Placement (per CONTRIBUTING.md)

- **Compile-time:** `ExecutionOptions` is a record; option presence is type-checked.
- **Pre-flight:** Selector parses + validates before any manifest mutation or step execution. Invalid selectors fail with a clean error before touching state.
- **Runtime:** `--no-cache` is honored when the scheduler reads the cache plan. `cache invalidate` runs entirely in pre-flight scope — it doesn't dispatch any steps.

## Testing Strategy

- CLI parser unit tests in `tests/Flowthru.Cli.Tests/`.
- Integration tests that exercise the full `run --no-cache` and `cache invalidate` paths against a fixture flow.
- A snapshot test for the help text output.

## Confirmation Criteria

- `nx run-many -t build` passes.
- `nx run affected -t test` passes; new CLI tests cover the surfaces above.
- `flowthru --help` documents the new flag and subcommand clearly.
- End-to-end demonstration: a worked flow runs once (populating manifest), then `cache invalidate --from <step>` removes manifest entries for that subgraph, then `run` re-executes only the invalidated subset.

## Risks

- **Subcommand parsing creep:** introducing `cache` as a sibling verb opens the door to many more subcommands (`cache show`, `cache stats`, etc.). Mitigation: the current parser remains minimal; if subcommand growth accelerates, the source-gen CLI work (deferred per the plan README) becomes more urgent.
- **Confirmation prompt UX:** scripting `cache invalidate` without `--yes` will hang on the prompt. Mitigation: detect non-interactive stdin and exit with a clear error directing the user to add `--yes`.
- **Slice resolution against a stale merged flow:** if the user changes step labels between runs, `cache invalidate --to OldStepName` will hit nothing. Mitigation: report `0 entries matched` clearly so the user notices.

## Follow-ups

- A `flowthru cache ls` / `flowthru cache show` family of inspection commands.
- A `flowthru cache plan --select <expr>` debug tool to preview the cache plan without running.
- The source-gen CLI work (deferred) would absorb these subcommands structurally.
