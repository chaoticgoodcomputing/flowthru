using Flowthru.Configuration;
using Flowthru.Data;
using Flowthru.Data.Storage.Strategies;
using Flowthru.Meta;
using Flowthru.Meta.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NetEscapades.Configuration.Yaml;

namespace Flowthru.Services;

/// <summary>
/// Fluent builder for configuring Flowthru service registration.
/// </summary>
/// <remarks>
/// <para>
/// This builder configures the service layer without CLI coupling.
/// Use it to register catalogs, pipelines, and optional features.
/// </para>
/// <para>
/// <strong>Basic Usage:</strong>
/// <code>
/// services.AddFlowthru(flowthru =>
/// {
///     flowthru.UseCatalog&lt;MyCatalog&gt;();
///     flowthru.RegisterPipeline&lt;MyCatalog&gt;("my_pipeline", MyPipeline.Create);
/// });
/// </code>
/// </para>
/// </remarks>
public sealed partial class FlowthruServiceBuilder
{
  private readonly IServiceCollection _services;
  private readonly List<Type> _registeredCatalogTypes = new();
  private readonly List<PipelineRegistrationEntry> _registrations = new();
  private PipelineRegistrationEntry? _lastRegistration;
  private IConfiguration? _configuration;

  internal FlowthruServiceBuilder(IServiceCollection services)
  {
    _services = services ?? throw new ArgumentNullException(nameof(services));
  }

  /// <summary>
  /// Internal entry type that carries a pipeline factory and its associated metadata.
  /// Replaces the PipelineRegistrar indirection for cleaner multi-catalog support.
  /// </summary>
  internal sealed class PipelineRegistrationEntry
  {
    public string Label { get; }
    public Func<IServiceProvider, Pipelines.Pipeline> Factory { get; }
    public string Description { get; set; } = "";

    internal PipelineRegistrationEntry(
      string label,
      Func<IServiceProvider, Pipelines.Pipeline> factory
    )
    {
      Label = label;
      Factory = factory;
    }
  }

  /// <summary>
  /// Registers a catalog type with constructor injection.
  /// </summary>
  /// <typeparam name="TCatalog">The catalog type</typeparam>
  /// <returns>This builder for method chaining</returns>
  /// <remarks>
  /// The catalog will be resolved from the DI container, allowing constructor
  /// parameter injection (e.g., IConfiguration, IOptions).
  /// </remarks>
  public FlowthruServiceBuilder UseCatalog<TCatalog>()
    where TCatalog : DataCatalogBase
  {
    _services.AddSingleton<TCatalog>();
    _registeredCatalogTypes.Add(typeof(TCatalog));
    return this;
  }

  /// <summary>
  /// Registers a catalog instance directly.
  /// </summary>
  /// <param name="catalog">The catalog instance</param>
  /// <returns>This builder for method chaining</returns>
  /// <remarks>
  /// Use this when the catalog doesn't require dependency injection.
  /// </remarks>
  public FlowthruServiceBuilder UseCatalog(DataCatalogBase catalog)
  {
    if (catalog == null)
    {
      throw new ArgumentNullException(nameof(catalog));
    }

    _services.AddSingleton(catalog.GetType(), catalog);
    _registeredCatalogTypes.Add(catalog.GetType());
    return this;
  }

  /// <summary>
  /// Registers a catalog factory that receives the service provider.
  /// </summary>
  /// <typeparam name="TCatalog">The concrete catalog type</typeparam>
  /// <param name="catalogFactory">Factory function to create the catalog</param>
  /// <returns>This builder for method chaining</returns>
  /// <remarks>
  /// Use this when the catalog needs to resolve services during construction,
  /// or when construction requires parameters unavailable at the call site.
  /// </remarks>
  public FlowthruServiceBuilder UseCatalog<TCatalog>(
    Func<IServiceProvider, TCatalog> catalogFactory
  )
    where TCatalog : DataCatalogBase
  {
    if (catalogFactory == null)
    {
      throw new ArgumentNullException(nameof(catalogFactory));
    }

    _services.AddSingleton<TCatalog>(catalogFactory);
    _registeredCatalogTypes.Add(typeof(TCatalog));
    return this;
  }

  /// <summary>
  /// Escape-hatch for registering pipelines via a full-access service provider factory.
  /// </summary>
  /// <param name="pipelineFactory">Factory function that receives the service provider and returns the pipeline dictionary</param>
  /// <returns>This builder for method chaining</returns>
  /// <remarks>
  /// Prefer <see cref="RegisterPipeline{TCatalog}"/> for standard pipeline registration.
  /// Use this only when you need full service provider access during pipeline construction.
  /// </remarks>
  public FlowthruServiceBuilder UsePipelines(
    Func<IServiceProvider, Dictionary<string, Pipelines.Pipeline>> pipelineFactory
  )
  {
    if (pipelineFactory == null)
    {
      throw new ArgumentNullException(nameof(pipelineFactory));
    }

    _services.AddSingleton(pipelineFactory);

    return this;
  }

  /// <summary>
  /// Registers a pipeline (inline registration with fluent chaining).
  /// </summary>
  /// <typeparam name="TCatalog">The catalog type</typeparam>
  /// <param name="label">Unique pipeline name</param>
  /// <param name="pipeline">Factory function that creates the pipeline from catalog</param>
  /// <returns>This builder for method chaining</returns>
  /// <remarks>
  /// Use this for inline pipeline registration. Fluent chaining with
  /// WithDescription() is supported.
  /// </remarks>
  public FlowthruServiceBuilder RegisterPipeline<TCatalog>(
    string label,
    Func<TCatalog, Pipelines.Pipeline> pipeline
  )
    where TCatalog : DataCatalogBase
  {
    var entry = new PipelineRegistrationEntry(
      label,
      sp => pipeline(sp.GetRequiredService<TCatalog>())
    );
    _registrations.Add(entry);
    _lastRegistration = entry;
    return this;
  }

  /// <summary>
  /// Registers a pipeline with parameters (inline registration).
  /// </summary>
  /// <typeparam name="TCatalog">The catalog type</typeparam>
  /// <typeparam name="TParams">The type of parameters the pipeline requires</typeparam>
  /// <param name="label">Unique pipeline name</param>
  /// <param name="pipeline">Factory function that creates the pipeline from catalog and parameters</param>
  /// <param name="parameters">Parameter instance to pass to the pipeline</param>
  /// <returns>This builder for method chaining</returns>
  public FlowthruServiceBuilder RegisterPipeline<TCatalog, TParams>(
    string label,
    Func<TCatalog, TParams, Pipelines.Pipeline> pipeline,
    TParams parameters
  )
    where TCatalog : DataCatalogBase
  {
    var entry = new PipelineRegistrationEntry(
      label,
      sp => pipeline(sp.GetRequiredService<TCatalog>(), parameters)
    );
    _registrations.Add(entry);
    _lastRegistration = entry;
    return this;
  }

  /// <summary>
  /// Registers a pipeline with parameters loaded from configuration.
  /// </summary>
  /// <typeparam name="TCatalog">The catalog type</typeparam>
  /// <typeparam name="TParams">The type of parameters the pipeline requires</typeparam>
  /// <param name="label">Unique pipeline name</param>
  /// <param name="pipeline">Factory function that creates the pipeline from catalog and parameters</param>
  /// <param name="configurationSection">Configuration section path</param>
  /// <returns>This builder for method chaining</returns>
  /// <exception cref="InvalidOperationException">Thrown if UseConfiguration() hasn't been called first</exception>
  public FlowthruServiceBuilder RegisterPipelineWithConfiguration<TCatalog, TParams>(
    string label,
    Func<TCatalog, TParams, Pipelines.Pipeline> pipeline,
    string configurationSection
  )
    where TCatalog : DataCatalogBase
    where TParams : class, new()
  {
    if (_configuration == null)
    {
      throw new InvalidOperationException(
        "Configuration has not been set up. Call UseConfiguration() before RegisterPipelineWithConfiguration()."
      );
    }

    var parameters = _configuration.GetValidated<TParams>(configurationSection);

    var entry = new PipelineRegistrationEntry(
      label,
      sp => pipeline(sp.GetRequiredService<TCatalog>(), parameters)
    );
    _registrations.Add(entry);
    _lastRegistration = entry;
    return this;
  }

  /// <summary>
  /// Adds a description to the most recently registered pipeline.
  /// </summary>
  /// <param name="description">Human-readable description of what the pipeline does</param>
  /// <returns>This builder for method chaining</returns>
  public FlowthruServiceBuilder WithDescription(string description)
  {
    if (_lastRegistration == null)
    {
      throw new InvalidOperationException(
        "WithDescription() can only be used after RegisterPipeline()."
      );
    }

    _lastRegistration.Description = description;
    return this;
  }

  /// <summary>
  /// Enables configuration loading from JSON and YAML files.
  /// </summary>
  /// <param name="configure">Optional action to configure how configuration files are loaded</param>
  /// <returns>This builder for method chaining</returns>
  /// <remarks>
  /// By default, configuration is loaded from appsettings.json and environment-specific overrides.
  /// </remarks>
  public FlowthruServiceBuilder UseConfiguration(
    Action<FlowthruConfigurationOptions>? configure = null
  )
  {
    var options = new FlowthruConfigurationOptions();
    configure?.Invoke(options);

    var environment = options.GetResolvedEnvironment();
    var configPath = options.ConfigurationPath;
    var baseFileName = options.ConfigurationFileName;

    var configBuilder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory());

    // Add base configuration (required)
    var baseJsonPath = Path.Combine(configPath, $"{baseFileName}.json");
    configBuilder.AddJsonFile(baseJsonPath, optional: false, reloadOnChange: false);

    // Add YAML support if enabled
    if (options.EnableYamlSupport)
    {
      var baseYamlPath = Path.Combine(configPath, $"{baseFileName}.yml");
      var baseYamlAltPath = Path.Combine(configPath, $"{baseFileName}.yaml");

      if (File.Exists(baseYamlPath))
      {
        configBuilder.AddYamlFile(baseYamlPath, optional: true, reloadOnChange: false);
      }
      else if (File.Exists(baseYamlAltPath))
      {
        configBuilder.AddYamlFile(baseYamlAltPath, optional: true, reloadOnChange: false);
      }
    }

    // Add environment-specific configuration (optional)
    var envJsonPath = Path.Combine(configPath, $"{baseFileName}.{environment}.json");
    configBuilder.AddJsonFile(envJsonPath, optional: true, reloadOnChange: false);

    if (options.EnableYamlSupport)
    {
      var envYamlPath = Path.Combine(configPath, $"{baseFileName}.{environment}.yml");
      var envYamlAltPath = Path.Combine(configPath, $"{baseFileName}.{environment}.yaml");

      if (File.Exists(envYamlPath))
      {
        configBuilder.AddYamlFile(envYamlPath, optional: true, reloadOnChange: false);
      }
      else if (File.Exists(envYamlAltPath))
      {
        configBuilder.AddYamlFile(envYamlAltPath, optional: true, reloadOnChange: false);
      }
    }

    // Add local configuration (optional, gitignored)
    var localJsonPath = Path.Combine(configPath, $"{baseFileName}.Local.json");
    configBuilder.AddJsonFile(localJsonPath, optional: true, reloadOnChange: false);

    if (options.EnableYamlSupport)
    {
      var localYamlPath = Path.Combine(configPath, $"{baseFileName}.Local.yml");
      var localYamlAltPath = Path.Combine(configPath, $"{baseFileName}.Local.yaml");

      if (File.Exists(localYamlPath))
      {
        configBuilder.AddYamlFile(localYamlPath, optional: true, reloadOnChange: false);
      }
      else if (File.Exists(localYamlAltPath))
      {
        configBuilder.AddYamlFile(localYamlAltPath, optional: true, reloadOnChange: false);
      }
    }

    _configuration = configBuilder.Build();
    _services.AddSingleton(_configuration);

    return this;
  }

  /// <summary>
  /// Registers a storage entry factory (for environment-specific entries).
  /// </summary>
  /// <typeparam name="TStrategy">The storage strategy type</typeparam>
  /// <returns>This builder for method chaining</returns>
  /// <remarks>
  /// <para>
  /// Storage strategies enable environment-specific catalog entries:
  /// </para>
  /// <code>
  /// if (env.IsDevelopment())
  /// {
  ///     flowthru.UseStorageStrategy&lt;CsvStorageEntryFactory&gt;();
  /// }
  /// else if (env.IsProduction())
  /// {
  ///     flowthru.UseStorageStrategy&lt;DatabaseStorageEntryFactory&gt;();
  /// }
  /// else if (env.IsTest())
  /// {
  ///     flowthru.UseStorageStrategy&lt;MemoryStorageEntryFactory&gt;();
  /// }
  /// </code>
  /// </remarks>
  public FlowthruServiceBuilder UseStorageStrategy<TStrategy>()
    where TStrategy : class, IStorageEntryFactory
  {
    _services.AddSingleton<IStorageEntryFactory, TStrategy>();
    return this;
  }

  /// <summary>
  /// Registers a storage entry factory instance.
  /// </summary>
  /// <param name="strategy">The storage strategy instance</param>
  /// <returns>This builder for method chaining</returns>
  public FlowthruServiceBuilder UseStorageStrategy(IStorageEntryFactory strategy)
  {
    if (strategy == null)
    {
      throw new ArgumentNullException(nameof(strategy));
    }

    _services.AddSingleton(strategy);
    return this;
  }

  /// <summary>
  /// Registers a storage entry factory using a factory function.
  /// </summary>
  /// <param name="strategyFactory">Factory function to create the strategy</param>
  /// <returns>This builder for method chaining</returns>
  public FlowthruServiceBuilder UseStorageStrategy(
    Func<IServiceProvider, IStorageEntryFactory> strategyFactory
  )
  {
    if (strategyFactory == null)
    {
      throw new ArgumentNullException(nameof(strategyFactory));
    }

    _services.AddSingleton(strategyFactory);
    return this;
  }

  /// <summary>
  /// Configures metadata export.
  /// </summary>
  /// <param name="configure">Action to configure the metadata builder</param>
  /// <returns>This builder for method chaining</returns>
  /// <remarks>
  /// <para>
  /// Metadata export is optional. If not configured, pipelines will execute
  /// without generating DAG diagrams or metadata files.
  /// </para>
  /// <para>
  /// <strong>Example:</strong>
  /// <code>
  /// flowthru.ConfigureMetadata(meta =>
  /// {
  ///     meta.WithOutputDirectory("metadata")
  ///         .AddProvider&lt;JsonMetadataProvider, JsonMetadataProviderBuilder&gt;()
  ///         .AddProvider&lt;MermaidMetadataProvider, MermaidMetadataProviderBuilder&gt;();
  /// });
  /// </code>
  /// </para>
  /// </remarks>
  public FlowthruServiceBuilder ConfigureMetadata(Action<FlowthruMetadataBuilder> configure)
  {
    if (configure == null)
    {
      throw new ArgumentNullException(nameof(configure));
    }

    var builder = new FlowthruMetadataBuilder();

    // Apply configuration from appsettings.json if available
    if (_configuration != null)
    {
      var metadataOptions = _configuration.GetSection("Flowthru:Metadata").Get<MetadataOptions>();

      if (metadataOptions != null)
      {
        ApplyMetadataOptions(builder, metadataOptions);
      }
    }

    // Apply programmatic configuration (overrides appsettings)
    configure(builder);

    _services.AddSingleton(builder);

    return this;
  }

  /// <summary>
  /// TODO: Remove this. I'm not sure if I am comfortable with the level of programmatic privilege that
  /// JSON and Mermaid metadata are receiving, here. We either need to not support AppSettings-level
  /// configuration, or find some way to make it extensible so that third-party metadata providers
  /// can comfortably add their own AppSettings-level config options.
  /// </summary>
  private static void ApplyMetadataOptions(FlowthruMetadataBuilder builder, MetadataOptions options)
  {
    // Register providers from configuration
    if (options.Providers != null && options.Providers.Count > 0)
    {
      foreach (var providerName in options.Providers)
      {
        var normalizedName = providerName.Trim().ToLowerInvariant();

        switch (normalizedName)
        {
          case "json":
            builder.AddProvider<JsonMetadataProvider, JsonMetadataProviderBuilder>(json =>
            {
              // Apply file configuration shared across providers
              ApplyFileConfiguration(json, options);

              // Apply JSON-specific options
              if (options.Json != null)
              {
                if (options.Json.UseCompactFormat)
                {
                  json.UseCompactFormat();
                }
                else
                {
                  json.UseIndentedFormat();
                }
              }
            });
            break;

          case "mermaid":
            builder.AddProvider<MermaidMetadataProvider, MermaidMetadataProviderBuilder>(mermaid =>
            {
              // Apply file configuration shared across providers
              ApplyFileConfiguration(mermaid, options);

              // Apply Mermaid-specific options
              if (options.Mermaid != null)
              {
                var direction = options.Mermaid.Direction.ToLowerInvariant() switch
                {
                  "toptobottom" or "tb" => MermaidMetadataProvider
                    .MermaidFlowchartDirection
                    .TopToBottom,
                  "bottomtotop" or "bt" => MermaidMetadataProvider
                    .MermaidFlowchartDirection
                    .BottomToTop,
                  "lefttoright" or "lr" => MermaidMetadataProvider
                    .MermaidFlowchartDirection
                    .LeftToRight,
                  "righttoleft" or "rl" => MermaidMetadataProvider
                    .MermaidFlowchartDirection
                    .RightToLeft,
                  _ => MermaidMetadataProvider.MermaidFlowchartDirection.LeftToRight,
                };
                mermaid.WithDirection(direction);

                // Apply color configuration for active nodes and data
                if (!string.IsNullOrEmpty(options.Mermaid.ActiveNodeColor))
                {
                  mermaid.WithActiveNodeColor(options.Mermaid.ActiveNodeColor);
                }
                if (!string.IsNullOrEmpty(options.Mermaid.ActiveDataColor))
                {
                  mermaid.WithActiveDataColor(options.Mermaid.ActiveDataColor);
                }
              }
            });
            break;

          default:
            // Ignore unknown providers silently (allows for future extensions)
            break;
        }
      }
    }
  }

  /// <summary>
  /// Helper to apply file configuration from MetadataOptions to provider builders.
  /// Works via duck typing — both JsonMetadataProviderBuilder and MermaidMetadataProviderBuilder
  /// expose these methods, but there's no shared interface.
  /// </summary>
  private static void ApplyFileConfiguration<TBuilder>(TBuilder builder, MetadataOptions options)
  {
    dynamic dynamicBuilder = builder!;

    if (!string.IsNullOrWhiteSpace(options.OutputDirectory))
    {
      dynamicBuilder.WithOutputDirectory(options.OutputDirectory);
    }

    if (!string.IsNullOrWhiteSpace(options.FilenameTemplate))
    {
      dynamicBuilder.WithFilenameTemplate(options.FilenameTemplate);
    }

    if (options.Timestamp != null && options.Timestamp.IncludeTimestamp)
    {
      dynamicBuilder.WithTimestamp(options.Timestamp.Format);
    }
  }

  /// <summary>
  /// Internal method called by AddFlowthru to register the catalog collection and
  /// pipeline dictionary into the DI container. Must be called after all UseCatalog
  /// and RegisterPipeline calls have been made.
  /// </summary>
  internal void RegisterPipelineDictionary()
  {
    // Always register the catalog collection so FlowthruService can inject all catalogs.
    var catalogTypes = _registeredCatalogTypes.ToList();
    _services.AddSingleton<IReadOnlyList<DataCatalogBase>>(sp =>
      catalogTypes.Select(t => (DataCatalogBase)sp.GetRequiredService(t)).ToList().AsReadOnly()
    );

    if (_registrations.Count == 0)
    {
      return; // No inline registrations; assume UsePipelines was called instead.
    }

    var snapshot = _registrations.ToList();
    _services.AddSingleton<Dictionary<string, Pipelines.Pipeline>>(sp =>
    {
      var result = new Dictionary<string, Pipelines.Pipeline>();
      foreach (var reg in snapshot)
      {
        var pipeline = reg.Factory(sp);
        pipeline.Name = reg.Label;
        pipeline.Description = reg.Description;
        result[reg.Label] = pipeline;
      }
      return result;
    });
  }
}
