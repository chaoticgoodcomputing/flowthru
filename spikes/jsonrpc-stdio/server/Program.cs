using StreamJsonRpc;

// SPIKE: validates that StreamJsonRpc can speak Content-Length-framed
// JSON-RPC on stdin/stdout in a way that vscode-jsonrpc on the other
// side understands. Mirrors what the real Inspector's JSON-RPC transport
// will look like per ADR-0012.

var formatter = new JsonMessageFormatter();
var handler = new HeaderDelimitedMessageHandler(
    Console.OpenStandardOutput(),
    Console.OpenStandardInput(),
    formatter
);
var rpc = new JsonRpc(handler, new SpikeServer());
rpc.StartListening();
await rpc.Completion;

internal sealed class SpikeServer
{
    public string Ping(string message) => $"pong: {message}";
}
