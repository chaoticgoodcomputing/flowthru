using Flowthru.Application;
using UmapReferenceComparisons.Data;
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
            label: "IrisComparison",
            pipeline: IrisComparisonPipeline.Create
          )
          .WithDescription(
            "Compare C# UMAP against Python reference for Iris dataset (150 samples, 4 features)"
          );

        // TODO: Add DigitsComparison and MnistComparison pipelines
      }
    );

    return await app.RunAsync();
  }
}
