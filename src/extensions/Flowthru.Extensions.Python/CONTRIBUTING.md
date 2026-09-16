# Contributing to Flowthru.Extensions.Python

This document is for developers working on **`Flowthru.Extensions.Python`** — the extension that lets a Flow Developer write a Step's transform in Python and have it scheduled, cached, and validated like any other Step.

**Audience scope:** assumes familiarity with [examples/CONTRIBUTING.md](/examples/CONTRIBUTING.md) (Flow / Catalog Developer vocabulary) and [src/extensions/CONTRIBUTING.md](/src/extensions/CONTRIBUTING.md) (the Extension surface and the Extension Developer role). Terms defined here are the *additional* vocabulary specific to the Python extension.

See [/CONTRIBUTING.md](/CONTRIBUTING.md) for the cross-cutting design rules every context shares.

## Why this package is its own context

A directory is a context iff it directly contains a `CONTRIBUTING.md`, and contexts are minted lazily — when a package accumulates vocabulary or decisions that have nowhere else to live. Python is the first extension to cross that line, for two reasons:

1. **It is the only extension that runs code Flowthru does not compile.** Everything else in `src/extensions/` closes a slice of the Extension surface in C#. Python ships a worker process, a wire protocol, a dependency algebra, and a launcher abstraction — a stack with its own failure modes and its own words for them.
2. **It owns decisions no other context can own.** The requirements algebra and the launcher abstraction are Python-specific trade-offs; they are recorded in this package's own [`docs/adr/`](/src/extensions/Flowthru.Extensions.Python/docs/adr) rather than cluttering the repo-wide directory.

## Honoring Fail-Fast across the language boundary

The language boundary is where Flowthru's promise is hardest to keep, because Python's errors are runtime errors by construction. The obligation is to pull as much as possible back across the boundary into an earlier phase:

- **Design-time** — the source generator reads `[PythonPackageRequirement]` attributes and the step's declared capabilities, so a mis-declared requirement is a Roslyn diagnostic, not an import error twenty minutes into a run.
- **Pre-flight** — the resolved requirement set is checked against what is actually installed in the target environment *before* any step executes. A missing package is a pre-flight failure with a name, not a `ModuleNotFoundError` in a worker log.
- **Runtime** — what genuinely cannot be known earlier: the transform's own logic, and the data it meets.

A change that moves a check *later* across this boundary needs a reason recorded in an ADR.

## Glossary

**Python step**: A Step whose transform executes in the Python worker rather than the CLR. It is an ordinary node in the DAG — scheduled by precedence, subject to [[Conflict]], and cacheable — so nothing downstream needs to know the transform was not C#.
_Avoid_: "Python task" (Flowthru's unit is a Step), "PySpark step" (unrelated engine).

**Python worker**: The subprocess that hosts the Python interpreter and executes Python steps, driven over a request/response pipe by `IPythonExecutor`. Registered as a singleton with a declared concurrency capacity of 1, which is why a Python-heavy Flow gains nothing from raising `ExecutionOptions.Parallelism` without the conflict relation accounting for it.
_Avoid_: "Python server" (it is a child process with a private pipe, not a network service), "interpreter" (the worker is the process *around* the interpreter).

**Requirements algebra**: The rules for combining the Python package requirements declared across a Flow into one resolvable environment — how per-step `[PythonPackageRequirement]` declarations, framework-level requirements, and a lockfile compose, and what constitutes a conflict. Named an algebra because composition is the operation that must be total and order-independent: any two requirement sets combine to a third, or fail with a named conflict.
_Avoid_: "dependency resolution" (that is the resolver's job downstream of the algebra), "requirements merge" (understates that conflicts are a defined outcome, not an error case).

**Launcher**: The strategy that decides *how* a Python step's process is started — in-process worker, `torchrun` for distributed training, `accelerate` for multi-GPU. One class per launcher rather than one generic launcher with flags, because each carries a genuinely different process topology and argument surface.
_Avoid_: "runner" (overloaded with test runners), "executor" (that is `IPythonExecutor`, which is the transport, not the topology).

**Code version**: The fingerprint of a Python step's transform — the `.py` source plus the interpreter and lockfile identity — that stands in for determinism when the cache planner decides whether the step is cacheable. It is what makes the Python worker *cache-neutral* despite being concurrency-constrained: its determinism is captured here, not inferred from its service lifetime.
_Avoid_: "hash" (understates what is included), "source version" (the interpreter and lockfile are part of it).
