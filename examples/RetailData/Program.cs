using Flowthru.Application;
using RetailData.Data;
using RetailData.Pipelines.Analytics;
using RetailData.Pipelines.DataProcessing;
using RetailData.Pipelines.Reporting;

namespace RetailData;

public class Program
{
  public static async Task<int> Main(string[] args)
  {
    var app = FlowthruApplication.Create(
      args,
      builder =>
      {
        // Load configuration from appsettings.json
        builder.UseConfiguration();

        // Register data processing pipeline
        builder
          .RegisterPipeline<Catalog>(
            label: "DataProcessing",
            pipeline: DataProcessingPipeline.Create
          )
          .WithDescription("Processes raw retail data, cleans it, and aggregates DTU metrics");

        // Register analytics pipeline
        builder
          .RegisterPipeline<Catalog>(label: "Analytics", pipeline: AnalyticsPipeline.Create)
          .WithDescription("Calculates country-to-country correlation analysis");

        // Register reporting pipeline
        builder
          .RegisterPipeline<Catalog>(label: "Reporting", pipeline: ReportingPipeline.Create)
          .WithDescription("Generates DTU and correlation visualizations");
      }
    );

    return await app.RunAsync();
  }
}
