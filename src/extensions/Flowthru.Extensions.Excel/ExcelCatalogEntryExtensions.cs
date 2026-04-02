using Flowthru.Abstractions;
using Flowthru.Data.Storage;
using Flowthru.Data.Storage.Container;
using Flowthru.Data.Storage.Format;
using Flowthru.Data.Storage.Medium;

namespace Flowthru.Data;

/// <summary>
/// Extension methods that add Excel support to <see cref="Items.Enumerable"/>.
/// </summary>
public static class ExcelCatalogEntryExtensions
{
  /// <summary>
  /// Creates a read-only Excel file catalog entry with IEnumerable container.
  /// </summary>
  /// <typeparam name="TRow">Row schema type (must be flat and text-serializable)</typeparam>
  /// <param name="_">The enumerable catalog entries factory (from <see cref="Items.Enumerable"/>)</param>
  /// <param name="label">Unique catalog label for DAG resolution</param>
  /// <param name="filePath">Path to Excel file (.xlsx)</param>
  /// <param name="sheetName">Name of the sheet to read</param>
  /// <returns>Catalog entry with read-only Excel support</returns>
  /// <remarks>
  /// <para>
  /// <strong>Requirements:</strong>
  /// </para>
  /// <list type="bullet">
  /// <item>TRow must implement IFlatSchema (Excel is tabular)</item>
  /// <item>TRow must implement ITextSerializable</item>
  /// </list>
  /// <para>
  /// <strong>Limitations:</strong> Read-only support via ExcelDataReader.
  /// Writing Excel files is not supported.
  /// </para>
  /// <para>
  /// <strong>Storage Traits:</strong>
  /// </para>
  /// <list type="bullet">
  /// <item>CanWrite: false (Excel adapter is read-only via ExcelDataReader)</item>
  /// </list>
  /// </remarks>
  public static Item<IEnumerable<TRow>> Excel<TRow>(
    this EnumerableItems _,
    string label,
    string filePath,
    string sheetName
  )
    where TRow : notnull, IFlatSchema, ITextSerializable
  {
    var medium = new FileStorageMedium(filePath);
    var format = new ExcelFormatSerializer<TRow>(sheetName);
    var container = new EnumerableContainerAdapter<TRow>();
    var storage = new ComposedStorageAdapter<IEnumerable<TRow>, TRow>(medium, format, container);

    return new Item<IEnumerable<TRow>>(label, storage);
  }
}
