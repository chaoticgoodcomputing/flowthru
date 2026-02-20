using Flowthru.Effects;

namespace Flowthru.Data.Storage;

/// <summary>
/// Interface for high-level storage operations - abstracts Load/Save with any storage implementation.
/// </summary>
/// <typeparam name="T">The data type (container with rows)</typeparam>
/// <remarks>
/// <para>
/// <strong>Responsibility:</strong> Provide simple Load/Save API regardless of underlying storage strategy.
/// </para>
/// <para>
/// <strong>Abstraction Layer:</strong>
/// </para>
/// <para>
/// This interface hides the complexity of:
/// - Medium selection (file vs memory)
/// - Format serialization (CSV vs JSON vs Parquet)
/// - Container adaptation (IEnumerable vs IDataView)
/// </para>
/// <para>
/// <strong>Implementation Strategies:</strong>
/// </para>
/// <list type="bullet">
/// <item><strong>Composed:</strong> <see cref="ComposedStorageAdapter{TContainer, TRow}"/> - composition of medium + format + container</item>
/// <item><strong>Custom:</strong> User-defined implementations (database, API, etc.)</item>
/// </list>
/// <para>
/// <strong>Effect Types:</strong>
/// </para>
/// <para>
/// All operations return <see cref="FlowIO{A}"/> effects to represent:
/// - I/O operations that can fail
/// - Async execution
/// - Cancellation support
/// - Functional composition
/// </para>
/// <para>
/// <strong>Usage in Catalog Entries:</strong>
/// </para>
/// <para>
/// <see cref="ICatalogEntry{T}"/> delegates to this interface:
/// </para>
/// <code>
/// public class CatalogEntry&lt;T&gt; : ICatalogEntry&lt;T&gt;
/// {
///     private readonly IStorageAdapter&lt;T&gt; _storage;
///
///     public FlowIO&lt;T&gt; Load() => _storage.Load();
///     public FlowIO&lt;FlowUnit&gt; Save(T data) => _storage.Save(data);
/// }
/// </code>
/// </remarks>
/// <example>
/// <code>
/// // Composed storage adapter
/// var storage = new ComposedStorageAdapter&lt;IEnumerable&lt;CompanySchema&gt;, CompanySchema&gt;(
///     medium: new FileStorageMedium("data.csv"),
///     format: new CsvFormatSerializer&lt;CompanySchema&gt;(),
///     container: new EnumerableContainerAdapter&lt;CompanySchema&gt;()
/// );
///
/// var loadResult = await storage.Load().Run();
/// loadResult.Match(
///     Succ: data => Console.WriteLine($"Loaded {data.Count()} rows"),
///     Fail: err => Console.WriteLine($"Load failed: {err}")
/// );
///
/// var saveResult = await storage.Save(companies).Run();
/// </code>
/// </example>
public interface IStorageAdapter<T>
{
  /// <summary>
  /// Loads data from storage.
  /// </summary>
  /// <returns>Effect that produces data on success</returns>
  /// <remarks>
  /// <para>
  /// <strong>Execution Flow:</strong>
  /// </para>
  /// <para>
  /// For composed adapters, this orchestrates:
  /// </para>
  /// <code>
  /// 1. medium.ReadStream()           → Stream
  /// 2. format.DeserializeRows()      → IAsyncEnumerable&lt;TRow&gt;
  /// 3. container.FromRows()          → TContainer
  /// </code>
  /// <para>
  /// <strong>Error Handling:</strong>
  /// </para>
  /// <para>
  /// Errors from any layer are propagated:
  /// - Medium errors (file not found, access denied)
  /// - Format errors (parse failures, schema mismatches)
  /// - Container errors (memory allocation, type conversion)
  /// </para>
  /// </remarks>
  FlowIO<T> Load();

  /// <summary>
  /// Saves data to storage.
  /// </summary>
  /// <param name="data">The data to save</param>
  /// <returns>Effect that completes on successful save</returns>
  /// <remarks>
  /// <para>
  /// <strong>Execution Flow:</strong>
  /// </para>
  /// <para>
  /// For composed adapters, this orchestrates:
  /// </para>
  /// <code>
  /// 1. container.ToRows()            → IAsyncEnumerable&lt;TRow&gt;
  /// 2. format.SerializeRows()        → Stream
  /// 3. medium.WriteStream()          → FlowUnit
  /// </code>
  /// <para>
  /// <strong>Atomicity:</strong>
  /// </para>
  /// <para>
  /// Implementations should strive for atomic saves to avoid partial writes on failure.
  /// </para>
  /// </remarks>
  FlowIO<FlowUnit> Save(T data);

  /// <summary>
  /// Checks if data exists at this storage location.
  /// </summary>
  /// <returns>Effect that produces true if data exists, false otherwise</returns>
  /// <remarks>
  /// <para>
  /// Delegates to the underlying medium's Exists check.
  /// Used to determine if a catalog entry is a seed (Layer 0 input).
  /// </para>
  /// </remarks>
  FlowIO<bool> Exists();
}
