namespace Flowthru.Pipelines;

/// <summary>
/// Defines a strategy for slicing a pipeline to execute a subset of nodes.
/// </summary>
/// <remarks>
/// <para>
/// Pipeline slicing allows executing only specific portions of a DAG while maintaining
/// execution validity. All slicing operations preserve the runnability guarantee:
/// the resulting sub-DAG must be executable without missing dependencies.
/// </para>
/// <para>
/// <strong>Slicing Strategies:</strong>
/// </para>
/// <list type="bullet">
/// <item><strong>Pipelines:</strong> Filter to nodes from specific named pipelines (in merged DAGs)</item>
/// <item><strong>FromNodes:</strong> Include specified nodes and all downstream dependents</item>
/// <item><strong>ToNodes:</strong> Include specified nodes and all upstream dependencies (run "up to" these nodes)</item>
/// <item><strong>FromData:</strong> Include nodes consuming specified catalog entries and all downstream dependents</item>
/// <item><strong>ToData:</strong> Include nodes producing specified catalog entries and all upstream dependencies</item>
/// <item><strong>OnlyNodes:</strong> Explicit allowlist of nodes plus minimal required dependencies</item>
/// </list>
/// <para>
/// <strong>Composition:</strong> Multiple strategies compose via intersection (additive filtering).
/// For example, <c>--from-nodes A --to-data B</c> produces nodes in the downstream dependency
/// tree of A that are also required to produce data B.
/// </para>
/// <para>
/// <strong>Runnability Guarantee:</strong> Slicing operations are additive only. Subtractive
/// operations (<c>--from-nodes A --except B</c>) would break the runnability guarantee and
/// are not supported.
/// </para>
/// </remarks>
public sealed class PipelineSliceStrategy
{
  /// <summary>
  /// Filter to nodes from these named pipelines (applies to merged pipelines).
  /// </summary>
  /// <remarks>
  /// In merged pipelines, nodes are prefixed with their pipeline name (e.g., "DataScience.TrainModel").
  /// This filter includes only nodes from the specified pipelines.
  /// Pipeline names are case-insensitive.
  /// </remarks>
  public IReadOnlySet<string>? Pipelines { get; init; }

  /// <summary>
  /// Start from these nodes, including all downstream dependents.
  /// </summary>
  /// <remarks>
  /// Expands to include all nodes that depend on these nodes (transitively).
  /// Useful for impact analysis - "what breaks if I change this node?"
  /// </remarks>
  public IReadOnlySet<string>? FromNodes { get; init; }

  /// <summary>
  /// End at these nodes, including all upstream dependencies needed to produce them.
  /// </summary>
  /// <remarks>
  /// Expands to include all transitive dependencies needed to run these nodes.
  /// Equivalent to "run everything up to and including these nodes".
  /// Useful for testing specific outputs without running the entire pipeline.
  /// </remarks>
  public IReadOnlySet<string>? ToNodes { get; init; }

  /// <summary>
  /// Start from nodes that consume these catalog entry labels, including all downstream dependents.
  /// </summary>
  /// <remarks>
  /// Finds all nodes that read the specified catalog entries, then expands downstream.
  /// Useful for impact analysis - "what breaks if I change this data?"
  /// </remarks>
  public IReadOnlySet<string>? FromData { get; init; }

  /// <summary>
  /// End at nodes that produce these catalog entry labels, including all upstream dependencies.
  /// </summary>
  /// <remarks>
  /// Finds the nodes that write the specified catalog entries, then expands upstream.
  /// Useful for targeted execution - "run everything needed to produce this data".
  /// </remarks>
  public IReadOnlySet<string>? ToData { get; init; }

  /// <summary>
  /// Explicit allowlist of node names (dependencies auto-included).
  /// </summary>
  /// <remarks>
  /// Specifies exactly which nodes to execute, then automatically includes any
  /// required dependencies to maintain DAG validity.
  /// </remarks>
  public IReadOnlySet<string>? OnlyNodes { get; init; }

  /// <summary>
  /// Whether any slicing is configured.
  /// </summary>
  public bool IsSliced =>
    Pipelines != null
    || FromNodes != null
    || ToNodes != null
    || FromData != null
    || ToData != null
    || OnlyNodes != null;

  /// <summary>
  /// No filtering - execute entire pipeline.
  /// </summary>
  public static PipelineSliceStrategy All() => new();
}
