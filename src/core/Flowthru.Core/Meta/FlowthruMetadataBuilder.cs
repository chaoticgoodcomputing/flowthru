using Flowthru.Meta.Providers;
using Microsoft.Extensions.Logging;

namespace Flowthru.Meta;

/// <summary>
/// Fluent builder for configuring metadata providers and export settings.
/// </summary>
/// <remarks>
/// <para>
/// Use this builder to register metadata providers with custom configuration.
/// Providers are executed in registration order during metadata export.
/// </para>
/// <para>
/// <strong>Example usage:</strong>
/// </para>
/// <code>
/// builder.ConfigureMetadata(meta => meta
///     .AddProvider&lt;JsonMetadataProvider, JsonMetadataProviderBuilder&gt;(json => json
///         .WithOutputDirectory("metadata")
///         .WithTimestamp("yyyy-MM-dd_HH-mm-ss")
///         .UseCompactFormat())
///     .AddProvider&lt;MermaidMetadataProvider, MermaidMetadataProviderBuilder&gt;(mermaid => mermaid
///         .WithOutputDirectory("metadata")
///         .WithDirection(MermaidMetadataProvider.MermaidFlowchartDirection.LeftToRight))
/// );
/// </code>
/// </remarks>
public class FlowthruMetadataBuilder
{
  private readonly List<IMetadataProvider> _providers = new();
  private bool _autoExport = true;

  /// <summary>
  /// Gets the list of registered metadata providers.
  /// </summary>
  internal IReadOnlyList<IMetadataProvider> Providers => _providers.AsReadOnly();

  /// <summary>
  /// Gets whether metadata should be auto-exported during pipeline execution.
  /// </summary>
  internal bool AutoExport => _autoExport;

  /// <summary>
  /// Enables or disables automatic metadata export during pipeline execution.
  /// </summary>
  /// <param name="enabled">True to auto-export (default), false to require manual export</param>
  /// <returns>This builder for fluent chaining</returns>
  public FlowthruMetadataBuilder WithAutoExport(bool enabled = true)
  {
    _autoExport = enabled;
    return this;
  }

  /// <summary>
  /// Adds a metadata provider with optional configuration.
  /// </summary>
  /// <typeparam name="TProvider">The metadata provider type (must implement <see cref="IMetadataProvider"/> and have <see cref="MetadataProviderBuilderAttribute"/>)</typeparam>
  /// <typeparam name="TBuilder">The builder type for the provider</typeparam>
  /// <param name="configure">Optional configuration action for the provider's builder</param>
  /// <returns>This builder for fluent chaining</returns>
  /// <exception cref="InvalidOperationException">Thrown when provider type lacks <see cref="MetadataProviderBuilderAttribute"/> or builder type mismatch</exception>
  public FlowthruMetadataBuilder AddProvider<TProvider, TBuilder>(
    Action<TBuilder>? configure = null
  )
    where TProvider : IMetadataProvider
    where TBuilder : new()
  {
    // Get the MetadataProviderBuilder attribute from the provider type
    var providerType = typeof(TProvider);
    var attribute =
      providerType
        .GetCustomAttributes(typeof(MetadataProviderBuilderAttribute), false)
        .FirstOrDefault() as MetadataProviderBuilderAttribute;

    if (attribute is null)
    {
      throw new InvalidOperationException(
        $"Provider type '{providerType.Name}' must be decorated with [MetadataProviderBuilder(typeof(...))] attribute."
      );
    }

    var expectedBuilderType = attribute.BuilderType;
    var actualBuilderType = typeof(TBuilder);

    if (expectedBuilderType != actualBuilderType)
    {
      throw new InvalidOperationException(
        $"Builder type mismatch: Provider '{providerType.Name}' expects builder '{expectedBuilderType.Name}', but '{actualBuilderType.Name}' was provided."
      );
    }

    // Instantiate the builder
    var builder = new TBuilder();

    // Apply configuration
    configure?.Invoke(builder);

    // Call Build() method to get the provider instance
    var buildMethod = actualBuilderType.GetMethod("Build");
    if (buildMethod == null)
    {
      throw new InvalidOperationException(
        $"Builder type '{actualBuilderType.Name}' must have a public 'Build()' method."
      );
    }

    var provider =
      buildMethod.Invoke(builder, null) as IMetadataProvider
      ?? throw new InvalidOperationException(
        $"Builder '{actualBuilderType.Name}'.Build() returned null or non-IMetadataProvider instance."
      );

    _providers.Add(provider);
    return this;
  }

  /// <summary>
  /// Adds a custom metadata provider instance directly.
  /// </summary>
  /// <param name="provider">The metadata provider to register</param>
  /// <returns>This builder for fluent chaining</returns>
  public FlowthruMetadataBuilder AddProvider(IMetadataProvider provider)
  {
    _providers.Add(provider ?? throw new ArgumentNullException(nameof(provider)));
    return this;
  }
}
