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
public interface ICatalogEntry {
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

  /// <summary>
  /// Gets the count of items in this catalog entry.
  /// </summary>
  /// <returns>
  /// The number of items/observations in the catalog entry.
  /// For collections, this is the enumerable count.
  /// For singleton entries, this returns 1 if data exists, 0 otherwise.
  /// For empty or non-existent entries, this returns 0.
  /// </returns>
  /// <remarks>
  /// Used for diagnostic logging and pipeline observability.
  /// Implementations should return the count efficiently without loading all data into memory when possible.
  /// </remarks>
  Task<int> GetCountAsync();
}

/// <summary>
/// Strongly-typed catalog entry for datasets (collections of items).
/// Represents a storage location for collections of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of individual items in the dataset (NOT IEnumerable&lt;T&gt;)</typeparam>
/// <remarks>
/// <para>
/// <strong>Breaking Change (v0.2.0):</strong> This interface replaces the generic ICatalogEntry&lt;IEnumerable&lt;T&gt;&gt; pattern.
/// Users now declare <c>ICatalogDataset&lt;Company&gt;</c> instead of <c>ICatalogEntry&lt;IEnumerable&lt;Company&gt;&gt;</c>.
/// </para>
/// <para>
/// <strong>Semantic Distinction:</strong> Use ICatalogDataset&lt;T&gt; for collections (CSV files, database tables, lists of entities).
/// For singleton objects (ML models, configuration, metrics), use <see cref="ICatalogObject{T}"/>.
/// </para>
/// <para>
/// <strong>LINQ Compatibility:</strong> Load() returns IEnumerable&lt;T&gt; which enables fluent LINQ operations:
/// <code>
/// var topCompanies = await catalog.Companies.Load()
///     .Where(c => c.Rating > 4.0)
///     .OrderByDescending(c => c.Rating)
///     .Take(10);
/// </code>
/// </para>
/// <para>
/// <strong>Design Pattern:</strong> Strategy Pattern - different implementations provide
/// different storage strategies (CsvCatalogEntry, ParquetCatalogEntry, MemoryCatalogEntry, etc.)
/// </para>
/// <para>
/// <strong>Compile-Time Safety:</strong> The generic type parameter T ensures that:
/// - Load() returns IEnumerable&lt;T&gt; with the correct item type
/// - Save() accepts only IEnumerable&lt;T&gt; with the correct item type
/// - Type mismatches are caught at compilation, not runtime
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // User declaration (in SpaceflightsCatalog)
/// public ICatalogDataset&lt;Company&gt; Companies => Dataset(() => 
///     new CsvCatalogEntry&lt;Company&gt;("data/companies.csv"));
/// 
/// // Usage (in nodes or pipelines)
/// var companies = await catalog.Companies.Load(); // IEnumerable&lt;Company&gt;
/// await catalog.Companies.Save(filteredCompanies);
/// </code>
/// </example>
public interface ICatalogDataset<T> : ICatalogEntry {
  /// <summary>
  /// Loads the dataset as a collection of items.
  /// </summary>
  /// <returns>An enumerable collection of items of type T</returns>
  /// <remarks>
  /// Implementations should be idempotent - calling Load() multiple times
  /// should return equivalent data (though not necessarily the same instances).
  /// For large datasets, implementations may return lazy-loading enumerables.
  /// </remarks>
  Task<IEnumerable<T>> Load();

  /// <summary>
  /// Saves a collection of items to the catalog entry.
  /// </summary>
  /// <param name="data">The collection of items to save</param>
  /// <remarks>
  /// Implementations typically overwrite existing data.
  /// For append-only semantics, use specialized catalog entry implementations.
  /// </remarks>
  Task Save(IEnumerable<T> data);
}

/// <summary>
/// Strongly-typed catalog entry for singleton objects (non-collection data).
/// Represents a storage location for a single object of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of the singleton object</typeparam>
/// <remarks>
/// <para>
/// <strong>Breaking Change (v0.2.0):</strong> This interface is new. Previously, singletons were awkwardly
/// wrapped in IEnumerable: <c>ICatalogEntry&lt;IEnumerable&lt;LinearRegressionModel&gt;&gt;</c>.
/// Now use: <c>ICatalogObject&lt;LinearRegressionModel&gt;</c>.
/// </para>
/// <para>
/// <strong>Semantic Distinction:</strong> Use ICatalogObject&lt;T&gt; for singular entities:
/// - Machine learning models (ITransformer, LinearRegressionModel)
/// - Configuration objects (ModelParams, PipelineConfig)
/// - Aggregated metrics (ModelMetrics, PerformanceReport)
/// - Reference data (schema definitions, lookup tables as single objects)
/// </para>
/// <para>
/// For collections of entities, use <see cref="ICatalogDataset{T}"/>.
/// </para>
/// <para>
/// <strong>No LINQ Operations:</strong> Unlike ICatalogDataset, Load() returns a single T object,
/// not a collection. This prevents confusion where LINQ operations wouldn't make semantic sense.
/// </para>
/// <para>
/// <strong>Design Pattern:</strong> Strategy Pattern - different implementations provide
/// different storage strategies (MemoryCatalogObject, JsonCatalogObject, etc.)
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // User declaration (in SpaceflightsCatalog)
/// public ICatalogObject&lt;LinearRegressionModel&gt; Regressor => Object(() => 
///     new MemoryCatalogObject&lt;LinearRegressionModel&gt;("regressor"));
/// 
/// // Usage (in nodes or pipelines)
/// var model = await catalog.Regressor.Load(); // LinearRegressionModel (not IEnumerable)
/// await catalog.Regressor.Save(trainedModel);
/// </code>
/// </example>
public interface ICatalogObject<T> : ICatalogEntry {
  /// <summary>
  /// Loads the singleton object.
  /// </summary>
  /// <returns>The loaded object of type T</returns>
  /// <remarks>
  /// Implementations should be idempotent - calling Load() multiple times
  /// should return equivalent data (though not necessarily the same instance).
  /// </remarks>
  Task<T> Load();

  /// <summary>
  /// Saves the singleton object to the catalog entry.
  /// </summary>
  /// <param name="data">The object to save</param>
  /// <remarks>
  /// Implementations typically overwrite existing data.
  /// </remarks>
  Task Save(T data);
}
