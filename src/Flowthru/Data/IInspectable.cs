using Flowthru.Data.Validation;

namespace Flowthru.Data;

/// <summary>
/// Marker interface for catalog entries that support shallow inspection.
/// </summary>
/// <typeparam name="T">The type of individual items in the dataset or the object type</typeparam>
/// <remarks>
/// <para>
/// <strong>Capability Interface:</strong> Similar to <see cref="IReadableCatalogDataset{T}"/>
/// and <see cref="IWritableCatalogDataset{T}"/>, this interface marks a catalog entry as
/// supporting shallow inspection capabilities.
/// </para>
/// <para>
/// <strong>What is Shallow Inspection?</strong>
/// </para>
/// <para>
/// Shallow inspection validates external data sources without loading the entire dataset:
/// </para>
/// <list type="bullet">
/// <item>Existence: File/resource exists and is accessible</item>
/// <item>Format: Data is parseable in the expected format (CSV/Excel/Parquet/etc.)</item>
/// <item>Schema: Headers and column names match expected schema</item>
/// <item>Sample: First N rows (default: 100) deserialize successfully</item>
/// </list>
/// <para>
/// <strong>Performance:</strong> Shallow inspection is designed to be fast (~10-100ms)
/// and suitable for default pre-execution validation of Layer 0 inputs.
/// </para>
/// <para>
/// <strong>Default Behavior:</strong> If a Layer 0 input implements this interface,
/// it will be automatically shallow-inspected before pipeline execution unless the
/// user explicitly opts out.
/// </para>
/// </remarks>
public interface IShallowInspectable<T> {
  /// <summary>
  /// Performs shallow inspection of this catalog entry.
  /// </summary>
  /// <param name="sampleSize">
  /// Number of rows to validate (default: 100).
  /// Implementations may inspect fewer rows if the dataset is smaller.
  /// </param>
  /// <returns>
  /// A <see cref="ValidationResult"/> containing any errors found during inspection.
  /// Returns <see cref="ValidationResult.Success()"/> if validation passes.
  /// </returns>
  /// <remarks>
  /// <para>
  /// Implementations should validate:
  /// </para>
  /// <list type="number">
  /// <item>The data source exists (file present, URL reachable, etc.)</item>
  /// <item>The format is valid and parseable</item>
  /// <item>Headers/schema match expectations</item>
  /// <item>The first <paramref name="sampleSize"/> rows deserialize successfully</item>
  /// </list>
  /// <para>
  /// This method should NOT load the entire dataset into memory. Use
  /// <see cref="IDeepInspectable{T}.InspectDeep"/> for comprehensive validation.
  /// </para>
  /// </remarks>
  Task<ValidationResult> InspectShallow(int sampleSize = 100);
}

/// <summary>
/// Marker interface for catalog entries that support deep inspection.
/// </summary>
/// <typeparam name="T">The type of individual items in the dataset or the object type</typeparam>
/// <remarks>
/// <para>
/// <strong>Capability Interface:</strong> Marks a catalog entry as supporting comprehensive
/// validation that examines the entire dataset.
/// </para>
/// <para>
/// <strong>What is Deep Inspection?</strong>
/// </para>
/// <para>
/// Deep inspection validates ALL data in the source:
/// </para>
/// <list type="bullet">
/// <item>All checks from <see cref="IShallowInspectable{T}"/> (existence, format, schema, sample)</item>
/// <item>Deserializes and validates EVERY row in the dataset</item>
/// <item>Checks for data quality issues throughout the entire dataset</item>
/// </list>
/// <para>
/// <strong>Performance Warning:</strong> Deep inspection loads the entire dataset and can
/// take seconds to minutes for large files. Use sparingly and opt-in explicitly.
/// </para>
/// <para>
/// <strong>When to Use:</strong>
/// </para>
/// <list type="bullet">
/// <item>Critical production deployments where data integrity is paramount</item>
/// <item>After external data updates that might have introduced errors</item>
/// <item>CI/CD regression testing to catch data corruption</item>
/// <item>When debugging suspected data quality issues</item>
/// </list>
/// <para>
/// <strong>Opt-In Required:</strong> Deep inspection must be explicitly requested by
/// the pipeline creator. It is never the default.
/// </para>
/// </remarks>
public interface IDeepInspectable<T> {
  /// <summary>
  /// Performs deep inspection of this catalog entry, validating ALL data.
  /// </summary>
  /// <returns>
  /// A <see cref="ValidationResult"/> containing any errors found during inspection.
  /// Returns <see cref="ValidationResult.Success()"/> if validation passes.
  /// </returns>
  /// <remarks>
  /// <para>
  /// <strong>Performance Warning:</strong> This method loads and validates the ENTIRE dataset.
  /// Execution time scales with dataset size.
  /// </para>
  /// <para>
  /// Implementations should:
  /// </para>
  /// <list type="number">
  /// <item>Perform all checks from shallow inspection</item>
  /// <item>Load and deserialize every row in the dataset</item>
  /// <item>Validate data types and required fields for all rows</item>
  /// <item>Report multiple errors (don't fail on first error)</item>
  /// </list>
  /// </remarks>
  Task<ValidationResult> InspectDeep();
}
