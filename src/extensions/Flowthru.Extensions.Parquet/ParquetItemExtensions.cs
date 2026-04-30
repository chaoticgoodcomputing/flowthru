using Flowthru.Core.Abstractions;
using Flowthru.Core.Data.Storage;
using Flowthru.Core.Data.Storage.Container;
using Flowthru.Core.Data.Storage.Format;
using Flowthru.Core.Data.Storage.Medium;

namespace Flowthru.Core.Data;

/// <summary>
/// Extension methods that add Parquet support to <see cref="ItemFactory.Enumerable"/>.
/// </summary>
public static class ParquetItemExtensions
{
  /// <summary>
  /// Creates a Parquet file catalog entry with IEnumerable container.
  /// </summary>
  /// <typeparam name="TRow">Row schema type (must be flat and binary-serializable)</typeparam>
  /// <param name="_">The enumerable catalog entries factory (from <see cref="ItemFactory.Enumerable"/>)</param>
  /// <param name="label">Unique catalog label for DAG resolution</param>
  /// <param name="filePath">Path or URI to Parquet file</param>
  /// <param name="options">
  /// Optional performance and behavior tuning. When <c>null</c>, production-ready defaults are
  /// used: Snappy compression, 1 000 000-row groups (≈100 MB), dictionary encoding enabled.
  /// </param>
  /// <param name="resolver">
  /// Optional resolver for remote URIs (e.g., <c>https://</c>, <c>sftp://</c>).
  /// Falls back to <see cref="Flowthru.Core.Data.Storage.Medium.FileStorageMedium"/> when <c>null</c>.
  /// </param>
  /// <param name="medium">
  /// Explicit medium override. Takes precedence over <paramref name="resolver"/> when both
  /// are supplied. Use for per-entry customisation or direct injection in tests.
  /// </param>
  /// <returns>Catalog entry with file + Parquet + IEnumerable composition</returns>
  /// <remarks>
  /// <para>
  /// <strong>Requirements:</strong>
  /// </para>
  /// <list type="bullet">
  /// <item>TRow must implement IFlatSchema (Parquet is columnar)</item>
  /// <item>TRow must implement IBinarySerializable</item>
  /// </list>
  /// <para>
  /// <strong>Performance:</strong> Write path streams in bounded row-group batches —
  /// peak memory scales with row-group size, not total dataset size. Suitable for 1–10 GB datasets.
  /// </para>
  /// </remarks>
  public static Item<IEnumerable<TRow>> Parquet<TRow>(
    this EnumerableItemFactory _,
    string label,
    string filePath,
    ParquetItemOptions<TRow>? options = null,
    IStorageMediumResolver? resolver = null,
    IStorageMedium? medium = null
  )
    where TRow : notnull, IFlatSchema, IBinarySerializable
  {
    var resolvedMedium = medium ?? resolver?.Resolve(filePath) ?? new FileStorageMedium(filePath);
    var format = new ParquetFormatSerializer<TRow>(options);
    var container = new EnumerableContainerAdapter<TRow>();
    var storage = new ComposedStorageAdapter<IEnumerable<TRow>, TRow>(
      resolvedMedium,
      format,
      container
    );

    return new Item<IEnumerable<TRow>>(label, storage);
  }

  /// <summary>
  /// Creates a catalog entry over a directory of Parquet files where each file is one
  /// independent row collection of the same schema. Read produces a
  /// <see cref="Directory{T}"/> keyed by full file path; Save writes one Parquet file per
  /// entry, deleting any existing <c>*.parquet</c> in the directory first so re-runs are
  /// deterministic.
  /// </summary>
  /// <typeparam name="TRow">Row schema type (must be flat and binary-serializable)</typeparam>
  /// <param name="_">The enumerable catalog entries factory (from <see cref="ItemFactory.Enumerable"/>)</param>
  /// <param name="label">Unique catalog label for DAG resolution</param>
  /// <param name="directoryPath">Path to the directory containing the Parquet files</param>
  /// <param name="options">
  /// Optional performance and behavior tuning applied uniformly to every file in the
  /// directory; see <see cref="Parquet{TRow}"/> for details.
  /// </param>
  /// <remarks>
  /// All files must share the same schema. This is intentionally not a partitioning
  /// primitive — each file represents an independent unit. If you need to chunk a single
  /// logical dataset across files, do that in a step before write and reassemble in a step
  /// after read.
  /// </remarks>
  public static Item<Directory<IEnumerable<TRow>>> ParquetDirectory<TRow>(
    this EnumerableItemFactory _,
    string label,
    string directoryPath,
    ParquetItemOptions<TRow>? options = null
  )
    where TRow : notnull, IFlatSchema, IBinarySerializable
  {
    var format = new ParquetFormatSerializer<TRow>(options);
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
        filePattern: "*.parquet",
        perFileAdapter: PerFileAdapter
      )
    );
  }
}
