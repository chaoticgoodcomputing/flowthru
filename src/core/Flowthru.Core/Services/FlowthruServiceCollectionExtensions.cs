using Flowthru.Core.Data.Storage;
using Flowthru.Core.Flows;
using Flowthru.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering Flowthru services with the DI container.
/// </summary>
public static class FlowthruServiceCollectionExtensions
{
  /// <summary>
  /// Registers Flowthru service with the DI container.
  /// </summary>
  /// <param name="services">The service collection</param>
  /// <param name="configure">Action to configure the Flowthru service</param>
  /// <returns>The service collection for method chaining</returns>
  /// <remarks>
  /// <para>
  /// This extension method provides a clean API for registering Flowthru
  /// in any .NET application with dependency injection.
  /// </para>
  /// <para>
  /// <strong>Example Usage:</strong>
  /// <code>
  /// // In Program.cs or Startup.cs
  /// services.AddFlowthru(flowthru =>
  /// {
  ///     flowthru.RegisterCatalog&lt;MyCatalog&gt;();
  ///     flowthru.RegisterPipelines(catalog => new Dictionary&lt;string, Pipeline&gt;
  ///     {
  ///         ["my_pipeline"] = MyPipeline.Create((MyCatalog)catalog)
  ///     });
  ///     flowthru.ConfigureMetadata(meta =>
  ///     {
  ///         meta.WithOutputDirectory("metadata")
  ///             .AddProvider&lt;MermaidMetadataProvider, MermaidMetadataProviderBuilder&gt;();
  ///     });
  /// });
  ///
  /// // Then inject IFlowthruService anywhere
  /// public class MyController
  /// {
  ///     private readonly IFlowthruService _flowthru;
  ///
  ///     public MyController(IFlowthruService flowthru)
  ///     {
  ///         _flowthru = flowthru;
  ///     }
  ///
  ///     public async Task&lt;IActionResult&gt; RunPipeline(string name)
  ///     {
  ///         var request = new PipelineExecutionRequest { FlowName = name };
  ///         var result = await _flowthru.ExecutePipelineAsync(request);
  ///         return Ok(result);
  ///     }
  /// }
  /// </code>
  /// </para>
  /// </remarks>
  public static IServiceCollection AddFlowthru(
    this IServiceCollection services,
    IConfiguration configuration,
    Action<IFlowthruBuilder> configure
  )
  {
    if (services == null)
    {
      throw new ArgumentNullException(nameof(services));
    }

    if (configuration == null)
    {
      throw new ArgumentNullException(nameof(configuration));
    }

    if (configure == null)
    {
      throw new ArgumentNullException(nameof(configure));
    }

    // Make IConfiguration injectable throughout the application.
    services.TryAddSingleton(configuration);

    // Register ExecutionOptions bound from the Flowthru:Execution config section.
    // Code-first overrides via ConfigureExecution() run after config binding (PostConfigure).
    services
      .AddOptions<ExecutionOptions>()
      .Configure<IConfiguration>((opts, cfg) => cfg.GetSection("Flowthru:Execution").Bind(opts))
      .ValidateOnStart();

    // Register the medium resolver before extensions run so providers registered
    // inside `configure` are collected by the resolver's DI constructor.
    services.TryAddSingleton<IStorageMediumResolver, StorageMediumResolver>();

    // Build service configuration
    var builder = new FlowthruServiceBuilder(services, configuration);
    configure(builder);

    // Register pipeline dictionary factory if inline registrations exist
    builder.RegisterFlowDictionary();

    // Register core service
    services.AddSingleton<IFlowthruService, FlowthruService>();

    return services;
  }
}
