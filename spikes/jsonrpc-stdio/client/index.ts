import { spawn } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import { dirname, resolve } from 'node:path';
import {
  createMessageConnection,
  StreamMessageReader,
  StreamMessageWriter,
} from 'vscode-jsonrpc/node.js';

// SPIKE: validates that vscode-jsonrpc can talk to a StreamJsonRpc
// server on stdin/stdout. Mirrors what the VSCode Editor Frontend's
// client side of the Inspector RPC will do per ADR-0012.

const __dirname = dirname(fileURLToPath(import.meta.url));
// Use the pre-built DLL directly — `dotnet run` would print build banner
// to stdout and corrupt the JSON-RPC framing.
// Flowthru's Directory.Build.props redirects outputs to dist/.
const serverDll = resolve(
  __dirname, '..', '..', '..',
  'dist', 'spikes', 'jsonrpc-stdio', 'server', 'Debug', 'net10.0', 'SpikeServer.dll'
);

const server = spawn('dotnet', [ serverDll ], {
  stdio: [ 'pipe', 'pipe', 'inherit' ],
});

const connection = createMessageConnection(
  new StreamMessageReader(server.stdout!),
  new StreamMessageWriter(server.stdin!),
);

connection.listen();

const withTimeout = <T>(p: Promise<T>, ms: number, label: string): Promise<T> =>
  Promise.race([
    p,
    new Promise<never>((_, reject) =>
      setTimeout(() => reject(new Error(`Timeout (${ms}ms) waiting for ${label}`)), ms)
    ),
  ]);

try {
  const result = await withTimeout(
    connection.sendRequest<string>('Ping', 'hello'),
    5000,
    'Ping response',
  );
  console.log(`Got response: ${result}`);
  process.exitCode = 0;
} catch (err) {
  console.error('Spike failed:', err);
  process.exitCode = 1;
} finally {
  connection.dispose();
  server.kill();
}
