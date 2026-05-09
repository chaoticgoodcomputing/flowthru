using Flowthru.Data.Schema;
using Flowthru.Data.Storage;
using Flowthru.Data.Storage.Csv;

namespace Flowthru.Data.Catalog;

/// <summary>
/// Extension methods that contribute CSV smart constructors into
/// <see cref="ItemFactory.Enumerable"/> and
/// <see cref="ItemFactory.Directory"/>. End users see them as
/// <c>ItemFactory.Enumerable.Csv&lt;TRow&gt;(...)</c> and
/// <c>ItemFactory.Directory.Csv&lt;TRow&gt;(...)</c> via a single
/// <c>using Flowthru.Data.Catalog;</c> import.
/// </summary>
public static class CsvItemFactoryExtensions
{
  /// <summary>
  /// CSV file holding rows of <typeparamref name="TRow"/>. Composes a
  /// resolver-dispatched <see cref="IStorageMedium"/> +
  /// <see cref="CsvFormatSerializer{TRow}"/> +
  /// <see cref="EnumerableContainerAdapter{TRow}"/>.
  /// </summary>
  /// <param name="_">The factory anchor — discriminates extension target.</param>
  /// <param name="label">Catalog label for DAG resolution.</param>
  /// <param name="filePath">
  /// Path or URI to the CSV source. Bare paths and <c>file://</c>
  /// URIs always resolve to a <see cref="FileStorageMedium"/>. Other
  /// schemes (e.g. <c>https://</c>) require <paramref name="resolver"/>
  /// to be supplied with the corresponding
  /// <see cref="IStorageMediumProvider"/> registered (e.g. via
  /// <c>builder.UseHttp()</c>).
  /// </param>
  /// <param name="resolver">
  /// Optional storage-medium resolver. When null, the smart
  /// constructor uses <see cref="StorageMediumResolver.Filesystem"/>
  /// — a singleton that only resolves bare paths and <c>file://</c>
  /// URIs. Pass a DI-resolved <see cref="IStorageMediumResolver"/>
  /// (typically via the catalog's constructor) to enable
  /// network-medium dispatch.
  /// </param>
  /// <param name="nullValues">
  /// Optional null-sentinel list for nullable properties. Defaults to
  /// <c>[""]</c> (empty cells round-trip as null). Pass
  /// <c>["", "NA", "N/A", "NULL"]</c> for pandas-style messy-data
  /// handling. The first entry is also the canonical write-side
  /// representation when a nullable property is null.
  /// </param>
  public static IItem<IEnumerable<TRow>> Csv<TRow>(
    this EnumerableItemFactory _,
    string label,
    string filePath,
    IStorageMediumResolver? resolver = null,
    IReadOnlyList<string>? nullValues = null
  )
    where TRow : notnull, IFlatSchema, ITextSerializable
  {
    var format = nullValues is null
      ? new CsvFormatSerializer<TRow>()
      : new CsvFormatSerializer<TRow>(nullValues);

    var medium = (resolver ?? StorageMediumResolver.Filesystem).Resolve(filePath);

    return new Item<IEnumerable<TRow>>(
      label,
      new ComposedStorageAdapter<IEnumerable<TRow>, TRow>(
        medium,
        format,
        new EnumerableContainerAdapter<TRow>()
      )
    );
  }

  /// <summary>
  /// Directory of CSV files, each containing an independent collection
  /// of <typeparamref name="TRow"/> rows of the same schema. Save
  /// hard-deletes existing <c>*.csv</c> files first so re-runs are
  /// deterministic.
  /// </summary>
  /// <remarks>
  /// All files must share identical headers. This is intentionally not
  /// a partitioning primitive — each file is an independent unit. To
  /// chunk one logical dataset across files, do that as a step before
  /// write and reassemble in a step after read.
  /// </remarks>
  public static IItem<DirectoryOf<IEnumerable<TRow>>> Csv<TRow>(
    this DirectoryItemFactory _,
    string label,
    string directoryPath,
    string filePattern = "*.csv",
    IReadOnlyList<string>? nullValues = null
  )
    where TRow : notnull, IFlatSchema, ITextSerializable
  {
    var format = nullValues is null
      ? new CsvFormatSerializer<TRow>()
      : new CsvFormatSerializer<TRow>(nullValues);

    return new Item<DirectoryOf<IEnumerable<TRow>>>(
      label,
      new DirectoryStorageAdapter<IEnumerable<TRow>>(
        directoryPath,
        filePattern,
        perFilePath => new ComposedStorageAdapter<IEnumerable<TRow>, TRow>(
          new FileStorageMedium(perFilePath),
          format,
          new EnumerableContainerAdapter<TRow>()
        )
      )
    );
  }
}
