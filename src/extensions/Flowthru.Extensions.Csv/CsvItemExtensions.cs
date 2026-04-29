using Flowthru.Core.Abstractions;
using Flowthru.Core.Data.Storage;
using Flowthru.Core.Data.Storage.Container;
using Flowthru.Core.Data.Storage.Format;
using Flowthru.Core.Data.Storage.Medium;

namespace Flowthru.Core.Data;

/// <summary>
/// Extension methods that add CSV support to <see cref="ItemFactory.Enumerable"/>.
/// </summary>
public static class CsvItemExtensions
{
  /// <summary>
  /// Creates a CSV file catalog entry with IEnumerable container.
  /// </summary>
  /// <typeparam name="TRow">Row schema type (must be flat and text-serializable)</typeparam>
  /// <param name="_">The enumerable catalog entries factory (from <see cref="ItemFactory.Enumerable"/>)</param>
  /// <param name="label">Unique catalog label for DAG resolution</param>
  /// <param name="filePath">Path or URI to CSV file</param>
  /// <param name="resolver">
  /// Optional resolver for remote URIs (e.g., <c>https://</c>, <c>sftp://</c>).
  /// Falls back to <see cref="Flowthru.Core.Data.Storage.Medium.FileStorageMedium"/> when <c>null</c>.
  /// </param>
  /// <param name="medium">
  /// Explicit medium override. Takes precedence over <paramref name="resolver"/> when both
  /// are supplied. Use for per-entry customisation or direct injection in tests.
  /// </param>
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
  /// <param name="nullValues">
  /// Optional set of strings that should deserialize to null for nullable properties.
  /// Defaults to <c>[""]</c> — empty cells (<c>,,</c>) are treated as null, matching CSV
  /// convention. Pass e.g. <c>["", "NA", "N/A", "NULL"]</c> for pandas-style handling of
  /// messy real-world data. The first entry is also used on the write side as the
  /// canonical representation of null.
  /// </param>
  public static Item<IEnumerable<TRow>> Csv<TRow>(
    this EnumerableItemFactory _,
    string label,
    string filePath,
    IStorageMediumResolver? resolver = null,
    IStorageMedium? medium = null,
    IReadOnlyList<string>? nullValues = null
  )
    where TRow : notnull, IFlatSchema, ITextSerializable
  {
    var resolvedMedium = medium ?? resolver?.Resolve(filePath) ?? new FileStorageMedium(filePath);
    var format = nullValues is null
      ? new CsvFormatSerializer<TRow>()
      : new CsvFormatSerializer<TRow>(nullValues);
    var container = new EnumerableContainerAdapter<TRow>();
    var storage = new ComposedStorageAdapter<IEnumerable<TRow>, TRow>(
      resolvedMedium,
      format,
      container
    );

    return new Item<IEnumerable<TRow>>(label, storage);
  }

  /// <summary>
  /// Creates a catalog entry that reads all CSV files in a directory and
  /// concatenates them into a single <see cref="IEnumerable{TRow}"/>.
  /// </summary>
  /// <typeparam name="TRow">Row schema type (must be flat and text-serializable)</typeparam>
  /// <param name="_">The enumerable catalog entries factory (from <see cref="ItemFactory.Enumerable"/>)</param>
  /// <param name="label">Unique catalog label for DAG resolution</param>
  /// <param name="directoryPath">Path to the directory containing the CSV files</param>
  /// <returns>Read-only catalog entry that concatenates every <c>*.csv</c> in the directory</returns>
  /// <remarks>
  /// Files are read in lexicographic order. All files must share the same schema.
  /// This entry is <strong>read-only</strong> — attempting to save will fail with
  /// <see cref="NotSupportedException"/>.
  /// </remarks>
  public static Item<IEnumerable<TRow>> CsvDirectory<TRow>(
    this EnumerableItemFactory _,
    string label,
    string directoryPath,
    IReadOnlyList<string>? nullValues = null
  )
    where TRow : notnull, IFlatSchema, ITextSerializable
  {
    return new Item<IEnumerable<TRow>>(
      label,
      new DirectoryCsvStorageAdapter<TRow>(directoryPath, nullValues)
    );
  }
}
