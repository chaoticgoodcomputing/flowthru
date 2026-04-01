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
  /// Main entry point for the UMAP reference comparison CLI application.
  /// </summary>
  /// <param name="args">Command-line arguments</param>
  public static Task<int> Main(string[] args) =>
    FlowthruCli.RunStandaloneAsync(
      args,
      services => ConfigureServices(services, Directory.GetCurrentDirectory())
    );

  /// <summary>
  /// Configures services for the application. Used by test infrastructure.
  /// </summary>
  /// <param name="basePath">Optional base path for data files (defaults to current directory)</param>
  public static IServiceProvider ConfigureServices(string? basePath = null)
  {
    var services = new ServiceCollection();
    ConfigureServices(services, basePath ?? Directory.GetCurrentDirectory());
    return services.BuildServiceProvider();
  }

  /// <summary>
  /// Shared service configuration logic.
  /// </summary>
  private static void ConfigureServices(IServiceCollection services, string basePath)
  {
    services.AddFlowthru(flowthru =>
    {
      // Load configuration
      flowthru.UseConfiguration(opts => opts.ConfigurationPath = basePath);
      flowthru.UseCatalog(_ => new Catalog(Path.Combine(basePath, "Data")));

      // Register comparison pipelines
      flowthru
        .RegisterPipeline(label: "IrisComparisonPipeline", pipeline: IrisComparisonPipeline.Create)
        .WithDescription(
          "Compare C# UMAP against Python reference for Iris dataset (150 samples, 4 features)"
        );

      flowthru
        .RegisterPipeline(
          label: "DigitsComparisonPipeline",
          pipeline: DigitsComparisonPipeline.Create
        )
        .WithDescription(
          "Compare C# UMAP against Python reference for Digits dataset (1,797 samples, 64 features, 8x8 images)"
        );

      // Fashion-MNIST pipeline disabled to keep repo lean (70K samples = ~12MB)
      // Uncomment when large dataset support is needed
      // flowthru
      //   .RegisterPipeline(
      //     label: "FashionComparisonPipeline",
      //     pipeline: Pipelines.FashionComparison.FashionComparisonPipeline.Create
      //   )
      //   .WithDescription(
      //     "Compare C# UMAP against Python reference for Fashion-MNIST dataset (70,000 samples, 784 features, 28x28 images)"
      //   );
    });

    services.AddLogging(logging =>
    {
      logging.AddConsole();
      logging.SetMinimumLevel(LogLevel.Information);
    });
  }
}
