using Flowthru.Core.Data.Capabilities;
using Flowthru.Core.Effects;

namespace Flowthru.Core.Data.Storage;

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
/// <see cref="IItem{T}"/> delegates to this interface:
/// </para>
/// <code>
/// public class Item&lt;T&gt; : IItem&lt;T&gt;
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
  /// Structural constraints and capabilities of this storage implementation.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Adapter authors must declare what their storage can and cannot do.
  /// These are intrinsic properties of the storage medium, not runtime state.
  /// </para>
  /// <para>
  /// Pipeline validation uses these traits to fail fast when a pipeline attempts
  /// invalid operations (e.g., writing to a read-only source).
  /// </para>
  /// </remarks>
  StorageTraits Traits { get; }

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

  /// <summary>
  /// Performs shallow validation by checking data availability and sampling a subset of data.
  /// </summary>
  /// <param name="sampleSize">Number of rows/records to sample for validation</param>
  /// <returns>Effect producing validation result</returns>
  /// <remarks>
  /// <para>
  /// <strong>Semantic Intent:</strong> Validate that data is available and accessible.
  /// </para>
  /// <para>
  /// <strong>Typical Checks:</strong>
  /// </para>
  /// <list type="bullet">
  /// <item>Data source exists (file, table, etc.)</item>
  /// <item>Data source is accessible (permissions, connectivity)</item>
  /// <item>Sample rows can be read and deserialized successfully</item>
  /// <item>Schema matches expected structure</item>
  /// </list>
  /// <para>
  /// <strong>Implementation Guidelines:</strong>
  /// </para>
  /// <list type="bullet">
  /// <item>File adapters: Check file exists, read and validate sample rows</item>
  /// <item>Memory adapters: Check if data has been initialized</item>
  /// <item>Database adapters: Check table exists, query sample rows</item>
  /// <item>Null adapters: Always return success (no data required)</item>
  /// </list>
  /// <para>
  /// <strong>Performance:</strong> Should be fast (~10-100ms) - suitable for pre-flight validation.
  /// </para>
  /// </remarks>
  FlowIO<Data.Validation.ValidationResult> InspectShallow(int sampleSize);

  /// <summary>
  /// Performs deep validation by examining the entire dataset.
  /// </summary>
  /// <returns>Effect producing validation result</returns>
  /// <remarks>
  /// <para>
  /// <strong>Semantic Intent:</strong> Validate that all data is available, accessible, and valid.
  /// </para>
  /// <para>
  /// <strong>Additional Checks Beyond Shallow:</strong>
  /// </para>
  /// <list type="bullet">
  /// <item>Validate ALL rows can be deserialized (not just sample)</item>
  /// <item>Check data quality constraints across entire dataset</item>
  /// <item>Detect corruption or inconsistencies throughout data</item>
  /// </list>
  /// <para>
  /// <strong>Implementation Guidelines:</strong>
  /// </para>
  /// <list type="bullet">
  /// <item>File adapters: Read and validate entire file</item>
  /// <item>Memory adapters: Validate all stored data</item>
  /// <item>Database adapters: Full table scan with validation</item>
  /// <item>Null adapters: Always return success (no data required)</item>
  /// </list>
  /// <para>
  /// <strong>Performance:</strong> Potentially expensive - only use when data integrity is critical.
  /// </para>
  /// </remarks>
  FlowIO<Data.Validation.ValidationResult> InspectDeep();

  /// <summary>
  /// Validates that this storage location is accessible as a write destination.
  /// </summary>
  /// <returns>Effect producing validation result</returns>
  /// <remarks>
  /// <para>
  /// <strong>Semantic Intent:</strong> Validate that the destination can accept writes
  /// before any pipeline step executes. This is distinct from <see cref="InspectShallow"/>,
  /// which validates that readable data exists.
  /// </para>
  /// <para>
  /// <strong>Typical Checks:</strong>
  /// </para>
  /// <list type="bullet">
  /// <item>File adapters: Parent directory exists and process has write permission</item>
  /// <item>Database adapters: Target table exists, schema is compatible, connection is valid</item>
  /// <item>Read-only adapters (<c>CanWrite = false</c>): Return success trivially</item>
  /// <item>Memory / null adapters: Return success trivially</item>
  /// </list>
  /// <para>
  /// <strong>When Called:</strong> During pre-flight validation, after external inputs are
  /// inspected and before any step executes. Skipped if <c>Traits.CanInspect = false</c>
  /// or if explicitly disabled via <c>ValidationOptions.SkipTargetInspection()</c>.
  /// </para>
  /// </remarks>
  FlowIO<Data.Validation.ValidationResult> InspectTarget();
}
