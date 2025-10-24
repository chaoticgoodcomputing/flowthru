using System.Text.Json.Serialization;

namespace Flowthru.Meta.Models;

/// <summary>
/// Schema information for a data type.
/// </summary>
/// <remarks>
/// Extracted from C# type definitions using reflection. Describes the structure
/// of data flowing through catalog entries, enabling Flowthru.Viz to display
/// data schemas and validate type compatibility.
/// </remarks>
public class SchemaMetadata {
  /// <summary>
  /// List of fields (properties) in the schema.
  /// </summary>
  [JsonPropertyName("fields")]
  public List<SchemaField> Fields { get; init; } = new();
}

/// <summary>
/// A single field (property) in a schema.
/// </summary>
public class SchemaField {
  /// <summary>
  /// Name of the property.
  /// </summary>
  /// <remarks>
  /// Example: "Id", "Name", "IataApproved"
  /// </remarks>
  [JsonPropertyName("name")]
  public required string Name { get; init; }

  /// <summary>
  /// C# type name of the property.
  /// </summary>
  /// <remarks>
  /// Simple type name without namespace.
  /// Example: "string", "int", "DateTime", "double"
  /// </remarks>
  [JsonPropertyName("type")]
  public required string Type { get; init; }

  /// <summary>
  /// Whether the property can be null.
  /// </summary>
  /// <remarks>
  /// Determined by nullable reference types (string?) or nullable value types (int?).
  /// </remarks>
  [JsonPropertyName("isNullable")]
  public bool IsNullable { get; init; }
}
