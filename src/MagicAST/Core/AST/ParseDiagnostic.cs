namespace MagicAST.Core.AST;

/// <summary>
/// Represents a parsing diagnostic (error, warning, or information message).
/// Used to track parse failures, limitations, and approximations in the AST.
/// </summary>
public class ParseDiagnostic
{
  /// <summary>
  /// Severity level of this diagnostic.
  /// </summary>
  public required DiagnosticSeverity Severity { get; init; }

  /// <summary>
  /// Diagnostic code for programmatic identification.
  /// Format: "AST{number}" (e.g., "AST001", "AST042")
  /// </summary>
  public required string Code { get; init; }

  /// <summary>
  /// Human-readable message describing the diagnostic.
  /// </summary>
  public required string Message { get; init; }

  /// <summary>
  /// The oracle text that failed to parse or was approximated.
  /// </summary>
  public string? SourceText { get; init; }

  /// <summary>
  /// Location in the source text where the diagnostic occurred.
  /// </summary>
  public SourceLocation? Location { get; init; }

  /// <summary>
  /// Creates an error diagnostic.
  /// </summary>
  public static ParseDiagnostic Error(string code, string message, string? sourceText = null)
  {
    return new ParseDiagnostic
    {
      Severity = DiagnosticSeverity.Error,
      Code = code,
      Message = message,
      SourceText = sourceText,
    };
  }

  /// <summary>
  /// Creates a warning diagnostic.
  /// </summary>
  public static ParseDiagnostic Warning(string code, string message, string? sourceText = null)
  {
    return new ParseDiagnostic
    {
      Severity = DiagnosticSeverity.Warning,
      Code = code,
      Message = message,
      SourceText = sourceText,
    };
  }

  /// <summary>
  /// Creates an info diagnostic.
  /// </summary>
  public static ParseDiagnostic Info(string code, string message, string? sourceText = null)
  {
    return new ParseDiagnostic
    {
      Severity = DiagnosticSeverity.Info,
      Code = code,
      Message = message,
      SourceText = sourceText,
    };
  }
}

/// <summary>
/// Severity levels for parse diagnostics.
/// </summary>
public enum DiagnosticSeverity
{
  /// <summary>
  /// Informational message about parse decisions.
  /// </summary>
  Info,

  /// <summary>
  /// Warning about parse approximation or limitation.
  /// AST is valid but may not fully represent the oracle text.
  /// </summary>
  Warning,

  /// <summary>
  /// Error indicating parse failure.
  /// Part of the oracle text could not be represented in the AST.
  /// </summary>
  Error,
}
