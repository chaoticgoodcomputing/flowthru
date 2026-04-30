using Flowthru.Core.Cli;
using Flowthru.Core.Services;
using Flowthru.Extensions.Python;
using Flowthru.Extensions.Python.Services;
using Flowthru.Meta;
using Flowthru.Meta.Providers;
using FlowthruCoverage.Data;
using FlowthruCoverage.Flows.Coverage;
using FlowthruCoverage.Flows.Reporting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FlowthruCoverage;

public class Program
{
  public static Task<int> Main(string[] args) =>
    FlowthruCli.RunStandaloneAsync(
      args,
      services => ConfigureServices(services, Directory.GetCurrentDirectory())
    );

  public static IServiceProvider ConfigureServices(string? basePath = null)
  {
    var services = new ServiceCollection();
    ConfigureServices(services, basePath ?? Directory.GetCurrentDirectory());
    return services.BuildServiceProvider();
  }

  private static void ConfigureServices(IServiceCollection services, string basePath)
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
        flowthru.UsePython(python =>
        {
          python.ModuleSearchPaths.Add(basePath);
          python.ModuleSearchPaths.Add(AppDomain.CurrentDomain.BaseDirectory);
          python.VenvPath = AppDomain.CurrentDomain.BaseDirectory;
        });

        flowthru.RegisterCatalog(_ => new Catalog(Path.Combine(basePath, "Data")));

        flowthru
          .RegisterFlow(label: "Coverage", flow: CoverageAnalysisFlow.Create)
          .WithDescription(
            "Aggregates staged Cobertura XML reports into a pivot-ready coverage heatmap CSV."
          );

        flowthru
          .RegisterFlow(label: "Reporting", flow: ReportingFlow.Create)
          .WithDescription(
            "Generates an interactive Plotly HTML heatmap from the aggregated coverage CSV."
          );

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
      }
    );

    services.AddLogging(logging =>
    {
      logging.AddConsole();
      logging.SetMinimumLevel(LogLevel.Information);
    });
  }
}
