using System.Text.Json.Serialization;

namespace Flowthru.Meta.Models;

/// <summary>
/// Metadata describing a single step in the Flow DAG.
/// </summary>
/// <remarks>
/// Steps are the processing units in a flow. Each step reads from one or more
/// catalog entries (inputs), performs a transformation, and writes to one or more
/// catalog entries (outputs).
/// </remarks>
public class StepMetadata
{
  /// <summary>
  /// Unique identifier for this step within the flow.
  /// </summary>
  /// <remarks>
  /// Typically the step name as defined when adding it to the flow.
  /// Example: "PreprocessCompanies", "TrainModel"
  /// </remarks>
  [JsonPropertyName("id")]
  public required string Id { get; init; }

  /// <summary>
  /// Human-readable display label for this step.
  /// </summary>
  /// <remarks>
  /// May be formatted for better display in Flowthru.Viz.
  /// Example: "Preprocess Companies", "Train Model"
  /// </remarks>
  [JsonPropertyName("label")]
  public required string Label { get; init; }

  /// <summary>
  /// The C# class type name implementing this step.
  /// </summary>
  /// <remarks>
  /// Simple type name without namespace or generic parameters.
  /// Example: "PreprocessCompaniesStep", "TrainModelStep"
  /// </remarks>
  [JsonPropertyName("stepType")]
  public required string StepType { get; init; }

  /// <summary>
  /// Execution layer assigned by the dependency analyzer.
  /// </summary>
  /// <remarks>
  /// Layer 0 steps have no dependencies (read external data only).
  /// Layer N steps depend only on steps in layers 0..N-1.
  /// </remarks>
  [JsonPropertyName("layer")]
  public int Layer { get; init; }

  /// <summary>
  /// Name of the parent Flow this step belongs to.
  /// </summary>
  /// <remarks>
  /// Important for merged flows where steps from multiple flows
  /// are combined into a single DAG.
  /// </remarks>
  [JsonPropertyName("flowName")]
  public required string FlowName { get; init; }

  /// <summary>
  /// List of catalog entry keys this step reads from.
  /// </summary>
  /// <remarks>
  /// For multi-input steps using CatalogMap, this contains all mapped entries.
  /// Example: ["Companies", "Shuttles", "Reviews"]
  /// </remarks>
  [JsonPropertyName("inputs")]
  public List<string> Inputs { get; init; } = new();

  /// <summary>
  /// List of catalog entry keys this step writes to.
  /// </summary>
  /// <remarks>
  /// For multi-output steps using CatalogMap, this contains all mapped entries.
  /// Example: ["XTrain", "XTest", "YTrain", "YTest"]
  /// </remarks>
  [JsonPropertyName("outputs")]
  public List<string> Outputs { get; init; } = new();
}
