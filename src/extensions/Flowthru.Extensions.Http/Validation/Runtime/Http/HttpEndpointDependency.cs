namespace Flowthru.Validation.Runtime.Http;

/// <summary>
/// Conflict identity of an HTTP endpoint a catalog item has been
/// <em>opted in</em> to throttle (ADR-0019, issue #104). HTTP mediums are
/// parallel-safe by default (idempotent reads, no shared mutable state),
/// so this dependency only exists when
/// <c>HttpOptions.MaxConcurrentRequestsPerHost</c> caps concurrency against
/// a rate-limited host. Surfaced through Core's
/// <see cref="ServiceDependency.External"/> — the medium declares it and
/// <c>ComposedStorageAdapter</c> carries it up to the item — so the
/// scheduler limits concurrent calls to the endpoint.
/// </summary>
/// <remarks>
/// Keyed on the endpoint authority (scheme + host + port), so every item
/// reading from one host shares a single conflict key and a single cap. A
/// rate limit caps all calls, so the same capacity governs the read and
/// write conflict keys (HTTP is read-only today, but the symmetry keeps
/// the cap honest if a writable medium adopts this).
/// </remarks>
internal sealed record HttpEndpointDependency(
  string Authority,
  int MaxConcurrency
) : IExtensionServiceDependency, ICapacityConstrainable
{
  /// <inheritdoc/>
  public string DagId => $"http:{Authority}";

  /// <inheritdoc/>
  public string DisplayName => $"http:{Authority}";

  /// <inheritdoc/>
  public string Category => "http";

  /// <inheritdoc/>
  public IExtensionServiceDependency ClampTo(int writeCapacity, int readCapacity) =>
    this with { MaxConcurrency = Math.Min(MaxConcurrency, Math.Min(writeCapacity, readCapacity)) };
}
