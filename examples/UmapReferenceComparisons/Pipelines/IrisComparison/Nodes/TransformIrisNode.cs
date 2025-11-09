using Flowthru.Extensions.ML.UMAP;
using Microsoft.ML;
using UmapReferenceComparisons.Data._01_Raw.Schemas;

namespace UmapReferenceComparisons.Pipelines.IrisComparison.Nodes;

/// <summary>
/// Applies C# UMAP transformation to Iris input data.
/// </summary>
/// <remarks>
/// Uses the same UMAP parameters as the Python reference implementation:
/// - n_neighbors: 50
/// - learning_rate: 0.5
/// - min_dist: 0.001
/// - n_components: 2
/// - random_state: 42
/// </remarks>
public static class TransformIrisNode
{
  public static Func<IEnumerable<IrisInputRow>, Task<IEnumerable<UmapOutputRow>>> Create()
  {
    return async (input) =>
    {
      var inputList = input.ToList();
      Console.WriteLine($"Transforming {inputList.Count} Iris samples with C# UMAP...");

      // Convert Iris rows to float[][] for UMAP
      var data = inputList
        .Select(row =>
          new float[] { row.SepalLength, row.SepalWidth, row.PetalLength, row.PetalWidth }
        )
        .ToArray();

      // Configure UMAP to match Python reference parameters
      var umapOptions = new UmapOptions
      {
        NumberOfNeighbors = 50,
        LearningRate = 0.5f,
        MinDist = 0.001f,
        NumberOfComponents = 2,
        RandomState = 42,
        Metric = "euclidean",
        NumberOfEpochs = null, // Use default
        Verbosity =
          2 // Show progress
        ,
      };

      // Create UMAP trainer
      var mlContext = new MLContext(seed: 42);
      var trainer = mlContext.CreateUmapTrainer(umapOptions);

      // Fit UMAP model and transform
      Console.WriteLine("Fitting UMAP model...");
      var (model, embedding) = trainer.FitTransform(data);
      Console.WriteLine(
        $"UMAP transformation complete. Output shape: ({embedding.Length}, {embedding[0].Length})"
      );

      // Convert to UmapEmbedding2D schema
      var result = embedding.Select(emb => new UmapOutputRow
      {
        Component0 = emb[0],
        Component1 = emb[1],
      });

      return await Task.FromResult(result);
    };
  }
}
