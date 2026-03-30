using Flowthru.Abstractions;
using Flowthru.Data.Storage;
using Flowthru.Data.Storage.Container;
using Flowthru.Data.Storage.Format;
using Flowthru.Data.Storage.Medium;

namespace Flowthru.Data;

/// <summary>
/// Extension methods that add CSV support to <see cref="CatalogEntries.Enumerable"/>.
/// </summary>
public static class CsvCatalogEntryExtensions
{
  /// <summary>
  /// Creates a CSV file catalog entry with IEnumerable container.
  /// </summary>
  /// <typeparam name="TRow">Row schema type (must be flat and text-serializable)</typeparam>
  /// <param name="_">The enumerable catalog entries factory (from <see cref="CatalogEntries.Enumerable"/>)</param>
  /// <param name="label">Unique catalog label for DAG resolution</param>
  /// <param name="filePath">Path to CSV file</param>
  /// <returns>Catalog entry with file + CSV + IEnumerable composition</returns>
  /// <remarks>
  /// <para>
  /// <strong>Requirements:</strong>
  /// </para>
  /// <list type="bullet">
  /// <item>TRow must implement IFlatSchema (no nested objects)</item>
  /// <item>TRow must implement ITextSerializable</item>
  /// </list>
  /// <para>
  /// <strong>Storage Traits:</strong>
  /// </para>
  /// <list type="bullet">
  /// <item>CanStream: true (CSV supports row-by-row streaming)</item>
  /// <item>All other traits use filesystem baseline defaults</item>
  /// </list>
  /// </remarks>
  public static CatalogEntry<IEnumerable<TRow>> Csv<TRow>(
    this EnumerableCatalogEntries _,
    string label,
    string filePath
  )
    where TRow : notnull, IFlatSchema, ITextSerializable
  {
    var medium = new FileStorageMedium(filePath);
    var format = new CsvFormatSerializer<TRow>();
    var container = new EnumerableContainerAdapter<TRow>();
    var storage = new ComposedStorageAdapter<IEnumerable<TRow>, TRow>(medium, format, container);

    return new CatalogEntry<IEnumerable<TRow>>(label, storage);
  }
}
