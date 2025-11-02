namespace Flowthru.Data.Capabilities;

/// <summary>
/// Capability interface for catalog entries that can be seed inputs (Layer 0).
/// </summary>
/// <remarks>
/// <para>
/// <strong>Purpose:</strong> Identify catalog entries that exist before pipeline execution.
/// </para>
/// <para>
/// <strong>Seed vs Derived Data:</strong>
/// </para>
/// <list type="bullet">
/// <item><strong>Seed (Layer 0):</strong> Exists before pipeline runs (external data sources)</item>
/// <item><strong>Derived (Layer N):</strong> Produced by nodes in the pipeline</item>
/// </list>
/// <para>
/// <strong>Dependency Resolution:</strong>
/// </para>
/// <para>
/// The pipeline executor uses this capability to:
/// </para>
/// <list type="bullet">
/// <item>Identify which nodes can run first (Layer 0 consumers)</item>
/// <item>Validate that all required seeds exist before execution</item>
/// <item>Build the dependency graph via topological sort</item>
/// </list>
/// <para>
/// <strong>Implementation Pattern:</strong>
/// </para>
/// <para>
/// Most storage adapters implement this by checking if data exists:
/// </para>
/// <code>
/// public bool CanBeSeed => Exists().Run().Match(
///     Succ: exists => exists,
///     Fail: _ => false
/// );
/// </code>
/// <para>
/// <strong>Special Cases:</strong>
/// </para>
/// <list type="bullet">
/// <item><strong>Memory-only entries:</strong> CanBeSeed = false (always produced by nodes)</item>
/// <item><strong>Read-only sources:</strong> CanBeSeed = true (Excel files, APIs)</item>
/// <item><strong>File-based entries:</strong> CanBeSeed = file exists</item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // File-based storage - seed if file exists
/// public class FileStorageAdapter&lt;T&gt; : IStorageAdapter&lt;T&gt;, ISeedable
/// {
///     public bool CanBeSeed => File.Exists(_filePath);
/// }
///
/// // Memory-based storage - never a seed
/// public class MemoryStorageAdapter&lt;T&gt; : IStorageAdapter&lt;T&gt;, ISeedable
/// {
///     public bool CanBeSeed => false;  // Always produced by pipeline
/// }
///
/// // Usage in pipeline executor
/// var seedEntries = pipeline.Inputs
///     .OfType&lt;ISeedable&gt;()
///     .Where(s => s.CanBeSeed)
///     .ToList();
/// </code>
/// </example>
public interface ISeedable
{
  /// <summary>
  /// Gets whether this catalog entry can be a seed (Layer 0 input).
  /// </summary>
  /// <remarks>
  /// <para>
  /// Returns true if:
  /// - Data exists at storage location before pipeline runs
  /// - Entry represents an external data source
  /// - Entry is read-only (cannot be produced by pipeline)
  /// </para>
  /// <para>
  /// Returns false if:
  /// - Data does not exist yet (will be produced by pipeline)
  /// - Entry is memory-only (transient)
  /// - Entry is an intermediate/output of pipeline
  /// </para>
  /// </remarks>
  bool CanBeSeed { get; }
}
