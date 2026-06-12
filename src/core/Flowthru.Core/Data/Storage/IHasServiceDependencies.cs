namespace Flowthru.Data.Storage;

/// <summary>
/// Capability marker for an <see cref="IStorageAdapter{T}"/> backed by a
/// shared runtime resource (a database, a rate-limited endpoint) that
/// concurrent steps can contend on. The adapter declares the
/// <see cref="ServiceDependency"/>s its reads and writes touch; the item
/// surfaces them as <see cref="Flowthru.Data.Catalog.INode.ServiceDependencies"/>,
/// and the scheduler gates steps that share a finite-capacity resource
/// (ADR-0019).
/// </summary>
/// <remarks>
/// <para>
/// Sibling pattern to <see cref="IHasStorageKind"/>,
/// <see cref="IHasEfficientCount"/>, and <see cref="ISupportsFingerprint"/>:
/// an adapter opts in by implementing the interface, and
/// <see cref="Flowthru.Data.Catalog.Item{T}"/> surfaces it via a runtime
/// <c>is</c> test. File-backed adapters don't implement it — their items
/// report no dependencies and gating stays a no-op.
/// </para>
/// <para>
/// A dependency reached through an <em>input</em> item is a read
/// (<see cref="ConflictOp.Read"/>); through an <em>output</em> item, a
/// write (<see cref="ConflictOp.Write"/>). The same dependency therefore
/// serves both, and its resolved <see cref="ServiceProfile"/> carries the
/// read and write capacities independently.
/// </para>
/// </remarks>
public interface IHasServiceDependencies
{
  /// <summary>The conflict resources this adapter's reads and writes contend on.</summary>
  IReadOnlyList<ServiceDependency> ServiceDependencies { get; }
}
