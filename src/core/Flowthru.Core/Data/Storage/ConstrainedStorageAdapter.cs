namespace Flowthru.Data.Storage;

/// <summary>
/// Wraps an <see cref="IStorageAdapter{T}"/> with a narrowed
/// <see cref="StorageTraits"/> surface. Operations forbidden by the
/// new traits short-circuit at the <see cref="FlowIO{A}"/> boundary
/// as <see cref="RuntimeError.ConstraintViolated"/> rather than
/// reaching the inner adapter.
/// </summary>
/// <remarks>
/// <para>
/// Constructed via <c>IItem&lt;T&gt;.Constrain(...)</c>; consumers
/// don't typically name this type directly.
/// </para>
/// <para>
/// <strong>One-way ratchet.</strong> The constructor validates that
/// the constrained traits only narrow — never widen — relative to
/// the inner adapter's claims. A widening attempt fails fast with
/// <see cref="ArgumentException"/> at catalog wire-up; bad
/// constraints are programming errors and surface them at the
/// declaration site, not mid-flow.
/// </para>
/// </remarks>
public sealed class ConstrainedStorageAdapter<T>
  : IStorageAdapter<T>, ISupportsFingerprint, IHasServiceDependencies
{
  private readonly IStorageAdapter<T> _inner;
  private readonly string _itemLabel;

  internal ConstrainedStorageAdapter(
    IStorageAdapter<T> inner,
    StorageTraits constrainedTraits,
    string itemLabel
  )
  {
    _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    _itemLabel = itemLabel ?? throw new ArgumentNullException(nameof(itemLabel));
    AssertOneWayRatchet(inner.Traits, constrainedTraits, itemLabel);
    Traits = constrainedTraits;
  }

  /// <inheritdoc/>
  public StorageTraits Traits { get; }

  /// <summary>The wrapped adapter — exposed for advanced consumers and testing.</summary>
  public IStorageAdapter<T> Inner => _inner;

  /// <inheritdoc/>
  public FlowIO<T> Load() =>
    Traits.CanRead
      ? _inner.Load()
      : FlowIO.Fail<T>(new RuntimeError.ConstraintViolated(_itemLabel, "Load", nameof(StorageTraits.CanRead)));

  /// <inheritdoc/>
  public FlowIO<FlowUnit> Save(T data) =>
    Traits.CanWrite
      ? _inner.Save(data)
      : FlowIO.Fail<FlowUnit>(new RuntimeError.ConstraintViolated(_itemLabel, "Save", nameof(StorageTraits.CanWrite)));

  /// <inheritdoc/>
  public FlowIO<bool> Exists() => _inner.Exists();

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectShallow(int sampleSize) =>
    _inner.InspectShallow(sampleSize);

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectDeep() => _inner.InspectDeep();

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectTarget() =>
    Traits.CanWrite
      ? _inner.InspectTarget()
      : FlowIO.Pure(ValidationResult.Success());

  /// <inheritdoc/>
  /// <remarks>
  /// Delegates to the inner adapter when it implements
  /// <see cref="ISupportsFingerprint"/>; otherwise surfaces a FlowIO
  /// failure so the cache plan records "fingerprint unknown" and
  /// downgrades the dependent step to a cache miss.
  /// </remarks>
  public FlowIO<string> Fingerprint() =>
    _inner is ISupportsFingerprint fingerprintable
      ? fingerprintable.Fingerprint()
      : FlowIO.Fail<string>(new RuntimeError.External(
          $"ConstrainedStorageAdapter.Fingerprint[{_itemLabel}]",
          new InvalidOperationException(
            $"Inner adapter '{_inner.GetType().Name}' does not implement "
            + "ISupportsFingerprint; constrained item cannot produce a leaf fingerprint."
          )));

  // ── IHasServiceDependencies ──────────────────────────────────────────

  /// <inheritdoc/>
  /// <remarks>
  /// Delegates to the inner adapter so a constrained item keeps the
  /// conflict resources it inherited — constraining (e.g. to read-only)
  /// must not silently drop the scheduler's gating. Tightening the
  /// inherited <em>capacity</em> through the constraint is layered on in a
  /// follow-up; here the inner adapter's declared capacity flows through
  /// unchanged.
  /// </remarks>
  public IReadOnlyList<ServiceDependency> ServiceDependencies =>
    _inner is IHasServiceDependencies declarer
      ? declarer.ServiceDependencies
      : Array.Empty<ServiceDependency>();

  // ── One-way-ratchet enforcement ──────────────────────────────────────

  private static void AssertOneWayRatchet(
    StorageTraits original, StorageTraits narrowed, string itemLabel
  )
  {
    // Each trait can only narrow (true → false). Widening (false → true)
    // is a constraint-loosening attempt — bad-by-construction, fail loud.
    AssertNarrowing(nameof(StorageTraits.CanRead), original.CanRead, narrowed.CanRead, itemLabel);
    AssertNarrowing(nameof(StorageTraits.CanWrite), original.CanWrite, narrowed.CanWrite, itemLabel);
    AssertNarrowing(nameof(StorageTraits.IsPersistent), original.IsPersistent, narrowed.IsPersistent, itemLabel);
    AssertNarrowing(nameof(StorageTraits.CanStream), original.CanStream, narrowed.CanStream, itemLabel);
    AssertNarrowing(nameof(StorageTraits.CanAppend), original.CanAppend, narrowed.CanAppend, itemLabel);
    AssertNarrowing(nameof(StorageTraits.IsTransactional), original.IsTransactional, narrowed.IsTransactional, itemLabel);
    // Capacity narrows by lowering: a constraint can tighten concurrency
    // (∞ → pool size → 1) but never loosen it past what the medium claims.
    AssertCapacityNarrowing(nameof(StorageTraits.WriteCapacity), original.WriteCapacity, narrowed.WriteCapacity, itemLabel);
    AssertCapacityNarrowing(nameof(StorageTraits.ReadCapacity), original.ReadCapacity, narrowed.ReadCapacity, itemLabel);
  }

  private static void AssertNarrowing(string traitName, bool original, bool narrowed, string itemLabel)
  {
    if (narrowed && !original)
    {
      throw new ArgumentException(
        $"Constraint on item '{itemLabel}' attempted to widen trait '{traitName}' "
        + $"from false to true. Constraints are a one-way ratchet — they can only narrow, never widen."
      );
    }
  }

  private static void AssertCapacityNarrowing(string traitName, int original, int narrowed, string itemLabel)
  {
    if (narrowed > original)
    {
      throw new ArgumentException(
        $"Constraint on item '{itemLabel}' attempted to raise capacity '{traitName}' "
        + $"from {original} to {narrowed}. Capacity constraints are a one-way ratchet — "
        + "they can only lower concurrency, never raise it past what the medium declares."
      );
    }
  }
}
