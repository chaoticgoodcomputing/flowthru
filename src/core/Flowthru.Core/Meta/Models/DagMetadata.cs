using System.Text.Json.Serialization;

namespace Flowthru.Meta.Models;

/// <summary>
/// Root metadata model representing a complete pipeline DAG (Directed Acyclic Graph).
/// </summary>
/// <remarks>
/// This model captures the structure of a built pipeline, including all nodes,
/// catalog entries, and their relationships. It serves as the backbone for
/// Flowthru.Viz visualization.
/// </remarks>
public class DagMetadata
{
  /// <summary>
  /// Name of the pipeline this DAG represents.
  /// </summary>
  [JsonPropertyName("pipelineName")]
  public required string PipelineName { get; init; }

  /// <summary>
  /// Timestamp when this metadata was generated.
  /// </summary>
  [JsonPropertyName("generatedAt")]
  public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;

  /// <summary>
  /// All nodes in the pipeline with their metadata.
  /// </summary>
  [JsonPropertyName("nodes")]
  public List<NodeMetadata> Nodes { get; init; } = new();

  /// <summary>
  /// All catalog entries (datasets) involved in the pipeline.
  /// </summary>
  [JsonPropertyName("catalogEntries")]
  public List<CatalogEntryMetadata> CatalogEntries { get; init; } = new();

  /// <summary>
  /// All edges representing data flow in the DAG.
  /// </summary>
  /// <remarks>
  /// Edges connect catalog entries to nodes and nodes to catalog entries,
  /// forming the complete data flow graph.
  /// </remarks>
  [JsonPropertyName("edges")]
  public List<EdgeMetadata> Edges { get; init; } = new();

  /// <summary>
  /// Slice criteria applied to generate this DAG, if any.
  /// </summary>
  /// <remarks>
  /// Present when the DAG represents a sliced subset of the full pipeline.
  /// Null when the DAG represents the complete, unsliced pipeline.
  /// Used for reproducibility, debugging, and filename generation.
  /// </remarks>
  [JsonPropertyName("appliedSlice")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public DagSliceMetadata? AppliedSlice { get; init; }

  /// <summary>
  /// Node IDs that are in the active execution slice, if a slice was applied.
  /// </summary>
  /// <remarks>
  /// When a slice is applied, this contains the IDs of nodes that will actually execute.
  /// The Nodes collection contains the full DAG, while this set identifies the subset.
  /// Null when no slice was applied (all nodes execute).
  /// Enables visualization tools to highlight execution paths while showing full context.
  /// </remarks>
  [JsonPropertyName("slicedNodeIds")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public HashSet<string>? SlicedNodeIds { get; init; }

  /// <summary>
  /// Catalog entry keys that are produced by nodes in the active execution slice.
  /// </summary>
  /// <remarks>
  /// When a slice is applied, this contains the keys of catalog entries (data) that
  /// will be written during execution. Derived from the outputs of sliced nodes.
  /// Null when no slice was applied (all data may be updated).
  /// Enables visualization tools to highlight both nodes and the data they produce.
  /// </remarks>
  [JsonPropertyName("slicedCatalogEntryKeys")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public HashSet<string>? SlicedCatalogEntryKeys { get; init; }
}
