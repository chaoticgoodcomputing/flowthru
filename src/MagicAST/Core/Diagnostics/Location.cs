namespace MagicAST.Core.Diagnostics;

/// <summary>
/// Represents a location in source text with rich context for error reporting.
/// </summary>
public sealed class Location
{
  /// <summary>
  /// The kind of location.
  /// </summary>
  public LocationKind Kind { get; }

  /// <summary>
  /// Source text span (start/end positions).
  /// </summary>
  public TextSpan SourceSpan { get; }

  /// <summary>
  /// Line position information.
  /// </summary>
  public LinePositionSpan LineSpan { get; }

  /// <summary>
  /// Source file path or identifier.
  /// </summary>
  public string? SourcePath { get; }

  /// <summary>
  /// The source text (for displaying context).
  /// </summary>
  public SourceText? SourceText { get; }

  /// <summary>
  /// A location that represents no location.
  /// </summary>
  public static readonly Location None = new Location(
    LocationKind.None,
    default,
    default,
    null,
    null
  );

  private Location(
    LocationKind kind,
    TextSpan sourceSpan,
    LinePositionSpan lineSpan,
    string? sourcePath,
    SourceText? sourceText
  )
  {
    Kind = kind;
    SourceSpan = sourceSpan;
    LineSpan = lineSpan;
    SourcePath = sourcePath;
    SourceText = sourceText;
  }

  /// <summary>
  /// Gets the text at this location.
  /// </summary>
  public string GetSourceText()
  {
    if (SourceText == null || Kind == LocationKind.None)
    {
      return string.Empty;
    }

    return SourceText.GetSubText(SourceSpan);
  }

  /// <summary>
  /// Creates a location from a text span in source text.
  /// </summary>
  public static Location Create(SourceText sourceText, TextSpan span, string? sourcePath = null)
  {
    var start = sourceText.GetLinePosition(span.Start);
    var end = sourceText.GetLinePosition(span.End);
    var lineSpan = new LinePositionSpan(start, end);

    return new Location(LocationKind.SourceFile, span, lineSpan, sourcePath, sourceText);
  }

  /// <summary>
  /// Creates a location from line positions in source text.
  /// </summary>
  public static Location Create(
    SourceText sourceText,
    LinePositionSpan lineSpan,
    string? sourcePath = null
  )
  {
    // Calculate absolute positions from line positions
    int start = GetAbsolutePosition(sourceText, lineSpan.Start);
    int end = GetAbsolutePosition(sourceText, lineSpan.End);
    var span = new TextSpan(start, end - start);

    return new Location(LocationKind.SourceFile, span, lineSpan, sourcePath, sourceText);
  }

  /// <summary>
  /// Creates a location from a Superpower Position.
  /// </summary>
  public static Location FromSuperpowerPosition(
    Superpower.Model.Position position,
    SourceText sourceText,
    int length = 1,
    string? sourcePath = null
  )
  {
    // Superpower positions are 1-based; convert to 0-based
    var linePosition = new LinePosition(position.Line - 1, position.Column - 1);
    var span = new TextSpan(position.Absolute, length);
    var lineSpan = new LinePositionSpan(linePosition, linePosition);

    return new Location(LocationKind.SourceFile, span, lineSpan, sourcePath, sourceText);
  }

  private static int GetAbsolutePosition(SourceText sourceText, LinePosition linePosition)
  {
    if (linePosition.Line >= sourceText.Lines.Length)
    {
      return sourceText.Length;
    }

    var line = sourceText.Lines[linePosition.Line];
    return line.Start + Math.Min(linePosition.Character, line.Length);
  }

  public override string ToString()
  {
    if (Kind == LocationKind.None)
    {
      return "None";
    }

    var path = SourcePath ?? "<unknown>";
    // Display as 1-based for user friendliness
    return $"{path}({LineSpan.Start.Line + 1},{LineSpan.Start.Character + 1})";
  }
}

/// <summary>
/// The kind of location.
/// </summary>
public enum LocationKind
{
  /// <summary>
  /// No location.
  /// </summary>
  None,

  /// <summary>
  /// Location in a source file.
  /// </summary>
  SourceFile,

  /// <summary>
  /// Location in metadata.
  /// </summary>
  Metadata,
}
