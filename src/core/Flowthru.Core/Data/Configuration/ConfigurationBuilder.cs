using Flowthru.Data.Catalog;
using Microsoft.Extensions.Configuration;

namespace Flowthru.Data.Configuration;

/// <summary>
/// Catalog-builder extensions for config-as-catalog. Mirrors the
/// JSON/Csv builders' tier-1 shape:
/// <code>
/// Item.Of&lt;FeatureFlagsConfig&gt;("feature-flags")
///   .FromConfiguration(_configuration)
///   .AtSection("FeatureFlags")
///   .Build();
/// </code>
/// </summary>
public static class ConfigurationItemBuilderExtensions
{
  /// <summary>
  /// Bind a catalog item to a section of an
  /// <see cref="IConfiguration"/>. The returned builder requires
  /// <see cref="ConfigurationItemBuilder{T}.AtSection"/> before
  /// <see cref="ConfigurationItemBuilder{T}.Build"/>.
  /// </summary>
  /// <typeparam name="T">
  /// Bound payload type. Must be a reference type with a parameterless
  /// constructor — <see cref="ConfigurationBinder.Get{T}(IConfiguration)"/>
  /// uses property setters to populate the instance.
  /// </typeparam>
  /// <param name="anchor">The catalog item anchor (from <see cref="Item.Of{T}"/>).</param>
  /// <param name="configuration">
  /// The host-registered <see cref="IConfiguration"/>. Inside a
  /// catalog constructor, this is typically a parameter resolved via
  /// DI after <c>UseConfiguration(...)</c> on the
  /// <c>FlowthruServiceBuilder</c>.
  /// </param>
  public static ConfigurationItemBuilder<T> FromConfiguration<T>(
    this ItemAnchor<T> anchor,
    IConfiguration configuration
  )
    where T : class, new()
  {
    if (anchor is null) throw new ArgumentNullException(nameof(anchor));
    if (configuration is null) throw new ArgumentNullException(nameof(configuration));
    return new ConfigurationItemBuilder<T>(anchor.Label, configuration);
  }
}

/// <summary>
/// Tier-1 builder for a <see cref="ConfigurationItem{T}"/>. Requires
/// <see cref="AtSection"/> before <see cref="Build"/>; the section
/// path is the colon-separated key (e.g. <c>"FeatureFlags"</c> or
/// <c>"App:Service:Region"</c>).
/// </summary>
/// <typeparam name="T">Bound payload type.</typeparam>
public sealed class ConfigurationItemBuilder<T>
  where T : class, new()
{
  private readonly string _label;
  private readonly IConfiguration _configuration;
  private string? _sectionPath;

  internal ConfigurationItemBuilder(string label, IConfiguration configuration)
  {
    _label = label;
    _configuration = configuration;
  }

  /// <summary>Catalog label set by <see cref="Item.Of{T}"/>.</summary>
  public string Label => _label;

  /// <summary>
  /// Set the colon-separated configuration section path
  /// (e.g. <c>"FeatureFlags"</c>, <c>"App:Service:Region"</c>).
  /// </summary>
  public ConfigurationItemBuilder<T> AtSection(string sectionPath)
  {
    if (sectionPath is null) throw new ArgumentNullException(nameof(sectionPath));
    if (string.IsNullOrWhiteSpace(sectionPath))
      throw new ArgumentException(
        "Configuration section path cannot be null or whitespace.",
        nameof(sectionPath));
    _sectionPath = sectionPath;
    return this;
  }

  /// <summary>
  /// Materialize the <see cref="ConfigurationItem{T}"/>.
  /// </summary>
  /// <exception cref="InvalidOperationException">
  /// Thrown when <see cref="AtSection"/> has not been called.
  /// </exception>
  public IReadOnlyItem<T> Build()
  {
    if (_sectionPath is null)
      throw new InvalidOperationException(
        $"Configuration item '{_label}' requires AtSection(...) before Build()."
      );
    return new ConfigurationItem<T>(_label, _configuration.GetSection(_sectionPath));
  }
}
