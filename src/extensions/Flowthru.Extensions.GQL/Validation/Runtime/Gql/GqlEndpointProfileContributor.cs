namespace Flowthru.Validation.Runtime.Gql;

/// <summary>
/// Resolves a <see cref="GqlEndpointDependency"/> to its
/// <see cref="ServiceProfile"/> — the opt-in concurrency cap a catalog
/// author placed on a rate-limited GraphQL endpoint (ADR-0019, #104).
/// Registered by <c>UseGql()</c> and aggregated by Core's
/// <c>CompositeServiceProfileProvider</c>; recognises only GQL endpoint
/// dependencies and stays silent on everything else.
/// </summary>
/// <remarks>
/// A rate limit caps every call to the endpoint, so the same
/// <see cref="GqlEndpointDependency.MaxConcurrency"/> governs both the
/// read (query) and write (mutation) conflict keys.
/// </remarks>
internal sealed class GqlEndpointProfileContributor : IServiceProfileContributor
{
  /// <inheritdoc/>
  public ServiceProfile? Contribute(ServiceDependency dependency) =>
    dependency is ServiceDependency.External { Cause: GqlEndpointDependency gql }
      ? new ServiceProfile { Capacity = gql.MaxConcurrency, ReadCapacity = gql.MaxConcurrency }
      : null;
}
