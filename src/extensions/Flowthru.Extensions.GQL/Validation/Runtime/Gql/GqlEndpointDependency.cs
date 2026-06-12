namespace Flowthru.Validation.Runtime.Gql;

/// <summary>
/// Conflict identity of a GraphQL endpoint a catalog item has been
/// <em>opted in</em> to throttle (ADR-0019, issue #104). GQL adapters are
/// parallel-safe by default — reads are idempotent and there's no shared
/// mutable state — so this dependency only exists when a catalog author
/// caps concurrency against a rate-limited endpoint via
/// <c>WithGqlConcurrency</c>. Surfaced through Core's
/// <see cref="ServiceDependency.External"/> so the scheduler limits
/// concurrent calls (query or mutation) to the named endpoint.
/// </summary>
/// <remarks>
/// <para>
/// The endpoint identity is author-supplied: a StrawberryShake client's
/// endpoint is opaque behind the operation delegate, so Flowthru can't
/// derive it. Items sharing the same endpoint key share one conflict key
/// and are throttled together. A rate limit caps <em>all</em> calls, so
/// the same capacity governs reads and writes alike.
/// </para>
/// </remarks>
internal sealed record GqlEndpointDependency(
  string Endpoint,
  int MaxConcurrency
) : IExtensionServiceDependency, ICapacityConstrainable
{
  /// <inheritdoc/>
  public string DagId => $"gql:{Endpoint}";

  /// <inheritdoc/>
  public string DisplayName => $"gql:{Endpoint}";

  /// <inheritdoc/>
  public string Category => "gql";

  /// <inheritdoc/>
  public IExtensionServiceDependency ClampTo(int writeCapacity, int readCapacity) =>
    this with { MaxConcurrency = Math.Min(MaxConcurrency, Math.Min(writeCapacity, readCapacity)) };
}
