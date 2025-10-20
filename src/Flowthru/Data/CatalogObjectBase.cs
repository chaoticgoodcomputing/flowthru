namespace Flowthru.Data;

/// <summary>
/// Abstract base class for catalog object implementations (singletons).
/// Provides default implementations of untyped operations using the strongly-typed methods.
/// </summary>
/// <typeparam name="T">The type of the singleton object</typeparam>
/// <remarks>
/// <para>
/// <strong>New in v0.2.0:</strong> This class provides a base for singleton catalog entries.
/// Previously, singletons were awkwardly stored as <c>ICatalogEntry&lt;IEnumerable&lt;T&gt;&gt;</c>.
/// </para>
/// <para>
/// <strong>Use Cases:</strong>
/// - Machine learning models (LinearRegressionModel, ITransformer)
/// - Configuration objects (ModelOptions, PipelineConfig)
/// - Aggregated metrics (ModelMetrics, PerformanceReport)
/// - Reference lookups as single objects
/// </para>
/// <para>
/// <strong>Template Method Pattern:</strong> This class defines the skeleton of catalog
/// object operations. Derived classes implement the abstract Load() and Save() methods,
/// while this base class provides the untyped variants by delegating to the typed versions.
/// </para>
/// <para>
/// Implementations must provide:
/// - Load(): Retrieve object from storage (returns T)
/// - Save(T data): Persist object to storage
/// - Exists(): Check if object is present
/// </para>
/// </remarks>
public abstract class CatalogObjectBase<T> : ICatalogObject<T>
{
  /// <summary>
  /// Creates a new catalog object with the specified key.
  /// </summary>
  /// <param name="key">Unique identifier for this catalog object</param>
  protected CatalogObjectBase(string key)
  {
    Key = key ?? throw new ArgumentNullException(nameof(key));
  }

  /// <inheritdoc/>
  public string Key { get; }

  /// <inheritdoc/>
  public Type DataType => typeof(T);

  /// <inheritdoc/>
  public abstract Task<T> Load();

  /// <inheritdoc/>
  public abstract Task Save(T data);

  /// <inheritdoc/>
  public abstract Task<bool> Exists();

  /// <inheritdoc/>
  /// <remarks>
  /// For singleton objects, this returns 1 if the object exists, 0 otherwise.
  /// </remarks>
  public virtual async Task<int> GetCountAsync()
  {
    return await Exists() ? 1 : 0;
  }

  /// <inheritdoc/>
  /// <remarks>
  /// Default implementation delegates to strongly-typed Load() and boxes the result.
  /// </remarks>
  public virtual async Task<object> LoadUntyped()
  {
    var data = await Load();
    return data!;
  }

  /// <inheritdoc/>
  /// <remarks>
  /// Default implementation casts the object to T and delegates to strongly-typed Save().
  /// </remarks>
  /// <exception cref="InvalidCastException">
  /// Thrown if <paramref name="data"/> cannot be cast to type <typeparamref name="T"/>
  /// </exception>
  public virtual async Task SaveUntyped(object data)
  {
    if (data is not T typedData)
    {
      throw new InvalidCastException(
          $"Cannot save data of type {data?.GetType().Name ?? "null"} " +
          $"to catalog object expecting type {typeof(T).Name}");
    }

    await Save(typedData);
  }
}
