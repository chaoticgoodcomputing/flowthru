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
    /// <param name="filePath">Path to Parquet file</param>
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
    /// <strong>Performance:</strong> Optimized for large datasets with columnar storage.
    /// </para>
    /// </remarks>
    public static Item<IEnumerable<TRow>> Parquet<TRow>(
      this EnumerableItemFactory _,
      string label,
      string filePath
    )
      where TRow : notnull, IFlatSchema, IBinarySerializable
    {
        var medium = new FileStorageMedium(filePath);
        var format = new ParquetFormatSerializer<TRow>();
        var container = new EnumerableContainerAdapter<TRow>();
        var storage = new ComposedStorageAdapter<IEnumerable<TRow>, TRow>(medium, format, container);

        return new Item<IEnumerable<TRow>>(label, storage);
    }
}
