using Flowthru.Application;
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
  public static async Task<int> Main(string[] args)
  {
    var app = FlowthruApplication.Create(
      args,
      builder =>
      {
        // Load configuration
        builder.UseConfiguration();

        // Register comparison pipelines
        builder
          .RegisterPipeline<Catalog>(
            label: "IrisComparisonPipeline",
            pipeline: IrisComparisonPipeline.Create
          )
          .WithDescription(
            "Compare C# UMAP against Python reference for Iris dataset (150 samples, 4 features)"
          );

        builder
          .RegisterPipeline<Catalog>(
            label: "DigitsComparisonPipeline",
            pipeline: DigitsComparisonPipeline.Create
          )
          .WithDescription(
            "Compare C# UMAP against Python reference for Digits dataset (1,797 samples, 64 features, 8x8 images)"
          );

        // builder
        //   .RegisterPipeline<Catalog>(
        //     label: "FashionComparisonPipeline",
        //     pipeline: Pipelines.FashionComparison.FashionComparisonPipeline.Create
        //   )
        //   .WithDescription(
        //     "Compare C# UMAP against Python reference for Fashion-MNIST dataset (70,000 samples, 784 features, 28x28 images)"
        //   );
      }
    );

    return await app.RunAsync();
  }
}
