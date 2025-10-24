using Flowthru.Meta.Providers;
using Microsoft.Extensions.Logging;

namespace Flowthru.Meta;

/// <summary>
/// Fluent builder for configuring metadata providers and export settings.
/// </summary>
/// <remarks>
/// <para>
/// Use this builder to register metadata providers (JSON, Mermaid, etc.) with custom configuration.
/// Providers are executed in registration order during metadata export.
/// </para>
/// <para>
/// <strong>Example usage:</strong>
/// </para>
/// <code>
/// builder.IncludeMetadata(meta => meta
///     .AddJson(json => json.UseCompactFormat())
///     .AddMermaid(mermaid => mermaid.WithDirection(MermaidFlowchartDirection.LeftToRight))
/// );
/// </code>
/// </remarks>
public class FlowthruMetadataBuilder {
  private readonly List<IMetadataProvider> _providers = new();
  private string _outputDirectory = "metadata";
  private bool _autoExport = true;

  /// <summary>
  /// Gets the list of registered metadata providers.
  /// </summary>
  internal IReadOnlyList<IMetadataProvider> Providers => _providers.AsReadOnly();

  /// <summary>
  /// Gets the output directory for metadata files.
  /// </summary>
  internal string OutputDirectory => _outputDirectory;

  /// <summary>
  /// Gets whether metadata should be auto-exported during pipeline execution.
  /// </summary>
  internal bool AutoExport => _autoExport;

  /// <summary>
  /// Sets the output directory for metadata files.
  /// </summary>
  /// <param name="directory">Directory path (relative or absolute)</param>
  /// <returns>This builder for fluent chaining</returns>
  public FlowthruMetadataBuilder WithOutputDirectory(string directory) {
    _outputDirectory = directory ?? throw new ArgumentNullException(nameof(directory));
    return this;
  }

  /// <summary>
  /// Enables or disables automatic metadata export during pipeline execution.
  /// </summary>
  /// <param name="enabled">True to auto-export (default), false to require manual export</param>
  /// <returns>This builder for fluent chaining</returns>
  public FlowthruMetadataBuilder WithAutoExport(bool enabled = true) {
    _autoExport = enabled;
    return this;
  }

  /// <summary>
  /// Adds a JSON metadata provider with optional configuration.
  /// </summary>
  /// <param name="configure">Optional configuration action for JSON provider</param>
  /// <returns>This builder for fluent chaining</returns>
  public FlowthruMetadataBuilder AddJson(Action<JsonMetadataProviderBuilder>? configure = null) {
    var builder = new JsonMetadataProviderBuilder();
    configure?.Invoke(builder);
    _providers.Add(builder.Build());
    return this;
  }

  /// <summary>
  /// Adds a Mermaid diagram provider with optional configuration.
  /// </summary>
  /// <param name="configure">Optional configuration action for Mermaid provider</param>
  /// <returns>This builder for fluent chaining</returns>
  public FlowthruMetadataBuilder AddMermaid(Action<MermaidMetadataProviderBuilder>? configure = null) {
    var builder = new MermaidMetadataProviderBuilder();
    configure?.Invoke(builder);
    _providers.Add(builder.Build());
    return this;
  }

  /// <summary>
  /// Adds a custom metadata provider.
  /// </summary>
  /// <param name="provider">The metadata provider to register</param>
  /// <returns>This builder for fluent chaining</returns>
  public FlowthruMetadataBuilder AddProvider(IMetadataProvider provider) {
    _providers.Add(provider ?? throw new ArgumentNullException(nameof(provider)));
    return this;
  }

  /// <summary>
  /// Creates a default configuration with JSON and Mermaid providers.
  /// </summary>
  /// <returns>New metadata builder with default providers</returns>
  internal static FlowthruMetadataBuilder CreateDefault() {
    var builder = new FlowthruMetadataBuilder();
    builder.AddJson();
    builder.AddMermaid();
    return builder;
  }
}

/// <summary>
/// Builder for configuring JSON metadata provider options.
/// </summary>
public class JsonMetadataProviderBuilder {
  private bool _useCompactFormat = false;

  /// <summary>
  /// Enables compact JSON format (no indentation).
  /// </summary>
  /// <returns>This builder for fluent chaining</returns>
  public JsonMetadataProviderBuilder UseCompactFormat() {
    _useCompactFormat = true;
    return this;
  }

  /// <summary>
  /// Enables indented JSON format (default).
  /// </summary>
  /// <returns>This builder for fluent chaining</returns>
  public JsonMetadataProviderBuilder UseIndentedFormat() {
    _useCompactFormat = false;
    return this;
  }

  internal JsonMetadataProvider Build() {
    return new JsonMetadataProvider(_useCompactFormat);
  }
}

/// <summary>
/// Builder for configuring Mermaid diagram provider options.
/// </summary>
public class MermaidMetadataProviderBuilder {
  private MermaidMetadataProvider.MermaidFlowchartDirection _direction =
    MermaidMetadataProvider.MermaidFlowchartDirection.TopToBottom;

  /// <summary>
  /// Sets the flowchart direction.
  /// </summary>
  /// <param name="direction">Direction for the flowchart (TB, LR, BT, RL)</param>
  /// <returns>This builder for fluent chaining</returns>
  public MermaidMetadataProviderBuilder WithDirection(MermaidMetadataProvider.MermaidFlowchartDirection direction) {
    _direction = direction;
    return this;
  }

  internal MermaidMetadataProvider Build() {
    return new MermaidMetadataProvider(_direction);
  }
}
