namespace Flowthru.Validation.Runtime;

/// <summary>
/// The resolved behavioural profile of a <see cref="ServiceRef"/> — how
/// Flowthru must treat the service across its mechanisms, on two
/// independent axes. Distinct from the <see cref="ServiceRef"/> itself,
/// which is pure identity; the profile is <em>resolved</em> per host
/// (capacity is contextual) by an <see cref="IServiceProfileProvider"/>.
/// </summary>
/// <remarks>
/// <para>
/// The two axes are orthogonal, which is why they are separate fields
/// rather than a subtype distinction (a service can affect outputs yet
/// be thread-safe, or be cache-neutral yet serial — the Python worker is
/// the latter). <see cref="AffectsOutputs"/> drives the cache planner;
/// <see cref="Capacity"/> drives the scheduler's conflict gating.
/// </para>
/// <para>
/// Default is fully permissive — unbounded concurrency, cache-affecting.
/// Capacity below <see cref="int.MaxValue"/> is <em>declared</em> by the
/// resource owner; it is never inferred.
/// </para>
/// </remarks>
public sealed record ServiceProfile
{
  /// <summary>
  /// Maximum number of steps that may concurrently hold this service.
  /// <see cref="int.MaxValue"/> means unbounded (∞); <c>1</c> is a
  /// mutex; <c>N</c> is a pool. Must be ≥ 1.
  /// </summary>
  public int Capacity { get; init; } = int.MaxValue;

  /// <summary>
  /// Whether the service's use can change a step's output values. When
  /// false (an observation surface, or a deterministic executor whose
  /// identity is otherwise fingerprinted), the cache planner treats the
  /// dependency as cache-neutral. Default <c>true</c> (conservative).
  /// </summary>
  public bool AffectsOutputs { get; init; } = true;

  /// <summary>True when the service constrains concurrency (capacity below ∞).</summary>
  public bool IsConcurrencyConstrained => Capacity < int.MaxValue;

  /// <summary>The permissive default — unbounded concurrency, cache-affecting.</summary>
  public static ServiceProfile Unbounded { get; } = new();
}
