# Contributing to Flowthru

Flowthru is a type-safe, no-bullshit data engineering framework for .NET. Its design philosophy can be summarized in one sentence: **developing an ETL workflow should be easy, and a broken workflow should fail fast.**

First of all — thank you for contributing! I appreciate you taking the time to help make Flowthru better. This document explains the theory behind that philosophy, and helps ensure new features and fixes are aligned with Flowthru's theories and end-user promises.

## Why Fail-Fast Matters

If you've worked with runtime-only ETL frameworks, these scenarios will be familiar:

**The silent schema break.** An upstream team renames a column in a source table from `customer_id` to `cust_id`. Your workflow launches, spends two hours processing raw data through three stages, then fails at a join that expected the old name. In the worst case, the compute is wasted, and the error message points at a symptom (`KeyError: 'customer_id'`) rather than the cause (a contract violation between the producer and consumer).

**The rogue edit.** You're using an interpreted language, and somebody on the team has made a typo in one of the later steps of the workflow. It happens! You start the workflow — a workflow that never *could* have finished — and it fails at the finish line. Somebody must find, and fix, the typo before the workflow can finish, delaying the output and wasting the lead-up computation necessary to reach that point in the step again.

**The silent overwrite.** Two workflow branches independently write to the same output table. Your data isn't part of the DAG — it's just a side effect. There's nothing to check for duplicate producers — whichever branch finishes last wins, and the other branch's output is silently lost. This race condition makes the data unpredictable.

Each of these failures shares a root cause: **your language and framework can't find errors until it *hits* errors**

## Maintaining Flowthru's Core Promise

Flowthru's promises are simple:

1. End-users can easily write ETL workflows, and have a development experience focused on what *their* Flows will do, not how Flowthru is handling the Flow.
2. If an error can occur in the Flow they've created, it will occur as soon in the development process as possible.

Flowthru's architecture is designed to balance these requirements: a straightforward API surface free of unnecessary ceremony or boilerplate, and an error surface that pushes errors as early in the development process as C#, .NET, and Roslyn can offer.

## The Three Error Phases

Every possible failure in a Flow falls into one of three phases:

1. **Design-time**: While projects are being developed (beautiful, gold standard, chef's kiss)
2. **Pre-flight**: After projects are built and run, but before any logic begins executing (tolerable, but aggravating)
3. **Runtime** (evil! should be destroyed wherever possible)

## Flowthru Development Roles

Contributions to Flowthru fall under one of four roles. Each role's full definition, conventions, and vocabulary live in a per-context CONTRIBUTING file:

- **Flow Developer** / **Catalog Developer** — writing Flows and Catalogs on top of Flowthru. See [examples/CONTRIBUTING.md](/examples/CONTRIBUTING.md).
- **Extension Developer** — extending Flowthru with new Catalog formats, Step types, or type-safety patterns. See [src/extensions/CONTRIBUTING.md](/src/extensions/CONTRIBUTING.md).
- **Core Developer** — curating Flowthru's core library and Roslyn surface. See [src/core/CONTRIBUTING.md](/src/core/CONTRIBUTING.md).

Testing-specific conventions for each context live in [tests/core/CONTRIBUTING.md](/tests/core/CONTRIBUTING.md) and [tests/extensions/CONTRIBUTING.md](/tests/extensions/CONTRIBUTING.md).

The design rules in this document apply to all four roles regardless of which context they're working in.

## What Flowthru *Won't* Be

Flowthru, at its core, will *not* be a full piece of orchestration software. The core library will not be concerned with when or how users want to run their Flows — just that it will be correctly configured, and as stable as possible, when they do.

This doesn't mean *ignoring* these concerns — it just means extending the API surface to allow end-users to run Flows flexibly (such as the service-based and CLI access options), as well as ensuring the core engine uses extensible patterns for modification (such as additional formats and methods for data access, and the ability to DI services into Steps for additional utility).

## Development Workflow

### Building and Testing

The project uses NX for task orchestration. When possible, use `nx run` targets over `dotnet` directives, as the `nx` targets may include prerequisites to target runs.

```bash
nx run-many -t build # Confirm solution builds fully
nx run affected -t test # IMPORTANT: Run all test projects affected by current changes
nx run Spaceflights # Run a specific Flowthru example Flow
```

`dotnet` can be used to run subsets of tests, or specific tests:

```bash
dotnet test --filter "Category=Compilation"
```
