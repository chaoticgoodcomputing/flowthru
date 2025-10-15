namespace Flowthru.Data;

/// <summary>
/// Non-generic base interface for catalog entries.
/// Provides untyped operations for internal use by the mapping layer.
/// </summary>
/// <remarks>
/// This interface enables the mapping layer to work with catalog entries
/// without knowing their specific type parameter at compile-time, using
/// reflection-based property mapping.
/// </remarks>
public interface ICatalogEntry
{
  /// <summary>
  /// Unique key identifying this catalog entry within the data catalog.
  /// </summary>
  string Key { get; }

  /// <summary>
  /// The runtime type of data stored in this catalog entry.
  /// </summary>
  Type DataType { get; }

  /// <summary>
  /// Loads data from the catalog entry as an untyped object.
  /// </summary>
  /// <returns>The loaded data as object</returns>
  /// <remarks>
  /// Used internally by CatalogMap for reflection-based property mapping.
  /// Callers should prefer the strongly-typed Load() method when possible.
  /// </remarks>
  Task<object> LoadUntyped();

  /// <summary>
  /// Saves untyped data to the catalog entry.
  /// </summary>
  /// <param name="data">The data to save (must be assignable to DataType)</param>
  /// <remarks>
  /// Used internally by CatalogMap for reflection-based property mapping.
  /// Callers should prefer the strongly-typed Save() method when possible.
  /// </remarks>
  Task SaveUntyped(object data);

  /// <summary>
  /// Checks if data exists at this catalog entry location.
  /// </summary>
  /// <returns>True if data exists, false otherwise</returns>
  Task<bool> Exists();
}

/// <summary>
/// Strongly-typed catalog entry interface.
/// Represents a storage location for data of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of data stored in this catalog entry</typeparam>
/// <remarks>
/// <para>
/// Catalog entries abstract away storage implementation details (CSV, Parquet, memory, etc.)
/// and provide a consistent async API for loading and saving data.
/// </para>
/// <para>
/// <strong>Design Pattern:</strong> Strategy Pattern - different implementations provide
/// different storage strategies (MemoryCatalogEntry, CsvCatalogEntry, etc.)
/// </para>
/// <para>
/// <strong>Compile-Time Safety:</strong> The generic type parameter T ensures that:
/// - Load() returns the expected type
/// - Save() accepts only the correct type
/// - Type mismatches are caught at compilation, not runtime
/// </para>
/// </remarks>
public interface ICatalogEntry<T> : ICatalogEntry
{
  /// <summary>
  /// Loads data from the catalog entry.
  /// </summary>
  /// <returns>The loaded data of type T</returns>
  /// <remarks>
  /// Implementations should be idempotent - calling Load() multiple times
  /// should return equivalent data (though not necessarily the same instance).
  /// </remarks>
  Task<T> Load();

  /// <summary>
  /// Saves data to the catalog entry.
  /// </summary>
  /// <param name="data">The data to save</param>
  /// <remarks>
  /// Implementations may overwrite existing data or append, depending on
  /// the storage strategy. Consult specific implementation documentation.
  /// </remarks>
  Task Save(T data);
}
