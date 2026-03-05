using System.Text.Json.Serialization;
using Flowthru.Pipelines;

namespace Flowthru.Meta.Models;

/// <summary>
/// Metadata describing how a pipeline was sliced during execution.
/// </summary>
/// <remarks>
/// Captures the criteria used to select a subset of nodes from the full pipeline DAG.
/// This information is essential for:
/// <list type="bullet">
/// <item>Reproducibility - rerun the exact same slice</item>
/// <item>Debugging - understand what was included/excluded when failures occur</item>
/// <item>Auditing - track which pipeline subsets were executed in production</item>
/// <item>Visualization - indicate sliced vs. full DAG in metadata exports</item>
/// </list>
/// </remarks>
public class DagSliceMetadata
{
  /// <summary>
  /// Pipeline names to include in the merged DAG.
  /// </summary>
  [JsonPropertyName("pipelines")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string[]? Pipelines { get; init; }

  /// <summary>
  /// Node names from which the slice expanded downstream (dependents included).
  /// </summary>
  [JsonPropertyName("fromNodes")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string[]? FromNodes { get; init; }

  /// <summary>
  /// Node names to which the slice expanded upstream (dependencies included).
  /// </summary>
  [JsonPropertyName("toNodes")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string[]? ToNodes { get; init; }

  /// <summary>
  /// Catalog entry labels whose consumers are included (expanded downstream).
  /// </summary>
  [JsonPropertyName("fromData")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string[]? FromData { get; init; }

  /// <summary>
  /// Catalog entry labels whose producers are included (expanded upstream).
  /// </summary>
  [JsonPropertyName("toData")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string[]? ToData { get; init; }

  /// <summary>
  /// Explicit allowlist of node names (with dependencies auto-included).
  /// </summary>
  [JsonPropertyName("onlyNodes")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string[]? OnlyNodes { get; init; }

  /// <summary>
  /// Creates a <see cref="DagSliceMetadata"/> from a <see cref="PipelineSliceStrategy"/>.
  /// </summary>
  internal static DagSliceMetadata? FromStrategy(PipelineSliceStrategy? strategy)
  {
    if (strategy == null)
    {
      return null;
    }

    return new DagSliceMetadata
    {
      Pipelines = strategy.Pipelines?.ToArray(),
      FromNodes = strategy.FromNodes?.ToArray(),
      ToNodes = strategy.ToNodes?.ToArray(),
      FromData = strategy.FromData?.ToArray(),
      ToData = strategy.ToData?.ToArray(),
      OnlyNodes = strategy.OnlyNodes?.ToArray(),
    };
  }

  /// <summary>
  /// Returns a human-readable descriptor of the slice type for UI display.
  /// </summary>
  internal string GetSliceTypeDescriptor()
  {
    // Count the number of active slice criteria
    int criteriaCount = 0;
    if (Pipelines?.Length > 0)
    {
      criteriaCount++;
    }
    if (FromNodes?.Length > 0)
    {
      criteriaCount++;
    }
    if (ToNodes?.Length > 0)
    {
      criteriaCount++;
    }
    if (FromData?.Length > 0)
    {
      criteriaCount++;
    }
    if (ToData?.Length > 0)
    {
      criteriaCount++;
    }
    if (OnlyNodes?.Length > 0)
    {
      criteriaCount++;
    }

    // No criteria
    if (criteriaCount == 0)
    {
      return "FullDag";
    }

    // Single criterion
    if (criteriaCount == 1)
    {
      if (Pipelines?.Length > 0)
      {
        return Pipelines.Length == 1 ? "Pipeline" : "Pipelines";
      }
      if (FromNodes?.Length > 0)
      {
        return FromNodes.Length == 1 ? "FromNode" : "FromNodes";
      }
      if (ToNodes?.Length > 0)
      {
        return ToNodes.Length == 1 ? "ToNode" : "ToNodes";
      }
      if (FromData?.Length > 0)
      {
        return FromData.Length == 1 ? "FromData" : "FromData";
      }
      if (ToData?.Length > 0)
      {
        return ToData.Length == 1 ? "ToData" : "ToData";
      }
      if (OnlyNodes?.Length > 0)
      {
        return OnlyNodes.Length == 1 ? "OnlyNode" : "OnlyNodes";
      }
    }

    // Multiple criteria
    return "ComposedSlice";
  }
}
