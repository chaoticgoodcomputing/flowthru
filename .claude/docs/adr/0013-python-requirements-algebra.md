# Python requirements algebra

The Python extension's framework-level Python dependencies (pyarrow for IPC, the `flowthru` Python companion package, launcher-specific packages like `accelerate`) are declared as data by the capabilities that need them, folded into a single closure at flow-construction time, and enforced at the earliest possible phase. The launcher seam in [ADR-0014](0014-python-launcher-and-distributed-training.md) is the first non-base consumer; the algebra exists independently so future capabilities (new marshallers, service inspectors with library requirements) compose into the same enforcement pipeline rather than each inventing its own dep-check story.

## What declares requirements

Any Python-extension capability that needs Python-side packages exposes them as `IReadOnlyList<PythonPackageRequirement>`. Three concrete declarers in v1:

- `PythonStepExtension` itself — `pyarrow` (Arrow IPC marshaller floor), `flowthru` (the Python-side companion package, version-pinned to the .NET package version).
- `IPythonLauncher` implementations — `AccelerateLauncher` declares `accelerate>=0.30`, `DeepspeedLauncher` declares `deepspeed`, `DirectPythonLauncher` declares nothing.
- `PythonServiceRegistration` — service inspectors that depend on a specific Python library declare it on registration.

Marshallers added later (e.g., a Parquet streaming marshaller) declare their own requirements through the same shape. The algebra is over the union of all capability-declared requirements present in the live DI container.

## The algebra

`PythonPackageRequirement` is `(Package, VersionConstraint, Reason)`. Folding the closure has two operations:

1. **Constraint intersection.** Two declarers asking for the same package collapse to the tighter constraint — `pyarrow>=14` ∩ `pyarrow>=15` → `pyarrow>=15`. Constraints follow PEP 440; `NuGet.Versioning` does not map cleanly (PEP 440 prereleases like `1.0a1` parse differently), so the algebra ships a small dedicated parser rather than coercing through a NuGet representation.
2. **Conflict detection.** Unsatisfiable intersections fail with both contributing capabilities named — *"AccelerateLauncher requires `pyarrow<16` but PythonStepExtension requires `pyarrow>=17`; these capabilities cannot coexist in the same configuration."* This is the killer feature: today, version-conflicting deps surface as cryptic `ImportError` at minute 47 of a flow.

## Two-layer enforcement

Same algebra, two backstops, errors caught at whichever phase has enough information:

- **Design-time (`FTPY1501`–`FTPY15xx`).** Roslyn analyzer walks the syntax tree for `UsePython(...)` calls, registered launchers, and registered service inspectors. Reads `uv.lock` (resolved from the project's output directory or `pyproject.toml`'s parent). Computes the algebra statically. Fails the build with a paste-ready `uv add <packages>` command in the diagnostic message. Ships with a companion code fix per Core's rule. Cannot see DI-injected capabilities the analyzer can't resolve statically — pre-flight catches those.
- **Pre-flight (`PythonRequirementsValidationHook : IFlowValidationHook`).** Collects all `IPythonCapability` instances from the live DI container. One `python -m pip list --format=json` subprocess (same short-lived pattern as the existing `--version` probe in `SubprocessPythonExecutor`). Computes the algebra against the actual venv. Accumulates errors via `Validated<PythonPreFlightError, FlowUnit>` so the user sees the full set, not the first failure.

## Non-goals

- **No `pyproject.toml` mutation.** The algebra reports what's needed; it never writes to the user's manifest. The user runs `uv add` themselves so the change is reviewed, committed, and pinned.
- **No auto-install at any phase.** Build-time `uv add`, run-time `pip install`-on-import, bundled side-venv overlays are all rejected — each breaks reproducibility (`uv.lock` drift) or the offline-build promise or Flowthru's "not an orchestrator" stance. The framework's job is to make the missing dep impossible to miss, not to silently fix it.
- **No transitive matrix maintenance.** Capabilities declare their direct package requirement; resolving the transitive closure is `uv`'s job, not Flowthru's.

## Why this matters for the fail-fast pitch

A three-hour fine-tune that crashes at minute 130 because `accelerate` is missing is the exact failure mode Flowthru exists to prevent. The algebra moves "missing Python dep" from runtime to design-time (when `uv.lock` is reachable) or pre-flight (when it isn't) — never runtime. Without it, every distributed-training launcher we ship is a runtime-error class we added to the framework and called an improvement.

This mirrors the existing capability algebra over container kinds (`[StepExtensionCapabilities]` + `IContainerMarshaller<TExtension>` + `FT1301`/`FT1303`). Same shape — capabilities declare promises, the framework computes the closure, analyzers and pre-flight hooks enforce — applied to Python-side dependencies instead of container shapes.

## Governed code

### Requirement declaration and algebra

- `src/extensions/Flowthru.Extensions.Python/Step/Python/PythonPackageRequirement.cs` — the `(Package, VersionConstraint, Reason)` record
- `src/extensions/Flowthru.Extensions.Python/Step/Python/PythonPackageRequirementAttribute.cs` — declarative attribute form for static analysis
- `src/extensions/Flowthru.Extensions.Python/Step/Python/IPythonCapability.cs` — marker interface; capabilities expose `Requirements` for folding
- `src/extensions/Flowthru.Extensions.Python/Step/Python/IPythonLauncher.cs` — launcher contract; `Requirements` and `Probe` participate in the algebra
- `src/extensions/Flowthru.Extensions.Python/Step/Python/Internal/PythonRequirementsAlgebra.cs` — fold/intersection core
- `src/extensions/Flowthru.Extensions.Python/Step/Python/Internal/PythonVersion.cs` — PEP 440 version parser
- `src/extensions/Flowthru.Extensions.Python/Step/Python/Internal/BasePythonExtensionCapability.cs` — floor capability (pyarrow, flowthru)

### Enforcement

- `src/extensions/Flowthru.Extensions.Python/Step/Python/Internal/InstalledPackageProbe.cs` — `pip list` subprocess probe feeding pre-flight
- `src/extensions/Flowthru.Extensions.Python/Validation/PreFlight/Python/PythonRequirementsValidationHook.cs` — pre-flight hook (FTPY3011/FTPY3012)
- `src/extensions/Flowthru.Extensions.Python/Validation/PreFlight/Python/PythonPreFlightError.cs` — `MissingRequirement` and `VersionConstraintNotSatisfied` error variants
- `src/extensions/Flowthru.Extensions.Python.SourceGenerators/PythonRequirementsAnalyzer.cs` — design-time analyzer (FTPY1501/FTPY1502)

### Registration

- `src/extensions/Flowthru.Extensions.Python/Hosting/PythonFlowthruBuilderExtensions.cs` — `UsePython()` wires capabilities, probe, and validation hooks

### Tests

- `tests/extensions/Flowthru.Extensions.Python.Tests/PythonRequirementsAlgebraTests.cs` — algebra unit tests
- `tests/extensions/Flowthru.Extensions.Python.Tests/PythonRequirementsValidationHookTests.cs` — pre-flight hook tests
- `tests/extensions/Flowthru.Extensions.Python.SourceGenerators.Tests/PythonRequirementsAnalyzerTests.cs` — design-time analyzer tests
