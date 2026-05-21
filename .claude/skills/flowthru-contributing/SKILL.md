---
name: flowthru-contributing
description: Use when working on Flowthru's core library, extensions, or tests — anything under src/ or tests/. Carries agent-only conventions for issues, triage, domain language, external sources, and the test workflow.
---

# Flowthru Contributing

This skill is the agent-only entry point for any work on Flowthru. The fail-fast philosophy, the three error phases, and the Flow / Step / Catalog / Schema vocabulary all live in `/CONTRIBUTING.md`, which is loaded into every session automatically — there is no need to restate it here.

All agent infrastructure (this skill, the docs it points at, hooks, settings) lives under `.claude/` or `.github/`. Never `docs/`, never root-level `CLAUDE.md` / `AGENTS.md`.

## External sources

Flowthru connects to many other projects. When you need to introspect one directly, check `docs/reference/misc/external/*/repo`. If the `repo` subdirectory is missing, pull it with `nx run xdocs:pull <source>`.

## Tests

The Agent stop hooks (`scripts/agents/hooks/on-stop/`) run affected tests automatically — don't manually run them as a final confirmation. For extended debugging output, run `dotnet test` against a specific target.

## Issues

GitHub issues at [chaoticgoodcomputing/flowthru](https://github.com/chaoticgoodcomputing/flowthru/issues), via the `gh` CLI. Non-trivial in-session reports become filed issues before being acted on beyond the conversation. Conventions: [.claude/docs/issue-tracker.md](/.claude/docs/issue-tracker.md).

## Triage

Five canonical role labels (`needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix`), used verbatim. Mapping and `gh label create` commands: [.claude/docs/triage-labels.md](/.claude/docs/triage-labels.md).

## Domain output rules

Behavioral rules for using `/CONTRIBUTING.md`'s vocabulary in your output (issue titles, hypotheses, test names) and for flagging contradictions: [.claude/docs/domain.md](/.claude/docs/domain.md).
