using Flowthru.Core.Data.Capabilities;
using Flowthru.Core.Data.Storage;
using Flowthru.Core.Data.Validation;
using Flowthru.Core.Effects;
using Flowthru.Core.Graph;

namespace Flowthru.Core.Data;

/// <summary>
/// Standard catalog item implementation that delegates to a storage adapter.
/// </summary>
/// <typeparam name="T">The data type (container with rows)</typeparam>
/// <remarks>
/// <para>
/// <strong>Delegation Pattern:</strong>
/// </para>
/// <para>
/// This class is a thin wrapper that delegates all operations to an <see cref="IStorageAdapter{T}"/>.
/// The storage adapter handles the actual I/O logic, while this class provides:
/// - ICatalogItem interface implementation
/// - Identity for DAG dependency resolution (via Key)
/// - Type erasure for Flow heterogeneous collections
/// </para>
/// <para>
/// <strong>Construction:</strong>
/// </para>
/// <para>
/// Typically created via static factory methods in <see cref="ItemFactory"/>:
/// </para>
/// <code>
/// var item = CatalogItemFactory.Csv&lt;CompanySchema&gt;("companies", "data.csv");
/// // Returns: ICatalogItem&lt;IEnumerable&lt;CompanySchema&gt;&gt;
/// </code>
/// <para>
/// <strong>Composition vs Inheritance:</strong>
/// </para>
/// <para>
/// Previous design: Inheritance hierarchy (CsvCatalogItem, JsonCatalogItem, etc.)
/// New design: Single class + composed storage adapter
/// </para>
/// <para>
/// Benefits:
/// - No class explosion for format × container combinations
/// - Custom storage via IStorageAdapter implementation
/// - Clear separation of concerns
/// </para>
/// <para>
/// <strong>Capability Forwarding:</strong>
/// </para>
/// <para>
/// The underlying storage adapter provides inspection methods, which this catalog
/// item automatically forwards. All storage adapters are required to implement inspection.
/// </para>
/// </remarks>
public sealed class Item<T> : IItem<T>
{
  private readonly IStorageAdapter<T> _storage;
  private InspectionLevel? _preferredInspectionLevel;
  private StorageTraits? _effectiveTraits;
  private string? _owningCatalogLabel;

  /// <summary>
  /// Creates a new catalog item with the specified key and storage adapter.
  /// </summary>
  /// <param name="label">Unique identifier for this catalog item</param>
  /// <param name="storage">Storage adapter that handles I/O operations</param>
  public Item(string label, IStorageAdapter<T> storage)
  {
    this.Label = label ?? throw new ArgumentNullException(nameof(label));
    _storage = storage ?? throw new ArgumentNullException(nameof(storage));
  }

  /// <inheritdoc/>
  public string Label { get; }

  /// <inheritdoc/>
  public Type DataType => typeof(T);

  /// <inheritdoc/>
  public InspectionLevel? PreferredInspectionLevel => _preferredInspectionLevel;

  /// <inheritdoc/>
  public string? OwningCatalogLabel => _owningCatalogLabel;

  /// <summary>Sets the owning catalog label. First-write-wins; subsequent calls are no-ops.</summary>
  internal void SetOwningCatalog(string label) => _owningCatalogLabel ??= label;

  /// <summary>
  /// Gets the effective storage traits for this catalog item.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This returns the adapter's traits merged with any user-specified constraints
  /// applied via <see cref="Constrain"/>.
  /// </para>
  /// <para>
  /// Used by Flow validation to enforce read-only constraints, network requirements, etc.
  /// </para>
  /// </remarks>
  public StorageTraits Traits => _effectiveTraits ?? _storage.Traits;

  /// <inheritdoc/>
  NodeTraits INode.Traits => Traits;

  /// <inheritdoc/>
  FlowIO<object> INode.ProduceUntyped() => LoadUntyped();

  /// <inheritdoc/>
  FlowIO<FlowUnit> INode.ConsumeUntyped(object data) => SaveUntyped(data);

  /// <inheritdoc/>
  public FlowIO<T> Load() => _storage.Load();

  /// <inheritdoc/>
  public FlowIO<FlowUnit> Save(T data) => _storage.Save(data);

  /// <inheritdoc/>
  public FlowIO<bool> Exists() => _storage.Exists();

  /// <inheritdoc/>
  public FlowIO<object> LoadUntyped() => Load().Map(data => (object)data!);

  /// <inheritdoc/>
  public FlowIO<FlowUnit> SaveUntyped(object data)
  {
    // Try direct cast
    if (data is T typedData)
    {
      return Save(typedData);
    }

    // Type mismatch - fail with descriptive error
    return FlowIO.Fail<FlowUnit>(
      new Exception(
        $"Type mismatch: Cannot save data of type '{data?.GetType().Name ?? "null"}' "
          + $"to catalog item '{Label}' expecting type '{typeof(T).Name}'"
      )
    );
  }

  /// <inheritdoc/>
  public FlowIO<int> GetCountAsync()
  {
    // For collection types, try to get actual count
    // For singletons, return 1 if exists
    var type = typeof(T);

    if (IsCollectionType(type))
    {
      return from exists in Exists()
        from count in exists ? LoadAndCount() : FlowIO.Pure(0)
        select count;
    }
    else
    {
      return from exists in Exists() select exists ? 1 : 0;
    }
  }

  private FlowIO<int> LoadAndCount()
  {
    // If the adapter can count server-side (e.g. SQL COUNT(*)), use it —
    // avoids materializing the full dataset just for diagnostic logging.
    if (_storage is IHasEfficientCount efficient)
      return efficient.GetCountAsync();

    return Load()
      .Map(data =>
      {
        if (data is System.Collections.IEnumerable enumerable and not string)
        {
          return enumerable.Cast<object>().Count();
        }
        return 0;
      });
  }

  private static bool IsCollectionType(Type type)
  {
    if (type == typeof(string))
    {
      return false;
    }

    return typeof(System.Collections.IEnumerable).IsAssignableFrom(type);
  }

  /// <summary>
  /// Sets the preferred inspection level for this catalog item.
  /// </summary>
  /// <param name="level">The inspection level to use</param>
  /// <returns>This catalog item for method chaining</returns>
  /// <remarks>
  /// <para>
  /// Used to configure how this item should be validated before Flow execution.
  /// </para>
  /// <para>
  /// Example:
  /// </para>
  /// <code>
  /// var item = CatalogItemFactory.Csv&lt;Company&gt;("companies", "data.csv")
  ///     .WithInspectionLevel(InspectionLevel.Deep);
  /// </code>
  /// </remarks>
  public Item<T> WithInspectionLevel(InspectionLevel level)
  {
    _preferredInspectionLevel = level;
    return this;
  }

  /// <summary>
  /// Applies user-specified constraints to the storage traits.
  /// </summary>
  /// <param name="constraintFn">Function that modifies traits to apply constraints</param>
  /// <returns>This catalog item for method chaining</returns>
  /// <remarks>
  /// <para>
  /// <strong>One-Way Ratchet:</strong> Constraints can only tighten, never loosen.
  /// You cannot grant capabilities the adapter doesn't support.
  /// </para>
  /// <para>
  /// <strong>Valid Constraints:</strong>
  /// </para>
  /// <list type="bullet">
  /// <item>Making a writable adapter read-only: <c>t => t with { CanWrite = false }</c></item>
  /// <item>Marking a local file as non-inspectable: <c>t => t with { CanInspect = false }</c></item>
  /// </list>
  /// <para>
  /// <strong>Invalid Constraints (will throw):</strong>
  /// </para>
  /// <list type="bullet">
  /// <item>Making a read-only adapter writable: <c>t => t with { CanWrite = true }</c></item>
  /// <item>Removing network requirement: <c>t => t with { RequiresNetwork = false }</c></item>
  /// </list>
  /// <para>
  /// <strong>Example:</strong>
  /// </para>
  /// <code>
  /// public ICatalogItem&lt;IEnumerable&lt;Company&gt;&gt; ReferenceData =>
  ///     CreateItem(() => CatalogItemFactory.Enumerable.Csv&lt;Company&gt;(
  ///         "ref_data", $"{_basePath}/reference.csv")
  ///         .Constrain(t => t with { CanWrite = false }));
  /// </code>
  /// </remarks>
  /// <exception cref="InvalidOperationException">
  /// Thrown when attempting to loosen a constraint (grant a capability the adapter doesn't support)
  /// </exception>
  public Item<T> Constrain(Func<StorageTraits, StorageTraits> constraintFn)
  {
    if (constraintFn == null)
    {
      throw new ArgumentNullException(nameof(constraintFn));
    }

    var currentTraits = Traits;
    var proposedTraits = constraintFn(currentTraits);

    // Validate one-way ratchet: can only tighten, never loosen
    ValidateConstraints(currentTraits, proposedTraits);

    _effectiveTraits = proposedTraits;
    return this;
  }

  /// <summary>
  /// Validates that proposed traits only tighten constraints, never loosen them.
  /// </summary>
  private static void ValidateConstraints(StorageTraits current, StorageTraits proposed)
  {
    // Check each boolean trait - proposed can only be more restrictive
    if (proposed.CanRead && !current.CanRead)
    {
      throw new InvalidOperationException(
        "Cannot grant CanRead capability - the underlying adapter does not support reading."
      );
    }

    if (proposed.CanWrite && !current.CanWrite)
    {
      throw new InvalidOperationException(
        "Cannot grant CanWrite capability - the underlying adapter does not support writing."
      );
    }

    if (proposed.CanInspect && !current.CanInspect)
    {
      throw new InvalidOperationException(
        "Cannot grant CanInspect capability - the underlying adapter does not support inspection."
      );
    }

    if (proposed.IsPersistent && !current.IsPersistent)
    {
      throw new InvalidOperationException(
        "Cannot grant IsPersistent capability - the underlying adapter is not persistent."
      );
    }

    if (!proposed.RequiresNetwork && current.RequiresNetwork)
    {
      throw new InvalidOperationException(
        "Cannot remove RequiresNetwork constraint - the underlying adapter requires network access."
      );
    }

    if (proposed.CanStream && !current.CanStream)
    {
      throw new InvalidOperationException(
        "Cannot grant CanStream capability - the underlying adapter does not support streaming."
      );
    }

    if (proposed.CanAppend && !current.CanAppend)
    {
      throw new InvalidOperationException(
        "Cannot grant CanAppend capability - the underlying adapter does not support appending."
      );
    }

    if (proposed.IsTransactional && !current.IsTransactional)
    {
      throw new InvalidOperationException(
        "Cannot grant IsTransactional capability - the underlying adapter is not transactional."
      );
    }
  }

  /// <inheritdoc/>
  /// <remarks>
  /// Forwards the call directly to the underlying storage adapter.
  /// All storage adapters must implement inspection.
  /// </remarks>
  public FlowIO<ValidationResult> InspectShallow(int sampleSize = 100)
  {
    return _storage.InspectShallow(sampleSize);
  }

  /// <inheritdoc/>
  /// <remarks>
  /// Forwards the call directly to the underlying storage adapter.
  /// All storage adapters must implement inspection.
  /// </remarks>
  public FlowIO<ValidationResult> InspectDeep()
  {
    return _storage.InspectDeep();
  }
}
