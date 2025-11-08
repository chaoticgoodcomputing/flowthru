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

        // TODO: Add MnistComparison and FashionMnistComparison pipelines
      }
    );

    return await app.RunAsync();
  }
}
