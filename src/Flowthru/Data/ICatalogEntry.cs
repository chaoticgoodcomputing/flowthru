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
/// Read-only interface for catalog datasets that support loading data.
/// </summary>
/// <typeparam name="T">The type of individual items in the dataset</typeparam>
/// <remarks>
/// <para>
/// <strong>Compile-Time Read Safety:</strong> This interface provides only Load() operations,
/// ensuring that read-only data sources (like Excel files, database views, or HTTP APIs)
/// cannot be accidentally used as pipeline outputs.
/// </para>
/// <para>
/// Use this interface for:
/// - Read-only file formats (Excel files without write support)
/// - Database views and read-only queries
/// - HTTP API data sources
/// - Any data source where writing is not supported or desired
/// </para>
/// </remarks>
public interface IReadableCatalogDataset<T> : ICatalogEntry {
  /// <summary>
  /// Loads the dataset as a collection of items.
  /// </summary>
  /// <returns>An enumerable collection of items of type T</returns>
  Task<IEnumerable<T>> Load();
}

/// <summary>
/// Write-only interface for catalog datasets that support saving data.
/// </summary>
/// <typeparam name="T">The type of individual items in the dataset</typeparam>
/// <remarks>
/// <para>
/// <strong>Compile-Time Write Safety:</strong> This interface provides only Save() operations.
/// Less common than readable datasets, but useful for append-only logs, message queues,
/// or write-only sinks.
/// </para>
/// <para>
/// Use this interface for:
/// - Log sinks and append-only storage
/// - Message queues and event streams
/// - Metrics collectors
/// - Any data destination where reading is not supported or desired
/// </para>
/// </remarks>
public interface IWritableCatalogDataset<T> : ICatalogEntry {
  /// <summary>
  /// Saves a collection of items to the catalog entry.
  /// </summary>
  /// <param name="data">The collection of items to save</param>
  Task Save(IEnumerable<T> data);
}

/// <summary>
/// Strongly-typed catalog entry for datasets (collections of items) with full read-write access.
/// Represents a storage location for collections of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of individual items in the dataset (NOT IEnumerable&lt;T&gt;)</typeparam>
/// <remarks>
/// <para>
/// <strong>Breaking Change (v0.2.0):</strong> This interface replaces the generic ICatalogEntry&lt;IEnumerable&lt;T&gt;&gt; pattern.
/// Users now declare <c>ICatalogDataset&lt;Company&gt;</c> instead of <c>ICatalogEntry&lt;IEnumerable&lt;Company&gt;&gt;</c>.
/// </para>
/// <para>
/// <strong>Breaking Change (v0.3.0):</strong> This interface now inherits from both <see cref="IReadableCatalogDataset{T}"/>
/// and <see cref="IWritableCatalogDataset{T}"/>, enabling compile-time enforcement of read/write capabilities.
/// </para>
/// <para>
/// <strong>Semantic Distinction:</strong> Use ICatalogDataset&lt;T&gt; for collections (CSV files, database tables, lists of entities).
/// For singleton objects (ML models, configuration, metrics), use <see cref="ICatalogObject{T}"/>.
/// For read-only sources, use <see cref="IReadableCatalogDataset{T}"/>.
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
/// - Read-only datasets cannot be used as pipeline outputs (compile error)
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
public interface ICatalogDataset<T> : IReadableCatalogDataset<T>, IWritableCatalogDataset<T> {
  // Inherits Load() from IReadableCatalogDataset<T>
  // Inherits Save() from IWritableCatalogDataset<T>
}

/// <summary>
/// Read-only interface for catalog objects that support loading data.
/// </summary>
/// <typeparam name="T">The type of the singleton object</typeparam>
/// <remarks>
/// <para>
/// <strong>Compile-Time Read Safety:</strong> This interface provides only Load() operations,
/// ensuring that read-only objects (like immutable configuration files or read-only serialized models)
/// cannot be accidentally used as pipeline outputs.
/// </para>
/// <para>
/// Use this interface for:
/// - Read-only configuration files
/// - Immutable reference data objects
/// - Pre-trained models loaded from external sources
/// - Any singleton object where writing is not supported or desired
/// </para>
/// </remarks>
public interface IReadableCatalogObject<T> : ICatalogEntry {
  /// <summary>
  /// Loads the singleton object.
  /// </summary>
  /// <returns>The loaded object of type T</returns>
  Task<T> Load();
}

/// <summary>
/// Write-only interface for catalog objects that support saving data.
/// </summary>
/// <typeparam name="T">The type of the singleton object</typeparam>
/// <remarks>
/// <para>
/// <strong>Compile-Time Write Safety:</strong> This interface provides only Save() operations.
/// Less common than readable objects, but useful for write-only sinks or output-only storage.
/// </para>
/// <para>
/// Use this interface for:
/// - Metrics or telemetry sinks
/// - Write-only model storage
/// - Any singleton object destination where reading is not supported or desired
/// </para>
/// </remarks>
public interface IWritableCatalogObject<T> : ICatalogEntry {
  /// <summary>
  /// Saves the singleton object to the catalog entry.
  /// </summary>
  /// <param name="data">The object to save</param>
  Task Save(T data);
}

/// <summary>
/// Strongly-typed catalog entry for singleton objects (non-collection data) with full read-write access.
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
/// <strong>Breaking Change (v0.3.0):</strong> This interface now inherits from both <see cref="IReadableCatalogObject{T}"/>
/// and <see cref="IWritableCatalogObject{T}"/>, enabling compile-time enforcement of read/write capabilities.
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
/// For read-only objects, use <see cref="IReadableCatalogObject{T}"/>.
/// </para>
/// <para>
/// <strong>No LINQ Operations:</strong> Unlike ICatalogDataset, Load() returns a single T object,
/// not a collection. This prevents confusion where LINQ operations wouldn't make semantic sense.
/// </para>
/// <para>
/// <strong>Design Pattern:</strong> Strategy Pattern - different implementations provide
/// different storage strategies (MemoryCatalogObject, JsonCatalogObject, etc.)
/// </para>
/// <para>
/// <strong>Compile-Time Safety:</strong> The generic type parameter T ensures that:
/// - Load() returns T with the correct type
/// - Save() accepts only T with the correct type
/// - Type mismatches are caught at compilation, not runtime
/// - Read-only objects cannot be used as pipeline outputs (compile error)
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
public interface ICatalogObject<T> : IReadableCatalogObject<T>, IWritableCatalogObject<T> {
  // Inherits Load() from IReadableCatalogObject<T>
  // Inherits Save() from IWritableCatalogObject<T>
}
