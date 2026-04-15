using Flowthru.Core.Graph;

namespace Flowthru.Core.Data.Storage;

/// <summary>
/// Implemented by storage adapters whose <see cref="IStorageAdapter{T}.Load"/> depends on
/// the resolved value of one or more other catalog items at runtime.
/// </summary>
/// <remarks>
/// <para>
/// The dependency analyzer inspects each catalog item consumed by a step. If the item's
/// adapter implements this interface, the declared <see cref="ItemDependencies"/> are treated
/// as implicit DAG edges: any step consuming that item is scheduled after every step that
/// produces one of its adapter dependencies.
/// </para>
/// <para>
/// This enables the "parameterized catalog item" pattern: a GQL or EFCore adapter can
/// declare that it needs the resolved value of an upstream catalog item before it can
/// construct and fire its query. The catalog layer retains ownership of data access;
/// steps remain pure transforms with no awareness of the filtering mechanism.
/// </para>
/// <para>
/// <strong>Pre-flight contract:</strong> Parameterized adapters whose dependencies are
/// produced by pipeline steps are not external inputs and will not be probed during
/// pre-flight. Adapters should reflect this in their <see cref="IStorageAdapter{T}.InspectShallow"/>
/// by returning a success result rather than attempting to fire a query with an unavailable parameter.
/// </para>
/// </remarks>
public interface IHasItemDependencies
{
  /// <summary>
  /// The catalog items whose data must be materialized before this adapter can execute
  /// its load operation.
  /// </summary>
  IReadOnlyList<INode> ItemDependencies { get; }
}
