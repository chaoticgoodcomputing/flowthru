using Flowthru.Data.Validation;
using Flowthru.Effects;

namespace Flowthru.Data.Capabilities;

/// <summary>
/// Capability interface for storage adapters that support deep validation.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Purpose:</strong> Enable comprehensive validation of entire datasets.
/// </para>
/// <para>
/// <strong>Deep Inspection Characteristics:</strong>
/// </para>
/// <list type="bullet">
/// <item><strong>Thorough:</strong> Validates all data in the dataset</item>
/// <item><strong>Expensive:</strong> Requires full scan - high I/O and memory</item>
/// <item><strong>Guaranteed:</strong> Catches all schema and data quality issues</item>
/// <item><strong>Blocking:</strong> May take significant time for large datasets</item>
/// </list>
/// <para>
/// <strong>When to Use:</strong>
/// </para>
/// <list type="bullet">
/// <item>Critical production data validation</item>
/// <item>External data sources (untrusted)</item>
/// <item>Post-ETL quality checks</item>
/// <item>Compliance/audit requirements</item>
/// </list>
/// <para>
/// <strong>Trade-off with Shallow Inspection:</strong>
/// </para>
/// <list type="bullet">
/// <item><see cref="IShallowInspectable"/> - Fast but may miss issues</item>
/// <item><see cref="IDeepInspectable"/> - Slow but comprehensive</item>
/// </list>
/// <para>
/// <strong>Discovery Pattern:</strong>
/// </para>
/// <para>
/// Catalog entries discover this capability at runtime:
/// </para>
/// <code>
/// if (catalogEntry is IDeepInspectable inspectable)
/// {
///     var validationResult = await inspectable.InspectDeep().Run();
/// }
/// </code>
/// </remarks>
/// <example>
/// <code>
/// public class CsvStorageAdapter&lt;T&gt; : IStorageAdapter&lt;IEnumerable&lt;T&gt;&gt;,
///                                       IShallowInspectable,
///                                       IDeepInspectable
/// {
///     public FlowIO&lt;ValidationResult&gt; InspectDeep()
///     {
///         return FlowIO.LiftAsync(async () =>
///         {
///             // Read ALL rows and validate schema
///             var allData = await Load().Run();
///             return ValidateAll(allData);
///         });
///     }
/// }
/// </code>
/// </example>
public interface IDeepInspectable
{
  /// <summary>
  /// Performs deep validation by scanning the entire dataset.
  /// </summary>
  /// <returns>Effect producing comprehensive validation result</returns>
  /// <remarks>
  /// <para>
  /// <strong>Implementation Strategy:</strong>
  /// </para>
  /// <list type="bullet">
  /// <item>Read ALL rows from storage</item>
  /// <item>Validate schema matches expected types</item>
  /// <item>Check comprehensive data quality rules</item>
  /// <item>Return aggregated validation result with statistics</item>
  /// </list>
  /// <para>
  /// <strong>Validation Checks:</strong>
  /// </para>
  /// <list type="bullet">
  /// <item>All shallow checks (schema, types, nulls)</item>
  /// <item>Data distribution analysis</item>
  /// <item>Referential integrity (if applicable)</item>
  /// <item>Business rule validation</item>
  /// <item>Statistical outlier detection</item>
  /// </list>
  /// <para>
  /// <strong>Performance Warning:</strong>
  /// </para>
  /// <para>
  /// This operation can be very expensive for large datasets.
  /// Consider using <see cref="IShallowInspectable.InspectShallow"/> for development/debugging.
  /// </para>
  /// </remarks>
  FlowIO<ValidationResult> InspectDeep();
}
