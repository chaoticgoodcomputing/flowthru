using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace MagicAST.Core.Diagnostics;

/// <summary>
/// Accumulates diagnostics during parsing/analysis.
/// Thread-safe for parallel parsing scenarios.
/// </summary>
public sealed class DiagnosticBag
{
  private readonly ConcurrentBag<Diagnostic> _diagnostics = new();

  /// <summary>
  /// Adds a diagnostic to the bag.
  /// </summary>
  public void Add(Diagnostic diagnostic)
  {
    if (diagnostic == null)
    {
      throw new ArgumentNullException(nameof(diagnostic));
    }

    _diagnostics.Add(diagnostic);
  }

  /// <summary>
  /// Adds multiple diagnostics.
  /// </summary>
  public void AddRange(IEnumerable<Diagnostic> diagnostics)
  {
    if (diagnostics == null)
    {
      throw new ArgumentNullException(nameof(diagnostics));
    }

    foreach (var d in diagnostics)
    {
      _diagnostics.Add(d);
    }
  }

  /// <summary>
  /// Reports a diagnostic using a descriptor.
  /// </summary>
  public void Report(
    DiagnosticDescriptor descriptor,
    Location? location,
    params object?[] messageArgs
  )
  {
    Add(Diagnostic.Create(descriptor, location, messageArgs));
  }

  /// <summary>
  /// Reports a diagnostic with custom severity.
  /// </summary>
  public void Report(
    DiagnosticDescriptor descriptor,
    Location? location,
    DiagnosticSeverity severity,
    params object?[] messageArgs
  )
  {
    Add(Diagnostic.Create(descriptor, location, severity, messageArgs));
  }

  /// <summary>
  /// Gets all diagnostics.
  /// </summary>
  public ImmutableArray<Diagnostic> ToImmutableArray() => _diagnostics.ToImmutableArray();

  /// <summary>
  /// Checks if there are any errors.
  /// </summary>
  public bool HasErrors => _diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);

  /// <summary>
  /// Filters diagnostics by severity.
  /// </summary>
  public IEnumerable<Diagnostic> GetDiagnostics(DiagnosticSeverity severity) =>
    _diagnostics.Where(d => d.Severity == severity);

  /// <summary>
  /// Gets the count of diagnostics.
  /// </summary>
  public int Count => _diagnostics.Count;

  /// <summary>
  /// Checks if the bag is empty.
  /// </summary>
  public bool IsEmpty => _diagnostics.IsEmpty;
}
