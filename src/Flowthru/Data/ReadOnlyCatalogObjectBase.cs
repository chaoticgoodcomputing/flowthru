using Flowthru.Data.Validation;

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
  private InspectionLevel? _preferredInspectionLevel;

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
  public InspectionLevel? PreferredInspectionLevel {
    get => _preferredInspectionLevel;
    protected set => _preferredInspectionLevel = value;
  }

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
        "Use a read-write object implementation (MemoryCatalogObject, JsonCatalogObject) " +
        "if you need to save data.");
  }

  /// <summary>
  /// Configures the preferred inspection level for this catalog entry.
  /// </summary>
  /// <param name="level">The inspection level to use for this entry</param>
  /// <returns>This instance for fluent chaining</returns>
  /// <remarks>
  /// <para>
  /// <strong>Fluent Configuration:</strong> This method enables catalog-level validation configuration
  /// using a fluent API pattern that integrates seamlessly with catalog property declarations.
  /// </para>
  /// <para>
  /// <strong>Usage Example:</strong>
  /// </para>
  /// <code>
  /// public IReadableCatalogObject&lt;ModelConfig&gt; Config =>
  ///   GetOrCreateReadOnlyObject(() => new JsonCatalogObject&lt;ModelConfig&gt;("config", "config/model.json")
  ///     .WithInspectionLevel(InspectionLevel.Shallow));
  /// </code>
  /// <para>
  /// <strong>Note:</strong> Read-only catalog objects typically don't implement inspection interfaces
  /// unless they represent external configuration files or other data sources that benefit from validation.
  /// </para>
  /// </remarks>
  public ReadOnlyCatalogObjectBase<T> WithInspectionLevel(InspectionLevel level) {
    PreferredInspectionLevel = level;
    return this;
  }
}
