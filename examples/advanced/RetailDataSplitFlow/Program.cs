using Flowthru.Core.Cli;
using Flowthru.Extensions.Python;
using Flowthru.Extensions.Python.Services;
using Flowthru.Core.Meta;
using Flowthru.Core.Meta.Providers;
using Flowthru.Core.Services;
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
    services.AddFlowthru(flowthru =>
    {
      flowthru.UseConfiguration(opts => opts.ConfigurationPath = basePath);

      // Configure Python runtime — makes Flows/ importable and exposes the @step decorator
      flowthru.UsePython(python =>
      {
        python.ModuleSearchPaths.Add(basePath);
        python.ModuleSearchPaths.Add(outputPath);
        python.VenvPath = outputPath;
      });

      var dataPath = Path.Combine(basePath, "Data");
      var config = new ConfigurationBuilder()
        .SetBasePath(basePath)
        .AddJsonFile("appsettings.json")
        .Build();
      var countries =
        config.GetSection("Analysis:Countries").Get<string[]>()
        ?? throw new InvalidOperationException(
          "Analysis:Countries not configured in appsettings.json"
        );

      // Core catalog: raw inputs → intermediate → reporting
      var coreCatalog = new CoreCatalog(dataPath);
      flowthru.RegisterCatalog(coreCatalog);

      // Shard catalogs: one per country, labelled {Country}ShardCatalog
      var shardCatalogs = countries.Select(c => new CountryShardCatalog(c, dataPath)).ToList();
      flowthru.RegisterCatalogs(shardCatalogs);

      // Static pipelines resolved via DI
      flowthru.RegisterFlow("DataIngestion", (CoreCatalog cat) => DataIngestionFlow.Create(cat));
      flowthru.RegisterFlow("Reporting", (CoreCatalog cat) => ReportingFlow.Create(cat));
      flowthru.RegisterFlow("Graphing", GraphingFlow.Create);

      // Dynamic per-country analysis pipelines + fan-in consolidation
      flowthru.RegisterFlows(_ =>
      {
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
    });

    services.AddLogging(logging =>
    {
      logging.AddConsole();
      logging.SetMinimumLevel(LogLevel.Information);
    });
  }
}
