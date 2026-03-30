using Flowthru.Abstractions;
using Flowthru.Data.Storage;

namespace Flowthru.Data;

public static partial class CatalogEntries
{
  /// <summary>
  /// Factory methods for single (non-collection) values.
  /// </summary>
  /// <remarks>
  /// <para>
  /// These methods create catalog entries for single objects rather than collections.
  /// </para>
  /// <para>
  /// <strong>Use Cases:</strong>
  /// </para>
  /// <list type="bullet">
  /// <item>Model files (ML models, configuration objects)</item>
  /// <item>Metrics and evaluation results (single JSON objects)</item>
  /// <item>Text reports (Markdown, plain text)</item>
  /// <item>Binary files (images, PDFs)</item>
  /// <item>Side-effect-only nodes (null/void semantics)</item>
  /// </list>
  /// </remarks>
  public static partial class Single
  {
    /// <summary>
    /// Creates a JSON file catalog entry for a single object (non-collection).
    /// </summary>
    /// <typeparam name="T">Object type (must be structured-serializable)</typeparam>
    /// <param name="label">Unique catalog label for DAG resolution</param>
    /// <param name="filePath">Path to JSON file</param>
    /// <returns>Catalog entry for singleton JSON object</returns>
    /// <remarks>
    /// <para>
    /// <strong>Use Case:</strong> Model files, configuration objects, metrics, single records
    /// </para>
    /// <para>
    /// <strong>Serialization:</strong> Single JSON object (not wrapped in array)
    /// </para>
    /// <para>
    /// <strong>Implementation:</strong> Uses SingletonJsonStorageAdapter which bypasses
    /// format/container composition for direct object serialization.
    /// </para>
    /// </remarks>
    public static CatalogEntry<T> Json<T>(string label, string filePath)
      where T : IStructuredSerializable
    {
      var storage = new SingletonJsonStorageAdapter<T>(filePath);
      return new CatalogEntry<T>(label, storage);
    }

    /// <summary>
    /// Creates a memory catalog entry for a single object (non-collection).
    /// </summary>
    /// <typeparam name="T">Object type</typeparam>
    /// <param name="label">Unique catalog label for DAG resolution</param>
    /// <returns>Catalog entry for in-memory singleton</returns>
    /// <remarks>
    /// <para>
    /// <strong>Use Case:</strong> Models, charts, computed metrics that stay in memory
    /// </para>
    /// <para>
    /// <strong>Examples:</strong>
    /// </para>
    /// <list type="bullet">
    /// <item>ML models (LinearRegressionModel)</item>
    /// <item>Charts (GenericChart from Plotly.NET)</item>
    /// <item>Metrics objects (ModelMetrics, CrossValidationResults)</item>
    /// <item>Any singleton data that doesn't need persistence</item>
    /// </list>
    /// </remarks>
    public static CatalogEntry<T> Memory<T>(string label)
    {
      var storage = new MemoryStorageAdapter<T>();
      return new CatalogEntry<T>(label, storage);
    }

    /// <summary>
    /// Creates a plain text file catalog entry.
    /// </summary>
    /// <param name="label">Unique catalog label for DAG resolution</param>
    /// <param name="filePath">Path to text file (.txt, .md, etc.)</param>
    /// <returns>Catalog entry for text file with string content</returns>
    /// <remarks>
    /// <para>
    /// <strong>Use Case:</strong> Markdown reports, plain text logs, configuration files
    /// </para>
    /// <para>
    /// <strong>Implementation:</strong> Reads entire file as single string.
    /// </para>
    /// <para>
    /// <strong>Storage Traits:</strong> All traits use filesystem baseline defaults
    /// </para>
    /// </remarks>
    public static CatalogEntry<string> Text(string label, string filePath)
    {
      var storage = new TextFileStorageAdapter(filePath);
      return new CatalogEntry<string>(label, storage);
    }

    /// <summary>
    /// Creates a binary file catalog entry.
    /// </summary>
    /// <param name="label">Unique catalog label for DAG resolution</param>
    /// <param name="filePath">Path to binary file (.png, .jpg, .pdf, etc.)</param>
    /// <returns>Catalog entry for binary file with byte array content</returns>
    /// <remarks>
    /// <para>
    /// <strong>Use Case:</strong> Images (PNG, JPG), PDFs, any binary data
    /// </para>
    /// <para>
    /// <strong>Implementation:</strong> Reads entire file as byte array.
    /// </para>
    /// <para>
    /// <strong>Storage Traits:</strong> All traits use filesystem baseline defaults
    /// </para>
    /// </remarks>
    public static CatalogEntry<byte[]> Binary(string label, string filePath)
    {
      var storage = new BinaryFileStorageAdapter(filePath);
      return new CatalogEntry<byte[]>(label, storage);
    }

    /// <summary>
    /// Creates a null catalog entry for side-effect-only nodes.
    /// </summary>
    /// <typeparam name="T">The data type (typically NoData)</typeparam>
    /// <param name="label">Unique catalog label for DAG resolution</param>
    /// <returns>Catalog entry for void/no-data semantics</returns>
    /// <remarks>
    /// <para>
    /// <strong>Use Case:</strong> Nodes that perform side effects (logging, visualization) without producing meaningful data
    /// </para>
    /// <para>
    /// <strong>Implementation:</strong> Uses NullStorageAdapter which performs no I/O operations.
    /// </para>
    /// <para>
    /// <strong>Storage Traits:</strong>
    /// </para>
    /// <list type="bullet">
    /// <item>CanWrite: false (Save is a no-op)</item>
    /// <item>CanRead: false (Load throws NotSupportedException)</item>
    /// </list>
    /// </remarks>
    public static CatalogEntry<T> Null<T>(string label)
    {
      var storage = new NullStorageAdapter<T>();
      return new CatalogEntry<T>(label, storage);
    }
  }
}
