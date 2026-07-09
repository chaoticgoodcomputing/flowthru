using Flowthru.Data.Storage;

namespace Flowthru.Data.Catalog;

/// <summary>
/// Standard catalog item implementation that delegates to a storage
/// adapter. Constructed via the smart constructors in
/// <see cref="ItemFactory"/> (<c>Json</c>, <c>Memory</c>, etc.) — Flow
/// and Catalog Developers don't typically name <see cref="Item{T}"/>
/// directly.
/// </summary>
/// <typeparam name="T">The data type stored at this item.</typeparam>
public sealed class Item<T> : IItem<T>
{
  private readonly IStorageAdapter<T> _storage;

  public Item(string label, IStorageAdapter<T> storage)
  {
    Label = label ?? throw new ArgumentNullException(nameof(label));
    _storage = storage ?? throw new ArgumentNullException(nameof(storage));
  }

  /// <inheritdoc/>
  public string Label { get; }

  /// <inheritdoc/>
  public NodeTraits Traits => new() { CanInspect = true };

  /// <summary>The underlying storage adapter (exposed for advanced consumers).</summary>
  public IStorageAdapter<T> Storage => _storage;

  /// <inheritdoc/>
  public FlowIO<T> Load() => _storage.Load();

  /// <inheritdoc/>
  public FlowIO<FlowUnit> Save(T data) => _storage.Save(data);

  /// <inheritdoc/>
  public FlowIO<bool> Exists() => _storage.Exists();

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectShallow(int sampleSize = 100) =>
    _storage.InspectShallow(sampleSize);

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectDeep() => _storage.InspectDeep();

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectTarget() => _storage.InspectTarget();

  /// <inheritdoc/>
  public FlowIO<ValidationResult> Validate() => InspectShallow();

  /// <inheritdoc/>
  public bool HasEfficientCount => _storage is IHasEfficientCount;

  /// <inheritdoc/>
  public FlowIO<int> GetCountAsync() =>
    _storage is IHasEfficientCount counter
      ? counter.GetCountAsync()
      : FlowIO.Fail<int>(new Validation.Runtime.RuntimeError.External(
          $"Item[{Label}].GetCountAsync",
          new InvalidOperationException(
            $"Storage adapter for item '{Label}' (type {_storage.GetType().Name}) "
            + "does not implement IHasEfficientCount.")));

  /// <inheritdoc/>
  public FlowIO<string>? TryGetFingerprint() =>
    _storage is ISupportsFingerprint fingerprintable ? fingerprintable.Fingerprint() : null;

  /// <inheritdoc/>
  public ISupportsBulkExport? TryGetBulkExport() => _storage as ISupportsBulkExport;

  /// <inheritdoc/>
  public ISupportsBulkImport? TryGetBulkImport() => _storage as ISupportsBulkImport;

  /// <inheritdoc/>
  public string? StorageKind =>
    _storage is IHasStorageKind kinded ? kinded.StorageKind : null;

  /// <inheritdoc/>
  /// <remarks>
  /// Surfaces the conflict resources an adapter declares via
  /// <see cref="IHasServiceDependencies"/> (a database scope, a
  /// rate-limited endpoint) so the scheduler can gate concurrent steps
  /// that share them. Adapters that don't implement the capability —
  /// every file-backed format — report none, and gating stays a no-op.
  /// </remarks>
  public IReadOnlyList<Validation.Runtime.ServiceDependency> ServiceDependencies =>
    _storage is IHasServiceDependencies declarer
      ? declarer.ServiceDependencies
      : Array.Empty<Validation.Runtime.ServiceDependency>();
}
