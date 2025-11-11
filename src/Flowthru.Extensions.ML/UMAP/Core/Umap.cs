using Flowthru.Extensions.ML.UMAP.Core.Markers;
using Flowthru.Extensions.ML.UMAP.Strategies.GraphRefinement.Implementations;
using Flowthru.Extensions.ML.UMAP.Strategies.LayoutInit;
using Flowthru.Extensions.ML.UMAP.Strategies.LayoutInit.Implementations;
using Flowthru.Extensions.ML.UMAP.Strategies.LayoutOptimization.Implementations;
using Flowthru.Extensions.ML.UMAP.Strategies.LocalMetric.Implementations;
using Flowthru.Extensions.ML.UMAP.Strategies.MembershipStrength.Implementations;
using Flowthru.Extensions.ML.UMAP.Strategies.NeighborSearch.Implementations;
using Flowthru.Extensions.ML.UMAP.Strategies.SamplingSchedule.Implementations;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Single;

namespace Flowthru.Extensions.ML.UMAP.Core;

/// <summary>
/// High-level factory for creating UMAP pipelines with smart defaults.
/// Provides a simple API that automatically configures strategies based on data characteristics.
/// </summary>
/// <remarks>
/// <para>
/// This factory provides the simplest possible API for UMAP:
/// </para>
/// <code>
/// var embedding = Umap.FitTransform(data);
/// </code>
/// <para>
/// The factory automatically:
/// </para>
/// <list type="bullet">
///   <item><description>Analyzes data shape and characteristics</description></item>
///   <item><description>Selects appropriate strategies (brute force for small data, approximate for large)</description></item>
///   <item><description>Configures parameters using Python UMAP heuristics</description></item>
///   <item><description>Handles data format conversions</description></item>
/// </list>
/// <para>
/// For advanced use cases requiring custom strategies or fine-tuned parameters,
/// use <see cref="UmapPipeline{TMetric}.CreateBuilder"/> directly.
/// </para>
/// </remarks>
public static class Umap
{
  /// <summary>
  /// Fits UMAP and transforms data in one step using default parameters and Euclidean metric.
  /// </summary>
  /// <param name="data">
  /// Input data as a 2D array where each row is a sample and each column is a feature.
  /// Shape: (n_samples, n_features)
  /// </param>
  /// <param name="parameters">
  /// Optional UMAP parameters. If null, uses defaults optimized for the data size.
  /// </param>
  /// <param name="initStrategy">
  /// Optional layout initialization strategy. If null, uses spectral initialization (Python UMAP default).
  /// Use 'random' for random initialization matching Python's init="random".
  /// </param>
  /// <returns>
  /// 2D embedding where each row corresponds to the input sample.
  /// Shape: (n_samples, n_components) where n_components is typically 2.
  /// </returns>
  /// <remarks>
  /// This is the simplest UMAP API. It:
  /// <list type="number">
  ///   <item><description>Analyzes data to determine size category (small/large)</description></item>
  ///   <item><description>Selects appropriate neighbor search strategy</description></item>
  ///   <item><description>Builds fuzzy simplicial set graph</description></item>
  ///   <item><description>Initializes and optimizes embedding</description></item>
  /// </list>
  /// </remarks>
  public static float[][] FitTransform(
    float[][] data,
    UmapParameters? parameters = null,
    string? initStrategy = null
  )
  {
    // Convert to matrix
    var matrix = DenseMatrix.OfRowArrays(data);

    // Call matrix overload
    var resultMatrix = FitTransform(matrix, parameters, initStrategy);

    // Convert back to 2D array
    var result = new float[resultMatrix.RowCount][];
    for (int i = 0; i < resultMatrix.RowCount; i++)
    {
      result[i] = resultMatrix.Row(i).ToArray();
    }

    return result;
  }

  /// <summary>
  /// Fits UMAP and transforms data in one step using default parameters and Euclidean metric.
  /// </summary>
  /// <param name="data">
  /// Input data matrix where each row is a sample and each column is a feature.
  /// Shape: (n_samples, n_features)
  /// </param>
  /// <param name="parameters">
  /// Optional UMAP parameters. If null, uses defaults optimized for the data size.
  /// </param>
  /// <param name="initStrategy">
  /// Optional layout initialization strategy. If null, uses spectral initialization (Python UMAP default).
  /// Valid values: "spectral" (default), "random"
  /// </param>
  /// <returns>
  /// Embedding matrix where each row corresponds to the input sample.
  /// Shape: (n_samples, n_components) where n_components is typically 2.
  /// </returns>
  public static Matrix<float> FitTransform(
    Matrix<float> data,
    UmapParameters? parameters = null,
    string? initStrategy = null
  )
  {
    // Analyze data shape
    var shape = AnalyzeDataShape(data);

    // Use provided parameters or create defaults based on data shape
    var effectiveParams = parameters ?? CreateDefaultParameters(shape);
    effectiveParams.Validate();

    // Normalize init strategy
    var effectiveInitStrategy = initStrategy?.ToLowerInvariant() ?? "spectral";

    // Select appropriate pipeline based on data size
    if (shape.IsSmallDataset)
    {
      return FitTransformSmallData(data, effectiveParams, effectiveInitStrategy);
    }
    else
    {
      // TODO: Implement large data pipeline with approximate search
      throw new NotImplementedException(
        "Large dataset support (approximate k-NN) is not yet implemented. "
          + $"Current data has {shape.Samples} samples (threshold: 4096)."
      );
    }
  }

  /// <summary>
  /// Pipeline for small datasets using exact brute-force k-NN.
  /// </summary>
  private static Matrix<float> FitTransformSmallData(
    Matrix<float> data,
    UmapParameters parameters,
    string initStrategy
  )
  {
    // Build pipeline with appropriate initialization
    // Select initialization strategy based on parameter
    var layoutInit =
      initStrategy == "random"
        ? (ILayoutInitStrategy<IEuclideanMetric>)new RandomInit<IEuclideanMetric>()
        : new SpectralInit<IEuclideanMetric>();

    // Build pipeline with selected initialization strategy
    var pipeline = UmapPipeline<IEuclideanMetric>
      .CreateBuilder(parameters)
      .WithNeighborSearch(new BruteForceSearch<IEuclideanMetric>())
      .WithLocalMetric(new BinarySearchSmoothing())
      .WithMembershipStrength(new ExponentialKernel())
      .WithGraphRefinement(new AdaptiveThresholding())
      .WithLayoutInit(layoutInit)
      .WithSamplingSchedule(new ProportionalSampling())
      .WithLayoutOptimization(new EuclideanSGD())
      .Build();

    // Execute complete UMAP algorithm
    var result = pipeline.FitTransform(data, EuclideanDistance);

    if (parameters.Verbosity >= 1)
    {
      Console.WriteLine(
        $"UMAP complete (init={initStrategy}): {result.GraphResult.Graph.NonZerosCount} edges, final embedding shape: ({result.Embedding.RowCount}, {result.Embedding.ColumnCount})"
      );
    }

    return result.Embedding;
  }

  /// <summary>
  /// Euclidean distance metric (L2 norm).
  /// </summary>
  private static float EuclideanDistance(ReadOnlySpan<float> x, ReadOnlySpan<float> y)
  {
    float sum = 0f;
    for (int i = 0; i < x.Length; i++)
    {
      float diff = x[i] - y[i];
      sum += diff * diff;
    }
    return MathF.Sqrt(sum);
  }

  /// <summary>
  /// Analyzes data characteristics to inform strategy selection.
  /// </summary>
  private static DataShape AnalyzeDataShape(Matrix<float> data)
  {
    return new DataShape
    {
      Samples = data.RowCount,
      Features = data.ColumnCount,
      IsSparse = false, // Dense matrix
      EstimatedMemoryBytes = data.RowCount * data.ColumnCount * sizeof(float),
    };
  }

  /// <summary>
  /// Creates default parameters optimized for the given data shape.
  /// Follows Python UMAP heuristics.
  /// </summary>
  private static UmapParameters CreateDefaultParameters(DataShape shape)
  {
    return new UmapParameters
    {
      NumberOfNeighbors = shape.RecommendedNeighbors,
      NumberOfEpochs = shape.RecommendedEpochs,
      NumberOfComponents = 2, // Standard for visualization
      MinDist = 0.1f,
      Spread = 1.0f,
      LocalConnectivity = 1.0f,
      LearningRate = 1.0f,
      RepulsionStrength = 1.0f,
      NegativeSampleRate = 5,
      SetOpMixRatio = 1.0f,
      Verbosity = 0,
    };
  }
}
