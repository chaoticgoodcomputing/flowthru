using System.Text.Json.Serialization;

namespace Flowthru.Data.Storage.Sheets;

/// <summary>
/// The kind of value a <see cref="FieldValue"/> carries — the small set of
/// natively-typed values a table field can hold, expressed in Flowthru-neutral
/// terms so no Google SDK type leaks across the <see cref="ISheetsGateway"/>
/// seam.
/// </summary>
public enum FieldKind
{
  /// <summary>An empty field — no value written, no value read.</summary>
  Empty = 0,

  /// <summary>A numeric value (carried as <see cref="double"/>).</summary>
  Number = 1,

  /// <summary>A boolean value.</summary>
  Bool = 2,

  /// <summary>A text value.</summary>
  Text = 3,

  /// <summary>
  /// A date/time value. Write-side only — the gateway emits it as a serial
  /// number plus the matching <c>numberFormat</c>. On read a serial date
  /// comes back as <see cref="Number"/>; coercion to a CLR
  /// <see cref="DateTime"/> is the adapter's job, driven by the schema.
  /// </summary>
  Temporal = 4,
}

/// <summary>
/// Distinguishes the three temporal shapes a table field can format, so the
/// gateway can pick the correct <c>numberFormat</c> on write.
/// </summary>
public enum TemporalKind
{
  /// <summary>A calendar date with no time component.</summary>
  Date = 0,

  /// <summary>A date together with a time-of-day component.</summary>
  DateTime = 1,

  /// <summary>A time-of-day with no date component.</summary>
  Time = 2,
}

/// <summary>
/// A single typed field value within a table row, expressed in Flowthru-neutral
/// terms — the vocabulary the <see cref="ISheetsGateway"/> seam speaks instead
/// of Google SDK types. Allocation-light (a <see langword="readonly struct"/>)
/// and trivially JSON-serializable so an offline gateway can persist a table's
/// rows as JSON.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Read/write asymmetry.</strong> On read, a gateway produces only
/// <see cref="FieldKind.Number"/>, <see cref="FieldKind.Bool"/>,
/// <see cref="FieldKind.Text"/>, and <see cref="FieldKind.Empty"/> — the values
/// API returns raw doubles, bools, and strings, and a serial date arrives as
/// a <see cref="FieldKind.Number"/>. <see cref="FieldKind.Temporal"/> is
/// primarily a write-side kind: it tells the gateway to emit a serial number
/// together with a date/time <c>numberFormat</c>.
/// </para>
/// <para>
/// Use the static factory methods (<see cref="Number"/>, <see cref="Bool"/>,
/// <see cref="Text"/>, <see cref="Temporal"/>, <see cref="Empty"/>) rather
/// than constructing directly.
/// </para>
/// </remarks>
public readonly struct FieldValue : IEquatable<FieldValue>
{
  [JsonConstructor]
  private FieldValue(
    FieldKind kind,
    double numberValue,
    bool boolValue,
    string? textValue,
    DateTime temporalValue,
    TemporalKind temporalKind)
  {
    Kind = kind;
    NumberValue = numberValue;
    BoolValue = boolValue;
    TextValue = textValue;
    TemporalValue = temporalValue;
    TemporalKind = temporalKind;
  }

  /// <summary>The kind of value this field carries.</summary>
  public FieldKind Kind { get; }

  /// <summary>The numeric payload; meaningful only when <see cref="Kind"/> is <see cref="FieldKind.Number"/>.</summary>
  public double NumberValue { get; }

  /// <summary>The boolean payload; meaningful only when <see cref="Kind"/> is <see cref="FieldKind.Bool"/>.</summary>
  public bool BoolValue { get; }

  /// <summary>The text payload; meaningful only when <see cref="Kind"/> is <see cref="FieldKind.Text"/>.</summary>
  public string? TextValue { get; }

  /// <summary>The date/time payload; meaningful only when <see cref="Kind"/> is <see cref="FieldKind.Temporal"/>.</summary>
  public DateTime TemporalValue { get; }

  /// <summary>Which temporal shape (date / date-time / time) this field is; meaningful only when <see cref="Kind"/> is <see cref="FieldKind.Temporal"/>.</summary>
  public TemporalKind TemporalKind { get; }

  /// <summary>An empty field.</summary>
  public static FieldValue Empty { get; } = new(FieldKind.Empty, default, default, null, default, default);

  /// <summary>A numeric field.</summary>
  public static FieldValue Number(double value) => new(FieldKind.Number, value, default, null, default, default);

  /// <summary>A boolean field.</summary>
  public static FieldValue Bool(bool value) => new(FieldKind.Bool, default, value, null, default, default);

  /// <summary>A text field.</summary>
  public static FieldValue Text(string value) => new(FieldKind.Text, default, default, value, default, default);

  /// <summary>
  /// A date/time field. The <paramref name="temporalKind"/> selects the
  /// <c>numberFormat</c> the gateway emits on write.
  /// </summary>
  public static FieldValue Temporal(DateTime value, TemporalKind temporalKind = TemporalKind.DateTime) =>
    new(FieldKind.Temporal, default, default, null, value, temporalKind);

  /// <inheritdoc/>
  public bool Equals(FieldValue other) =>
    Kind == other.Kind
    && Kind switch
    {
      FieldKind.Number => NumberValue.Equals(other.NumberValue),
      FieldKind.Bool => BoolValue == other.BoolValue,
      FieldKind.Text => TextValue == other.TextValue,
      FieldKind.Temporal => TemporalValue == other.TemporalValue && TemporalKind == other.TemporalKind,
      _ => true,
    };

  /// <inheritdoc/>
  public override bool Equals(object? obj) => obj is FieldValue other && Equals(other);

  /// <inheritdoc/>
  public override int GetHashCode() => Kind switch
  {
    FieldKind.Number => HashCode.Combine(Kind, NumberValue),
    FieldKind.Bool => HashCode.Combine(Kind, BoolValue),
    FieldKind.Text => HashCode.Combine(Kind, TextValue),
    FieldKind.Temporal => HashCode.Combine(Kind, TemporalValue, TemporalKind),
    _ => HashCode.Combine(Kind),
  };

  /// <inheritdoc/>
  public override string ToString() => Kind switch
  {
    FieldKind.Number => $"Number({NumberValue})",
    FieldKind.Bool => $"Bool({BoolValue})",
    FieldKind.Text => $"Text({TextValue})",
    FieldKind.Temporal => $"Temporal({TemporalValue:o}, {TemporalKind})",
    _ => "Empty",
  };
}
