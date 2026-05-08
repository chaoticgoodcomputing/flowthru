namespace Flowthru.Diagnostics;

/// <summary>
/// Display-ready packaging of a <see cref="RuntimeError"/>: the
/// FT-range diagnostic code, the human-readable category label, the
/// rendered message, and the underlying error itself for callers
/// that want richer rendering. Consumers (CLI, hosting log output,
/// metadata exporters) name <see cref="RuntimeErrorReport"/> rather
/// than re-classifying every <see cref="RuntimeError"/> at the call
/// site.
/// </summary>
public sealed record RuntimeErrorReport(
  string DiagnosticCode,
  string Category,
  string Message,
  RuntimeError Error
);
