using Flowthru.Diagnostics.Json;

namespace Flowthru.Diagnostics;

/// <summary>
/// Extension methods that contribute the JSON metadata provider into
/// <see cref="FlowthruMetadataBuilder"/>. End users see them as
/// <c>builder.AddJsonMetadata(opt =&gt; ...)</c> via a single
/// <c>using Flowthru.Diagnostics;</c> import.
/// </summary>
public static class JsonMetadataBuilderExtensions
{
  /// <summary>
  /// Register a <see cref="JsonMetadataProvider"/> as both a pre-run
  /// and post-run metadata provider. Provides defaults usable without
  /// configuration; pass a <paramref name="configure"/> action to tune
  /// output directory, filename templates, timestamps, or compactness.
  /// </summary>
  /// <param name="builder">The host's metadata builder.</param>
  /// <param name="configure">
  /// Optional configuration callback. When omitted, the provider
  /// writes to a <c>metadata/</c> directory beside the host process
  /// using the default templates and indented JSON.
  /// </param>
  /// <example>
  /// <code>
  /// services.AddFlowthru(b =&gt; b.ConfigureMetadata(m =&gt;
  ///   m.AddJsonMetadata(opt =&gt; opt
  ///     .WithOutputDirectory("artifacts/metadata")
  ///     .WithTimestamp())));
  /// </code>
  /// </example>
  public static FlowthruMetadataBuilder AddJsonMetadata(
    this FlowthruMetadataBuilder builder,
    Action<JsonMetadataProviderBuilder>? configure = null
  )
  {
    if (builder is null) throw new ArgumentNullException(nameof(builder));

    var providerBuilder = new JsonMetadataProviderBuilder();
    configure?.Invoke(providerBuilder);
    var provider = providerBuilder.Build();

    builder.AddProvider((IMetadataProvider)provider);
    builder.AddProvider((IPostRunMetadataProvider)provider);
    return builder;
  }
}
