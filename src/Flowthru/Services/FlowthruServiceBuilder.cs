using Flowthru.Configuration;
using Flowthru.Data;
using Flowthru.Data.Storage.Strategies;
using Flowthru.Meta;
using Flowthru.Meta.Providers;
using Flowthru.Registry;
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
///     flowthru.UsePipelines(catalog => new Dictionary&lt;string, Pipeline&gt;
///     {
///         ["my_pipeline"] = MyPipeline.Create(catalog)
///     });
/// });
/// </code>
/// </para>
/// </remarks>
public sealed class FlowthruServiceBuilder
{
  private readonly IServiceCollection _services;
  private readonly List<Action<PipelineRegistrar<DataCatalogBase>>> _inlineRegistrations = new();
  private IConfiguration? _configuration;

  internal FlowthruServiceBuilder(IServiceCollection services)
  {
    _services = services ?? throw new ArgumentNullException(nameof(services));
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
    _services.AddSingleton<DataCatalogBase, TCatalog>();
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

    _services.AddSingleton(catalog);
    return this;
  }

  /// <summary>
  /// Registers a catalog factory that receives the service provider.
  /// </summary>
  /// <param name="catalogFactory">Factory function to create the catalog</param>
  /// <returns>This builder for method chaining</returns>
  /// <remarks>
  /// Use this when the catalog needs to resolve services during construction.
  /// </remarks>
  public FlowthruServiceBuilder UseCatalog(Func<IServiceProvider, DataCatalogBase> catalogFactory)
  {
    if (catalogFactory == null)
    {
      throw new ArgumentNullException(nameof(catalogFactory));
    }

    _services.AddSingleton(catalogFactory);
    return this;
  }

  /// <summary>
  /// Registers pipelines using a factory that receives the catalog.
  /// </summary>
  /// <param name="pipelineFactory">Factory function to create pipeline dictionary</param>
  /// <returns>This builder for method chaining</returns>
  /// <remarks>
  /// <para>
  /// The factory receives the resolved catalog and returns a dictionary of
  /// pipeline name to pipeline instance.
  /// </para>
  /// <para>
  /// <strong>Example:</strong>
  /// <code>
  /// flowthru.UsePipelines(catalog =>
  /// {
  ///     var myCatalog = (MyCatalog)catalog;
  ///     return new Dictionary&lt;string, Pipeline&gt;
  ///     {
  ///         ["data_processing"] = DataProcessingPipeline.Create(myCatalog),
  ///         ["model_training"] = ModelTrainingPipeline.Create(myCatalog)
  ///     };
  /// });
  /// </code>
  /// </para>
  /// </remarks>
  public FlowthruServiceBuilder UsePipelines(
    Func<DataCatalogBase, Dictionary<string, Pipelines.Pipeline>> pipelineFactory
  )
  {
    if (pipelineFactory == null)
    {
      throw new ArgumentNullException(nameof(pipelineFactory));
    }

    _services.AddSingleton(sp =>
    {
      var catalog = sp.GetRequiredService<DataCatalogBase>();
      return pipelineFactory(catalog);
    });

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
    _inlineRegistrations.Add(registrar =>
      registrar.Register(label, catalog => pipeline((TCatalog)catalog))
    );
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
    _inlineRegistrations.Add(registrar =>
      registrar.Register(label, (catalog, p) => pipeline((TCatalog)catalog, (TParams)p), parameters)
    );
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

    _inlineRegistrations.Add(registrar =>
      registrar.Register(label, (catalog, p) => pipeline((TCatalog)catalog, (TParams)p), parameters)
    );
    return this;
  }

  /// <summary>
  /// Adds a description to the most recently registered pipeline.
  /// </summary>
  /// <param name="description">Human-readable description of what the pipeline does</param>
  /// <returns>This builder for method chaining</returns>
  public FlowthruServiceBuilder WithDescription(string description)
  {
    if (_inlineRegistrations.Count == 0)
    {
      throw new InvalidOperationException(
        "WithDescription() can only be used after RegisterPipeline()."
      );
    }

    _inlineRegistrations.Add(registrar => registrar.WithDescription(description));
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
  ///         .AddJson()
  ///         .AddMermaid();
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
        ApplyOptionsToBuilder(builder, metadataOptions);
      }
    }

    // Apply programmatic configuration (overrides appsettings)
    configure(builder);

    _services.AddSingleton(builder);

    return this;
  }

  private static void ApplyOptionsToBuilder(
    FlowthruMetadataBuilder builder,
    MetadataOptions options
  )
  {
    if (!string.IsNullOrWhiteSpace(options.OutputDirectory))
    {
      builder.WithOutputDirectory(options.OutputDirectory);
    }

    if (options.Timestamp != null)
    {
      if (options.Timestamp.IncludeTimestamp)
      {
        builder.WithTimestamp(options.Timestamp.Format);
      }
    }

    if (!string.IsNullOrWhiteSpace(options.FilenameTemplate))
    {
      builder.WithFilenameTemplate(options.FilenameTemplate);
    }

    // Register providers from configuration
    if (options.Providers != null && options.Providers.Count > 0)
    {
      foreach (var providerName in options.Providers)
      {
        var normalizedName = providerName.Trim().ToLowerInvariant();

        switch (normalizedName)
        {
          case "json":
            builder.AddJson(json =>
            {
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
            builder.AddMermaid(mermaid =>
            {
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
  /// Internal method to register pipeline dictionary from inline registrations.
  /// Called by FlowthruServiceCollectionExtensions.AddFlowthru.
  /// </summary>
  internal void RegisterPipelineDictionary()
  {
    if (_inlineRegistrations.Count == 0)
    {
      return; // No inline registrations, assume UsePipelines was called instead
    }

    _services.AddSingleton(sp =>
    {
      var catalog = sp.GetRequiredService<DataCatalogBase>();
      var registrar = new PipelineRegistrar<DataCatalogBase>(catalog);

      // Replay all registration actions
      foreach (var registration in _inlineRegistrations)
      {
        registration(registrar);
      }

      return registrar.Build();
    });
  }
}
