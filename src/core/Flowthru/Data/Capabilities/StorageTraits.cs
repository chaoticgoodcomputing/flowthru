namespace Flowthru.Data.Capabilities;

/// <summary>
/// Describes the structural constraints and capabilities of a storage implementation.
/// Defaults represent filesystem-file baseline behavior.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Design Philosophy:</strong>
/// </para>
/// <para>
/// A filesystem file is the median storage mechanism — the "zero" from which we measure deviations.
/// A <strong>constraint</strong> narrows from this baseline (e.g., read-only, non-persistent, non-inspectable).
/// A <strong>capability</strong> widens beyond it (e.g., streamable, appendable, transactional).
/// </para>
/// <para>
/// <strong>Constraint Examples:</strong>
/// </para>
/// <list type="bullet">
/// <item><strong>Read-only sources:</strong> HTTP GET endpoints, Excel files, database views — set <c>CanWrite = false</c></item>
/// <item><strong>Write-only sinks:</strong> Logging endpoints, append-only audit tables — set <c>CanRead = false</c></item>
/// <item><strong>Non-inspectable:</strong> Remote sources that can't be sampled cheaply — set <c>CanInspect = false</c></item>
/// <item><strong>Non-persistent:</strong> In-memory caches, temporary buffers — set <c>IsPersistent = false</c></item>
/// <item><strong>Network-dependent:</strong> Remote databases, S3, HTTP — set <c>RequiresNetwork = true</c></item>
/// </list>
/// <para>
/// <strong>Capability Examples:</strong>
/// </para>
/// <list type="bullet">
/// <item><strong>Streamable:</strong> CSV files, database queries, Parquet — set <c>CanStream = true</c></item>
/// <item><strong>Appendable:</strong> Log files, Spark SaveMode.Append, append-only tables — set <c>CanAppend = true</c></item>
/// <item><strong>Transactional:</strong> Database writes, ACID-compliant stores — set <c>IsTransactional = true</c></item>
/// </list>
/// <para>
/// <strong>Two-Level Constraint Model:</strong>
/// </para>
/// <para>
/// Traits are declared at two levels:
/// </para>
/// <list type="bullet">
/// <item><strong>Adapter level:</strong> The adapter author declares what the storage medium intrinsically supports.
/// These are structural truths (e.g., an HTTP GET endpoint cannot write).</item>
/// <item><strong>Catalog level:</strong> The pipeline author can further constrain an adapter using <c>CatalogEntry.Constrain()</c>.
/// Constraints can only tighten, never loosen (one-way ratchet).</item>
/// </list>
/// <para>
/// <strong>Usage in Adapters:</strong>
/// </para>
/// <code>
/// public sealed class EFCoreStorageAdapter&lt;T&gt; : IStorageAdapter&lt;IEnumerable&lt;T&gt;&gt;
/// {
///     public StorageTraits Traits { get; }
///
///     public EFCoreStorageAdapter(DbContext context, bool readOnly = false)
///     {
///         Traits = new StorageTraits
///         {
///             CanWrite = !readOnly,
///             RequiresNetwork = true,
///             IsTransactional = true,
///             CanStream = true,
///         };
///     }
/// }
/// </code>
/// <para>
/// <strong>Usage in Catalogs:</strong>
/// </para>
/// <code>
/// public ICatalogEntry&lt;IEnumerable&lt;Company&gt;&gt; ReferenceData =&gt;
///     GetOrCreateEntry(() =&gt; CatalogEntries.Enumerable.Csv&lt;Company&gt;(
///         "ref_data", $"{_basePath}/reference.csv")
///         .Constrain(t =&gt; t with { CanWrite = false }));
/// </code>
/// </remarks>
public record StorageTraits
{
  // ── Constraints (narrow from baseline = filesystem file) ──

  /// <summary>
  /// Can data be read from this source?
  /// </summary>
  /// <remarks>
  /// Default: <c>true</c> (filesystem files are readable).
  /// Set to <c>false</c> for write-only sinks (logging endpoints, audit tables).
  /// </remarks>
  public bool CanRead { get; init; } = true;

  /// <summary>
  /// Can data be written to this source?
  /// </summary>
  /// <remarks>
  /// Default: <c>true</c> (filesystem files are writable).
  /// Set to <c>false</c> for read-only sources (HTTP GET, Excel files, database views).
  /// </remarks>
  public bool CanWrite { get; init; } = true;

  /// <summary>
  /// Can the source be inspected for pre-flight validation?
  /// </summary>
  /// <remarks>
  /// Default: <c>true</c> (filesystem files can be sampled).
  /// Set to <c>false</c> for sources that are expensive to validate (remote HTTP, distributed Spark).
  /// </remarks>
  public bool CanInspect { get; init; } = true;

  /// <summary>
  /// Does data survive across pipeline runs?
  /// </summary>
  /// <remarks>
  /// Default: <c>true</c> (filesystem files persist).
  /// Set to <c>false</c> for in-memory caches, temporary buffers, or transient state.
  /// </remarks>
  public bool IsPersistent { get; init; } = true;

  /// <summary>
  /// Does this storage require network access?
  /// </summary>
  /// <remarks>
  /// Default: <c>false</c> (filesystem files are local).
  /// Set to <c>true</c> for remote databases, S3, HTTP endpoints.
  /// Used for pre-flight validation in offline/CI environments.
  /// </remarks>
  public bool RequiresNetwork { get; init; } = false;

  // ── Capabilities (widen beyond baseline = filesystem file) ──

  /// <summary>
  /// Can data be lazily streamed without full materialization?
  /// </summary>
  /// <remarks>
  /// Default: <c>false</c> (filesystem file I/O typically buffers).
  /// Set to <c>true</c> for CSV streaming, database cursors, Parquet row groups.
  /// Enables memory-efficient processing of large datasets.
  /// </remarks>
  public bool CanStream { get; init; } = false;

  /// <summary>
  /// Can data be appended without replacing existing data?
  /// </summary>
  /// <remarks>
  /// Default: <c>false</c> (filesystem file writes typically overwrite).
  /// Set to <c>true</c> for append-only logs, Spark SaveMode.Append, incremental tables.
  /// </remarks>
  public bool CanAppend { get; init; } = false;

  /// <summary>
  /// Are writes atomic (all-or-nothing)?
  /// </summary>
  /// <remarks>
  /// Default: <c>false</c> (filesystem file writes are not ACID).
  /// Set to <c>true</c> for database transactions, ACID-compliant stores.
  /// </remarks>
  public bool IsTransactional { get; init; } = false;
}
