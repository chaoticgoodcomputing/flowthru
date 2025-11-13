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

  public static Func<IEnumerable<UmapInput>, Task<(IEnumerable<UmapOutputRow>, string)>> Create(
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

      // Use new FitTransformWithReport API to get both embedding and timing metrics
      var fitResult = UmapPipeline
        .Create(options.UmapParameters)
        .FitTransformWithReport(
          MathNet.Numerics.LinearAlgebra.Single.DenseMatrix.OfRowArrays(featureArray)
        );

      // Convert embedding matrix to output schema
      var embedding = Enumerable
        .Range(0, fitResult.Embedding.RowCount)
        .Select(i => new UmapOutputRow
        {
          Component0 = fitResult.Embedding[i, 0],
          Component1 = fitResult.Embedding[i, 1],
        });

      // Format runtime report as human-readable text
      var runtimeReport = FormatRuntimeReport(
        options.DatasetName,
        fitResult.RuntimeReport,
        inputList.Count
      );

      return await Task.FromResult((embedding, runtimeReport));
    };
  }

  private static string FormatRuntimeReport(
    string datasetName,
    UmapRuntimeReport report,
    int sampleCount
  )
  {
    var sb = new System.Text.StringBuilder();
    sb.AppendLine($"UMAP Runtime Report: {datasetName}");
    sb.AppendLine($"Samples: {sampleCount}");
    sb.AppendLine();
    sb.AppendLine("Phase Timings:");

    foreach (var (stage, milliseconds) in report.Timings.OrderBy(kv => kv.Key))
    {
      sb.AppendLine($"  {stage, -25} {milliseconds, 6} ms");
    }

    sb.AppendLine();
    sb.AppendLine(
      $"Total Time: {report.TotalTimeMs} ms ({report.TotalTimeMs / 1000.0:F2} seconds)"
    );

    return sb.ToString();
  }
}
