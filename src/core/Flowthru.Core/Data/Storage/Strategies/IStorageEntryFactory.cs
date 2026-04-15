using Flowthru.Core.Abstractions;

namespace Flowthru.Core.Data.Storage.Strategies;

/// <summary>
/// Factory for creating catalog entries with environment-specific storage.
/// </summary>
/// <remarks>
/// <para>
/// The strategy pattern enables the same catalog to use different storage
/// backends based on the environment:
/// </para>
/// <list type="bullet">
/// <item><strong>Development:</strong> CSV files for easy inspection and version control</item>
/// <item><strong>Production:</strong> Database tables for scalability and transactions</item>
/// <item><strong>Testing:</strong> In-memory storage for fast, isolated tests</item>
/// </list>
/// <para>
/// <strong>Usage Pattern:</strong>
/// </para>
/// <code>
/// public class MyCatalog : DataCatalogBase
/// {
///     private readonly IStorageEntryFactory _storage;
///
///     public MyCatalog(IStorageEntryFactory storage)
///     {
///         _storage = storage;
///         InitializeCatalogProperties();
///     }
///
///     public IItem&lt;IEnumerable&lt;Company&gt;&gt; Companies =>
///         CreateEntry(() => _storage.CreateEnumerable&lt;Company&gt;("Companies"));
/// }
/// </code>
/// </remarks>
public interface IStorageEntryFactory
{
    /// <summary>
    /// Creates a catalog entry for an enumerable dataset.
    /// </summary>
    /// <typeparam name="T">Schema type (must implement IFlatSchema and ITextSerializable)</typeparam>
    /// <param name="label">Catalog label for the entry</param>
    /// <param name="options">Optional storage options</param>
    /// <returns>Configured catalog entry</returns>
    /// <remarks>
    /// <para>
    /// Type constraints ensure compatibility with CSV and Parquet serialization.
    /// Memory storage also works since it has no serialization requirements.
    /// </para>
    /// <para>
    /// If options.Path is null, the label is used to derive a default path
    /// (e.g., "Companies" → "Companies.csv" or "dbo.Companies").
    /// </para>
    /// </remarks>
    IItem<IEnumerable<T>> CreateEnumerable<T>(string label, StorageOptions? options = null)
      where T : notnull, IFlatSchema, ITextSerializable;

    /// <summary>
    /// Creates a catalog entry for a singleton object.
    /// </summary>
    /// <typeparam name="T">Object type (must implement IStructuredSerializable)</typeparam>
    /// <param name="label">Catalog label for the entry</param>
    /// <param name="options">Optional storage options</param>
    /// <returns>Configured catalog entry</returns>
    /// <remarks>
    /// <para>
    /// Type constraint ensures compatibility with JSON serialization.
    /// Memory storage also works since it has no serialization requirements.
    /// </para>
    /// <para>
    /// Typically uses structured formats (JSON, MessagePack) for singletons.
    /// </para>
    /// </remarks>
    IItem<T> CreateSingle<T>(string label, StorageOptions? options = null)
      where T : IStructuredSerializable;
}
