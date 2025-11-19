using MagicAST.Core.AST;

namespace MagicAST.DTOs;

/// <summary>
/// Output DTO representing a parsed Magic: The Gathering card AST with diagnostics.
/// Designed for pipeline output to JSON storage or downstream processing.
/// </summary>
/// <remarks>
/// This wraps the CardNode AST with serialization metadata and parse diagnostics.
/// Suitable for JSON output, database storage, or passing to downstream pipeline stages.
/// </remarks>
public record CardOutputDto
{
  /// <summary>
  /// The parsed AST as JSON-serializable data.
  /// Null if parsing completely failed.
  /// </summary>
  public CardAstDto? AST { get; init; } = null;

  /// <summary>
  /// Parse diagnostics (errors, warnings, info messages).
  /// </summary>
  public List<DiagnosticDto> Diagnostics { get; init; } = new();

  /// <summary>
  /// Whether parsing succeeded (AST is non-null and no errors).
  /// </summary>
  public bool ParseSucceeded { get; init; }
}

/// <summary>
/// Diagnostic information DTO for serialization.
/// </summary>
public record DiagnosticDto
{
  /// <summary>
  /// Severity level: Info, Warning, Error.
  /// </summary>
  public required string Severity { get; init; }

  /// <summary>
  /// Diagnostic code (e.g., "AST001").
  /// </summary>
  public required string Code { get; init; }

  /// <summary>
  /// Human-readable message.
  /// </summary>
  public required string Message { get; init; }

  /// <summary>
  /// Source text that triggered the diagnostic.
  /// </summary>
  public string? SourceText { get; init; }

  /// <summary>
  /// Creates a DiagnosticDto from a ParseDiagnostic.
  /// </summary>
  public static DiagnosticDto FromParseDiagnostic(ParseDiagnostic diagnostic)
  {
    return new DiagnosticDto
    {
      Severity = diagnostic.Severity.ToString(),
      Code = diagnostic.Code,
      Message = diagnostic.Message,
      SourceText = diagnostic.SourceText,
    };
  }
}
