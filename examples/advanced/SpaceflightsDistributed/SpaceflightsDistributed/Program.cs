using Flowthru.Cli;
using Flowthru.Meta;
using Flowthru.Meta.Providers;
using Flowthru.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SpaceflightsDistributed.DataProcessing.Data;
using SpaceflightsDistributed.DataProcessing.Pipelines.DataProcessing;
using SpaceflightsDistributed.DataScience.Data;
using SpaceflightsDistributed.DataScience.Pipelines.DataScience;
using SpaceflightsDistributed.Reporting.Data;
using SpaceflightsDistributed.Reporting.Pipelines.Reporting;

namespace SpaceflightsDistributed;

/// <summary>
/// Entry point for the SpaceflightsDistributed pipeline.
///
/// This example demonstrates Flowthru's multi-catalog pipeline registration API.
/// Three independently-versioned library projects each define their own catalog
/// and the pipelines between them are expressed via multi-catalog RegisterPipeline
/// overloads — making cross-domain data dependencies part of the type signature, not
/// a runtime convention.
///
///   DataProcessingPipeline.Create(DataProcessingCatalog dc)
///   DataSciencePipeline.Create(DataProcessingCatalog dp, DataScienceCatalog ds, Params p)
///   ReportingPipeline.Create(DataProcessingCatalog dp, DataScienceCatalog ds, ReportingCatalog r)
/// </summary>
public class Program
{
  public static Task<int> Main(string[] args) =>
    FlowthruCli.RunStandaloneAsync(
      args,
      services => ConfigureServices(services, Directory.GetCurrentDirectory())
    );

  /// <summary>
  /// Configures services for the application. Used by test infrastructure.
  /// </summary>
  public static IServiceProvider ConfigureServices(string? basePath = null)
  {
    var services = new ServiceCollection();
    ConfigureServices(services, basePath ?? Directory.GetCurrentDirectory());
    return services.BuildServiceProvider();
  }

  private static void ConfigureServices(IServiceCollection services, string basePath)
  {
    var dataPath = Path.Combine(basePath, "Data");

    services.AddFlowthru(flowthru =>
    {
      flowthru.UseConfiguration(opts => opts.ConfigurationPath = basePath);

      // ─── Catalog Registration ──────────────────────────────────────────────
      // Each library owns its own catalog. Registered by concrete type so each
      // pipeline factory receives the correct strongly-typed instance.

      flowthru.UseCatalog<DataProcessingCatalog>(_ => new DataProcessingCatalog(dataPath));
      flowthru.UseCatalog<DataScienceCatalog>(_ => new DataScienceCatalog(dataPath));
      flowthru.UseCatalog<ReportingCatalog>(_ => new ReportingCatalog(dataPath));

      // ─── Metadata Providers ───────────────────────────────────────────────

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

      // ─── Pipeline Registration ─────────────────────────────────────────────
      // Single-catalog pipeline: reads from and writes to DataProcessingCatalog only.
      flowthru
        .RegisterPipeline<DataProcessingCatalog>(
          label: "DataProcessing",
          pipeline: DataProcessingPipeline.Create
        )
        .WithDescription("Preprocesses companies and shuttles data into a model input table");

      // 2-catalog pipeline: reads model input from DataProcessing, writes model artifacts to DataScience.
      // The two-catalog signature makes the cross-domain dependency an explicit compile-time contract.
      flowthru
        .RegisterPipelineWithConfiguration<
          DataProcessingCatalog,
          DataScienceCatalog,
          DataSciencePipeline.Params
        >(
          label: "DataScience",
          pipeline: DataSciencePipeline.Create,
          configurationSection: "Flowthru:Pipelines:DataScience"
        )
        .WithDescription("Trains linear regression model for shuttle price prediction");

      // 3-catalog pipeline: reads preprocessed shuttles (DataProcessing) and model predictions
      // (DataScience), writes all reports and charts to ReportingCatalog.
      flowthru
        .RegisterPipelineWithConfiguration<
          DataProcessingCatalog,
          DataScienceCatalog,
          ReportingCatalog,
          ReportingPipeline.Params
        >(
          label: "Reporting",
          pipeline: ReportingPipeline.Create,
          configurationSection: "Flowthru:Pipelines:Reporting"
        )
        .WithDescription(
          "Generates passenger capacity reports and confusion matrix visualizations"
        );
    });

    services.AddLogging(logging =>
    {
      logging.AddConsole();
      logging.SetMinimumLevel(LogLevel.Information);
    });
  }
}
