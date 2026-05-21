# Issue tracker: GitHub

Issues and PRDs for Flowthru live as GitHub issues at [chaoticgoodcomputing/flowthru](https://github.com/chaoticgoodcomputing/flowthru/issues). Use the `gh` CLI for all operations — it infers the repo from `git remote -v` automatically when run inside the clone.

## Conventions

- **Create an issue**: `gh issue create --title "..." --body "..."`. Use a heredoc for multi-line bodies.
- **Read an issue**: `gh issue view <number> --comments`.
- **List issues**: `gh issue list --state open --json number,title,body,labels,comments --jq '[.[] | {number, title, body, labels: [.labels[].name], comments: [.comments[].body]}]'` with appropriate `--label` and `--state` filters.
- **Comment on an issue**: `gh issue comment <number> --body "..."`.
- **Apply / remove labels**: `gh issue edit <number> --add-label "..."` / `--remove-label "..."`.
- **Close**: `gh issue close <number> --comment "..."`.

## In-session work that should become an issue

It is common for work in Flowthru to begin as an in-session user report — a bug noticed mid-conversation, a refactor sketched out before being scoped, a PRD drafted in chat. Treat the in-session conversation as the *draft*; the GitHub issue is the *artifact*.

When work would benefit from being tracked beyond the current conversation:

1. Synthesise the relevant context (problem, reproduction, scope, decisions made) into a self-contained issue body — assume the reader has none of the conversation context.
2. File it with `gh issue create`. Apply `needs-triage` unless the issue is already fully scoped, in which case apply the appropriate ready-state label (see [triage-labels.md](./triage-labels.md)).
3. Reference the issue number in any follow-up work (commits, PRs, related issues).

Do not skip this step for non-trivial work — an unfiled bug or unscoped PRD is invisible to anyone outside the conversation.

## When a skill says "publish to the issue tracker"

Create a GitHub issue.

## When a skill says "fetch the relevant ticket"

Run `gh issue view <number> --comments`.
