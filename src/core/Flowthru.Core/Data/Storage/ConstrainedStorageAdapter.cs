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
public sealed class ConstrainedStorageAdapter<T> : IStorageAdapter<T>
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
}
