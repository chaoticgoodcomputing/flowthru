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
public class DagMetadata {
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
}
