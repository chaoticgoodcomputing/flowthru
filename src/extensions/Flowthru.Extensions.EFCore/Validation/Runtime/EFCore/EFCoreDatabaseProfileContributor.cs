namespace Flowthru.Validation.Runtime.EFCore;

/// <summary>
/// Resolves an <see cref="EFCoreDatabaseDependency"/> to its
/// <see cref="ServiceProfile"/> — the read and write capacities the
/// EF Core adapter declared for the database (ADR-0019). Registered by
/// <c>UseEFCore()</c> and aggregated by Core's
/// <c>CompositeServiceProfileProvider</c> alongside every other
/// extension's contributor; it recognises only EF Core database
/// dependencies and stays silent on everything else.
/// </summary>
/// <remarks>
/// The capacities ride on the dependency (the adapter set them from the
/// provider), so this contributor is a pure translation. SQLite resolves
/// to write capacity 1 / read capacity ∞ — concurrent writers serialize,
/// readers parallelize; other providers resolve to unbounded and never
/// gate. <see cref="ServiceProfile.AffectsOutputs"/> is irrelevant here:
/// the cache planner consults a <em>step's</em> own dependencies, and a
/// database dependency only ever reaches the scheduler through an item.
/// </remarks>
internal sealed class EFCoreDatabaseProfileContributor : IServiceProfileContributor
{
  /// <inheritdoc/>
  public ServiceProfile? Contribute(ServiceDependency dependency) =>
    dependency is ServiceDependency.External { Cause: EFCoreDatabaseDependency db }
      ? new ServiceProfile { Capacity = db.WriteCapacity, ReadCapacity = db.ReadCapacity }
      : null;
}
