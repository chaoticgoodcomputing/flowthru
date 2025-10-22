namespace Flowthru.Data;

/// <summary>
/// Abstract base class for read-only catalog object implementations (singletons).
/// Provides default implementations of untyped operations using the strongly-typed Load() method.
/// </summary>
/// <typeparam name="T">The type of the singleton object</typeparam>
/// <remarks>
/// <para>
/// <strong>Compile-Time Read-Only Enforcement:</strong> This class implements only <see cref="IReadableCatalogObject{T}"/>,
/// ensuring that read-only objects cannot be used as pipeline outputs. The Save() method is completely omitted,
/// making it impossible to accidentally write to read-only sources.
/// </para>
/// <para>
/// <strong>Template Method Pattern:</strong> This class defines the skeleton of catalog object read operations.
/// Derived classes implement the abstract Load() method, while this base class provides untyped variants
/// by delegating to the typed version.
/// </para>
/// <para>
/// <strong>Use Cases:</strong>
/// - Read-only configuration files
/// - Immutable reference data objects
/// - Pre-trained models loaded from external sources
/// - Any singleton object where writing is not supported
/// </para>
/// <para>
/// Implementations must provide:
/// - Load(): Retrieve object from storage (returns T)
/// - Exists(): Check if object is present
/// </para>
/// </remarks>
public abstract class ReadOnlyCatalogObjectBase<T> : IReadableCatalogObject<T> {
  /// <summary>
  /// Creates a new read-only catalog object with the specified key.
  /// </summary>
  /// <param name="key">Unique identifier for this catalog object</param>
  protected ReadOnlyCatalogObjectBase(string key) {
    Key = key ?? throw new ArgumentNullException(nameof(key));
  }

  /// <inheritdoc/>
  public string Key { get; }

  /// <inheritdoc/>
  public Type DataType => typeof(T);

  /// <inheritdoc/>
  public abstract Task<T> Load();

  /// <inheritdoc/>
  public abstract Task<bool> Exists();

  /// <inheritdoc/>
  /// <remarks>
  /// Default implementation returns 1 if the object exists, 0 otherwise.
  /// Singleton objects always have a count of 0 or 1.
  /// </remarks>
  public virtual async Task<int> GetCountAsync() {
    return await Exists() ? 1 : 0;
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
  /// because this is a read-only catalog object. This should never be called in properly-typed
  /// pipeline code due to compile-time enforcement, but exists for runtime safety in edge cases
  /// (e.g., reflection-based scenarios).
  /// </remarks>
  /// <exception cref="NotSupportedException">Always thrown - this is a read-only object</exception>
  public virtual Task SaveUntyped(object data) {
    throw new NotSupportedException(
        $"Cannot save to read-only catalog object '{Key}' of type {GetType().Name}. " +
        "This object implements IReadableCatalogObject<T> and does not support write operations. " +
        "Use a read-write object implementation (MemoryCatalogObject, etc.) " +
        "if you need to save data.");
  }
}
