namespace Flowthru.Data;

/// <summary>
/// Abstract base class for catalog dataset implementations (collections).
/// Provides default implementations of untyped operations using the strongly-typed methods.
/// </summary>
/// <typeparam name="T">The type of individual items in the dataset (NOT IEnumerable&lt;T&gt;)</typeparam>
/// <remarks>
/// <para>
/// <strong>Template Method Pattern:</strong> This class defines the skeleton of catalog
/// dataset operations. Derived classes implement the abstract Load() and Save() methods,
/// while this base class provides the untyped variants by delegating to the typed versions.
/// </para>
/// <para>
/// Implementations must provide:
/// - Load(): Retrieve dataset from storage (returns IEnumerable&lt;T&gt;)
/// - Save(IEnumerable&lt;T&gt; data): Persist dataset to storage
/// - Exists(): Check if dataset is present
/// </para>
/// </remarks>
public abstract class CatalogDatasetBase<T> : ICatalogDataset<T> {
  /// <summary>
  /// Creates a new catalog dataset with the specified key.
  /// </summary>
  /// <param name="key">Unique identifier for this catalog dataset</param>
  protected CatalogDatasetBase(string key) {
    Key = key ?? throw new ArgumentNullException(nameof(key));
  }

  /// <inheritdoc/>
  public string Key { get; }

  /// <inheritdoc/>
  public Type DataType => typeof(IEnumerable<T>);

  /// <inheritdoc/>
  public abstract Task<IEnumerable<T>> Load();

  /// <inheritdoc/>
  public abstract Task Save(IEnumerable<T> data);

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
  /// Default implementation casts the object to IEnumerable&lt;T&gt; and delegates to strongly-typed Save().
  /// </remarks>
  /// <exception cref="InvalidCastException">
  /// Thrown if <paramref name="data"/> cannot be cast to type IEnumerable&lt;T&gt;
  /// </exception>
  public virtual async Task SaveUntyped(object data) {
    if (data is not IEnumerable<T> typedData) {
      throw new InvalidCastException(
          $"Cannot save data of type {data?.GetType().Name ?? "null"} " +
          $"to catalog dataset expecting type IEnumerable<{typeof(T).Name}>");
    }

    await Save(typedData);
  }
}
