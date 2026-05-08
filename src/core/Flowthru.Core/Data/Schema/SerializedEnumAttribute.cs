namespace Flowthru.Data.Schema;

/// <summary>
/// Specifies the serialized string value for an enum member when written
/// to or read from storage. Format-agnostic — applies uniformly across
/// CSV, Excel, JSON, Parquet, and any future format. Required for every
/// enum member used in a serializable schema (validated at build time).
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class SerializedEnumAttribute : Attribute
{
  /// <summary>The string value used when serializing this enum member.</summary>
  public string Value { get; }

  public SerializedEnumAttribute(string value)
  {
    if (value is null)
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
