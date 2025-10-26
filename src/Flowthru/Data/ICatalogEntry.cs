using Flowthru.Data.Validation;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Flowthru.Data;

/// <summary>
/// Non-generic base interface for catalog entries.
/// Provides untyped operations for internal use by the pipeline executor and mapping layer.
/// </summary>
/// <remarks>
/// This interface enables the pipeline to work with catalog entries
/// without knowing their specific type parameter at compile-time.
/// </remarks>
public interface ICatalogEntry {
  /// <summary>
  /// Unique key identifying this catalog entry within the data catalog.
  /// </summary>
  string Key { get; }

  /// <summary>
  /// The runtime type of data stored in this catalog entry.
  /// For singletons: Returns typeof(T).
  /// For collections: Returns typeof(Seq&lt;T&gt;) or typeof(IEnumerable&lt;T&gt;).
  /// </summary>
  Type DataType { get; }

  /// <summary>
  /// Gets the preferred inspection level for this catalog entry.
  /// </summary>
  InspectionLevel? PreferredInspectionLevel { get; }

  /// <summary>
  /// Loads data from the catalog entry as an untyped object.
  /// Returns an effect that can fail.
  /// The returned type matches the DataType property.
  /// </summary>
  Aff<object> LoadUntyped();

  /// <summary>
  /// Saves untyped data to the catalog entry.
  /// Returns an effect that can fail.
  /// The data type must be compatible with the DataType property.
  /// </summary>
  Aff<Unit> SaveUntyped(object data);

  /// <summary>
  /// Checks if data exists at this catalog entry location.
  /// Returns an effect that can fail.
  /// </summary>
  Aff<bool> Exists();

  /// <summary>
  /// Gets the count of items in this catalog entry.
  /// For collections (IEnumerable&lt;T&gt;), returns the enumerable count.
  /// For singletons, returns 1 if exists, 0 otherwise.
  /// </summary>
  Aff<int> GetCountAsync();
}

/// <summary>
/// Unified catalog entry with cardinality encoded in the type parameter.
/// </summary>
/// <typeparam name="T">
/// The data type stored in this catalog entry.
/// Cardinality is determined by T itself:
/// - For singletons: Use T directly (e.g., LinearRegressionModel, ModelMetrics)
/// - For collections: Use Seq&lt;T&gt; or IEnumerable&lt;T&gt; (e.g., Seq&lt;FeatureRow&gt;)
/// </typeparam>
/// <remarks>
/// <para>
/// <strong>Unified Design:</strong> This single interface replaces the previous dual-interface
/// system (ICatalogObject/ICatalogDataset). Cardinality is now purely a type-level concern.
/// </para>
/// <para>
/// <strong>Type Alignment:</strong> Node TInput/TOutput types should directly match catalog entry
/// T types, eliminating the need for wrapping/unwrapping ceremony.
/// </para>
/// <para>
/// <strong>Effect Types:</strong> All operations return Aff&lt;T&gt; - an effect that represents
/// an async computation that can fail. This provides:
/// - Explicit error handling
/// - Cancellation support
/// - Retry/timeout semantics
/// - Functional composition
/// </para>
/// </remarks>
public interface ICatalogEntry<T> : ICatalogEntry {
  /// <summary>
  /// Load data as an effect (can fail, is async, can be cancelled).
  /// Returns T directly, which may itself be an IEnumerable or Seq.
  /// </summary>
  Aff<T> Load();

  /// <summary>
  /// Save data as an effect.
  /// Accepts T directly, which may itself be an IEnumerable or Seq.
  /// </summary>
  Aff<Unit> Save(T data);
}

/// <summary>
/// Read-only catalog entry interface.
/// For catalog entries that can only be read from (e.g., Excel files, external APIs).
/// </summary>
/// <typeparam name="T">The data type stored in this catalog entry</typeparam>
public interface IReadableCatalogEntry<T> : ICatalogEntry {
  /// <summary>
  /// Load data as an effect.
  /// </summary>
  Aff<T> Load();
}

/// <summary>
/// Write-only catalog entry interface.
/// For catalog entries that can only be written to.
/// </summary>
/// <typeparam name="T">The data type stored in this catalog entry</typeparam>
public interface IWritableCatalogEntry<T> : ICatalogEntry {
  /// <summary>
  /// Save data as an effect.
  /// </summary>
  Aff<Unit> Save(T data);
}
