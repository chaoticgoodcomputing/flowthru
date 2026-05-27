import * as vscode from 'vscode';

// SPIKE extension for ADR-0011's property-bag carriage assumption.
// Subscribes to diagnostic changes; for any diagnostic with code
// `FLSPIKE001`, dumps everything we can pull off the Diagnostic object
// — especially the `data` field, which is the LSP-spec extension point
// most likely to carry Roslyn's `Diagnostic.Properties` through.

const SPIKE_CODE = 'FLSPIKE001';

export function activate(context: vscode.ExtensionContext): void {
  const output = vscode.window.createOutputChannel('Spike: Property Bag');
  output.appendLine('[Spike] Property-bag spike extension activated.');
  output.appendLine('[Spike] Waiting for diagnostics with code FLSPIKE001...');
  output.show(true);

  const inspect = (uri: vscode.Uri): void => {
    const diagnostics = vscode.languages.getDiagnostics(uri);
    for (const diag of diagnostics) {
      const code = typeof diag.code === 'object' ? diag.code.value : diag.code;
      if (String(code) !== SPIKE_CODE) continue;

      output.appendLine('');
      output.appendLine(`[Spike] Diagnostic detected at ${uri.fsPath}:${diag.range.start.line + 1}`);
      output.appendLine(`  code:     ${String(code)}`);
      output.appendLine(`  source:   ${diag.source ?? '(none)'}`);
      output.appendLine(`  severity: ${vscode.DiagnosticSeverity[diag.severity]}`);
      output.appendLine(`  message:  ${diag.message}`);

      // The actual unknown — does the LSP `data` field carry the
      // property bag? VSCode's TypeScript API doesn't expose `data`
      // formally on Diagnostic, but it's often present at runtime.
      const data = (diag as unknown as { data?: unknown }).data;
      output.appendLine(`  data:     ${data === undefined ? 'undefined' : JSON.stringify(data, null, 4)}`);

      // Dump the full enumerable surface for whatever else might carry it.
      const dump: Record<string, unknown> = {};
      for (const key of Object.keys(diag)) {
        dump[key] = (diag as Record<string, unknown>)[key];
      }
      output.appendLine(`  full:     ${JSON.stringify(dump, null, 4)}`);
    }
  };

  // Inspect on every diagnostic-change event.
  context.subscriptions.push(
    vscode.languages.onDidChangeDiagnostics((event) => {
      for (const uri of event.uris) inspect(uri);
    }),
  );

  // Inspect any already-open document at activation time.
  for (const doc of vscode.workspace.textDocuments) inspect(doc.uri);
}

export function deactivate(): void {
  // nothing to clean up; the output channel disposes via the context subscriptions.
}
