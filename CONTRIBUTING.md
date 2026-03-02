# Contributing to Flowthru

Flowthru is a type-safe, no-bullshit data engineering framework for .NET. Its design philosophy can be summarized in one sentence: **developing a data pipeline should be easy, and a broken pipeline should fail fast.**

First of all — thank you for contributing! I appreciate you taking the time to help make Flowthru better. This document explains the theory behind that philosophy, and helps ensure new features and fixes are aligned with Flowthru's theories and end-user promises.

## Why Fail-Fast Matters

If you've worked with runtime-only pipeline frameworks, these scenarios will be familiar:

**The silent schema break.** An upstream team renames a column in a source table from `customer_id` to `cust_id`. Your pipeline launches, spends two hours processing raw data through three stages, then fails at a join node that expected the old name. In the worst case, the compute is wasted, and the error message points at a symptom (`KeyError: 'customer_id'`) rather than the cause (a contract violation between the producer and consumer).

**The rogue edit.** Somebody on the team has made a typo in one of the later steps of the pipeline. It happens! However, a hours-long pipeline, that never would have finished, fails at the last step. Somebody must find, and fix, the typo before the pipeline can finish, delaying the output and wasting the lead-up computation necessary to reach that point in the node again.

**The silent overwrite.** Two pipeline branches independently write to the same output table. There's no build-time or pre-flight check for duplicate producers — whichever branch finishes last wins, and the other branch's output is silently lost. This race condition makes the data unpredictable.

Each of these failures shares a root cause: **your framework didn't fail fast enough.**

## Maintaining Flowthru's Core Promise

Flowthru's promises are simple:

1. End-users can easily write data pipelines, and have a development experience focused on what *their* pipelines will do, not how Flowthru is handling the pipeline.
2. If an error can occur in the pipeline they've created, it will occur as soon in the development process as possible.

As developers of Flowthru, then, these are split into two primary concerns:

1. The **API Surface:** how users experience, and work with, Flowthru; and
2. The **Error Surface:** how — and **when** — errors in Flowthru pipelines can occur.

Flowthru's architecture is designed to balance these requirements: a straightforward API surface free of unnecessary ceremony or boilerplate, and an error surface that pushes errors as early in the development process as C#, .NET, and Roslyn can offer.

The end-user experience should feel almost identical to similar pipeline frameworks, but with **free** gains in stability and developer experience.

## The Three Error Phases

Every possible failure in a Flowthru pipeline falls into one of three phases:

1. Build-time (beautiful, gold standard, chef's kiss)
2. Pre-flight (tolerable, but aggravating)
3. Runtime (evil! should be destroyed wherever possible)

### 1. Build / Compile-Time

Compile-time errors always show when the pipeline is built — and, if the user is using a C# language server, as squigglies in the IDE during development. This is always the goal — that an error will be shown at the best time for a developer to fix it: **during development.**

Flowthru achieves this through a combination of mechanisms that share a common pattern: **pipeline structure is expressed in the type system, not in strings or configuration.**

- **Schemas are typed contracts.** Every node declares the schema(s) it consumes and produces via generic type parameters. Every Data Catalog entry uses one of these same schemas. If a node tries to input from, or output to, a data catalog entry with mismatched schemas, the pipeline won't build.
- **Each node is a contract that cannot be broken.** A developer must define schemas up-front when they're writing their node. If they're supposed to write a node that inputs schema A, and outputs schema B, the compiler **requires** that the code they write follows through on this contract to go from A to B.
- **Schemas can only be stored in data formats that can support them.** The `[FlowthruSchema]` source generator analyzes schema structure and emits marker interfaces that gate which serializers a schema can be used with. Attempting to save nested format to a CSV? That shouldn't even build.
- **Wiring is done with types, not strings** Anytime you need to hook two things together — data to nodes, nodes to pipelines, pipelines to other pipelines — it should be done using types, **never** strings.

### 2. Pre-Flight Checks

While build-time errors are the gold standard, there are some cases where a problem simply cannot be caught that early. To cope with this, a **pre-flight phase** happens after the pipeline is invoked, but before **any** pipeline logic runs.

- **DAG validation.** Before execution, the pipeline's dependency graph is analyzed. Duplicate producers (two nodes writing to the same entry) as well as circular dependencies, are rejected.
- **External input inspection.** External inputs (files, connections) are inspected before the first node runs. Missing files, mismatched headers, and schema drift in external data are all surfaced up front. Even if an external data file is only used at the end of the pipeline, it should be confirmed accessible before the pipeline starts.
- **Dry-run mode.** All pre-flight checks can be executed with zero side effects, validating that the pipeline *would* succeed without actually running it.

**Design invariant:** A pipeline that passes pre-flight checks should always complete successfully. If it doesn't, that's a bug in Flowthru — either a missing pre-flight check or a missing compile-time constraint.

### 3. Runtime

Runtime errors include **anything** that could go wrong during actual node execution. These might include:

- network drops
- out-of-memory conditions
- general acts of God

Flowthru handles these through an **effect type** called `FlowIO<T>`. If you're familiar with Spark's lazy evaluation model — where transformations build up a plan and only an *action* triggers execution — `FlowIO<T>` applies a similar principle to I/O operations. Loading and saving data returns a `FlowIO<T>` rather than performing the operation immediately. Side effects must be deliberately triggered, not accidentally dropped. This makes I/O boundaries explicit and ensures that errors at those boundaries are captured in structured results rather than thrown as unhandled exceptions.

The key runtime guarantees:

- **All I/O is lazy and explicit.** Side effects cannot be accidentally dropped or silently ignored.
- **Errors are captured, never swallowed.** Node failures propagate to structured pipeline results. Silent `catch {}` blocks are a bug.
- **Nodes are isolated.** A failing node halts execution and reports which node failed and why — partial silent failures are not possible.

## Decision Rules for Contributors

When adding a new feature or fixing a bug, use these rules to determine where validation belongs:

1. **Can the C# type system express this constraint?** → Add it as a generic constraint, source generator diagnostic, or interface requirement. The compiler is the first line of defense.

2. **Is it an environmental concern (files, connections, external schemas)?** → Add it to the pre-flight validation layer. It must run before any node executes.

3. **Is it truly unpredictable (network failure, machine error, act of God)?** → Handle it in the runtime layer via `FlowIO` effects. Ensure the error is captured in structured results, not swallowed.

The known error points of Flowthru — its **error surface** — should be documented and tested. Flowthru's promise to fail fast **is a feature**. When working on Flowthru, or any extension, it is important to ask yourself not just "Will this work?" but "**When** will it break?"

When adding features or extensions, read [the testing philosophy](/tests/README.md) to understand how best to test.

## What Flowthru *Won't* Be

Flowthru, at its core, will *not* be a full piece of orchestration software. The core library will not be concerned with when or how users want to run their pipeline — just that it will be correctly configured, and as stable as possible, when they do.

This doesn't mean *ignoring* these concerns — it just means extending the API surface to allow end-users to run pipelines flexibly (such as the service-based and CLI access options), as well as ensuring the core engine uses extensible patterns for modification (such as additional formats and methods for data access, and the ability to DI services into nodes for additional utility).

## Development Workflow

### Building and Testing

The project uses NX for task orchestration. Common commands:

```bash
nx run ft:build                   # Build the solution
nx run ft:test                    # Run all tests with coverage
nx run ft:format:csharp           # Format code with CSharpier
```

To run a subset of tests by category:

```bash
dotnet test --filter "Category=Compilation"
```

### Running Example Pipelines

From an example project directory:

```bash
cd examples/starter/KedroIris
dotnet run -- DataEngineering     # Run a specific pipeline
dotnet run                        # Run all registered pipelines
```

### Code Style

- Format C# code with CSharpier before committing
- Follow existing patterns in the codebase for new features
- Add XML documentation comments to public APIs
