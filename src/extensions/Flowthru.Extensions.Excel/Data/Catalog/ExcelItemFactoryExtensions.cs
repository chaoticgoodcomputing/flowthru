using Flowthru.Data.Schema;
using Flowthru.Data.Storage;
using Flowthru.Data.Storage.Excel;

namespace Flowthru.Data.Catalog;

/// <summary>
/// Extension methods that contribute Excel smart constructors into
/// <see cref="ItemFactory.Enumerable"/> and
/// <see cref="ItemFactory.Directory"/>. Excel is a read-only format —
/// the resulting <see cref="IItem{T}"/> reports
/// <see cref="StorageTraits.CanWrite"/> = <c>false</c>; calling
/// <c>Save</c> fails fast before touching the workbook.
/// </summary>
public static class ExcelItemFactoryExtensions
{
  /// <summary>
  /// Read-only Excel (.xlsx) file holding rows of
  /// <typeparamref name="TRow"/> on a named sheet. Composes
  /// <see cref="FileStorageMedium"/> +
  /// <see cref="ExcelFormatSerializer{TRow}"/> +
  /// <see cref="EnumerableContainerAdapter{TRow}"/> via the
  /// reader-only <see cref="ComposedStorageAdapter{TContainer, TRow}"/>
  /// constructor.
  /// </summary>
  /// <param name="factory">The factory anchor — discriminates the extension target.</param>
  /// <param name="label">Catalog label for DAG resolution.</param>
  /// <param name="filePath">Path to the .xlsx file.</param>
  /// <param name="sheetName">Name of the sheet to read.</param>
  /// <param name="nullValues">
  /// Optional null-sentinel list for nullable properties. Defaults to
  /// <c>[""]</c> (only DBNull / empty cells round-trip as null). Pass
  /// <c>["", "NA", "N/A", "NULL"]</c> for messy-spreadsheet handling.
  /// </param>
  public static IItem<IEnumerable<TRow>> Excel<TRow>(
    this EnumerableItemFactory factory,
    string label,
    string filePath,
    string sheetName,
    IStorageMediumResolver? resolver = null,
    IReadOnlyList<string>? nullValues = null
  )
    where TRow : notnull, IFlatSchema, ITextSerializable
  {
    var format = nullValues is null
      ? new ExcelFormatSerializer<TRow>(sheetName)
      : new ExcelFormatSerializer<TRow>(sheetName, nullValues);

    var medium = (resolver ?? StorageMediumResolver.Filesystem).Resolve(filePath);

    return new Item<IEnumerable<TRow>>(
      label,
      new ComposedStorageAdapter<IEnumerable<TRow>, TRow>(
        medium,
        reader: format,
        writer: null,
        new EnumerableContainerAdapter<TRow>()
      )
    );
  }

  /// <summary>
  /// Read-only directory of Excel (.xlsx) files, each carrying an
  /// independent collection of <typeparamref name="TRow"/> rows on
  /// the same named sheet. All files must share identical headers.
  /// </summary>
  public static IItem<Directory<IEnumerable<TRow>>> Excel<TRow>(
    this DirectoryItemFactory factory,
    string label,
    string directoryPath,
    string sheetName,
    string filePattern = "*.xlsx",
    IReadOnlyList<string>? nullValues = null
  )
    where TRow : notnull, IFlatSchema, ITextSerializable
  {
    var format = nullValues is null
      ? new ExcelFormatSerializer<TRow>(sheetName)
      : new ExcelFormatSerializer<TRow>(sheetName, nullValues);

    return new Item<Directory<IEnumerable<TRow>>>(
      label,
      new DirectoryStorageAdapter<IEnumerable<TRow>>(
        directoryPath,
        filePattern,
        perFilePath => new ComposedStorageAdapter<IEnumerable<TRow>, TRow>(
          new FileStorageMedium(perFilePath),
          reader: format,
          writer: null,
          new EnumerableContainerAdapter<TRow>()
        )
      )
    );
  }
}
