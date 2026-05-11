using Flowthru.Diagnostics.Mermaid;

namespace Flowthru.Diagnostics;

/// <summary>
/// Extension methods that contribute the Mermaid metadata provider
/// into <see cref="FlowthruMetadataBuilder"/>. End users see them as
/// <c>builder.AddMermaidMetadata(opt =&gt; ...)</c> via a single
/// <c>using Flowthru.Diagnostics;</c> import.
/// </summary>
public static class MermaidMetadataBuilderExtensions
{
  /// <summary>
  /// Register a <see cref="MermaidMetadataProvider"/> as both a pre-run
  /// and post-run metadata provider. Defaults to a top-to-bottom
  /// flowchart written into a <c>metadata/</c> directory; pass a
  /// <paramref name="configure"/> action to tune layout, colours, or
  /// filename templates.
  /// </summary>
  /// <example>
  /// <code>
  /// services.AddFlowthru(b =&gt; b.ConfigureMetadata(m =&gt;
  ///   m.AddMermaidMetadata(opt =&gt; opt
  ///     .WithDirection(MermaidFlowchartDirection.LeftToRight)
  ///     .WithOutputDirectory("artifacts/diagrams"))));
  /// </code>
  /// </example>
  public static FlowthruMetadataBuilder AddMermaidMetadata(
    this FlowthruMetadataBuilder builder,
    Action<MermaidMetadataProviderBuilder>? configure = null
  )
  {
    if (builder is null) throw new ArgumentNullException(nameof(builder));

    var providerBuilder = new MermaidMetadataProviderBuilder();
    configure?.Invoke(providerBuilder);
    var provider = providerBuilder.Build();

    builder.AddProvider((IMetadataProvider)provider);
    builder.AddProvider((IPostRunMetadataProvider)provider);
    return builder;
  }
}
