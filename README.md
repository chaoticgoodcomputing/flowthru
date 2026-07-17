# Flowthru: We'll Never Fail Again

[![codecov](https://codecov.io/gh/chaoticgoodcomputing/flowthru/branch/main/graph/badge.svg)](https://codecov.io/gh/chaoticgoodcomputing/flowthru)

Flowthru is a data pipeline framework for .NET that promises a stable, fault-free data science and engineering process. The premise is simple:

**A good pipeline will always finish. A broken pipeline will break fast.**

## Agent skills

Flowthru ships an [Agent Skill](https://agentskills.io) so your coding agent knows how to author Flows, Catalogs, schemas, and steps. Install it with the [`skills` CLI](https://skills.sh) — it works with Claude Code, Cursor, Codex, and [many more](https://skills.sh):

```bash
# The umbrella skill — the model, the project structure, and an index of every extension
npx skills add chaoticgoodcomputing/flowthru --skill flowthru
```

The umbrella carries an index of Flowthru's extensions and the command to pull a **deep skill** for each. Add the ones matching your stack:

```bash
# e.g. a project using Python, Parquet, and EF Core
npx skills add chaoticgoodcomputing/flowthru \
  --skill flowthru flowthru-python flowthru-parquet flowthru-efcore
```

The skills live in this repo and ship inside every tagged release, so they carry the **same version code** as the published packages. Pin them to a release the same way you'd pin a package:

```bash
npx skills add chaoticgoodcomputing/flowthru@v0.28.1 --skill flowthru
```

New projects created from a Flowthru `dotnet new` template already bundle the umbrella skill under `.claude/skills/flowthru/` — no extra step.

<!-- TODO -->
