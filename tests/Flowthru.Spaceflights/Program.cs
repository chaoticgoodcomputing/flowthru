using Flowthru.Spaceflights.Data;
using Flowthru.Spaceflights.Pipelines;
using Flowthru.Spaceflights.Pipelines.DataScience.Nodes;
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
    Console.WriteLine("=== DEBUG: Program.Main() started ===");
    Console.WriteLine($"=== DEBUG: Args: [{string.Join(", ", args)}] ===");

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

    Console.WriteLine("=== DEBUG: Logger configured ===");

    // ═══════════════════════════════════════════════════════════════
    // STEP 2: Build Data Catalog
    // ═══════════════════════════════════════════════════════════════

    logger.LogInformation("Building data catalog...");
    Console.WriteLine("=== DEBUG: About to build catalog ===");
    var catalog = SpaceflightsCatalog.Build("tests/Flowthru.Spaceflights/Data/Datasets");
    Console.WriteLine("=== DEBUG: Catalog built ===");
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
    Console.WriteLine($"=== DEBUG: Pipeline name: {pipelineName} ===");

    if (!pipelines.ContainsKey(pipelineName))
    {
      logger.LogError("Pipeline '{Name}' not found. Available: {Names}",
          pipelineName,
          string.Join(", ", pipelines.Keys));
      Console.WriteLine($"=== DEBUG: Pipeline not found! ===");
      return;
    }

    logger.LogInformation("Running pipeline: {Name}", pipelineName);
    Console.WriteLine($"=== DEBUG: About to get pipeline from registry ===");
    var pipeline = pipelines[pipelineName];
    Console.WriteLine($"=== DEBUG: Got pipeline, about to run ===");

    // Attach logger to pipeline for node execution diagnostics
    pipeline.Logger = logger;

    try
    {
      Console.WriteLine($"=== DEBUG: Calling pipeline.RunAsync() ===");
      var result = await pipeline.RunAsync();
      Console.WriteLine($"=== DEBUG: RunAsync() returned, Success={result.Success} ===");

      if (result.Success)
      {
        logger.LogInformation("✓ Pipeline '{Name}' completed successfully", pipelineName);
        Console.WriteLine($"=== DEBUG: Pipeline succeeded ===");
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
        Console.WriteLine($"=== DEBUG: Pipeline FAILED ===");
        logger.LogError("✗ Pipeline '{Name}' failed",
            pipelineName);
        logger.LogError("Error: {Message}", result.Exception?.Message);
        Console.WriteLine($"=== DEBUG: Exception: {result.Exception?.Message} ===");
        Console.WriteLine($"=== DEBUG: Stack: {result.Exception?.StackTrace} ===");

        // Find the failed node
        var failedNode = result.NodeResults.Values.FirstOrDefault(n => !n.Success);
        if (failedNode != null)
        {
          logger.LogError("Failed at node: {NodeName}", failedNode.NodeName);
          logger.LogError("{StackTrace}", failedNode.Exception?.StackTrace);
        }
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"=== DEBUG: EXCEPTION CAUGHT ===");
      Console.WriteLine($"=== DEBUG: Message: {ex.Message} ===");
      Console.WriteLine($"=== DEBUG: Stack: {ex.StackTrace} ===");
      logger.LogError(ex, "Unhandled exception during pipeline execution");
    }

    Console.WriteLine($"=== DEBUG: Program.Main() ending ===");

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
