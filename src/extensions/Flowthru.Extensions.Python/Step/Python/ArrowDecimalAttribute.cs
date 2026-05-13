namespace Flowthru.Step.Python;

/// <summary>
/// Declares the Arrow Decimal128 precision and scale for a
/// <see cref="decimal"/> property on a <c>[FlowthruSchema]</c>-decorated
/// type. When absent, the marshaller defaults to (28, 9), which fits
/// every <c>System.Decimal</c> value the CLR can represent within a
/// sensible monetary scale.
/// </summary>
/// <remarks>
/// Apache Arrow's Decimal128 is a fixed-point representation with
/// precision (total digits) and scale (digits after the decimal point).
/// Values that exceed the declared precision are rejected by Arrow at
/// encode time, so picking a precision is a load-bearing contract.
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ArrowDecimalAttribute : Attribute
{
  /// <summary>Total number of significant digits (1..38).</summary>
  public int Precision { get; }

  /// <summary>Digits after the decimal point (0..<see cref="Precision"/>).</summary>
  public int Scale { get; }

  /// <summary>
  /// Construct an explicit Decimal128 precision/scale for the annotated
  /// <see cref="decimal"/> property.
  /// </summary>
  public ArrowDecimalAttribute(int precision, int scale)
  {
    if (precision is < 1 or > 38)
      throw new ArgumentOutOfRangeException(nameof(precision), "Decimal128 precision must be 1..38.");
    if (scale < 0 || scale > precision)
      throw new ArgumentOutOfRangeException(nameof(scale), "Scale must satisfy 0 <= scale <= precision.");
    Precision = precision;
    Scale = scale;
  }
}
