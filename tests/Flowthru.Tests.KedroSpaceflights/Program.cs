using Flowthru.Tests.KedroSpaceflights.Data;
using Flowthru.Tests.KedroSpaceflights.Pipelines;
using Flowthru.Tests.KedroSpaceflights.Pipelines.DataScience.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.Tests.KedroSpaceflights;

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
    var catalog = new SpaceflightsCatalog("Data/Datasets");
    logger.LogInformation("Data catalog built successfully");

    // ═══════════════════════════════════════════════════════════════
    // STEP 4: Register Pipelines
    // ═══════════════════════════════════════════════════════════════

    logger.LogInformation("Registering pipelines...");
    var pipelines = PipelineRegistry.RegisterPipelines(catalog);
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
          string.Join(", ", pipelines.Keys)); return;
    }

    logger.LogInformation("Running pipeline: {Name}", pipelineName); var pipeline = pipelines[pipelineName];
    // Attach logger to pipeline for node execution diagnostics
    pipeline.Logger = logger;

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
      logger.LogError("✗ Pipeline '{Name}' failed",
          pipelineName);
      logger.LogError("Error: {Message}", result.Exception?.Message); Console.WriteLine($"=== DEBUG: Stack: {result.Exception?.StackTrace} ===");

      // Find the failed node
      var failedNode = result.NodeResults.Values.FirstOrDefault(n => !n.Success);
      if (failedNode != null)
      {
        logger.LogError("Failed at node: {NodeName}", failedNode.NodeName);
        logger.LogError("{StackTrace}", failedNode.Exception?.StackTrace);
      }
    }
  }
}
