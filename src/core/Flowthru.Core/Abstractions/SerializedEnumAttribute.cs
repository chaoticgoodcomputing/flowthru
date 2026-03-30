namespace Flowthru.Abstractions;

/// <summary>
/// Specifies the serialized string value for an enum member when written to or read from storage.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Purpose:</strong> Provides explicit string mappings for enum values across all storage formats.
/// </para>
/// <para>
/// <strong>Format Agnostic:</strong> Works uniformly across CSV, Excel, JSON, Parquet, and future formats.
/// The underlying serialization mechanism is abstracted away.
/// </para>
/// <para>
/// <strong>Required for All Enum Members:</strong> Every enum member must have this attribute when used
/// in schemas that will be serialized. This ensures explicit, documented mappings and prevents
/// accidental mismatches between C# names and external data formats.
/// </para>
/// <para>
/// <strong>Validation:</strong> The Flowthru pipeline validates at build time that all enum members
/// used in schemas have this attribute applied.
/// </para>
/// <para>
/// <strong>Common Use Cases:</strong>
/// </para>
/// <list type="bullet">
/// <item>Abbreviated values: <c>[SerializedEnum("W")] White</c></item>
/// <item>Lowercase conventions: <c>[SerializedEnum("common")] Common</c></item>
/// <item>Snake case: <c>[SerializedEnum("double_faced_token")] DoubleFacedToken</c></item>
/// <item>Legacy formats: <c>[SerializedEnum("STATUS_ACTIVE")] Active</c></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Example: Magic: The Gathering color enum
/// public enum MtgColor
/// {
///     [SerializedEnum("W")]
///     White,
///
///     [SerializedEnum("U")]
///     Blue,
///
///     [SerializedEnum("B")]
///     Black,
///
///     [SerializedEnum("R")]
///     Red,
///
///     [SerializedEnum("G")]
///     Green
/// }
///
/// // Example: Rarity enum with lowercase convention
/// public enum Rarity
/// {
///     [SerializedEnum("common")]
///     Common,
///
///     [SerializedEnum("uncommon")]
///     Uncommon,
///
///     [SerializedEnum("rare")]
///     Rare,
///
///     [SerializedEnum("mythic")]
///     Mythic
/// }
///
/// // Usage in schema - no additional configuration needed
/// public record Card(
///     string Name,
///     MtgColor Color,  // Automatically serializes using [SerializedEnum] mappings
///     Rarity Rarity    // Works across all formats (JSON, CSV, Excel, Parquet)
/// ) : IFlatSchema, ITextSerializable;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class SerializedEnumAttribute : Attribute
{
  /// <summary>
  /// Gets the serialized string value for this enum member.
  /// </summary>
  public string Value { get; }

  /// <summary>
  /// Initializes a new instance of the <see cref="SerializedEnumAttribute"/> class.
  /// </summary>
  /// <param name="value">The string value to use when serializing this enum member.</param>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
  /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is empty or whitespace.</exception>
  public SerializedEnumAttribute(string value)
  {
    if (value == null)
    {
      throw new ArgumentNullException(nameof(value));
    }

    if (string.IsNullOrWhiteSpace(value))
    {
      throw new ArgumentException(
        "Serialized enum value cannot be empty or whitespace.",
        nameof(value)
      );
    }

    Value = value;
  }
}
