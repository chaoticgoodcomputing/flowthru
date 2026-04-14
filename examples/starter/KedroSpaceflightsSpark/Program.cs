using Flowthru.Core.Cli;
using Flowthru.Core.Services;
using Flowthru.Extensions.Spark;
using Flowthru.Extensions.Spark.Runtime;
using Flowthru.Extensions.Spark.Services;
using Flowthru.Meta;
using Flowthru.Meta.Providers;
using KedroSpaceflightsSpark.Data;
using KedroSpaceflightsSpark.Flows.DataProcessing;
using KedroSpaceflightsSpark.Flows.DataScience;
using KedroSpaceflightsSpark.Flows.Reporting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KedroSpaceflightsSpark;

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
    services.AddFlowthru(flowthru =>
    {
      flowthru.UseConfiguration(opts => opts.ConfigurationPath = basePath);

      flowthru.RegisterCatalog<Catalog>(sp => new Catalog(
        Path.Combine(basePath, "Data"),
        sp.GetRequiredService<SparkFrameProvider>(),
        sp.GetRequiredService<SparkRuntime>()
      ));

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

      flowthru
        .RegisterFlow(label: "DataProcessing", flow: DataProcessingFlow.Create)
        .WithDescription("Preprocesses companies, shuttles, and reviews data using Spark DataFrames");

      flowthru
        .RegisterFlow(
          label: "DataScience",
          flow: DataScienceFlow.Create,
          configurationSection: "Flowthru:Flows:DataScience"
        )
        .WithDescription("Trains linear regression model for price prediction");

      flowthru
        .RegisterFlow(
          label: "Reporting",
          flow: ReportingFlow.Create,
          configurationSection: "Flowthru:Flows:Reporting"
        )
        .WithDescription("Generates passenger capacity reports and visualizations");

      flowthru.UseSpark();
    });

    services.AddLogging(logging =>
    {
      logging.AddConsole();
      logging.SetMinimumLevel(LogLevel.Information);
    });
  }
}
