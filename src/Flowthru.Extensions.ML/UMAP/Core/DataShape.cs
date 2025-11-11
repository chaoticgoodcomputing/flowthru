namespace Flowthru.Extensions.ML.UMAP.Core;

/// <summary>
/// Describes the shape and characteristics of input data.
/// Used by strategy factories to select appropriate default strategies.
/// </summary>
/// <remarks>
/// Analyzing data shape allows the UMAP pipeline to automatically choose
/// optimal strategies. For example:
/// - Small datasets (&lt; 4096 samples) can use exact k-NN
/// - Large datasets benefit from approximate nearest neighbor search
/// - Sparse data requires specialized algorithms
/// - High-dimensional data may benefit from PCA initialization
/// </remarks>
public sealed record DataShape
{
  /// <summary>
  /// Number of samples (rows) in the dataset.
  /// </summary>
  public required int Samples { get; init; }

  /// <summary>
  /// Number of features (columns) in the dataset.
  /// </summary>
  public required int Features { get; init; }

  /// <summary>
  /// Whether the data is stored in a sparse format.
  /// </summary>
  public required bool IsSparse { get; init; }

  /// <summary>
  /// Sparsity ratio (proportion of zero elements) if applicable.
  /// Only meaningful when <see cref="IsSparse"/> is true.
  /// </summary>
  public float? SparsityRatio { get; init; }

  /// <summary>
  /// Approximate memory footprint in bytes.
  /// </summary>
  public long EstimatedMemoryBytes { get; init; }

  /// <summary>
  /// Whether the dataset is considered "small" (typically &lt; 4096 samples).
  /// Small datasets can use exact algorithms.
  /// </summary>
  public bool IsSmallDataset => Samples < 4096;

  /// <summary>
  /// Whether the dataset is considered "large" (typically ≥ 4096 samples).
  /// Large datasets should use approximate algorithms.
  /// </summary>
  public bool IsLargeDataset => Samples >= 4096;

  /// <summary>
  /// Whether the dataset is high-dimensional (typically &gt; 100 features).
  /// High-dimensional data may benefit from dimensionality reduction in initialization.
  /// </summary>
  public bool IsHighDimensional => Features > 100;

  /// <summary>
  /// Whether the dataset is very high-dimensional (typically &gt; 1000 features).
  /// Very high-dimensional data may require PCA pre-processing.
  /// </summary>
  public bool IsVeryHighDimensional => Features > 1000;

  /// <summary>
  /// Recommended number of nearest neighbors based on dataset size.
  /// Follows Python UMAP heuristics: typically 15, but adjusted for very small datasets.
  /// </summary>
  public int RecommendedNeighbors
  {
    get
    {
      if (Samples < 15)
        return Math.Max(2, Samples - 1);
      return 15;
    }
  }

  /// <summary>
  /// Recommended number of training epochs based on dataset size.
  /// Follows Python UMAP heuristics: 500 for small datasets, 200 for large.
  /// </summary>
  public int RecommendedEpochs
  {
    get
    {
      if (Samples <= 10000)
        return 500;
      return 200;
    }
  }
}
