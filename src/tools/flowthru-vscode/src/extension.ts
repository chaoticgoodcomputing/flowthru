import * as vscode from 'vscode';

/**
 * Editor Frontend activation entry point.
 *
 * Scaffold only — real feature wiring lands here as it's built:
 *   - F2: Inspector-anchored diagnostic rendering on a project DAG canvas
 *   - F3: right-click run dispatch from canvas via Inspector RPC
 *   - F4: post-run status overlay
 *   - F5: failure debug panel (logs + input/output peek via Inspector)
 *   - F6: built-last vs. now diff against the .flowthru/manifests/ snapshot
 *
 * Contracts this extension consumes: the snapshot lifecycle
 * ([ADR-0002](/src/tools/docs/adr/0002-snapshot-lifecycle.md)), and the
 * Inspector RPC protocol, whose ADR is parked on the `adr/inspector-rpc` branch
 * until the Inspector is built.
 */
export function activate(context: vscode.ExtensionContext): void {
  context.subscriptions.push(
    vscode.commands.registerCommand('flowthru-vscode.hello', () => {
      void vscode.window.showInformationMessage('Flowthru extension is loaded.');
    }),
  );
}

export function deactivate(): void {
  // Inspector handle disposal and other cleanup will wire in here.
}
