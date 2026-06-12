using Flowthru.Validation.Runtime;
using Flowthru.Validation.Runtime.Http;

namespace Flowthru.Data.Storage.Http.Internal;

/// <summary>
/// Builds the scheduler conflict dependency for an HTTP medium (ADR-0019,
/// #104). Shared by <see cref="HttpStorageMedium"/> and
/// <see cref="CachedHttpStorageMedium"/> so both key the endpoint
/// identically. Returns nothing when concurrency is unbounded — HTTP's
/// default — so a non-throttled item declares no dependency and the
/// scheduler never gates it.
/// </summary>
internal static class HttpEndpointConflict
{
  public static IReadOnlyList<ServiceDependency> For(Uri uri, int maxConcurrentRequestsPerHost) =>
    maxConcurrentRequestsPerHost >= int.MaxValue
      ? Array.Empty<ServiceDependency>()
      : new ServiceDependency[]
        {
          new ServiceDependency.External(new HttpEndpointDependency(
            uri.GetLeftPart(UriPartial.Authority), maxConcurrentRequestsPerHost)),
        };
}
