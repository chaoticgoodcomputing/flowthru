using Flowthru.Core.Abstractions;
using Flowthru.Core.Data.Storage;
using Flowthru.Core.Data.Storage.Container;
using Flowthru.Core.Data.Storage.Format;
using Flowthru.Core.Data.Storage.Medium;

namespace Flowthru.Core.Data;

/// <summary>
/// Extension methods that add Excel support to <see cref="ItemFactory.Enumerable"/>.
/// </summary>
public static class ExcelItemExtensions
{
  /// <summary>
  /// Creates a read-only Excel file catalog entry with IEnumerable container.
  /// </summary>
  /// <typeparam name="TRow">Row schema type (must be flat and text-serializable)</typeparam>
  /// <param name="_">The enumerable catalog entries factory (from <see cref="ItemFactory.Enumerable"/>)</param>
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
  /// <param name="nullValues">
  /// Optional set of strings that should deserialize to null for nullable properties.
  /// Defaults to <c>[""]</c> — only genuinely empty cells (DBNull) become null. Pass e.g.
  /// <c>["", "NA", "N/A", "NULL"]</c> to also treat those string sentinels as null on read.
  /// </param>
  public static Item<IEnumerable<TRow>> Excel<TRow>(
    this EnumerableItemFactory _,
    string label,
    string filePath,
    string sheetName,
    IReadOnlyList<string>? nullValues = null
  )
    where TRow : notnull, IFlatSchema, ITextSerializable
  {
    var medium = new FileStorageMedium(filePath);
    var format = nullValues is null
      ? new ExcelFormatSerializer<TRow>(sheetName)
      : new ExcelFormatSerializer<TRow>(sheetName, nullValues);
    var container = new EnumerableContainerAdapter<TRow>();
    var storage = new ComposedStorageAdapter<IEnumerable<TRow>, TRow>(medium, format, container);

    return new Item<IEnumerable<TRow>>(label, storage);
  }
}
