using Flowthru.Application;
using Flowthru.Tests.KedroSpaceflights.Data;
using Flowthru.Tests.KedroSpaceflights.Pipelines.DataProcessing;
using Flowthru.Tests.KedroSpaceflights.Pipelines.DataScience;
using Flowthru.Tests.KedroSpaceflights.Pipelines.DataScience.Nodes;
using Flowthru.Tests.KedroSpaceflights.Pipelines.DataValidation;
using Flowthru.Tests.KedroSpaceflights.Pipelines.DataValidation.Nodes;
using Flowthru.Tests.KedroSpaceflights.Pipelines.Reporting;
using Microsoft.Extensions.Logging;
using static Flowthru.Meta.Providers.MermaidMetadataProvider;

namespace Flowthru.Tests.KedroSpaceflights;

/// <summary>
/// Entry point for the Spaceflights FlowThru example.
/// Demonstrates a hybrid configuration approach:
/// - Infrastructure (catalog, metadata, logging) configured in appsettings.json
/// - Pipeline registration in code for compile-time safety
/// - Pipeline parameters loaded from appsettings.json for easy tuning
/// </summary>
public class Program {
  public static async Task<int> Main(string[] args) {
    var app = FlowthruApplication.Create(args, builder => {

      // Enable configuration loading from appsettings.json files
      // This loads: appsettings.json (base) -> appsettings.{Environment}.json -> appsettings.Local.json
      // Catalog and metadata will be auto-configured from the Flowthru section
      builder.UseConfiguration();

      // Register pipelines explicitly in code for compile-time safety and discoverability
      // Parameters are loaded from configuration for easy tuning without code changes

      builder
        .RegisterPipeline<SpaceflightsCatalog>(
          label: "DataProcessing",
          creator: DataProcessingPipeline.Create
        )
        .WithDescription("Preprocesses raw data and creates model input table")
        .WithTags("preprocessing", "features");

      builder
        .RegisterPipelineWithConfiguration<SpaceflightsCatalog, DataSciencePipelineParams>(
          label: "DataScience",
          creator: DataSciencePipeline.Create,
          configurationSection: "Flowthru:Pipelines:DataScience"
        )
        .WithDescription("Trains and evaluates ML model")
        .WithTags("ml", "training", "evaluation");

      builder
        .RegisterPipeline<SpaceflightsCatalog>(
          label: "DataValidation",
          creator: DataValidationPipeline.Create
        )
        .WithDescription("Validates pipeline outputs against Kedro reference and exports diagnostic data")
        .WithTags("validation", "quality");

      builder
        .RegisterPipeline<SpaceflightsCatalog>(
          label: "Reporting",
          creator: ReportingPipeline.Create
        )
        .WithDescription("Generates reports and visualizations")
        .WithTags("reporting", "visualization");
    });

    return await app.RunAsync();
  }
}
