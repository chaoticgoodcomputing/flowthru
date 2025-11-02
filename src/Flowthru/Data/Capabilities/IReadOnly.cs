namespace Flowthru.Data.Capabilities;

/// <summary>
/// Capability interface for catalog entries that are read-only.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Purpose:</strong> Prevent writes to data sources that don't support modification.
/// </para>
/// <para>
/// <strong>Read-Only Data Sources:</strong>
/// </para>
/// <list type="bullet">
/// <item><strong>Excel files:</strong> Complex format with formatting/formulas - risky to modify</item>
/// <item><strong>API endpoints:</strong> External services with no write access</item>
/// <item><strong>Database views:</strong> Read-only query results</item>
/// <item><strong>Archived data:</strong> Immutable historical snapshots</item>
/// <item><strong>Reference data:</strong> Master data that shouldn't change</item>
/// </list>
/// <para>
/// <strong>Compile-Time Safety:</strong>
/// </para>
/// <para>
/// While we can't enforce read-only at compile-time (catalog entries use the same interface),
/// runtime checks prevent accidental writes:
/// </para>
/// <code>
/// if (catalogEntry is IReadOnly { IsReadOnly: true })
/// {
///     throw new InvalidOperationException($"Cannot write to read-only entry '{catalogEntry.Key}'");
/// }
/// </code>
/// <para>
/// <strong>Design Philosophy:</strong>
/// </para>
/// <para>
/// This follows Flowthru's principle of "fail early":
/// - Development: Discover read-only violations during first test run
/// - Production: Prevent data corruption from misconfigured pipelines
/// </para>
/// <para>
/// <strong>Pipeline Implications:</strong>
/// </para>
/// <para>
/// Read-only entries can only appear as:
/// - Node inputs (reads are allowed)
/// - Pipeline seeds (Layer 0 external data)
/// </para>
/// <para>
/// Read-only entries CANNOT appear as:
/// - Node outputs (writes would fail)
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Excel catalog entry - read-only by default
/// public class ExcelStorageAdapter&lt;T&gt; : IStorageAdapter&lt;IEnumerable&lt;T&gt;&gt;, IReadOnly
/// {
///     public bool IsReadOnly => true;
///
///     public IO&lt;Unit&gt; Save(IEnumerable&lt;T&gt; data)
///     {
///         return IO.fail&lt;Unit&gt;(Error.New("Cannot write to Excel files"));
///     }
/// }
///
/// // API endpoint - read-only
/// public class ApiStorageAdapter&lt;T&gt; : IStorageAdapter&lt;T&gt;, IReadOnly
/// {
///     public bool IsReadOnly => true;
/// }
///
/// // CSV file - writable
/// public class CsvStorageAdapter&lt;T&gt; : IStorageAdapter&lt;IEnumerable&lt;T&gt;&gt;, IReadOnly
/// {
///     public bool IsReadOnly => false;  // Supports writes
/// }
///
/// // Usage in pipeline builder
/// pipeline.AddNode(
///     name: "ProcessData",
///     transform: node,
///     input: catalog.ExcelData,    // ✅ Read-only as input - OK
///     output: catalog.CsvOutput    // ✅ Writable output - OK
/// );
///
/// pipeline.AddNode(
///     name: "InvalidNode",
///     transform: node,
///     input: catalog.CsvInput,
///     output: catalog.ExcelData    // ❌ Read-only as output - Runtime error!
/// );
/// </code>
/// </example>
public interface IReadOnly
{
  /// <summary>
  /// Gets whether this catalog entry is read-only.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Returns true if:
  /// - Data source doesn't support writes (Excel, API)
  /// - Data is archived/immutable
  /// - Writes would be dangerous (reference data)
  /// </para>
  /// <para>
  /// Returns false if:
  /// - Data source supports writes (CSV, JSON, Parquet, database)
  /// - Modifications are safe and expected
  /// </para>
  /// </remarks>
  bool IsReadOnly { get; }
}
