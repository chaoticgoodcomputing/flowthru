using Flowthru.Abstractions;
using Flowthru.Data.Storage;
using Flowthru.Data.Storage.Container;
using Flowthru.Data.Storage.Format;
using Flowthru.Data.Storage.Medium;
using Flowthru.Effects;

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
/// <para>
/// <strong>Field Name Mapping with SerializedLabel:</strong>
/// </para>
/// <para>
/// Use the <see cref="SerializedLabelAttribute"/> to map C# property names to external field names
/// when they differ. This works uniformly across CSV, Excel, JSON, and most formats.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Tier 1: No annotations - property names match external field names
/// public record SimpleSchema(
///     int Id,           // Looks for "Id" in CSV/Excel/JSON
///     string Name       // Looks for "Name" in CSV/Excel/JSON
/// ) : IFlatSchema, ITextSerializable;
///
/// var simple = CatalogEntries.Enumerable.Csv&lt;SimpleSchema&gt;("data", "data.csv");
///
/// // Tier 2: Explicit annotations - handle naming mismatches
/// public record ShuttleSchema(
///     [SerializedLabel("id")]
///     string Id,
///
///     [SerializedLabel("shuttle_location")]        // snake_case in file
///     string ShuttleLocation,
///
///     [SerializedLabel("d_check_complete")]        // snake_case in file
///     bool DCheckComplete,
///
///     [SerializedLabel("Company ID")]              // space-separated in file
///     int CompanyId
/// ) : IFlatSchema, ITextSerializable;
///
/// var shuttles = CatalogEntries.Enumerable.Excel&lt;ShuttleSchema&gt;(
///     "shuttles",
///     "data/shuttles.xlsx",
///     "Sheet1"
/// );
///
/// // Same schema works across all formats
/// var csv = CatalogEntries.Enumerable.Csv&lt;ShuttleSchema&gt;("shuttles", "data/shuttles.csv");
/// var json = CatalogEntries.Enumerable.Json&lt;ShuttleSchema&gt;("shuttles", "data/shuttles.json");
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
  /// <strong>Storage Traits:</strong>
  /// </para>
  /// <list type="bullet">
  /// <item>CanWrite: false (Save is a no-op)</item>
  /// <item>CanRead: false (Load throws NotSupportedException)</item>
  /// </list>
  /// </remarks>
  public static CatalogEntry<T> Null<T>(string label)
  {
    var storage = new NullStorageAdapter<T>();
    return new CatalogEntry<T>(label, storage);
  }
}
