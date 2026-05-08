using Flowthru.Data.Schema;
using Flowthru.Data.Storage;
using Flowthru.Data.Storage.Parquet;

namespace Flowthru.Data.Catalog;

/// <summary>
/// Extension methods that contribute Parquet smart constructors into
/// <see cref="ItemFactory.Enumerable"/> and
/// <see cref="ItemFactory.Directory"/>. End users see them as
/// <c>ItemFactory.Enumerable.Parquet&lt;TRow&gt;(...)</c> and
/// <c>ItemFactory.Directory.Parquet&lt;TRow&gt;(...)</c>.
/// </summary>
public static class ParquetItemFactoryExtensions
{
  /// <summary>
  /// Parquet file holding rows of <typeparamref name="TRow"/>. Composes
  /// <see cref="FileStorageMedium"/> +
  /// <see cref="ParquetFormatSerializer{TRow}"/> +
  /// <see cref="EnumerableContainerAdapter{TRow}"/>.
  /// </summary>
  /// <param name="factory">The factory anchor — discriminates the extension target.</param>
  /// <param name="label">Catalog label for DAG resolution.</param>
  /// <param name="filePath">Path to the Parquet file.</param>
  /// <param name="options">
  /// Optional performance and behavior tuning. When <c>null</c>,
  /// production-ready defaults apply: Snappy compression, 1 000 000-row
  /// groups, dictionary encoding enabled.
  /// </param>
  public static IItem<IEnumerable<TRow>> Parquet<TRow>(
    this EnumerableItemFactory factory,
    string label,
    string filePath,
    IStorageMediumResolver? resolver = null,
    ParquetItemOptions<TRow>? options = null
  )
    where TRow : notnull, IFlatSchema, IBinarySerializable
  {
    var medium = (resolver ?? StorageMediumResolver.Filesystem).Resolve(filePath);
    return new Item<IEnumerable<TRow>>(
      label,
      new ComposedStorageAdapter<IEnumerable<TRow>, TRow>(
        medium,
        new ParquetFormatSerializer<TRow>(options),
        new EnumerableContainerAdapter<TRow>()
      )
    );
  }

  /// <summary>
  /// Directory of Parquet files, each containing an independent
  /// collection of <typeparamref name="TRow"/> rows of the same schema.
  /// Save hard-deletes existing <c>*.parquet</c> files first so re-runs
  /// are deterministic.
  /// </summary>
  /// <remarks>
  /// All files must share an identical schema. This is intentionally
  /// not a partitioning primitive — each file is an independent unit.
  /// To chunk one logical dataset across files, do that as a step
  /// before write and reassemble in a step after read.
  /// </remarks>
  public static IItem<Directory<IEnumerable<TRow>>> Parquet<TRow>(
    this DirectoryItemFactory factory,
    string label,
    string directoryPath,
    string filePattern = "*.parquet",
    ParquetItemOptions<TRow>? options = null
  )
    where TRow : notnull, IFlatSchema, IBinarySerializable =>
    new Item<Directory<IEnumerable<TRow>>>(
      label,
      new DirectoryStorageAdapter<IEnumerable<TRow>>(
        directoryPath,
        filePattern,
        perFilePath => new ComposedStorageAdapter<IEnumerable<TRow>, TRow>(
          new FileStorageMedium(perFilePath),
          new ParquetFormatSerializer<TRow>(options),
          new EnumerableContainerAdapter<TRow>()
        )
      )
    );
}
