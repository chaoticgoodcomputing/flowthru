namespace MagicAST.Core.Diagnostics;

/// <summary>
/// Represents a text span with start position and length.
/// Compatible with Superpower's position tracking.
/// </summary>
public readonly struct TextSpan : IEquatable<TextSpan>
{
  /// <summary>
  /// The start position (0-based).
  /// </summary>
  public int Start { get; }

  /// <summary>
  /// The length of the span.
  /// </summary>
  public int Length { get; }

  /// <summary>
  /// The end position (Start + Length).
  /// </summary>
  public int End => Start + Length;

  public TextSpan(int start, int length)
  {
    if (start < 0)
    {
      throw new ArgumentOutOfRangeException(nameof(start));
    }

    if (length < 0)
    {
      throw new ArgumentOutOfRangeException(nameof(length));
    }

    Start = start;
    Length = length;
  }

  public bool Equals(TextSpan other) => Start == other.Start && Length == other.Length;

  public override bool Equals(object? obj) => obj is TextSpan span && Equals(span);

  public override int GetHashCode() => HashCode.Combine(Start, Length);

  public static bool operator ==(TextSpan left, TextSpan right) => left.Equals(right);

  public static bool operator !=(TextSpan left, TextSpan right) => !left.Equals(right);

  public override string ToString() => $"[{Start}..{End})";
}

/// <summary>
/// Represents a position in source text as line and character.
/// Lines and characters are 0-based.
/// </summary>
public readonly struct LinePosition : IEquatable<LinePosition>, IComparable<LinePosition>
{
  /// <summary>
  /// The line number (0-based).
  /// </summary>
  public int Line { get; }

  /// <summary>
  /// The character position within the line (0-based).
  /// </summary>
  public int Character { get; }

  public LinePosition(int line, int character)
  {
    if (line < 0)
    {
      throw new ArgumentOutOfRangeException(nameof(line));
    }

    if (character < 0)
    {
      throw new ArgumentOutOfRangeException(nameof(character));
    }

    Line = line;
    Character = character;
  }

  public bool Equals(LinePosition other) => Line == other.Line && Character == other.Character;

  public override bool Equals(object? obj) => obj is LinePosition position && Equals(position);

  public override int GetHashCode() => HashCode.Combine(Line, Character);

  public int CompareTo(LinePosition other)
  {
    var lineComparison = Line.CompareTo(other.Line);
    if (lineComparison != 0)
    {
      return lineComparison;
    }

    return Character.CompareTo(other.Character);
  }

  public static bool operator ==(LinePosition left, LinePosition right) => left.Equals(right);

  public static bool operator !=(LinePosition left, LinePosition right) => !left.Equals(right);

  public static bool operator <(LinePosition left, LinePosition right) => left.CompareTo(right) < 0;

  public static bool operator >(LinePosition left, LinePosition right) => left.CompareTo(right) > 0;

  public static bool operator <=(LinePosition left, LinePosition right) =>
    left.CompareTo(right) <= 0;

  public static bool operator >=(LinePosition left, LinePosition right) =>
    left.CompareTo(right) >= 0;

  public override string ToString() => $"({Line},{Character})";
}

/// <summary>
/// Represents a span in source text using line positions.
/// </summary>
public readonly struct LinePositionSpan : IEquatable<LinePositionSpan>
{
  /// <summary>
  /// The start position.
  /// </summary>
  public LinePosition Start { get; }

  /// <summary>
  /// The end position.
  /// </summary>
  public LinePosition End { get; }

  public LinePositionSpan(LinePosition start, LinePosition end)
  {
    if (end < start)
    {
      throw new ArgumentException("End position must be >= start position");
    }

    Start = start;
    End = end;
  }

  public bool Equals(LinePositionSpan other) => Start == other.Start && End == other.End;

  public override bool Equals(object? obj) => obj is LinePositionSpan span && Equals(span);

  public override int GetHashCode() => HashCode.Combine(Start, End);

  public static bool operator ==(LinePositionSpan left, LinePositionSpan right) =>
    left.Equals(right);

  public static bool operator !=(LinePositionSpan left, LinePositionSpan right) =>
    !left.Equals(right);

  public override string ToString() => $"{Start}-{End}";
}
