using Flowthru.Abstractions;
using Flowthru.Data.Storage;
using Flowthru.Data.Storage.Container;
using Flowthru.Data.Storage.Format;
using Flowthru.Data.Storage.Medium;

namespace Flowthru.Data;

public static partial class CatalogEntries
{
  /// <summary>
  /// Factory methods for IEnumerable&lt;T&gt; container types.
  /// </summary>
  /// <remarks>
  /// <para>
  /// IEnumerable&lt;T&gt; is the standard .NET collection interface.
  /// </para>
  /// <para>
  /// <strong>Characteristics:</strong>
  /// </para>
  /// <list type="bullet">
  /// <item><strong>Lazy evaluation:</strong> LINQ queries deferred until enumeration</item>
  /// <item><strong>Re-enumerable:</strong> Can cause side effects (multiple DB hits, file reads)</item>
  /// <item><strong>Mutable:</strong> Underlying collection can be modified</item>
  /// <item><strong>Standard .NET:</strong> Works with all .NET libraries</item>
  /// </list>
  /// <para>
  /// <strong>Use Cases:</strong>
  /// </para>
  /// <list type="bullet">
  /// <item>Standard data processing pipelines (90% of cases)</item>
  /// <item>Interop with .NET libraries expecting IEnumerable</item>
  /// <item>LINQ query composition</item>
  /// <item>Large datasets where you'll enumerate only once</item>
  /// </list>
  /// </remarks>
  public static class Enumerable
  {
    /// <summary>
    /// Creates a CSV file catalog entry with IEnumerable container.
    /// </summary>
    /// <typeparam name="TRow">Row schema type (must be flat and text-serializable)</typeparam>
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
    /// <item>TRow must have parameterless constructor</item>
    /// </list>
    /// <para>
    /// <strong>Capabilities:</strong>
    /// </para>
    /// <list type="bullet">
    /// <item>ISeedable: Can be Layer 0 input if file exists</item>
    /// <item>IReadOnly: false (CSV files are read-write)</item>
    /// </list>
    /// </remarks>
    public static ICatalogEntry<IEnumerable<TRow>> Csv<TRow>(string label, string filePath)
      where TRow : IFlatSchema, ITextSerializable, new()
    {
      var medium = new FileStorageMedium(filePath);
      var format = new CsvFormatSerializer<TRow>();
      var container = new EnumerableContainerAdapter<TRow>();
      var storage = new ComposedStorageAdapter<IEnumerable<TRow>, TRow>(medium, format, container);

      return new CatalogEntry<IEnumerable<TRow>>(label, storage);
    }

    /// <summary>
    /// Creates a JSON file catalog entry with IEnumerable container for collections.
    /// </summary>
    /// <typeparam name="TRow">Row schema type (must be structured-serializable)</typeparam>
    /// <param name="label">Unique catalog label for DAG resolution</param>
    /// <param name="filePath">Path to JSON file</param>
    /// <returns>Catalog entry with file + JSON + IEnumerable composition</returns>
    /// <remarks>
    /// <para>
    /// <strong>Requirements:</strong>
    /// </para>
    /// <list type="bullet">
    /// <item>TRow must implement IStructuredSerializable</item>
    /// <item>TRow must have parameterless constructor</item>
    /// <item>TRow supports both flat and nested schemas</item>
    /// </list>
    /// <para>
    /// <strong>Serialization:</strong> JSON array format for collections
    /// </para>
    /// </remarks>
    public static ICatalogEntry<IEnumerable<TRow>> Json<TRow>(string label, string filePath)
      where TRow : IStructuredSerializable, new()
    {
      var medium = new FileStorageMedium(filePath);
      var format = new JsonFormatSerializer<TRow>();
      var container = new EnumerableContainerAdapter<TRow>();
      var storage = new ComposedStorageAdapter<IEnumerable<TRow>, TRow>(medium, format, container);

      return new CatalogEntry<IEnumerable<TRow>>(label, storage);
    }

    /// <summary>
    /// Creates a Parquet file catalog entry with IEnumerable container.
    /// </summary>
    /// <typeparam name="TRow">Row schema type (must be flat and binary-serializable)</typeparam>
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
    /// <item>TRow must have parameterless constructor</item>
    /// </list>
    /// <para>
    /// <strong>Performance:</strong> Optimized for large datasets with columnar storage
    /// </para>
    /// </remarks>
    public static ICatalogEntry<IEnumerable<TRow>> Parquet<TRow>(string label, string filePath)
      where TRow : IFlatSchema, IBinarySerializable, new()
    {
      var medium = new FileStorageMedium(filePath);
      var format = new ParquetFormatSerializer<TRow>();
      var container = new EnumerableContainerAdapter<TRow>();
      var storage = new ComposedStorageAdapter<IEnumerable<TRow>, TRow>(medium, format, container);

      return new CatalogEntry<IEnumerable<TRow>>(label, storage);
    }

    /// <summary>
    /// Creates a read-only Excel file catalog entry with IEnumerable container.
    /// </summary>
    /// <typeparam name="TRow">Row schema type (must be flat and text-serializable)</typeparam>
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
    /// <item>TRow must have parameterless constructor</item>
    /// </list>
    /// <para>
    /// <strong>Limitations:</strong> Read-only support via ExcelDataReader.
    /// Writing Excel files is not supported.
    /// </para>
    /// <para>
    /// <strong>Capabilities:</strong>
    /// </para>
    /// <list type="bullet">
    /// <item>ISeedable: true (can be Layer 0 input)</item>
    /// <item>IReadOnly: true (writes will throw NotSupportedException)</item>
    /// </list>
    /// </remarks>
    public static ICatalogEntry<IEnumerable<TRow>> Excel<TRow>(
      string label,
      string filePath,
      string sheetName
    )
      where TRow : IFlatSchema, ITextSerializable, new()
    {
      var medium = new FileStorageMedium(filePath);
      var format = new ExcelFormatSerializer<TRow>(sheetName);
      var container = new EnumerableContainerAdapter<TRow>();
      var storage = new ComposedStorageAdapter<IEnumerable<TRow>, TRow>(medium, format, container);

      return new CatalogEntry<IEnumerable<TRow>>(label, storage);
    }

    /// <summary>
    /// Creates an in-memory transient catalog entry with IEnumerable container.
    /// </summary>
    /// <typeparam name="TRow">Row schema type</typeparam>
    /// <param name="label">Unique catalog label for DAG resolution</param>
    /// <returns>Catalog entry with memory storage (no serialization)</returns>
    /// <remarks>
    /// <para>
    /// <strong>Use Case:</strong> Intermediate pipeline data that doesn't need persistence
    /// </para>
    /// <para>
    /// <strong>Capabilities:</strong>
    /// </para>
    /// <list type="bullet">
    /// <item>ISeedable: false (memory entries cannot be Layer 0 inputs)</item>
    /// <item>IReadOnly: false</item>
    /// </list>
    /// <para>
    /// <strong>Lifecycle:</strong> Data lost when process ends
    /// </para>
    /// </remarks>
    public static ICatalogEntry<IEnumerable<TRow>> Memory<TRow>(string label)
    {
      var storage = new MemoryStorageAdapter<IEnumerable<TRow>>();
      return new CatalogEntry<IEnumerable<TRow>>(label, storage);
    }
  }
}
