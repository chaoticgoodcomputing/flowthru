namespace Flowthru.Data;

/// <summary>
/// Abstract base class for read-only catalog dataset implementations (collections).
/// Provides default implementations of untyped operations using the strongly-typed Load() method.
/// </summary>
/// <typeparam name="T">The type of individual items in the dataset (NOT IEnumerable&lt;T&gt;)</typeparam>
/// <remarks>
/// <para>
/// <strong>Compile-Time Read-Only Enforcement:</strong> This class implements only <see cref="IReadableCatalogDataset{T}"/>,
/// ensuring that read-only data sources cannot be used as pipeline outputs. The Save() method is completely omitted,
/// making it impossible to accidentally write to read-only sources.
/// </para>
/// <para>
/// <strong>Template Method Pattern:</strong> This class defines the skeleton of catalog dataset read operations.
/// Derived classes implement the abstract Load() method, while this base class provides untyped variants
/// by delegating to the typed version.
/// </para>
/// <para>
/// <strong>Use Cases:</strong>
/// - Excel files (no write support)
/// - Database views and read-only queries
/// - HTTP API data sources
/// - Immutable reference data
/// - Any data source where writing is not supported
/// </para>
/// <para>
/// Implementations must provide:
/// - Load(): Retrieve dataset from storage (returns IEnumerable&lt;T&gt;)
/// - Exists(): Check if dataset is present
/// </para>
/// </remarks>
public abstract class ReadOnlyCatalogDatasetBase<T> : IReadableCatalogDataset<T> {
  /// <summary>
  /// Creates a new read-only catalog dataset with the specified key.
  /// </summary>
  /// <param name="key">Unique identifier for this catalog dataset</param>
  protected ReadOnlyCatalogDatasetBase(string key) {
    Key = key ?? throw new ArgumentNullException(nameof(key));
  }

  /// <inheritdoc/>
  public string Key { get; }

  /// <inheritdoc/>
  public Type DataType => typeof(IEnumerable<T>);

  /// <inheritdoc/>
  public abstract Task<IEnumerable<T>> Load();

  /// <inheritdoc/>
  public abstract Task<bool> Exists();

  /// <inheritdoc/>
  /// <remarks>
  /// Default implementation loads the data and counts the items.
  /// Derived classes should override this for better performance when possible
  /// (e.g., reading record count from file metadata without loading all data).
  /// </remarks>
  public virtual async Task<int> GetCountAsync() {
    if (!await Exists()) {
      return 0;
    }

    var data = await Load();
    return data.Count();
  }

  /// <inheritdoc/>
  /// <remarks>
  /// Default implementation delegates to strongly-typed Load() and boxes the result.
  /// </remarks>
  public virtual async Task<object> LoadUntyped() {
    var data = await Load();
    return data!;
  }

  /// <inheritdoc/>
  /// <remarks>
  /// <strong>Read-Only Violation:</strong> This method throws <see cref="NotSupportedException"/>
  /// because this is a read-only catalog dataset. This should never be called in properly-typed
  /// pipeline code due to compile-time enforcement, but exists for runtime safety in edge cases
  /// (e.g., reflection-based scenarios).
  /// </remarks>
  /// <exception cref="NotSupportedException">Always thrown - this is a read-only dataset</exception>
  public virtual Task SaveUntyped(object data) {
    throw new NotSupportedException(
        $"Cannot save to read-only catalog dataset '{Key}' of type {GetType().Name}. " +
        "This dataset implements IReadableCatalogDataset<T> and does not support write operations. " +
        "Use a read-write dataset implementation (CsvCatalogDataset, ParquetCatalogDataset, MemoryCatalogDataset) " +
        "if you need to save data.");
  }
}
