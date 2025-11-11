using Flowthru.Extensions.ML.UMAP.Core;
using UmapReferenceComparisons.Data._01_Raw.Schemas;

namespace UmapReferenceComparisons.Helpers.Nodes;

/// <summary>
/// Applies C# UMAP transformation to universal UmapInput data using the new strategy architecture.
/// </summary>
public static class TransformWithUmapNode
{
  public record Params
  {
    /// <summary>
    /// Dataset name for logging.
    /// </summary>
    public required string DatasetName { get; init; }

    /// <summary>
    /// UMAP hyperparameters.
    /// </summary>
    public required UmapParameters UmapParameters { get; init; }

    /// <summary>
    /// Layout initialization strategy. Valid values: "spectral" (default), "random"
    /// Matches Python UMAP's init parameter.
    /// </summary>
    public string InitStrategy { get; init; } = "spectral";
  }

  public static Func<IEnumerable<UmapInput>, Task<IEnumerable<UmapOutputRow>>> Create(
    Params options
  )
  {
    return async (input) =>
    {
      var inputList = input.ToList();
      Console.WriteLine(
        $"Transforming {inputList.Count} {options.DatasetName} samples with C# UMAP (new strategy architecture)..."
      );

      // Extract feature vectors from UmapInput
      var featureArray = inputList.Select(row => row.Features).ToArray();

      // Use simplified high-level API with specified initialization strategy
      var embeddingMatrix = UmapPipeline.Create(options.UmapParameters).FitTransform(featureArray);

      // Convert matrix result to output schema
      var result = Enumerable
        .Range(0, embeddingMatrix.Length)
        .Select(i => new UmapOutputRow
        {
          Component0 = embeddingMatrix[i][0],
          Component1 = embeddingMatrix[i][1],
        });

      return await Task.FromResult(result);
    };
  }
}
