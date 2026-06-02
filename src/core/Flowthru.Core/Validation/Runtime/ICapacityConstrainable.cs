namespace Flowthru.Validation.Runtime;

/// <summary>
/// An <see cref="IExtensionServiceDependency"/> whose conflict capacities
/// can be ratcheted down when the item that declares it is constrained
/// (ADR-0019). The dependency owns the narrowing — it knows which of its
/// fields are capacities — so the generic
/// <c>ConstrainedStorageAdapter</c> can lower an inherited resource's
/// concurrency without knowing the extension's concrete dependency type.
/// </summary>
/// <remarks>
/// Implemented by extension dependencies that carry per-operation
/// capacities (e.g. an EF Core database). A dependency that doesn't
/// implement this is passed through a constraint unchanged — constraining
/// an item's traits then has no effect on that dependency's gating.
/// </remarks>
public interface ICapacityConstrainable
{
  /// <summary>
  /// A copy of this dependency whose write and read capacities are no
  /// greater than the supplied limits. Capacities already below a limit
  /// are kept — this only lowers, mirroring the one-way constraint
  /// ratchet (<see cref="int.MaxValue"/> means "no limit from this side").
  /// </summary>
  IExtensionServiceDependency ClampTo(int writeCapacity, int readCapacity);
}
