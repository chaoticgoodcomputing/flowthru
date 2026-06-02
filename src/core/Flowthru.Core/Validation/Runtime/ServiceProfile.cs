namespace Flowthru.Validation.Runtime;

/// <summary>
/// The operation by which a step touches a resource — the op-class half
/// of a conflict key. A step <see cref="Use"/>s its own injected
/// services; it <see cref="Read"/>s the items it loads and
/// <see cref="Write"/>s the items it saves. The scheduler keys conflicts
/// by <c>(op, resource)</c>, so reads and writes of the same resource are
/// distinct — the SQLite reality of many concurrent readers but one writer.
/// </summary>
public enum ConflictOp
{
  /// <summary>A service injected into the step's transform.</summary>
  Use,
  /// <summary>A resource read via an input item.</summary>
  Read,
  /// <summary>A resource written via an output item.</summary>
  Write,
}

/// <summary>
/// The resolved behavioural profile of a <see cref="ServiceDependency"/> — how
/// Flowthru must treat the service across its mechanisms, on two
/// independent axes. Distinct from the <see cref="ServiceDependency"/> itself,
/// which is pure identity; the profile is <em>resolved</em> per host
/// (capacity is contextual) by an <see cref="IServiceProfileProvider"/>.
/// </summary>
/// <remarks>
/// <para>
/// The two axes are orthogonal, which is why they are separate fields
/// rather than a subtype distinction (a service can affect outputs yet
/// be thread-safe, or be cache-neutral yet serial — the Python worker is
/// the latter). <see cref="AffectsOutputs"/> drives the cache planner;
/// <see cref="Capacity"/> / <see cref="ReadCapacity"/> drive the
/// scheduler's conflict gating.
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
  /// Maximum concurrent holders for <see cref="ConflictOp.Use"/> and
  /// <see cref="ConflictOp.Write"/> operations. <see cref="int.MaxValue"/>
  /// is unbounded (∞); <c>1</c> is a mutex; <c>N</c> is a pool. Must be ≥ 1.
  /// </summary>
  public int Capacity { get; init; } = int.MaxValue;

  /// <summary>
  /// Maximum concurrent <see cref="ConflictOp.Read"/> holders. Default
  /// unbounded — concurrent reads of a shared resource usually don't
  /// conflict (e.g. SQLite allows many readers). A resource that can't be
  /// read concurrently lowers this.
  /// </summary>
  public int ReadCapacity { get; init; } = int.MaxValue;

  /// <summary>
  /// Whether the service's use can change a step's output values. When
  /// false (an observation surface, or a deterministic executor whose
  /// identity is otherwise fingerprinted), the cache planner treats the
  /// dependency as cache-neutral. Default <c>true</c> (conservative).
  /// </summary>
  public bool AffectsOutputs { get; init; } = true;

  /// <summary>The capacity governing <paramref name="op"/>.</summary>
  public int CapacityFor(ConflictOp op) =>
    op == ConflictOp.Read ? ReadCapacity : Capacity;

  /// <summary>The permissive default — unbounded concurrency, cache-affecting.</summary>
  public static ServiceProfile Unbounded { get; } = new();
}
