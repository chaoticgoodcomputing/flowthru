using Flowthru.Abstractions;
using Flowthru.Data.Storage;
using Flowthru.Data.Storage.Container;
using Flowthru.Data.Storage.Format;
using Flowthru.Data.Storage.Medium;
using LanguageExt;

namespace Flowthru.Data;

/// <summary>
/// Static factory methods for creating catalog entries with common configurations.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Design Pattern:</strong> Static factory methods that compose storage adapters
/// from medium + format + container layers.
/// </para>
/// <para>
/// <strong>Discoverability:</strong> All factory methods are in one place with IntelliSense support.
/// </para>
/// <para>
/// <strong>Type Safety:</strong> Generic constraints enforce schema compatibility at compile-time.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // CSV file with IEnumerable container
/// var companies = CatalogEntries.Csv&lt;CompanySchema&gt;("companies", "data/companies.csv");
///
/// // JSON file with IEnumerable container
/// var model = CatalogEntries.Json&lt;LinearRegressionModel&gt;("model", "models/regression.json");
///
/// // Parquet file with IEnumerable container
/// var features = CatalogEntries.Parquet&lt;FeatureRow&gt;("features", "data/features.parquet");
///
/// // In-memory transient storage
/// var temp = CatalogEntries.Memory&lt;ProcessedData&gt;("temp");
/// </code>
/// </example>
public static partial class CatalogEntries
{
  /// <summary>
  /// Creates a null catalog entry for side-effect-only nodes.
  /// </summary>
  /// <typeparam name="T">The data type (typically NoData)</typeparam>
  /// <param name="label">Unique catalog label for DAG resolution</param>
  /// <returns>Catalog entry for void/no-data semantics</returns>
  /// <remarks>
  /// <para>
  /// <strong>Use Case:</strong> Nodes that perform side effects (logging, visualization) without producing meaningful data
  /// </para>
  /// <para>
  /// <strong>Implementation:</strong> Uses NullStorageAdapter which performs no I/O operations.
  /// </para>
  /// <para>
  /// <strong>Capabilities:</strong>
  /// </para>
  /// <list type="bullet">
  /// <item>ISeedable: false (null entries cannot be seeds)</item>
  /// <item>IReadOnly: true (Load/Save are no-ops)</item>
  /// </list>
  /// </remarks>
  public static ICatalogEntry<T> Null<T>(string label)
  {
    var storage = new NullStorageAdapter<T>();
    return new CatalogEntry<T>(label, storage);
  }
}
