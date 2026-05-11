using Flowthru.Data.Storage;

namespace Flowthru.Data.Catalog;

/// <summary>
/// Engine-level umbrella contract for every DAG node — both items
/// (places) and steps (arrows). The dependency analyzer and metadata
/// builders walk graphs of <see cref="INode"/> uniformly without
/// caring whether a vertex is an item or a step.
/// </summary>
/// <remarks>
/// <para>
/// Per §2.4 the bipartite practical structure stays — items have
/// <c>Load</c>/<c>Save</c>/<c>Inspect</c> semantics, steps have a
/// <c>Transform</c> — but they share this umbrella so the engine can
/// dispatch over mixed sequences. Operations that exist on only one
/// archetype (e.g., loading data) live on the archetype's interface,
/// not here.
/// </para>
/// </remarks>
public interface INode
{
  /// <summary>Unique label identifying this node within the DAG.</summary>
  string Label { get; }

  /// <summary>Capability metadata describing this node's properties.</summary>
  NodeTraits Traits { get; }

  /// <summary>
  /// Pre-flight validation. Semantics vary by archetype: data items check
  /// existence and schema; steps return success (correctness validated
  /// via tests).
  /// </summary>
  FlowIO<ValidationResult> Validate();
}
