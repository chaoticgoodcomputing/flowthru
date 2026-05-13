---
name: flowthru-contributing
description: Use when working on Flowthru's core library, extensions, or tests — anything under src/ or tests/. Covers the fail-fast architecture, the three error phases (compile-time, pre-flight, runtime), and the decision rule for where validation logic belongs.
---

# Flowthru Contributing

Before writing or modifying code, read these:

- [/CONTRIBUTING.md](/CONTRIBUTING.md) — design philosophy, the three error phases, and decision rules for where validations belong.
- [/docs/CONTRIBUTING.md](/docs/CONTRIBUTING.md) — documentation tone and the Diátaxis framework. Keep documentation synchronized with code changes.

When adding or modifying Flowthru internals:

- Push errors to compile-time via type constraints and source generators when possible.
- Add pre-flight validation for environmental concerns (files, connections, external schemas).
- Reserve runtime error handling for truly unpredictable failures.
- Ask not just "Will this work?" but "When will it break?"

Flowthru also depends on, and connects to, many other projects. Projects that may be helpful to directly introspect can be found in `docs/reference/misc/external/*/repo` subdirectories. If a source's `repo` subdirectory is missing, it can be pulled by running `nx run xdocs:pull <source>`.

When developing, do not manually run tests as part of a final confirmation — this will be handled automatically by the Agent stop hook from `.claude/settings.json`. However, tests can be run selectively via `dotnet test` if extended output is required for debugging sessions.
