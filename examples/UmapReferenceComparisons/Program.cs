using Flowthru.Cli;
using Flowthru.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UmapReferenceComparisons.Data;
using UmapReferenceComparisons.Pipelines.DigitsComparison;
using UmapReferenceComparisons.Pipelines.IrisComparison;

namespace UmapReferenceComparisons;

/// <summary>
/// UMAP Reference Comparison Application.
/// </summary>
/// <remarks>
/// Compares C# UMAP implementation against Python reference data to validate
/// data integrity and implementation correctness.
/// </remarks>
public class Program
{
  /// <summary>
  /// Configures Flowthru services for dependency injection.
  /// </summary>
  /// <returns>Configured service provider</returns>
  public static IServiceProvider ConfigureServices()
  {
    var services = new ServiceCollection();

    services.AddFlowthru(flowthru =>
    {
      // Load configuration
      flowthru.UseConfiguration();
      flowthru.UseCatalog(_ => new Catalog("Data"));

      // Register comparison pipelines
      flowthru
        .RegisterPipeline<Catalog>(
          label: "IrisComparisonPipeline",
          pipeline: IrisComparisonPipeline.Create
        )
        .WithDescription(
          "Compare C# UMAP against Python reference for Iris dataset (150 samples, 4 features)"
        );

      flowthru
        .RegisterPipeline<Catalog>(
          label: "DigitsComparisonPipeline",
          pipeline: DigitsComparisonPipeline.Create
        )
        .WithDescription(
          "Compare C# UMAP against Python reference for Digits dataset (1,797 samples, 64 features, 8x8 images)"
        );

      flowthru
        .RegisterPipeline<Catalog>(
          label: "FashionComparisonPipeline",
          pipeline: Pipelines.FashionComparison.FashionComparisonPipeline.Create
        )
        .WithDescription(
          "Compare C# UMAP against Python reference for Fashion-MNIST dataset (70,000 samples, 784 features, 28x28 images)"
        );
    });

    services.AddLogging(logging =>
    {
      logging.AddConsole();
      logging.SetMinimumLevel(LogLevel.Information);
    });

    return services.BuildServiceProvider();
  }

  public static async Task<int> Main(string[] args)
  {
    var services = ConfigureServices();

    // Resolve core service and construct CLI wrapper
    var service = services.GetRequiredService<IFlowthruService>();
    var logger = services.GetRequiredService<ILogger<FlowthruCli>>();
    var cli = new FlowthruCli(service, logger);

    return await cli.RunAsync(args);
  }
}
