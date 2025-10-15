namespace Flowthru.Data;

/// <summary>
/// Abstract base class for catalog entry implementations.
/// Provides default implementations of untyped operations using the strongly-typed methods.
/// </summary>
/// <typeparam name="T">The type of data stored in this catalog entry</typeparam>
/// <remarks>
/// <para>
/// <strong>Template Method Pattern:</strong> This class defines the skeleton of catalog
/// entry operations. Derived classes implement the abstract Load() and Save() methods,
/// while this base class provides the untyped variants by delegating to the typed versions.
/// </para>
/// <para>
/// Implementations must provide:
/// - Load(): Retrieve data from storage
/// - Save(T data): Persist data to storage
/// - Exists(): Check if data is present
/// </para>
/// </remarks>
public abstract class CatalogEntryBase<T> : ICatalogEntry<T>
{
  /// <summary>
  /// Creates a new catalog entry with the specified key.
  /// </summary>
  /// <param name="key">Unique identifier for this catalog entry</param>
  protected CatalogEntryBase(string key)
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
          $"to catalog entry expecting type {typeof(T).Name}");
    }

    await Save(typedData);
  }
}
