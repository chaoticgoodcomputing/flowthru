using System.Collections.Immutable;

namespace MagicAST.Core.Diagnostics;

/// <summary>
/// Represents source text for error reporting.
/// Provides line-based access to the original text.
/// </summary>
public sealed class SourceText
{
  private readonly string _text;
  private readonly ImmutableArray<TextLine> _lines;

  private SourceText(string text, ImmutableArray<TextLine> lines)
  {
    _text = text;
    _lines = lines;
  }

  /// <summary>
  /// The full text.
  /// </summary>
  public string Text => _text;

  /// <summary>
  /// The lines in the source text.
  /// </summary>
  public ImmutableArray<TextLine> Lines => _lines;

  /// <summary>
  /// The length of the source text.
  /// </summary>
  public int Length => _text.Length;

  /// <summary>
  /// Creates a SourceText from a string.
  /// </summary>
  public static SourceText From(string text)
  {
    if (string.IsNullOrEmpty(text))
    {
      return new SourceText(string.Empty, ImmutableArray<TextLine>.Empty);
    }

    var lines = ParseLines(text);
    return new SourceText(text, lines);
  }

  /// <summary>
  /// Gets a substring from the source text.
  /// </summary>
  public string GetSubText(TextSpan span)
  {
    if (span.Start < 0 || span.Start > _text.Length || span.End > _text.Length)
    {
      throw new ArgumentOutOfRangeException(nameof(span));
    }

    return _text.Substring(span.Start, span.Length);
  }

  private static ImmutableArray<TextLine> ParseLines(string text)
  {
    var lines = new List<TextLine>();
    int position = 0;
    int lineStart = 0;

    while (position < text.Length)
    {
      char c = text[position];

      if (c == '\r')
      {
        // Handle \r\n
        if (position + 1 < text.Length && text[position + 1] == '\n')
        {
          lines.Add(new TextLine(lineStart, position - lineStart, position - lineStart + 2));
          position += 2;
          lineStart = position;
        }
        else
        {
          // Just \r
          lines.Add(new TextLine(lineStart, position - lineStart, position - lineStart + 1));
          position++;
          lineStart = position;
        }
      }
      else if (c == '\n')
      {
        lines.Add(new TextLine(lineStart, position - lineStart, position - lineStart + 1));
        position++;
        lineStart = position;
      }
      else
      {
        position++;
      }
    }

    // Add final line if it doesn't end with newline
    if (lineStart < text.Length)
    {
      lines.Add(new TextLine(lineStart, text.Length - lineStart, text.Length - lineStart));
    }

    return lines.ToImmutableArray();
  }

  /// <summary>
  /// Gets the line position (line and character) for an absolute position.
  /// </summary>
  public LinePosition GetLinePosition(int position)
  {
    if (position < 0 || position > _text.Length)
    {
      throw new ArgumentOutOfRangeException(nameof(position));
    }

    for (int i = 0; i < _lines.Length; i++)
    {
      var line = _lines[i];
      if (position >= line.Start && position <= line.Start + line.LengthIncludingLineBreak)
      {
        return new LinePosition(i, position - line.Start);
      }
    }

    // Position is at end of text
    if (_lines.Length > 0)
    {
      var lastLine = _lines[_lines.Length - 1];
      return new LinePosition(_lines.Length - 1, lastLine.Length);
    }

    return new LinePosition(0, 0);
  }

  public override string ToString() => _text;
}

/// <summary>
/// Represents a single line in source text.
/// </summary>
public readonly struct TextLine
{
  /// <summary>
  /// Absolute start position in source text.
  /// </summary>
  public int Start { get; }

  /// <summary>
  /// Length excluding line break characters.
  /// </summary>
  public int Length { get; }

  /// <summary>
  /// Length including line break characters.
  /// </summary>
  public int LengthIncludingLineBreak { get; }

  /// <summary>
  /// Absolute end position (Start + Length).
  /// </summary>
  public int End => Start + Length;

  public TextLine(int start, int length, int lengthIncludingLineBreak)
  {
    Start = start;
    Length = length;
    LengthIncludingLineBreak = lengthIncludingLineBreak;
  }
}
