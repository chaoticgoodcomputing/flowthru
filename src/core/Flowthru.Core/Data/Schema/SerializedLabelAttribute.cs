namespace Flowthru.Data.Schema;

/// <summary>
/// Specifies the external field name for a property when serialized to or
/// from storage. Format-agnostic — applies uniformly across CSV, Excel,
/// JSON, Parquet, and any future format. If absent, the C# property name
/// is used as-is.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class SerializedLabelAttribute : Attribute
{
  /// <summary>The external field name in the serialized data.</summary>
  public string Label { get; }

  public SerializedLabelAttribute(string label)
  {
    if (label is null)
    {
      throw new ArgumentNullException(nameof(label));
    }
    if (string.IsNullOrWhiteSpace(label))
    {
      throw new ArgumentException("Label cannot be empty or whitespace.", nameof(label));
    }
    Label = label;
  }
}
