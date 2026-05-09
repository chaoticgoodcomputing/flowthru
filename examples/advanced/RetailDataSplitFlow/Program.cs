using Flowthru.Cli;
using Flowthru.Data.Storage;
using Flowthru.Diagnostics;
using Flowthru.Diagnostics.Json;
using Flowthru.Diagnostics.Mermaid;
using Flowthru.Hosting;
using Flowthru.Step.Python;
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
    services.AddSingleton<IConfiguration>(configuration);

    var dataPath = Path.Combine(basePath, "Data");
    var countries =
      configuration.GetSection("Analysis:Countries").Get<string[]>()
      ?? throw new InvalidOperationException(
        "Analysis:Countries not configured in appsettings.json"
      );

    // Shard catalogs are constructed once and closure-captured by the
    // flow factories below; no DI registration needed because each shard
    // is bound to a specific country.
    var shardCatalogs = countries.Select(c => new CountryShardCatalog(c, dataPath)).ToList();

    services.AddFlowthru(flowthru =>
    {
      // HTTP storage medium — routes https:// catalog item paths through
      // a cached HTTP client. Conditional GET avoids re-downloading the
      // 43MB CSV on every run.
      flowthru.UseHttp(http =>
      {
        http.Cache = new Flowthru.Data.Storage.Http.HttpCacheOptions
        {
          Directory = Path.Combine(basePath, ".http-cache"),
          MaxAge = TimeSpan.FromHours(24),
        };
      });

      flowthru.UsePython(python =>
      {
        python.ModuleSearchPaths.Add(basePath);
        python.ModuleSearchPaths.Add(outputPath);
        python.VenvPath = outputPath;
      });

      flowthru.RegisterCatalog<CoreCatalog>(sp => new CoreCatalog(
        dataPath,
        sp.GetRequiredService<IStorageMediumResolver>()
      ));

      flowthru
        .RegisterFlow<CoreCatalog>("DataIngestion", DataIngestionFlow.Create)
        .WithDescription("Parses raw retail CSV (HTTP) into typed Parquet");

      flowthru
        .RegisterFlow<CoreCatalog>("Analysis", core => AnalysisFlow.Create(core, shardCatalogs))
        .WithDescription("Per-country weekly DTU computation (one step per country)");

      flowthru
        .RegisterFlow<CoreCatalog>("Consolidation", core => ConsolidationFlow.Create(core, shardCatalogs))
        .WithDescription("Variadic fan-in over all per-country shards");

      flowthru
        .RegisterFlow<CoreCatalog, IPythonExecutor>("Graphing", GraphingFlow.Create)
        .WithDescription("Plotly line charts (PNG) from consolidated DTU dataset");

      flowthru
        .RegisterFlow<CoreCatalog>("Reporting", ReportingFlow.Create)
        .WithDescription("Country debit/credit summary CSV");

      flowthru.ConfigureMetadata(meta =>
      {
        var metadataPath = Path.Combine(basePath, "Metadata");
        meta.AddJsonMetadata(opt => opt.WithOutputDirectory(metadataPath));
        meta.AddMermaidMetadata(opt => opt.WithOutputDirectory(metadataPath));
      });
    });

    services.AddLogging(logging =>
    {
      logging.AddConsole();
      logging.SetMinimumLevel(LogLevel.Information);
    });
  }
}
