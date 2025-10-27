using FlowthruIris.Data;
using FlowthruIris.Pipelines.Analysis;
using Flowthru.Application;
using Microsoft.Extensions.Logging;

namespace FlowthruIris;

/// <summary>
/// Iris Classification Demo using Flowthru Framework
/// 
/// <para><strong>Overview:</strong></para>
/// <para>
/// This application demonstrates Flowthru's capabilities for building type-safe,
/// descriptive machine learning pipelines using the classic Iris dataset.
/// </para>
/// 
/// <para><strong>Pipelines:</strong></para>
/// <list type="number">
/// <item><strong>Analysis</strong>: Processes raw Iris data, trains two ML.NET models (SDCA and OVA+FastTree)</item>
/// <item><strong>Reports</strong>: Generates visualizations and Markdown reports from trained models</item>
/// </list>
/// 
/// <para><strong>Usage:</strong></para>
/// <code>
/// dotnet run Analysis    # Run analysis pipeline (train models)
/// dotnet run Reports     # Run reports pipeline (generate visualizations and reports)
/// dotnet run             # Run all pipelines sequentially
/// </code>
/// 
/// <para><strong>Output Artifacts:</strong></para>
/// <list type="bullet">
/// <item>Models/sdca_model.zip - SDCA Maximum Entropy model</item>
/// <item>Models/ova_model.zip - OneVersusAll + FastTree model</item>
/// <item>Reports/data_scatter.png - Iris data scatter plot</item>
/// <item>Reports/sdca_confusion.png - SDCA confusion matrix</item>
/// <item>Reports/ova_confusion.png - OVA confusion matrix</item>
/// <item>Reports/sdca_report.md - SDCA performance report</item>
/// <item>Reports/ova_report.md - OVA performance report</item>
/// </list>
/// </summary>
class Program {
  static async Task<int> Main(string[] args) {
    // Initialize catalog
    var catalog = new IrisCatalog();

    // Create Flowthru application
    var app = FlowthruApplication.Create(args, builder => {
      builder
          // Configure logging
          .ConfigureLogging(logging => {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Information);
          })

          // Register data catalog
          .UseCatalog(catalog)

          // Register Analysis pipeline
          .RegisterPipeline<IrisCatalog>(
              label: "Analysis",
              creator: AnalysisPipeline.Create
          );
    });

    // Run application with command-line arguments
    return await app.RunAsync();
  }
}
