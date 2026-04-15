namespace Flowthru.Core.Abstractions;

/// <summary>
/// Specifies the external field name for a property when serialized to/from storage.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Purpose:</strong> Bridges C# property names with external data field names across all storage formats.
/// </para>
/// <para>
/// <strong>Format Agnostic:</strong> Works uniformly across CSV, Excel, JSON, Parquet, and future formats.
/// The serialization mechanism (CsvHelper, ExcelDataReader, System.Text.Json, etc.) is abstracted away.
/// </para>
/// <para>
/// <strong>Opt-in Override:</strong> If not present, the property name is used as-is.
/// Use this attribute when the external field name differs from the C# property name.
/// </para>
/// <para>
/// <strong>Common Use Cases:</strong>
/// </para>
/// <list type="bullet">
/// <item>snake_case data sources: <c>[SerializedLabel("company_id")] int CompanyId</c></item>
/// <item>kebab-case data sources: <c>[SerializedLabel("company-id")] int CompanyId</c></item>
/// <item>Space-separated names: <c>[SerializedLabel("Company ID")] int CompanyId</c></item>
/// <item>Legacy column names: <c>[SerializedLabel("CMPNY_ID")] int CompanyId</c></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Tier 1: No annotation - property name matches external field name
/// public record SimpleSchema(
///     int Id,           // Looks for "Id" in external data
///     string Name       // Looks for "Name" in external data
/// ) : IFlatSchema, ITextSerializable;
///
/// // Tier 2: Explicit annotation - handle naming mismatches
/// public record ShuttleSchema(
///     string Id,                                    // Looks for "Id"
///     [SerializedLabel("shuttle_location")]         // Looks for "shuttle_location"
///     string ShuttleLocation,
///
///     [SerializedLabel("d_check_complete")]         // Looks for "d_check_complete"
///     bool DCheckComplete,
///
///     [SerializedLabel("company id")]               // Handles space-separated names
///     int CompanyId
/// ) : IFlatSchema, ITextSerializable;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class SerializedLabelAttribute : Attribute
{
    /// <summary>
    /// Gets the external field name for serialization.
    /// </summary>
    public string Label { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SerializedLabelAttribute"/> class.
    /// </summary>
    /// <param name="label">The external field name in the serialized data</param>
    /// <exception cref="ArgumentNullException">Thrown when label is null</exception>
    /// <exception cref="ArgumentException">Thrown when label is empty or whitespace</exception>
    public SerializedLabelAttribute(string label)
    {
        if (label == null)
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
