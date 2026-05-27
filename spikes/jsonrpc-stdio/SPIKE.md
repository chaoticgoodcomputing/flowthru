# Spike: JSON-RPC over stdio

## What we're testing

Whether `StreamJsonRpc` (.NET) and `vscode-jsonrpc` (TS) can interoperate over Content-Length-framed JSON-RPC on stdin/stdout — the wire protocol [ADR-0012](/.claude/docs/adr/0012-inspector-rpc-protocol-and-surface.md) commits the Inspector to.

## How to run

```bash
# Build the server once
dotnet build server

# Install client deps + run end-to-end
cd client
pnpm install
pnpm start
```

The client spawns the server as a child process, sends a `Ping` request with `"hello"`, and prints the response.

## What success looks like

```
Got response: pong: hello
```

## What failure looks like

- **Hang/timeout** — message framing mismatch (one side header-delimited, the other newline-delimited).
- **JSON parse error** — encoding issue or framing-bytes leaked into payload.
- **`Method not found`** — naming convention mismatch (lowercase vs. PascalCase, etc.).
- **Server crash** — .NET-side exception; check `stderr` (inherited from parent).

Any failure mode is informative. The spike is *successful as a spike* if we capture the failure mode and either fix it (the libraries can be made to interop) or document a follow-on change to ADR-0012 (the libraries can't).

## Cleanup

Delete `spikes/jsonrpc-stdio/` once the conclusion is captured in the Phase 0 issue.
