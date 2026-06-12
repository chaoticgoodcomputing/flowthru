using Flowthru.Data.Storage.Gql;
using Flowthru.Validation.Runtime;
using Flowthru.Validation.Runtime.Gql;

namespace Flowthru.Data.Catalog;

/// <summary>
/// Opt-in concurrency throttling for GraphQL catalog items (ADR-0019,
/// issue #104). GQL adapters are parallel-safe by default (capacity ∞);
/// a catalog author calls <see cref="WithGqlConcurrency{T}"/> on an item
/// bound to a rate-limited endpoint to cap concurrent calls.
/// </summary>
public static class GqlThrottleExtensions
{
  /// <summary>
  /// Cap concurrent GraphQL calls (queries and mutations) against
  /// <paramref name="endpoint"/> to <paramref name="maxConcurrency"/>.
  /// Items sharing the same <paramref name="endpoint"/> key are throttled
  /// together; the cap is enforced by the scheduler once
  /// <c>UseGql()</c> has registered the resolving contributor.
  /// </summary>
  /// <remarks>
  /// <para>
  /// A pure-function combinator (the same shape as
  /// <c>IItem&lt;T&gt;.Constrain</c>): it returns a new item whose adapter
  /// declares the endpoint as a conflict resource, leaving the original
  /// unchanged. The endpoint key is author-supplied because a
  /// StrawberryShake client's endpoint is opaque behind the operation
  /// delegate — use the same string for every item that hits one endpoint.
  /// </para>
  /// </remarks>
  /// <param name="item">The GQL-backed item to throttle.</param>
  /// <param name="endpoint">Stable identity of the endpoint — the shared conflict key.</param>
  /// <param name="maxConcurrency">Maximum concurrent calls to the endpoint. Must be ≥ 1.</param>
  public static IItem<T> WithGqlConcurrency<T>(
    this IItem<T> item,
    string endpoint,
    int maxConcurrency
  )
  {
    if (item is null) throw new ArgumentNullException(nameof(item));
    if (string.IsNullOrWhiteSpace(endpoint))
      throw new ArgumentException("Endpoint key cannot be null or whitespace.", nameof(endpoint));
    if (maxConcurrency < 1)
      throw new ArgumentOutOfRangeException(nameof(maxConcurrency), maxConcurrency,
        "Concurrency cap must be at least 1.");

    if (item is not Item<T> concrete)
    {
      throw new ArgumentException(
        $"WithGqlConcurrency requires the item to expose its underlying IStorageAdapter<{typeof(T).Name}>. "
        + $"Item '{item.Label}' (type {item.GetType().Name}) does not — build it through the "
        + "GQL ItemFactory smart constructors before throttling.",
        nameof(item));
    }

    var dependency = new ServiceDependency.External(
      new GqlEndpointDependency(endpoint, maxConcurrency));
    return new Item<T>(item.Label, new ThrottledGqlAdapter<T>(concrete.Storage, dependency));
  }
}
