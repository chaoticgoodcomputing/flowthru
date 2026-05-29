namespace Flowthru.Data.Storage.Sheets;

/// <summary>
/// The Flowthru-neutral type of a single table column — the subset of Google
/// Sheets column types Flowthru maps onto. A column carries exactly one type,
/// like a database column; this is the store's schema vocabulary, not a
/// per-cell concept.
/// </summary>
/// <remarks>
/// The Google-string mapping (the verified <c>TEXT</c> / <c>DOUBLE</c> /
/// <c>DATE_TIME</c> tokens and friends) lives only inside the translator —
/// nothing on the seam speaks Google strings.
/// </remarks>
public enum ColumnType
{
  /// <summary>A free-text column.</summary>
  Text = 0,

  /// <summary>A numeric column (Sheets <c>DOUBLE</c>).</summary>
  Number = 1,

  /// <summary>A boolean column.</summary>
  Bool = 2,

  /// <summary>A date-and-time column (Sheets <c>DATE_TIME</c>).</summary>
  DateTime = 3,

  /// <summary>A calendar-date column with no time component.</summary>
  Date = 4,

  /// <summary>A time-of-day column with no date component.</summary>
  Time = 5,
}
