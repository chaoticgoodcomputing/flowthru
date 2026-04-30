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
  /// Creates a catalog entry over a directory of CSV files where each file is one
  /// independent row collection of the same schema. Read produces a
  /// <see cref="Directory{T}"/> keyed by full file path; Save writes one CSV per entry,
  /// deleting any existing <c>*.csv</c> in the directory first so re-runs are deterministic.
  /// </summary>
  /// <typeparam name="TRow">Row schema type (must be flat and text-serializable)</typeparam>
  /// <param name="_">The enumerable catalog entries factory (from <see cref="ItemFactory.Enumerable"/>)</param>
  /// <param name="label">Unique catalog label for DAG resolution</param>
  /// <param name="directoryPath">Path to the directory containing the CSV files</param>
  /// <param name="nullValues">
  /// Optional set of strings that should deserialize to null for nullable properties; see
  /// <see cref="Csv{TRow}"/> for details. Applied uniformly to every file in the directory.
  /// </param>
  /// <remarks>
  /// All files must share the same schema (identical column headers). This is intentionally
  /// not a partitioning primitive — each file represents an independent unit. If you need
  /// to chunk a single logical dataset across files, do that in a step before write and
  /// reassemble in a step after read.
  /// </remarks>
  public static Item<Directory<IEnumerable<TRow>>> CsvDirectory<TRow>(
    this EnumerableItemFactory _,
    string label,
    string directoryPath,
    IReadOnlyList<string>? nullValues = null
  )
    where TRow : notnull, IFlatSchema, ITextSerializable
  {
    var format = nullValues is null
      ? new CsvFormatSerializer<TRow>()
      : new CsvFormatSerializer<TRow>(nullValues);
    var container = new EnumerableContainerAdapter<TRow>();

    IStorageAdapter<IEnumerable<TRow>> PerFileAdapter(string path) =>
      new ComposedStorageAdapter<IEnumerable<TRow>, TRow>(
        new FileStorageMedium(path),
        format,
        container
      );

    return new Item<Directory<IEnumerable<TRow>>>(
      label,
      new DirectoryStorageAdapter<IEnumerable<TRow>>(
        directoryPath: directoryPath,
        filePattern: "*.csv",
        perFileAdapter: PerFileAdapter
      )
    );
  }
}
