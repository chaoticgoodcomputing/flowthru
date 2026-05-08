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
}
