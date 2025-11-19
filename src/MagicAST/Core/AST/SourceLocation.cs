namespace MagicAST.Core.AST;

/// <summary>
/// Represents a location in the source text.
/// Used for error reporting and debugging.
/// </summary>
public class SourceLocation
{
  /// <summary>
  /// Line number in the source (1-based).
  /// </summary>
  public int Line { get; init; }

  /// <summary>
  /// Column number in the source (1-based).
  /// </summary>
  public int Column { get; init; }

  /// <summary>
  /// Optional source file name or identifier.
  /// </summary>
  public string? SourceFile { get; init; }

  /// <summary>
  /// Returns a string representation of this location.
  /// </summary>
  public override string ToString()
  {
    if (!string.IsNullOrEmpty(SourceFile))
    {
      return $"{SourceFile}:{Line}:{Column}";
    }
    return $"{Line}:{Column}";
  }
}
