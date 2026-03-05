using Flowthru.Data.Validation;
using Flowthru.Effects;

namespace Flowthru.Data.Capabilities;

/// <summary>
/// Capability interface for storage adapters that support shallow validation.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Purpose:</strong> Enable sampling-based validation before pipeline execution.
/// </para>
/// <para>
/// <strong>Shallow Inspection Characteristics:</strong>
/// </para>
/// <list type="bullet">
/// <item><strong>Fast:</strong> Validates only a sample of data (e.g., first 100 rows)</item>
/// <item><strong>Lightweight:</strong> Minimal memory usage and I/O</item>
/// <item><strong>Early Failure:</strong> Catches obvious schema issues without full scan</item>
/// <item><strong>Best Effort:</strong> May miss issues in unsampled data</item>
/// </list>
/// <para>
/// <strong>When to Use:</strong>
/// </para>
/// <list type="bullet">
/// <item>Development/debugging - quick validation feedback</item>
/// <item>Large datasets where full validation is too expensive</item>
/// <item>Pre-flight checks before expensive pipeline runs</item>
/// </list>
/// <para>
/// <strong>Discovery Pattern:</strong>
/// </para>
/// <para>
/// Catalog entries discover this capability at runtime:
/// </para>
/// <code>
/// if (catalogEntry is IShallowInspectable inspectable)
/// {
///     var validationResult = await inspectable.InspectShallow(sampleSize: 100).Run();
/// }
/// </code>
/// </remarks>
/// <example>
/// <code>
/// public class CsvStorageAdapter&lt;T&gt; : IStorageAdapter&lt;IEnumerable&lt;T&gt;&gt;, IShallowInspectable
/// {
///     public FlowIO&lt;ValidationResult&gt; InspectShallow(int sampleSize)
///     {
///         return FlowIO.LiftAsync(async () =>
///         {
///             // Read first N rows and validate schema
///             var sample = await ReadFirstNRows(sampleSize);
///             return ValidateSchema(sample);
///         });
///     }
/// }
/// </code>
/// </example>
public interface IShallowInspectable
{
  /// <summary>
  /// Performs shallow validation by sampling a subset of data.
  /// </summary>
  /// <param name="sampleSize">Number of rows/records to sample</param>
  /// <returns>Effect producing validation result</returns>
  /// <remarks>
  /// <para>
  /// <strong>Implementation Strategy:</strong>
  /// </para>
  /// <list type="bullet">
  /// <item>Read first N rows from storage</item>
  /// <item>Validate schema matches expected types</item>
  /// <item>Check for obvious data quality issues</item>
  /// <item>Return aggregated validation result</item>
  /// </list>
  /// <para>
  /// <strong>Validation Checks:</strong>
  /// </para>
  /// <list type="bullet">
  /// <item>Column presence and names</item>
  /// <item>Type compatibility</item>
  /// <item>Null handling</item>
  /// <item>Basic range checks</item>
  /// </list>
  /// </remarks>
  FlowIO<ValidationResult> InspectShallow(int sampleSize);
}
