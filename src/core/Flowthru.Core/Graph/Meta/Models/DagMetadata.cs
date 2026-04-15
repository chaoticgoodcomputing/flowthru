using System.Text.Json.Serialization;

namespace Flowthru.Core.Graph.Meta.Models;

/// <summary>
/// Root metadata model representing a complete FlowthruService DAG (Directed Acyclic Graph).
/// </summary>
/// <remarks>
/// This model captures the structure of a built flow, including all steps,
/// catalog items, and their relationships. It serves as the backbone for
/// Flowthru.Core.Viz visualization.
/// </remarks>
public class DagMetadata
{
  /// <summary>
  /// Name of the Flow this DAG represents.
  /// </summary>
  [JsonPropertyName("flowName")]
  public required string FlowName { get; init; }

  /// <summary>
  /// Timestamp when this metadata was generated.
  /// </summary>
  [JsonPropertyName("generatedAt")]
  public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;

  /// <summary>
  /// All steps in the Flow with their metadata.
  /// </summary>
  [JsonPropertyName("steps")]
  public List<StepMetadata> Steps { get; init; } = new();

  /// <summary>
  /// All catalog items involved in the flow.
  /// </summary>
  [JsonPropertyName("catalogItems")]
  public List<ItemMetadata> CatalogItems { get; init; } = new();

  /// <summary>
  /// All edges representing data Flow in the DAG.
  /// </summary>
  /// <remarks>
  /// Edges connect catalog items to steps and steps to catalog items,
  /// forming the complete graph.
  /// </remarks>
  [JsonPropertyName("edges")]
  public List<EdgeMetadata> Edges { get; init; } = new();

  /// <summary>
  /// Slice criteria applied to generate this DAG, if any.
  /// </summary>
  /// <remarks>
  /// Present when the DAG represents a sliced subset of the full Flow.
  /// Null when the DAG represents the complete, unsliced flow.
  /// Used for reproducibility, debugging, and filename generation.
  /// </remarks>
  [JsonPropertyName("appliedSlice")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public DagSliceMetadata? AppliedSlice { get; init; }

  /// <summary>
  /// Step IDs that are in the active execution slice, if a slice was applied.
  /// </summary>
  /// <remarks>
  /// When a slice is applied, this contains the IDs of steps that will actually execute.
  /// The Steps collection contains the full DAG, while this set identifies the subset.
  /// Null when no slice was applied (all steps execute).
  /// Enables visualization tools to highlight execution paths while showing full context.
  /// </remarks>
  [JsonPropertyName("slicedStepIds")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public HashSet<string>? SlicedStepIds { get; init; }

  /// <summary>
  /// Catalog item IDsthat are produced by steps in the active execution slice.
  /// </summary>
  /// <remarks>
  /// When a slice is applied, this contains the keys of catalog items (data) that
  /// will be written during execution. Derived from the outputs of sliced steps.
  /// Null when no slice was applied (all data may be updated).
  /// Enables visualization tools to highlight both steps and the data they produce.
  /// </remarks>
  [JsonPropertyName("slicedCatalogItemIds")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public HashSet<string>? SlicedCatalogItemIds { get; init; }
}
