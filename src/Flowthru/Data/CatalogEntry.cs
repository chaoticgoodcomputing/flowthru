using Flowthru.Data.Storage;
using Flowthru.Data.Validation;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Flowthru.Data;

/// <summary>
/// Standard catalog entry implementation that delegates to a storage adapter.
/// </summary>
/// <typeparam name="T">The data type (container with rows)</typeparam>
/// <remarks>
/// <para>
/// <strong>Delegation Pattern:</strong>
/// </para>
/// <para>
/// This class is a thin wrapper that delegates all operations to an <see cref="IStorageAdapter{T}"/>.
/// The storage adapter handles the actual I/O logic, while this class provides:
/// - ICatalogEntry interface implementation
/// - Identity for DAG dependency resolution (via Key)
/// - Type erasure for pipeline heterogeneous collections
/// </para>
/// <para>
/// <strong>Construction:</strong>
/// </para>
/// <para>
/// Typically created via static factory methods in <see cref="CatalogEntries"/>:
/// </para>
/// <code>
/// var entry = CatalogEntries.Csv&lt;CompanySchema&gt;("companies", "data.csv");
/// // Returns: ICatalogEntry&lt;IEnumerable&lt;CompanySchema&gt;&gt;
/// </code>
/// <para>
/// <strong>Composition vs Inheritance:</strong>
/// </para>
/// <para>
/// Previous design: Inheritance hierarchy (CsvCatalogEntry, JsonCatalogEntry, etc.)
/// New design: Single class + composed storage adapter
/// </para>
/// <para>
/// Benefits:
/// - No class explosion for format × container combinations
/// - Custom storage via IStorageAdapter implementation
/// - Clear separation of concerns
/// </para>
/// </remarks>
public sealed class CatalogEntry<T> : ICatalogEntry<T>
{
  private readonly IStorageAdapter<T> _storage;
  private InspectionLevel? _preferredInspectionLevel;

  /// <summary>
  /// Creates a new catalog entry with the specified key and storage adapter.
  /// </summary>
  /// <param name="label">Unique identifier for this catalog entry</param>
  /// <param name="storage">Storage adapter that handles I/O operations</param>
  public CatalogEntry(string label, IStorageAdapter<T> storage)
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
  public IO<T> Load() => _storage.Load();

  /// <inheritdoc/>
  public IO<Unit> Save(T data) => _storage.Save(data);

  /// <inheritdoc/>
  public IO<bool> Exists() => _storage.Exists();

  /// <inheritdoc/>
  public IO<object> LoadUntyped() => Load().Map(data => (object)data!);

  /// <inheritdoc/>
  public IO<Unit> SaveUntyped(object data)
  {
    // Try direct cast
    if (data is T typedData)
    {
      return Save(typedData);
    }

    // Type mismatch - fail with descriptive error
    return IO.fail<Unit>(
      new Exception(
        $"Type mismatch: Cannot save data of type '{data?.GetType().Name ?? "null"}' "
          + $"to catalog entry '{Label}' expecting type '{typeof(T).Name}'"
      )
    );
  }

  /// <inheritdoc/>
  public IO<int> GetCountAsync()
  {
    // For collection types, try to get actual count
    // For singletons, return 1 if exists
    var type = typeof(T);

    if (IsCollectionType(type))
    {
      return from exists in Exists()
        from count in exists ? LoadAndCount() : IO.pure(0)
        select count;
    }
    else
    {
      return from exists in Exists() select exists ? 1 : 0;
    }
  }

  private IO<int> LoadAndCount()
  {
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
  /// Sets the preferred inspection level for this catalog entry.
  /// </summary>
  /// <param name="level">The inspection level to use</param>
  /// <returns>This catalog entry for method chaining</returns>
  /// <remarks>
  /// <para>
  /// Used to configure how this entry should be validated before pipeline execution.
  /// </para>
  /// <para>
  /// Example:
  /// </para>
  /// <code>
  /// var entry = CatalogEntries.Csv&lt;Company&gt;("companies", "data.csv")
  ///     .WithInspectionLevel(InspectionLevel.Deep);
  /// </code>
  /// </remarks>
  public CatalogEntry<T> WithInspectionLevel(InspectionLevel level)
  {
    _preferredInspectionLevel = level;
    return this;
  }
}
