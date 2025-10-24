using System.Text.Json.Serialization;

namespace Flowthru.Meta.Models;

/// <summary>
/// Metadata describing a single catalog entry (dataset) in the pipeline.
/// </summary>
/// <remarks>
/// Catalog entries represent data sources and sinks. They can be external files,
/// intermediate pipeline outputs, or final results. Each entry is uniquely identified
/// by its key.
/// </remarks>
public class CatalogEntryMetadata {
  /// <summary>
  /// Unique key identifying this catalog entry.
  /// </summary>
  /// <remarks>
  /// Corresponds to the catalog property name or explicitly set key.
  /// Example: "Companies", "CleanedCompanies", "ModelInputTable"
  /// </remarks>
  [JsonPropertyName("key")]
  public required string Key { get; init; }

  /// <summary>
  /// Human-readable display label for this catalog entry.
  /// </summary>
  /// <remarks>
  /// May be formatted for better display in Flowthru.Viz.
  /// Example: "Companies", "Cleaned Companies", "Model Input Table"
  /// </remarks>
  [JsonPropertyName("label")]
  public required string Label { get; init; }

  /// <summary>
  /// The C# type name of data stored in this catalog entry.
  /// </summary>
  /// <remarks>
  /// Simple type name without namespace.
  /// Example: "Company", "Shuttle", "ModelInput"
  /// </remarks>
  [JsonPropertyName("dataType")]
  public required string DataType { get; init; }

  /// <summary>
  /// Schema information inferred from the data type.
  /// </summary>
  /// <remarks>
  /// Null for simple types or when schema inference fails.
  /// Contains property names, types, and nullability for complex types.
  /// </remarks>
  [JsonPropertyName("schema")]
  public SchemaMetadata? Schema { get; init; }

  /// <summary>
  /// Additional metadata fields specific to the catalog entry type.
  /// </summary>
  /// <remarks>
  /// <para>Examples of fields:</para>
  /// <list type="bullet">
  /// <item><c>filepath</c>: Path to file for file-based datasets</item>
  /// <item><c>catalogType</c>: Type of catalog dataset (CsvCatalogDataset, ParquetCatalogDataset, etc.)</item>
  /// <item><c>isReadOnly</c>: Whether the dataset is read-only</item>
  /// <item><c>inspectionLevel</c>: Validation inspection level (None, Shallow, Deep)</item>
  /// </list>
  /// </remarks>
  [JsonPropertyName("fields")]
  public Dictionary<string, object> Fields { get; init; } = new();

  /// <summary>
  /// Node ID that produces (writes to) this catalog entry.
  /// </summary>
  /// <remarks>
  /// Null for external inputs (Layer 0 inputs that exist before pipeline execution).
  /// Example: "PreprocessCompanies"
  /// </remarks>
  [JsonPropertyName("producer")]
  public string? Producer { get; init; }

  /// <summary>
  /// List of node IDs that consume (read from) this catalog entry.
  /// </summary>
  /// <remarks>
  /// Empty for pipeline outputs that aren't consumed by other nodes.
  /// Example: ["CreateModelInputTable", "ValidateData"]
  /// </remarks>
  [JsonPropertyName("consumers")]
  public List<string> Consumers { get; init; } = new();
}
