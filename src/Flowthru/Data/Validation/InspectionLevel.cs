namespace Flowthru.Data.Validation;

/// <summary>
/// Defines the level of inspection to perform on catalog entries before pipeline execution.
/// </summary>
/// <remarks>
/// <para>
/// Inspection levels are used to validate external data sources (Layer 0 inputs) before
/// the pipeline begins execution, following the "fail-fast" principle.
/// </para>
/// <para>
/// <strong>Default Behavior:</strong>
/// - External inputs (Layer 0): Shallow inspection if <see cref="IShallowInspectable{T}"/> is implemented, otherwise None
/// - Intermediate outputs (Layer 1+): Always None (not inspected, they're created by the pipeline)
/// </para>
/// <para>
/// <strong>When to Use Each Level:</strong>
/// </para>
/// <list type="bullet">
/// <item>
/// <term>None</term>
/// <description>
/// Skip validation entirely. Use for trusted data sources or when validation overhead is prohibitive.
/// </description>
/// </item>
/// <item>
/// <term>Shallow</term>
/// <description>
/// Validate existence, format, headers, and a sample of rows. Fast and catches most common issues.
/// <strong>This is the default for Layer 0 inputs.</strong>
/// </description>
/// </item>
/// <item>
/// <term>Deep</term>
/// <description>
/// Validate all rows in the dataset. Thorough but expensive. Use for critical data or after
/// external updates. Must be explicitly opted-in.
/// </description>
/// </item>
/// </list>
/// </remarks>
public enum InspectionLevel {
  /// <summary>
  /// Skip inspection entirely.
  /// </summary>
  /// <remarks>
  /// Use when:
  /// - Data source is trusted and validated externally
  /// - Validation overhead is prohibitive for large datasets
  /// - You're explicitly opting out of safety checks
  /// </remarks>
  None = 0,

  /// <summary>
  /// Perform shallow inspection: existence, format, headers, and sample rows.
  /// </summary>
  /// <remarks>
  /// <para>
  /// <strong>What Shallow Inspection Checks:</strong>
  /// </para>
  /// <list type="bullet">
  /// <item>File/resource exists</item>
  /// <item>Format is valid (parseable as CSV/Excel/Parquet/etc.)</item>
  /// <item>Headers match expected schema (column names, property mappings)</item>
  /// <item>First N rows (default: 100) deserialize successfully</item>
  /// <item>Data types are compatible with schema</item>
  /// </list>
  /// <para>
  /// Performance: Minimal overhead (~10-100ms for typical files)
  /// </para>
  /// <para>
  /// <strong>This is the default for Layer 0 inputs that implement <see cref="IShallowInspectable{T}"/>.</strong>
  /// </para>
  /// </remarks>
  Shallow = 1,

  /// <summary>
  /// Perform deep inspection: all checks from Shallow plus validation of ALL rows.
  /// </summary>
  /// <remarks>
  /// <para>
  /// <strong>What Deep Inspection Adds:</strong>
  /// </para>
  /// <list type="bullet">
  /// <item>All checks from Shallow inspection</item>
  /// <item>Validates EVERY row deserializes successfully</item>
  /// <item>Checks for data quality issues throughout entire dataset</item>
  /// </list>
  /// <para>
  /// Performance: Potentially significant overhead (seconds to minutes for large datasets)
  /// </para>
  /// <para>
  /// <strong>Use Cases:</strong>
  /// </para>
  /// <list type="bullet">
  /// <item>Critical production deployments</item>
  /// <item>After external data updates</item>
  /// <item>CI/CD regression testing</item>
  /// <item>When data corruption is suspected</item>
  /// </list>
  /// <para>
  /// <strong>Must be explicitly opted-in by the pipeline creator.</strong>
  /// </para>
  /// </remarks>
  Deep = 2
}
