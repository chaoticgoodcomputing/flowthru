---
description: Guidelines for routing Flowthru sessions based on development focus.
applyTo: "**"
---

# Flowthru Session Routing

Flowthru is a type-safe data engineering framework for .NET. Before starting work, determine which type of session this is:

<pipeline_development>

## Flow Development

Working in `examples/` or building flows as a downstream user?

Review `/docs/explanation/anatomy-of-a-flow.md` for project structure, schemas, catalogs, steps, and configuration patterns.

When creating or modifying flows:
- Follow the layered data directory convention (`_01_Raw`, `_02_Intermediate`, etc.)
- Use catalog items as typed properties, not string keys
- Wire steps to catalog items with matching schema types

</pipeline_development>

<library_development>

## Library Development

Working on `src/`, `tests/`, or Flowthru extensions?

Review `/CONTRIBUTING.md` for the fail-fast architecture, the three error phases (compile → pre-flight → runtime), and decision rules for where validations belong.

Review `/docs/CONTRIBUTING.md` to ensure documentation remains synchronized with code changes.

Review `/docs/` to understand the API surface of the application.

Any docs in the `advanced/` subdirectories are contributor-focused. They explain internal architecture and design decisions.

When adding or modifying Flowthru internals:
- Push errors to compile-time via type constraints and source generators when possible
- Add pre-flight validation for environmental concerns (files, connections, external schemas)
- Reserve runtime error handling for truly unpredictable failures
- Ask not just "Will this work?" but "When will it break?"

</library_development>
