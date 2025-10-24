using System.Text.Json.Serialization;

namespace Flowthru.Meta.Models;

/// <summary>
/// Metadata describing a single node in the pipeline DAG.
/// </summary>
/// <remarks>
/// Nodes are the processing units in a pipeline. Each node reads from one or more
/// catalog entries (inputs), performs a transformation, and writes to one or more
/// catalog entries (outputs).
/// </remarks>
public class NodeMetadata {
  /// <summary>
  /// Unique identifier for this node within the pipeline.
  /// </summary>
  /// <remarks>
  /// Typically the node name as defined when adding it to the pipeline.
  /// Example: "PreprocessCompanies", "TrainModel"
  /// </remarks>
  [JsonPropertyName("id")]
  public required string Id { get; init; }

  /// <summary>
  /// Human-readable display label for this node.
  /// </summary>
  /// <remarks>
  /// May be formatted for better display in Flowthru.Viz.
  /// Example: "Preprocess Companies", "Train Model"
  /// </remarks>
  [JsonPropertyName("label")]
  public required string Label { get; init; }

  /// <summary>
  /// The C# class type name implementing this node.
  /// </summary>
  /// <remarks>
  /// Simple type name without namespace or generic parameters.
  /// Example: "PreprocessCompaniesNode", "TrainModelNode"
  /// </remarks>
  [JsonPropertyName("nodeType")]
  public required string NodeType { get; init; }

  /// <summary>
  /// Execution layer assigned by the dependency analyzer.
  /// </summary>
  /// <remarks>
  /// Layer 0 nodes have no dependencies (read external data only).
  /// Layer N nodes depend only on nodes in layers 0..N-1.
  /// </remarks>
  [JsonPropertyName("layer")]
  public int Layer { get; init; }

  /// <summary>
  /// Name of the parent pipeline this node belongs to.
  /// </summary>
  /// <remarks>
  /// Important for merged pipelines where nodes from multiple pipelines
  /// are combined into a single DAG.
  /// </remarks>
  [JsonPropertyName("pipelineName")]
  public required string PipelineName { get; init; }

  /// <summary>
  /// List of catalog entry keys this node reads from.
  /// </summary>
  /// <remarks>
  /// For multi-input nodes using CatalogMap, this contains all mapped entries.
  /// Example: ["Companies", "Shuttles", "Reviews"]
  /// </remarks>
  [JsonPropertyName("inputs")]
  public List<string> Inputs { get; init; } = new();

  /// <summary>
  /// List of catalog entry keys this node writes to.
  /// </summary>
  /// <remarks>
  /// For multi-output nodes using CatalogMap, this contains all mapped entries.
  /// Example: ["XTrain", "XTest", "YTrain", "YTest"]
  /// </remarks>
  [JsonPropertyName("outputs")]
  public List<string> Outputs { get; init; } = new();
}
