using Flowthru.Data;
using Flowthru.Data.Validation;

namespace Flowthru.Flows.Validation;

/// <summary>
/// Configuration for pipeline validation behavior.
/// </summary>
/// <remarks>
/// <para>
/// ValidationOptions provides pipeline-level overrides for validation configuration.
/// The primary mechanism for validation configuration is catalog-level via the
/// <see cref="IItem.PreferredInspectionLevel"/> property and the fluent
/// <c>.WithInspectionLevel()</c> API.
/// </para>
/// <para>
/// <strong>Default Behavior (if not configured):</strong>
/// </para>
/// <list type="bullet">
/// <item>Catalog entry has PreferredInspectionLevel set → use that level</item>
/// <item>Layer 0 inputs → Shallow inspection (all storage adapters support inspection)</item>
/// <item>All intermediate outputs (Layer 1+) → None (never inspected)</item>
/// </list>
/// <para>
/// <strong>Catalog-Level Configuration (Recommended):</strong>
/// </para>
/// <code>
/// public ICatalogDataset&lt;Company&gt; Companies =>
///   GetOrCreateDataset(() => new CsvCatalogDataset&lt;Company&gt;("companies", "data/companies.csv")
///     .WithInspectionLevel(InspectionLevel.Deep));
/// </code>
/// <para>
/// <strong>Pipeline-Level Override (Advanced):</strong>
/// </para>
/// <code>
/// builder
///   .RegisterPipeline&lt;MyCatalog&gt;("data_processing", MyPipeline.Create)
///   .WithValidation(validation => {
///     // Override catalog-level setting for this specific pipeline
///     validation.Inspect(catalog.Companies, InspectionLevel.Shallow); // Temporarily use shallow
///   });
/// </code>
/// <para>
/// <strong>Design Rationale:</strong>
/// </para>
/// <para>
/// Validation configuration is primarily a property of the data source itself, not the pipeline
/// consuming it. Critical external datasets should always be deeply validated, regardless of
/// which pipeline uses them. Pipeline-level overrides exist for rare cases where different
/// validation is needed temporarily (e.g., performance testing, debugging).
/// </para>
/// </remarks>
public class ValidationOptions
{
  private readonly Dictionary<string, InspectionLevel> _catalogEntryInspectionLevels = new();

  /// <summary>
  /// Specifies the inspection level for a specific catalog entry.
  /// </summary>
  /// <param name="catalogEntry">The catalog entry to configure</param>
  /// <param name="level">The inspection level to use for this entry</param>
  /// <returns>This ValidationOptions instance for fluent chaining</returns>
  /// <remarks>
  /// This configuration only applies to Layer 0 inputs (external data).
  /// Intermediate outputs are never inspected regardless of this setting.
  /// </remarks>
  public ValidationOptions Inspect(IItem catalogEntry, InspectionLevel level)
  {
    if (catalogEntry == null)
    {
      throw new ArgumentNullException(nameof(catalogEntry));
    }

    _catalogEntryInspectionLevels[catalogEntry.Label] = level;
    return this;
  }

  /// <summary>
  /// Gets the configured inspection level for a catalog entry, or null if not configured.
  /// </summary>
  /// <param name="catalogKey">The catalog entry key</param>
  /// <returns>The configured inspection level, or null if using default behavior</returns>
  internal InspectionLevel? GetInspectionLevel(string catalogKey)
  {
    return _catalogEntryInspectionLevels.TryGetValue(catalogKey, out var level) ? level : null;
  }

  /// <summary>
  /// Determines the effective inspection level for a catalog entry.
  /// </summary>
  /// <param name="catalogEntry">The catalog entry to inspect</param>
  /// <returns>The inspection level to use (considering configuration and defaults)</returns>
  /// <remarks>
  /// <para>
  /// <strong>Resolution Logic (Priority Order):</strong>
  /// </para>
  /// <list type="number">
  /// <item><strong>Pipeline-level override:</strong> If explicitly configured via WithValidation().Inspect() → use that level (highest priority)</item>
  /// <item><strong>Catalog-level preference:</strong> If entry.PreferredInspectionLevel is set → use that level (medium priority)</item>
  /// <item><strong>Default:</strong> Shallow inspection for all entries (all storage adapters implement inspection)</item>
  /// </list>
  /// <para>
  /// <strong>Design Rationale:</strong>
  /// </para>
  /// <para>
  /// Validation configuration follows a data-centric approach: the inspection level is primarily
  /// a property of the data source itself, not the pipeline consuming it. Critical external datasets
  /// should always be deeply validated, regardless of which pipeline uses them.
  /// </para>
  /// <para>
  /// Pipeline-level overrides exist for rare cases where different validation is needed temporarily
  /// (e.g., skipping validation in performance tests, or enabling deep validation during debugging).
  /// </para>
  /// </remarks>
  internal InspectionLevel GetEffectiveInspectionLevel(IItem catalogEntry)
  {
    if (catalogEntry == null)
    {
      throw new ArgumentNullException(nameof(catalogEntry));
    }

    // 1. Check for explicit pipeline-level configuration (highest priority)
    var configuredLevel = GetInspectionLevel(catalogEntry.Label);
    if (configuredLevel.HasValue)
    {
      return configuredLevel.Value;
    }

    // 2. Check for catalog-level preference (medium priority)
    if (catalogEntry.PreferredInspectionLevel.HasValue)
    {
      return catalogEntry.PreferredInspectionLevel.Value;
    }

    // 3. Default to Shallow (all storage adapters support inspection)
    return InspectionLevel.Shallow;
  }

  /// <summary>
  /// Creates a new ValidationOptions instance with default settings.
  /// </summary>
  public static ValidationOptions Default() => new ValidationOptions();
}
