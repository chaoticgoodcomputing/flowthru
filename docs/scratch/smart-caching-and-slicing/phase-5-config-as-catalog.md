# Phase 5 — Reintroduce Config-as-Catalog

> **Created:** 2026-05-13
> **Status:** Pending
> **Depends on:** —
> **Unblocks:** Canonicalization rule for Phase 6 (steps with `IConfiguration` dependencies must model them as fingerprintable inputs to be cacheable).

## Motivation

Pre-0.17.0, Flowthru exposed a configuration catalog pattern: the FlowthruService accepted an `IConfiguration` (via the standard .NET configuration builder), wrapped it as a specially-constructed catalog, and made config values available to flows and steps as catalog inputs. The 0.16 → 0.17 functional-programming rewrite lost the pattern.

Bringing it back is a soft prerequisite for caching correctness. The locked design's **canonicalization rule** says:

> All step-influencing state must be modeled as either a fingerprintable input or a `ServiceDependency`.

Today, a step that reads `IConfiguration["FeatureFlags:UseV2"]` has hidden state — change the config value, the step's behavior changes, but nothing in the DAG reflects it. With config-as-catalog, that value is an input item with a fingerprint, and the cache plan correctly invalidates the step.

This also lets the Environment-variable surface participate (via `EnvironmentVariablesConfigurationProvider`) and lets static-field-at-module-load state be re-expressed canonically as either a config input or an upstream step's output.

## Scope

**In scope:**
- A specially-constructed catalog item type backed by `IConfiguration` — accepts a section path, exposes typed access, and implements `ISupportsFingerprint`.
- Hosting integration: `FlowthruServiceBuilder` accepts an `IConfiguration` and registers it for catalog use.
- Step authoring surface: steps receive config values as inputs the same way they receive any other catalog item.
- A worked example in `examples/` showing a step parameterized by a config value.

**Out of scope:**
- Dynamic config reloading (`IOptionsMonitor`-style). v1 binds config once at FlowthruService construction; behavior is identical across a run.
- Secret management. Configs containing secrets are out of scope for fingerprinting until we have a documented redaction story.

## Design

### Item shape

```csharp
namespace Flowthru.Data.Configuration;

/// <summary>
/// A catalog item backed by a section of <see cref="IConfiguration"/>.
/// The item's value is the bound type T; its fingerprint is a hash
/// of the section's configuration values at the time of fingerprint
/// computation.
/// </summary>
public sealed class ConfigurationItem<T> : IItem<T>
    where T : notnull, new()
{
    private readonly string _label;
    private readonly IConfigurationSection _section;

    public string Label => _label;
    public Type DataType => typeof(T);

    public FlowIO<T> Load() => FlowIO.From(() => _section.Get<T>() ?? new T());
    public FlowIO<FlowUnit> Save(T value) =>
        FlowIO.Fail(new InvalidOperationException(
            "Configuration items are read-only; flows must not write to config."));
    public FlowIO<bool> Exists() => FlowIO.Pure(_section.Exists());

    // ISupportsFingerprint via the underlying section
    public FlowIO<string>? TryGetFingerprint() =>
        FlowIO.From(() => HashSection(_section));

    private static string HashSection(IConfigurationSection section)
    {
        using var sha = SHA256.Create();
        foreach (var (k, v) in EnumerateAll(section))
        {
            sha.AppendData(Encoding.UTF8.GetBytes($"{k}={v ?? ""};"));
        }
        return Convert.ToHexString(sha.GetCurrentHash())[..16];
    }
}
```

### Catalog surface

Mirrors the existing `Item.Of<T>().Json().AtPath(...).Build()` ergonomics:

```csharp
Item.Of<FeatureFlagsConfig>("feature-flags")
    .FromConfiguration()
    .AtSection("FeatureFlags")
    .Build();
```

### Host wiring

```csharp
services.AddFlowthru(b =>
{
    b.UseConfiguration(builderContext.Configuration);
    b.RegisterCatalog<MyCatalog>(_ => new MyCatalog());
    b.RegisterFlow<MyCatalog>(catalog =>
        FlowBuilder.CreateFlow("main", p => /* steps */));
});
```

`UseConfiguration` registers the `IConfiguration` as a singleton resolvable by `ConfigurationItem<T>` via the ambient `IServiceProvider` flow established by Phase 1.

### Read-only enforcement

`ConfigurationItem<T>.Save(...)` always fails. The dependency analyzer detects: if a flow's step declares a `ConfigurationItem<T>` as output (impossible by type — `IItem<T>` is invariant in `T` but `Save` is on the item, not on a producer interface, so this happens at runtime). A pre-flight check `MustNotBeProducer(ConfigurationItem)` catches it earlier.

Better: introduce a marker `IReadOnlyItem<T>` and have `Step<TIn, TOut>` constructors reject read-only items in their output position via a compile-time analyzer (FT-code diagnostic). This pushes the error to build-time, per CONTRIBUTING.md's preference.

## Tasks

1. **`src/core/Flowthru.Core/Data/Configuration/ConfigurationItem.cs`** — New file. Item implementation as above.

2. **`src/core/Flowthru.Core/Data/Configuration/ConfigurationBuilder.cs`** — `.FromConfiguration().AtSection(path).Build()` builder pipeline analogous to the JSON builders.

3. **`src/core/Flowthru.Core/Hosting/FlowthruServiceBuilder.cs`** — Add `UseConfiguration(IConfiguration)` method. Registers the configuration as a DI singleton.

4. **`src/core/Flowthru.Core/Data/Catalog/IItem.cs`** — Add an optional `IReadOnlyItem<T>` marker interface (for outputs-rejected analyzer to recognize).

5. **`src/core/Flowthru.Core.SourceGenerators/`** — New `FT-code` diagnostic: a step declares a `IReadOnlyItem<T>` in its output list → compile error with a clear message.

6. **`examples/`** — A small example showing a step parameterized by a config section. The example demonstrates that changing the config value invalidates the cached output of the step (once Phase 6 ships).

7. **Tests:**
   - `ConfigurationItem<T>` round-trips a bound type through `Load()`.
   - `Save()` always fails with the expected diagnostic.
   - `TryGetFingerprint()` is stable across reads, sensitive to config changes.
   - Compile-time analyzer fires when a step lists a `ConfigurationItem` in its outputs.
   - Integration: a flow declares a config-driven step; the framework recognizes the config as an input.

## Public Surface Changes

Additive:
- `ConfigurationItem<T>` type.
- `Item.Of<T>("...").FromConfiguration().AtSection("...")` builder chain.
- `FlowthruServiceBuilder.UseConfiguration(IConfiguration)` method.
- `IReadOnlyItem<T>` marker interface.

No breaking changes. Existing flows that don't use config-as-catalog are unaffected.

## Phase Placement (per CONTRIBUTING.md)

- **Compile-time:** Analyzer rejects `ConfigurationItem` in step output positions. The bound type `T` must be reachable for `IConfiguration.Get<T>()` — analyzer can verify it has a parameterless constructor.
- **Pre-flight:** `Exists()` validates the section is present; `TryGetFingerprint()` computes the hash. If the section is missing, surfaces as `PreFlightError.MissingInput`.
- **Runtime:** `Load()` binds the section to the typed value via standard `IConfiguration` plumbing.

## Testing Strategy

- Unit tests in `tests/Flowthru.Core.Tests/Data/Configuration/`.
- Analyzer regression in `tests/Flowthru.Core.SourceGenerators.Tests/`.
- End-to-end example test under `tests/Examples/` running the worked example.

## Confirmation Criteria

- `nx run-many -t build` passes; analyzer fires the expected error when a step misuses a `ConfigurationItem`.
- `nx run affected -t test` passes.
- The worked example runs successfully with a JSON config, an env-var config, and a chained config. Changing a config value changes the fingerprint (verified once Phase 6 ships).
- A pre-existing flow can adopt config-as-catalog with no other changes to its step or catalog definitions.

## Risks

- **Secrets in config:** hashing a section containing secret values means the hash itself can leak through telemetry if logged. Mitigation: document the limitation; recommend wrapping secret-bearing sections in a `IReadOnlyItem<T>` that explicitly opts out of fingerprinting (returns `null` from `TryGetFingerprint`). Steps consuming such items remain uncacheable.
- **Config reload during a run:** unsupported in v1. Mitigation: `IConfiguration` is captured at FlowthruService construction; later host-level reloads don't propagate. Documented.
- **Schema drift between config sources** (JSON vs env vs CLI args): standard `IConfiguration` precedence applies; not Flowthru's problem to solve. Documented.

## Follow-ups

- Phase 6 uses config fingerprints in the cache plan composition. No separate work; this phase just makes config visible as a fingerprintable input.
- A "secrets-aware fingerprint" RFC could let users tag sections as "use a redacted hash" — defer until needed.
