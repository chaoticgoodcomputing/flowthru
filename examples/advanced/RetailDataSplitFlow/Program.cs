using Flowthru.Core.Cli;
using Flowthru.Core.Data.Storage;
using Flowthru.Core.Services;
using Flowthru.Extensions.Http.Services;
using Flowthru.Extensions.Python;
using Flowthru.Extensions.Python.Services;
using Flowthru.Meta;
using Flowthru.Meta.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RetailDataMultipipeline.Data;
using RetailDataMultipipeline.Flows.Analysis;
using RetailDataMultipipeline.Flows.Consolidation;
using RetailDataMultipipeline.Flows.DataIngestion;
using RetailDataMultipipeline.Flows.Graphing;
using RetailDataMultipipeline.Flows.Reporting;

namespace RetailDataMultipipeline;

public class Program
{
  public static Task<int> Main(string[] args) =>
    FlowthruCli.RunStandaloneAsync(
      args,
      services =>
        ConfigureServices(
          services,
          Directory.GetCurrentDirectory(),
          AppDomain.CurrentDomain.BaseDirectory
        )
    );

  public static IServiceProvider ConfigureServices(
    string? basePath = null,
    string? outputPath = null
  )
  {
    var services = new ServiceCollection();
    ConfigureServices(
      services,
      basePath ?? Directory.GetCurrentDirectory(),
      outputPath ?? AppDomain.CurrentDomain.BaseDirectory
    );
    return services.BuildServiceProvider();
  }

  private static void ConfigureServices(
    IServiceCollection services,
    string basePath,
    string outputPath
  )
  {
    var configuration = new ConfigurationBuilder()
      .SetBasePath(basePath)
      .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
      .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false)
      .Build();

    services.AddFlowthru(
      configuration,
      flowthru =>
      {
        // Enable HTTP(S) remote file access with local disk cache.
        // Conditional GET (ETag/Last-Modified) avoids re-downloading the 43MB CSV on every run.
        flowthru.UseHttp(http =>
        {
          http.Cache = new Flowthru.Extensions.Http.HttpCacheOptions
          {
            Directory = Path.Combine(basePath, ".http-cache"),
            MaxAge = TimeSpan.FromHours(24),
          };
        });

        // Configure Python runtime — makes Flows/ importable and exposes the @step decorator
        flowthru.UsePython(python =>
        {
          python.ModuleSearchPaths.Add(basePath);
          python.ModuleSearchPaths.Add(outputPath);
          python.VenvPath = outputPath;
        });

        var dataPath = Path.Combine(basePath, "Data");
        var countries =
          flowthru.Configuration.GetSection("Analysis:Countries").Get<string[]>()
          ?? throw new InvalidOperationException(
            "Analysis:Countries not configured in appsettings.json"
          );

        // Core catalog: raw inputs → intermediate → reporting.
        // Registered via DI factory so the resolver (populated by UseHttp above) is injected.
        flowthru.RegisterCatalog<CoreCatalog>(sp => new CoreCatalog(
          dataPath,
          sp.GetRequiredService<IStorageMediumResolver>()
        ));

        // Shard catalogs: one per country, labelled {Country}ShardCatalog
        var shardCatalogs = countries.Select(c => new CountryShardCatalog(c, dataPath)).ToList();
        flowthru.RegisterCatalogs(shardCatalogs);

        // Static pipelines resolved via DI
        flowthru.RegisterFlow("DataIngestion", (CoreCatalog cat) => DataIngestionFlow.Create(cat));
        flowthru.RegisterFlow("Reporting", (CoreCatalog cat) => ReportingFlow.Create(cat));
        flowthru.RegisterFlow("Graphing", GraphingFlow.Create);

        // Dynamic per-country analysis pipelines + fan-in consolidation.
        // CoreCatalog is resolved from DI here (same singleton registered above).
        flowthru.RegisterFlows(sp =>
        {
          var coreCatalog = sp.GetRequiredService<CoreCatalog>();
          var pipelines = new Dictionary<string, Flowthru.Core.Flows.Flow>();
          foreach (var shard in shardCatalogs)
          {
            pipelines[$"Analysis_{shard.Country.Replace(' ', '_')}"] = AnalysisFlow.Create(
              coreCatalog,
              shard
            );
          }
          pipelines["Consolidation"] = ConsolidationFlow.Create(coreCatalog, shardCatalogs);
          return pipelines;
        });

        // Output pipeline metadata
        flowthru.ConfigureMetadata(meta =>
        {
          var metadataPath = Path.Combine(basePath, "Metadata");
          meta.AddProvider<JsonMetadataProvider, JsonMetadataProviderBuilder>(json =>
              json.WithOutputDirectory(metadataPath)
            )
            .AddProvider<MermaidMetadataProvider, MermaidMetadataProviderBuilder>(mermaid =>
              mermaid.WithOutputDirectory(metadataPath)
            );
        });

        flowthru.ConfigureExecution(opts => opts.MaxDegreeOfParallelism = 8);
      }
    );

    services.AddLogging(logging =>
    {
      logging.AddConsole();
      logging.SetMinimumLevel(LogLevel.Information);
    });
  }
}
