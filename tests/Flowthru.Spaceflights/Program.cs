using Flowthru.Spaceflights.Data;
using Flowthru.Spaceflights.Pipelines;
using Flowthru.Spaceflights.Pipelines.DataScience.Parameters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.Spaceflights;

/// <summary>
/// Entry point for the Spaceflights FlowThru example.
/// Demonstrates the complete user-facing API for defining and running data pipelines.
/// </summary>
public class Program
{
  public static async Task Main(string[] args)
  {
    // ═══════════════════════════════════════════════════════════════
    // STEP 1: Configure Dependency Injection and Logging
    // ═══════════════════════════════════════════════════════════════

    var services = new ServiceCollection();
    services.AddLogging(builder =>
    {
      builder.AddConsole();
      builder.SetMinimumLevel(LogLevel.Information);
    });

    var serviceProvider = services.BuildServiceProvider();
    var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

    // ═══════════════════════════════════════════════════════════════
    // STEP 2: Build Data Catalog
    // ═══════════════════════════════════════════════════════════════

    logger.LogInformation("Building data catalog...");
    var catalog = CatalogConfiguration.BuildCatalog();
    logger.LogInformation("Data catalog built successfully");

    // ═══════════════════════════════════════════════════════════════
    // STEP 3: Configure Model Options (Parameters)
    // ═══════════════════════════════════════════════════════════════

    var modelOptions = new ModelOptions
    {
      TestSize = 0.2,
      RandomState = 3,
      Features = new List<string>
            {
                "Engines",
                "PassengerCapacity",
                "Crew",
                "DCheckComplete",
                "MoonClearanceComplete",
                "IataApproved",
                "CompanyRating",
                "ReviewScoresRating"
            }
    };

    // ═══════════════════════════════════════════════════════════════
    // STEP 4: Register Pipelines
    // ═══════════════════════════════════════════════════════════════

    logger.LogInformation("Registering pipelines...");
    var pipelines = PipelineRegistry.RegisterPipelines(catalog, modelOptions);
    logger.LogInformation("Registered {Count} pipelines: {Names}",
        pipelines.Count,
        string.Join(", ", pipelines.Keys));

    // ═══════════════════════════════════════════════════════════════
    // STEP 5: Execute Pipelines
    // ═══════════════════════════════════════════════════════════════

    // Parse command line arguments
    var pipelineName = args.Length > 0 ? args[0] : "data_processing";

    if (!pipelines.ContainsKey(pipelineName))
    {
      logger.LogError("Pipeline '{Name}' not found. Available: {Names}",
          pipelineName,
          string.Join(", ", pipelines.Keys));
      return;
    }

    logger.LogInformation("Running pipeline: {Name}", pipelineName);
    var pipeline = pipelines[pipelineName];

    try
    {
      var result = await pipeline.RunAsync();

      if (result.Success)
      {
        logger.LogInformation("✓ Pipeline '{Name}' completed successfully", pipelineName);
        logger.LogInformation("Executed {Count} nodes", result.NodeResults.Count);

        foreach (var (nodeName, nodeResult) in result.NodeResults)
        {
          logger.LogInformation("  ✓ {Node} ({Time:F2}s)",
              nodeName,
              nodeResult.ExecutionTime.TotalSeconds);
        }
      }
      else
      {
        logger.LogError("✗ Pipeline '{Name}' failed at node: {FailedNode}",
            pipelineName,
            result.FailedNode);

        if (result.NodeResults.TryGetValue(result.FailedNode!, out var failedResult))
        {
          logger.LogError("Error: {Message}",
              failedResult.Exception?.Message);
          logger.LogError("{StackTrace}",
              failedResult.Exception?.StackTrace);
        }
      }
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Unhandled exception during pipeline execution");
    }

    // ═══════════════════════════════════════════════════════════════
    // EXAMPLE OUTPUT:
    // ═══════════════════════════════════════════════════════════════
    // info: Building data catalog...
    // info: Data catalog built successfully
    // info: Registering pipelines...
    // info: Registered 3 pipelines: data_processing, data_science, __default__
    // info: Running pipeline: data_processing
    // info: Loading data from 'companies' (CsvDataset)...
    // info: Running node: preprocess_companies
    // info: Saving data to 'preprocessed_companies' (ParquetDataset)...
    // info: Loading data from 'shuttles' (ExcelDataset)...
    // info: Running node: preprocess_shuttles
    // info: Saving data to 'preprocessed_shuttles' (ParquetDataset)...
    // info: Loading data from 'preprocessed_shuttles', 'preprocessed_companies', 'reviews'...
    // info: Running node: create_model_input_table
    // info: Saving data to 'model_input_table' (ParquetDataset)...
    // info: ✓ Pipeline 'data_processing' completed successfully
    // info: Executed 3 nodes
    // info:   ✓ preprocess_companies (0.42s)
    // info:   ✓ preprocess_shuttles (0.31s)
    // info:   ✓ create_model_input_table (0.18s)
  }
}
