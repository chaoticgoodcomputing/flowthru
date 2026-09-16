# Issue tracker: GitHub

Issues and PRDs for Flowthru live as GitHub issues at [chaoticgoodcomputing/flowthru](https://github.com/chaoticgoodcomputing/flowthru/issues).

## Use the GitHub MCP server

**The GitHub MCP server is the primary interface for all issue-tracker operations.** Two servers are configured at user scope, both running `ghcr.io/github/github-mcp-server`:

| Server            | Token                       | Use for                                    |
| ----------------- | --------------------------- | ------------------------------------------ |
| `github-cgc`      | Github CGC Access Token     | `chaoticgoodcomputing/*` — **this repo**   |
| `github-personal` | Github Personal Access Token | Spelkington's personal repos              |

Use `github-cgc` for Flowthru. Its tools are namespaced `mcp__github-cgc__*` and are deferred — load their schemas with `ToolSearch` before calling them:

```
ToolSearch("select:mcp__github-cgc__issue_read,mcp__github-cgc__issue_write,mcp__github-cgc__add_issue_comment,mcp__github-cgc__list_issues")
```

Note the read/write operations are **consolidated** behind `issue_read` / `issue_write` with a `method` parameter — there is no `get_issue` or `create_issue`. Every call takes `owner: chaoticgoodcomputing`, `repo: flowthru`.

| Operation                | Call                                                          |
| ------------------------ | ------------------------------------------------------------- |
| Read an issue            | `issue_read` `method: "get"`                                   |
| Read its comments        | `issue_read` `method: "get_comments"`                          |
| Read its labels          | `issue_read` `method: "get_labels"`                            |
| List / filter issues     | `list_issues` (takes `labels`, `state: OPEN\|CLOSED`, `orderBy`) |
| Create an issue          | `issue_write` `method: "create"`                               |
| Apply labels / close     | `issue_write` `method: "update"` (`labels`, `state`, `state_reason`) |
| Comment on an issue      | `add_issue_comment`                                            |

`issue_write` `method: "update"` **replaces** the whole `labels` array rather than adding to it — read the current labels first and send the full intended set, or you will silently drop labels. When closing, always set `state_reason` (`completed` / `not_planned` / `duplicate`).

## Do not fall back to `gh` yourself

The `gh` CLI is wrapped by the 1Password shell plugin. In a non-interactive agent shell, `op plugin run -- gh` raises an authorization prompt the agent cannot answer, and the call fails with `authorization prompt dismissed, please try again`. The same applies to the MCP servers themselves — they read their tokens via `op read`, so a dismissed or timed-out 1Password prompt surfaces as an MCP `CONNECT_TIMEOUT`.

**When the MCP server is unavailable, do not retry `gh` in a Bash tool call.** Instead, hand the commands to the maintainer to run themselves:

1. State plainly that the GitHub MCP server failed to connect, and why it matters for the task at hand.
2. Give the **exact, complete** `gh` command(s) needed — copy-pasteable, one per line, with all flags and heredocs filled in. No placeholders the maintainer has to substitute.
3. Suggest they run them with the `!` prefix (`! gh issue view 153 --comments`) so the output lands back in the conversation.
4. Wait for the output before continuing. Do not guess at issue contents.

Reference forms for those handoff commands:

- **Read an issue**: `gh issue view <number> --comments`
- **List issues**: `gh issue list --state open --label needs-triage --json number,title,labels,createdAt`
- **Create an issue**: `gh issue create --title "..." --body "..."` (heredoc for multi-line bodies)
- **Comment**: `gh issue comment <number> --body "..."`
- **Labels**: `gh issue edit <number> --add-label "..." --remove-label "..."`
- **Close**: `gh issue close <number> --comment "..."`

If the maintainer would rather fix the connection than run commands by hand, the usual cause is 1Password: unlock the `my.1password.com` account and re-approve the CLI integration, then restart the session so the MCP servers reconnect.

## In-session work that should become an issue

It is common for work in Flowthru to begin as an in-session user report — a bug noticed mid-conversation, a refactor sketched out before being scoped, a PRD drafted in chat. Treat the in-session conversation as the *draft*; the GitHub issue is the *artifact*.

When work would benefit from being tracked beyond the current conversation:

1. Synthesise the relevant context (problem, reproduction, scope, decisions made) into a self-contained issue body — assume the reader has none of the conversation context.
2. File it with `issue_write` `method: "create"`. Apply `needs-triage` unless the issue is already fully scoped, in which case apply the appropriate ready-state label (see [triage-labels.md](./triage-labels.md)).
3. Reference the issue number in any follow-up work (commits, PRs, related issues).

Do not skip this step for non-trivial work — an unfiled bug or unscoped PRD is invisible to anyone outside the conversation.

## When a skill says "publish to the issue tracker"

Create a GitHub issue with `mcp__github-cgc__issue_write`, `method: "create"`.

## When a skill says "fetch the relevant ticket"

Call `mcp__github-cgc__issue_read` with `method: "get"`, then again with `method: "get_comments"` for the discussion.
