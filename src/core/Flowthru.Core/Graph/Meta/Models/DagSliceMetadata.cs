using System.Text.Json.Serialization;
using Flowthru.Core.Graph;

namespace Flowthru.Core.Graph.Meta.Models;

/// <summary>
/// Metadata describing how a flow was sliced during execution.
/// </summary>
/// <remarks>
/// Captures the criteria used to select a subset of steps from the full flow DAG.
/// This information is essential for:
/// <list type="bullet">
/// <item>Reproducibility - rerun the exact same slice</item>
/// <item>Debugging - understand what was included/excluded when failures occur</item>
/// <item>Auditing - track which flow subsets were executed in production</item>
/// <item>Visualization - indicate sliced vs. full DAG in metadata exports</item>
/// </list>
/// </remarks>
public class DagSliceMetadata
{
    /// <summary>
    /// Flow names used to filter the merged DAG.
    /// </summary>
    [JsonPropertyName("flows")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string[]? Flows { get; init; }

    /// <summary>
    /// Node labels from which the slice expanded downstream. Each label may be a step
    /// label or a catalog item label (resolved to its consumer steps).
    /// </summary>
    [JsonPropertyName("from")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string[]? From { get; init; }

    /// <summary>
    /// Node labels to which the slice expanded upstream. Each label may be a step
    /// label or a catalog item label (resolved to its producer step).
    /// </summary>
    [JsonPropertyName("to")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string[]? To { get; init; }

    /// <summary>
    /// Explicit allowlist of node labels (with upstream dependencies auto-included). Each label
    /// may be a step label or a catalog item label (resolved to its producer step).
    /// </summary>
    [JsonPropertyName("only")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string[]? Only { get; init; }

    /// <summary>
    /// Creates a <see cref="DagSliceMetadata"/> from a <see cref="FlowSliceStrategy"/>.
    /// </summary>
    internal static DagSliceMetadata? FromStrategy(FlowSliceStrategy? strategy)
    {
        if (strategy == null)
        {
            return null;
        }

        return new DagSliceMetadata
        {
            Flows = strategy.Flows?.ToArray(),
            From = strategy.From?.ToArray(),
            To = strategy.To?.ToArray(),
            Only = strategy.Only?.ToArray(),
        };
    }

    /// <summary>
    /// Returns a human-readable descriptor of the slice type for UI display.
    /// </summary>
    internal string GetSliceTypeDescriptor()
    {
        int criteriaCount = 0;
        if (Flows?.Length > 0)
        {
            criteriaCount++;
        }

        if (From?.Length > 0)
        {
            criteriaCount++;
        }

        if (To?.Length > 0)
        {
            criteriaCount++;
        }

        if (Only?.Length > 0)
        {
            criteriaCount++;
        }

        if (criteriaCount == 0)
        {
            return "FullDag";
        }

        if (criteriaCount == 1)
        {
            if (Flows?.Length > 0)
            {
                return Flows.Length == 1 ? "Flow" : "Flows";
            }

            if (From?.Length > 0)
            {
                return From.Length == 1 ? "From" : "From";
            }

            if (To?.Length > 0)
            {
                return To.Length == 1 ? "To" : "To";
            }

            if (Only?.Length > 0)
            {
                return Only.Length == 1 ? "Only" : "Only";
            }
        }

        return "ComposedSlice";
    }
}
