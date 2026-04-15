# Testing Philosophy

Recall from the [Contributor's Guide](/CONTRIBUTING.md) that Flowthru has two core promises to end-users:

> 1. End-users can easily write data pipelines, and have a development experience focused on what *their* pipelines will do, not how Flowthru is handling the pipeline.
> 2. If an error can occur in the pipeline they've created, it will occur as soon in the development process as possible.

The tests projects are meant to enforce how we fulfill these promises, and cover both the API surface *and* the error surface of Flowthru and its extensions.

## API Surface Tests (How and when should Flowthru work?)

These tests verify that Flowthru works correctly when used as intended. They validate the first promise: that developers can write pipelines easily, without unnecessary ceremony or boilerplate, and expect it to Just Work™.

### Unit Tests

Unit tests validate individual components in isolation:

- **Execution tests:** Verify that nodes execute correctly, that catalog entries load and save data, and that pipelines orchestrate node execution properly.
- **Service tests:** Verify dependency injection, configuration loading, and the service layer that hosts pipelines.

These tests use in-memory storage and minimal fixtures to exercise specific code paths without external dependencies.

### Integration Tests (`Flowthru.Tests.Examples`)

**Every example project in `/examples` is executed as a test.** This serves two purposes:

1. **Examples are documentation.** They demonstrate real-world usage patterns for end-users.
2. **Examples are executable contracts.** If an example breaks, either the API surface changed (a breaking change that needs documentation) or Flowthru has a regression.

The integration test suite:
- Discovers all example projects via reflection
- Invokes their `Program` entry point through the service layer
- Verifies they complete successfully
- Provides code coverage through actual pipeline execution

This approach ensures that the code we show to users actually works, and that refactoring Flowthru's internals doesn't silently break usage patterns we've documented.

## Error Surface Tests (How and when should Flowthru fail?)

These tests validate the second promise: that errors surface as early in the development process as possible. They're organized by the Flowthru philosophy's documented three stages of errors:

> 1. Build-time (beautiful, gold standard, chef's kiss)
> 2. Pre-flight (tolerable, but aggravating)
> 3. Runtime (evil! should be destroyed wherever possible)

When developing features and fixes, we should **always** be considering not just *how* Flowthru can fail, but *when*.

### Compilation Tests

Verify that the type system and source generators catch configuration errors at build time:

- Schemas with mismatched types between nodes and catalog entries don't compile
- Incompatible schema/serializer combinations (e.g., nested schemas with CSV) produce build errors
- Source generator diagnostics (missing `partial`, conflicting interfaces) are emitted correctly

These tests often use Roslyn analyzers or verify that certain code patterns produce expected compiler errors. When writing these tests, consider: could this constraint be expressed as a generic constraint? Could a source generator emit a diagnostic?

### Pre-Flight Tests

Verify that environmental and structural errors are caught before any node executes:

- Duplicate producers (two nodes writing to the same entry) are rejected during DAG construction
- Circular dependencies are detected
- Missing external inputs are caught during validation
- Schema drift in external files is detected before execution

**If a pre-flight check passes, the pipeline must complete successfully.** If it doesn't, either the check is incomplete or a compile-time constraint is missing. When a pre-flight test fails, ask: is this truly an environmental concern, or could it have been a type-level constraint?

### Runtime Tests

Runtime errors are like suspicious moles: they're horrible, dangerous, and should be documented & tracked. Runtime error-surface tests are how we accomplish this documentation and tracking. This category of tests serves two purposes:

1. Replicate user reports of runtime errors
2. Act as a staging ground for confirmed runtime errors to either be:
  1. Fixed, and moved to a unit/integration test; or
  2. Be moved up to the build or pre-flight error surface

**Tests should not remain here for very long!** Once a user report of a runtime error has been cataloged here, it should be moved to the appropriate location — fixed if possible, or moved up to a pre-flight or build error.

### Evaluating Error Tests

When you encounter a runtime or pre-flight error during development:

1. **Can the C# type system express this constraint?** → Research generic constraints, source generators, or Roslyn analyzers that could move it to compile-time.
2. **Is it truly environmental?** → If external state (files, network, databases) is the only variable, pre-flight is appropriate.
3. **Is it truly unpredictable?** → Network drops, OOM, and hardware failures belong at runtime. Most logic errors do not.

Error surface tests are an ongoing audit. As C# and Roslyn evolve, revisit runtime and pre-flight tests to see if they can migrate earlier.

## Running Tests

### Basic Test Execution

Run all tests across the solution:
```bash
nx run flowthru:test
```

Run specific test categories (unit tests only):
```bash
nx run test/unit:compilation   # Build-time error tests
nx run test/unit:validation     # Pre-flight error tests
nx run test/unit:execution      # Runtime execution tests
```

Run integration tests (all examples):
```bash
nx run test/examples:test
```

### Coverage Collection

Tests are run with coverage collection enabled in CI via `coverlet.runsettings`. Coverage reports are aggregated and tracked by [Codecov](https://codecov.io/gh/chaoticgoodcomputing/flowthru), with per-flag carryforward so partial `nx affected` runs don't erase unaffected projects.

To force a clean test run by removing previous `TestResults` artifacts:
```bash
nx run tests:purge
```

### What Gets Measured

Code coverage is collected for all `Flowthru*` assemblies:
- `Flowthru` (core framework)
- `Flowthru.Integrations.*` (integration libraries)
- `Flowthru.Extensions.*` (extension libraries)

The following are excluded from coverage:
- Test assemblies (`*.Tests`, `*.Tests.*`)
- Generated code (source generators, designers)
- Third-party libraries (xunit, NUnit, Microsoft, System)

Coverage configuration is defined in `coverlet.runsettings` at the repository root.
