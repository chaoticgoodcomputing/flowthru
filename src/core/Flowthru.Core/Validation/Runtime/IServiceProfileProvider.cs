namespace Flowthru.Validation.Runtime;

/// <summary>
/// Resolves a <see cref="ServiceDependency"/> to its <see cref="ServiceProfile"/>.
/// The resolution seam mirrors <c>IServiceDependencyDispatcher</c>: Core ships the
/// <see cref="CompositeServiceProfileProvider"/> as the default, and extensions
/// declare capacities by registering <see cref="IServiceProfileContributor"/>s.
/// Resolution happens at pre-flight, before any step is dispatched.
/// </summary>
public interface IServiceProfileProvider
{
  /// <summary>Resolve the profile for <paramref name="dependency"/>.</summary>
  ServiceProfile Resolve(ServiceDependency dependency);
}

/// <summary>
/// The permissive fallback <see cref="IServiceProfileProvider"/> — every
/// service is unbounded and cache-affecting. Used where no DI-resolved
/// provider is available (the scheduler's parameterless path, cache
/// planning without a host); equivalent to a
/// <see cref="CompositeServiceProfileProvider"/> with no contributors.
/// </summary>
public sealed class DefaultServiceProfileProvider : IServiceProfileProvider
{
  /// <inheritdoc/>
  public ServiceProfile Resolve(ServiceDependency dependency) => ServiceProfile.Unbounded;
}

/// <summary>
/// The default <see cref="IServiceProfileProvider"/> — aggregates every
/// registered <see cref="IServiceProfileContributor"/> by conservative
/// meet. Mirrors the <c>StorageMediumResolver</c> composition pattern:
/// resolve <c>IEnumerable&lt;IServiceProfileContributor&gt;</c> and fold.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Meet semantics.</strong> A dependency recognised by no
/// contributor resolves to <see cref="ServiceProfile.Unbounded"/> (so
/// gating stays a no-op until a resource declares itself). Among the
/// contributors that <em>do</em> recognise it, the result is the
/// most-restrictive combination: <see cref="ServiceProfile.Capacity"/>
/// and <see cref="ServiceProfile.ReadCapacity"/> take the minimum, and
/// <see cref="ServiceProfile.AffectsOutputs"/> ORs to true (any source
/// that believes the dep affects outputs wins, the cache-safe direction).
/// </para>
/// </remarks>
public sealed class CompositeServiceProfileProvider : IServiceProfileProvider
{
  private readonly IReadOnlyList<IServiceProfileContributor> _contributors;

  public CompositeServiceProfileProvider(IEnumerable<IServiceProfileContributor> contributors)
  {
    _contributors = (contributors ?? throw new ArgumentNullException(nameof(contributors))).ToList();
  }

  /// <inheritdoc/>
  public ServiceProfile Resolve(ServiceDependency dependency)
  {
    if (dependency is null) throw new ArgumentNullException(nameof(dependency));

    ServiceProfile? met = null;
    foreach (var contributor in _contributors)
    {
      var contributed = contributor.Contribute(dependency);
      if (contributed is null) continue;
      met = met is null
        ? contributed
        : new ServiceProfile
        {
          Capacity = Math.Min(met.Capacity, contributed.Capacity),
          ReadCapacity = Math.Min(met.ReadCapacity, contributed.ReadCapacity),
          AffectsOutputs = met.AffectsOutputs || contributed.AffectsOutputs,
        };
    }
    return met ?? ServiceProfile.Unbounded;
  }
}
