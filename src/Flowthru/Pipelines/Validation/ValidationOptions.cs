using Flowthru.Data;
using Flowthru.Data.Validation;

namespace Flowthru.Pipelines.Validation;

/// <summary>
/// Configuration for pipeline validation behavior.
/// </summary>
/// <remarks>
/// <para>
/// ValidationOptions allows pipeline creators to configure how external data sources
/// (Layer 0 inputs) are validated before pipeline execution begins.
/// </para>
/// <para>
/// <strong>Default Behavior (if not configured):</strong>
/// </para>
/// <list type="bullet">
/// <item>Layer 0 inputs that implement <see cref="IShallowInspectable{T}"/> → Shallow inspection</item>
/// <item>Layer 0 inputs that don't implement inspection interfaces → None (skip)</item>
/// <item>All intermediate outputs (Layer 1+) → None (never inspected)</item>
/// </list>
/// <para>
/// <strong>Usage Example:</strong>
/// </para>
/// <code>
/// builder
///   .RegisterPipeline&lt;MyCatalog&gt;("data_processing", MyPipeline.Create)
///   .WithValidation(validation => {
///     // Opt into deep inspection for critical inputs
///     validation.Inspect(catalog.Companies, InspectionLevel.Deep);
///     validation.Inspect(catalog.Shuttles, InspectionLevel.Deep);
///     // Other Layer 0 inputs will use default Shallow inspection
///   });
/// </code>
/// </remarks>
public class ValidationOptions {
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
  public ValidationOptions Inspect(ICatalogEntry catalogEntry, InspectionLevel level) {
    if (catalogEntry == null) {
      throw new ArgumentNullException(nameof(catalogEntry));
    }

    _catalogEntryInspectionLevels[catalogEntry.Key] = level;
    return this;
  }

  /// <summary>
  /// Gets the configured inspection level for a catalog entry, or null if not configured.
  /// </summary>
  /// <param name="catalogKey">The catalog entry key</param>
  /// <returns>The configured inspection level, or null if using default behavior</returns>
  internal InspectionLevel? GetInspectionLevel(string catalogKey) {
    return _catalogEntryInspectionLevels.TryGetValue(catalogKey, out var level)
      ? level
      : null;
  }

  /// <summary>
  /// Determines the effective inspection level for a catalog entry.
  /// </summary>
  /// <param name="catalogEntry">The catalog entry to inspect</param>
  /// <returns>The inspection level to use (considering configuration and defaults)</returns>
  /// <remarks>
  /// <para>
  /// <strong>Resolution Logic:</strong>
  /// </para>
  /// <list type="number">
  /// <item>If explicitly configured via Inspect() → use that level</item>
  /// <item>If entry implements <see cref="IShallowInspectable{T}"/> → Shallow</item>
  /// <item>Otherwise → None</item>
  /// </list>
  /// </remarks>
  internal InspectionLevel GetEffectiveInspectionLevel(ICatalogEntry catalogEntry) {
    if (catalogEntry == null) {
      throw new ArgumentNullException(nameof(catalogEntry));
    }

    // 1. Check for explicit configuration
    var configuredLevel = GetInspectionLevel(catalogEntry.Key);
    if (configuredLevel.HasValue) {
      return configuredLevel.Value;
    }

    // 2. Check if entry supports shallow inspection (default behavior)
    var entryType = catalogEntry.GetType();
    var implementsShallowInspectable = entryType.GetInterfaces()
      .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IShallowInspectable<>));

    if (implementsShallowInspectable) {
      return InspectionLevel.Shallow;
    }

    // 3. Default to None if no inspection capability
    return InspectionLevel.None;
  }

  /// <summary>
  /// Creates a new ValidationOptions instance with default settings.
  /// </summary>
  public static ValidationOptions Default() => new ValidationOptions();
}
