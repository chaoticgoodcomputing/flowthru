namespace Flowthru.Core.Graph;

/// <summary>
/// Defines a strategy for slicing a Flow to execute a subset of nodes.
/// </summary>
/// <remarks>
/// <para>
/// Flow slicing allows executing only specific portions of a DAG while maintaining
/// execution validity. All slicing operations preserve the runnability guarantee:
/// the resulting sub-DAG must be executable without missing dependencies.
/// </para>
/// <para>
/// Because a Flowthru flow is a bipartite graph of steps and catalog items, all slice
/// targets are addressed uniformly by label — whether the label belongs to a step or a
/// catalog item. The resolver checks the step index first; if no step matches, it falls
/// back to the catalog item index and resolves to the relevant producer or consumer steps.
/// </para>
/// <para>
/// <strong>Slicing Strategies:</strong>
/// </para>
/// <list type="bullet">
/// <item><strong>Flows:</strong> Filter to nodes from specific named flows (in merged DAGs)</item>
/// <item><strong>From:</strong> Include specified nodes and all downstream dependents. Accepts step labels or catalog item labels (resolves to consumers).</item>
/// <item><strong>To:</strong> Include specified nodes and all upstream dependencies. Accepts step labels or catalog item labels (resolves to producer).</item>
/// <item><strong>Only:</strong> Explicit allowlist plus minimal required dependencies. Accepts step labels or catalog item labels (resolves to producer).</item>
/// </list>
/// <para>
/// <strong>Composition:</strong> Multiple strategies compose via intersection (additive filtering).
/// For example, <c>--from A --to B</c> produces nodes in the downstream dependency
/// tree of A that are also required to produce B.
/// </para>
/// <para>
/// <strong>Runnability Guarantee:</strong> Slicing operations are additive only. Subtractive
/// operations (<c>--from A --except B</c>) would break the runnability guarantee and
/// are not supported.
/// </para>
/// </remarks>
public sealed class FlowSliceStrategy
{
  /// <summary>
  /// Filter to nodes from these named flows (applies to merged flows).
  /// </summary>
  /// <remarks>
  /// In merged flows, steps are prefixed with their Flow name (e.g., "DataScience.TrainModel").
  /// This filter includes only steps from the specified flows.
  /// Flow names are case-insensitive.
  /// </remarks>
  public IReadOnlySet<string>? Flows { get; init; }

  /// <summary>
  /// Start from these nodes, including all downstream dependents.
  /// </summary>
  /// <remarks>
  /// Each label is resolved against the step index first. If no step matches, the label is
  /// treated as a catalog item and resolved to all steps that consume it.
  /// Expands to include all transitively dependent steps.
  /// Useful for impact analysis: "what is affected if I change this step or item?"
  /// </remarks>
  public IReadOnlySet<string>? From { get; init; }

  /// <summary>
  /// End at these nodes, including all upstream dependencies needed to produce them.
  /// </summary>
  /// <remarks>
  /// Each label is resolved against the step index first. If no step matches, the label is
  /// treated as a catalog item and resolved to the step that produces it.
  /// Expands to include all transitive dependencies.
  /// Equivalent to "run everything up to and including these nodes".
  /// Useful for targeted execution: "run everything needed to produce this step or item".
  /// </remarks>
  public IReadOnlySet<string>? To { get; init; }

  /// <summary>
  /// Explicit allowlist of nodes (dependencies auto-included).
  /// </summary>
  /// <remarks>
  /// Each label is resolved against the step index first. If no step matches, the label is
  /// treated as a catalog item and resolved to the step that produces it.
  /// Automatically includes all transitive upstream dependencies to maintain DAG validity.
  /// </remarks>
  public IReadOnlySet<string>? Only { get; init; }

  /// <summary>
  /// Whether any slicing is configured.
  /// </summary>
  public bool IsSliced => Flows != null || From != null || To != null || Only != null;
}
