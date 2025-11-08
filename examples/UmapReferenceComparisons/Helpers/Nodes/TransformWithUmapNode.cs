using Flowthru.Extensions.MLPure.UMAP;
using Microsoft.ML;
using UmapReferenceComparisons.Data._01_Raw.Schemas;

namespace UmapReferenceComparisons.Helpers.Nodes;

/// <summary>
/// Applies C# UMAP transformation to universal UmapInput data.
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
    public required UmapOptions UmapOptions { get; init; }
  }

  public static Func<IEnumerable<UmapInput>, Task<IEnumerable<UmapOutputRow>>> Create(
    Params options
  )
  {
    return async (input) =>
    {
      var inputList = input.ToList();
      Console.WriteLine(
        $"Transforming {inputList.Count} {options.DatasetName} samples with C# UMAP..."
      );

      // Extract feature vectors from UmapInput
      var data = inputList.Select(row => row.Features).ToArray();

      // Create UMAP trainer
      var mlContext = new MLContext(seed: options.UmapOptions.RandomState ?? 42);
      var trainer = mlContext.CreateUmapTrainer(options.UmapOptions);

      // Fit UMAP model and transform
      Console.WriteLine("Fitting UMAP model...");
      var (model, embedding) = trainer.FitTransform(data);
      Console.WriteLine(
        $"UMAP transformation complete. Output shape: ({embedding.Length}, {embedding[0].Length})"
      );

      // Convert to UmapOutputRow schema
      var result = embedding.Select(emb => new UmapOutputRow
      {
        Component0 = emb[0],
        Component1 = emb[1],
      });

      return await Task.FromResult(result);
    };
  }
}
