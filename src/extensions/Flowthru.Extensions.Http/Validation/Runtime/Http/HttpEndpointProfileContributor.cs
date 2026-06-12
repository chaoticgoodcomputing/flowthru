namespace Flowthru.Validation.Runtime.Http;

/// <summary>
/// Resolves an <see cref="HttpEndpointDependency"/> to its
/// <see cref="ServiceProfile"/> — the opt-in concurrency cap on a
/// rate-limited HTTP host (ADR-0019, #104). Registered by <c>UseHttp()</c>
/// and aggregated by Core's <c>CompositeServiceProfileProvider</c>;
/// recognises only HTTP endpoint dependencies and stays silent otherwise.
/// </summary>
/// <remarks>
/// A rate limit caps every call to the host, so the same
/// <see cref="HttpEndpointDependency.MaxConcurrency"/> governs both the
/// read and write conflict keys.
/// </remarks>
internal sealed class HttpEndpointProfileContributor : IServiceProfileContributor
{
  /// <inheritdoc/>
  public ServiceProfile? Contribute(ServiceDependency dependency) =>
    dependency is ServiceDependency.External { Cause: HttpEndpointDependency http }
      ? new ServiceProfile { Capacity = http.MaxConcurrency, ReadCapacity = http.MaxConcurrency }
      : null;
}
