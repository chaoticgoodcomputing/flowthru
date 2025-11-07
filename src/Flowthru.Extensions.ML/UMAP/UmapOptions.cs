namespace Flowthru.Extensions.ML.UMAP;

/// <summary>
/// Configuration options for UMAP dimensionality reduction.
/// </summary>
/// <remarks>
/// Based on the UMAP Python implementation by Leland McInnes.
/// <para>
/// Citation: McInnes, L, Healy, J, "UMAP: Uniform Manifold Approximation and Projection
/// for Dimension Reduction", ArXiv e-prints 1802.03426, 2018
/// https://arxiv.org/abs/1802.03426
/// </para>
/// </remarks>
public sealed class UmapOptions
{
  /// <summary>
  /// The number of neighboring points used in local approximations of manifold structure.
  /// </summary>
  /// <remarks>
  /// Larger values result in more global structure being preserved at the loss of detailed
  /// local structure. Typical values are in the range 5-50, with 15 being a reasonable default.
  /// </remarks>
  public int NumberOfNeighbors { get; init; } = 15;

  /// <summary>
  /// The target dimensionality of the low-dimensional embedding.
  /// </summary>
  /// <remarks>
  /// Defaults to 2 for easy visualization, but can be any positive integer.
  /// Common values are 2 or 3 for visualization, or higher for general dimensionality reduction.
  /// </remarks>
  public int NumberOfComponents { get; init; } = 2;

  /// <summary>
  /// The distance metric to use for measuring distances in high-dimensional space.
  /// </summary>
  /// <remarks>
  /// Supported metrics: "euclidean" (default), "cosine", "correlation", "manhattan".
  /// </remarks>
  public string Metric { get; init; } = "euclidean";

  /// <summary>
  /// The minimum distance between points in the low-dimensional embedding.
  /// </summary>
  /// <remarks>
  /// Controls how tightly the embedding is allowed to compress points together.
  /// Smaller values (0.001-0.1) allow more accurate local structure preservation.
  /// Larger values (0.1-0.5) result in more even distribution. Default is 0.1.
  /// </remarks>
  public float MinDist { get; init; } = 0.1f;

  /// <summary>
  /// The effective scale of embedded points.
  /// </summary>
  /// <remarks>
  /// In combination with MinDist, this determines how clustered/clumped the
  /// embedded points are. Default is 1.0.
  /// </remarks>
  public float Spread { get; init; } = 1.0f;

  /// <summary>
  /// The number of training epochs for optimizing the low-dimensional embedding.
  /// </summary>
  /// <remarks>
  /// If null (default), a value will be selected based on the size of the input dataset
  /// (200 for large datasets, 500 for small). More epochs result in more accurate embeddings.
  /// </remarks>
  public int? NumberOfEpochs { get; init; } = null;

  /// <summary>
  /// The initial learning rate for the SGD optimization.
  /// </summary>
  public float LearningRate { get; init; } = 1.0f;

  /// <summary>
  /// The local connectivity required -- i.e., the number of nearest neighbors
  /// that should be assumed to be connected at a local level.
  /// </summary>
  /// <remarks>
  /// Higher values make the manifold more locally connected. Should not be more
  /// than the local intrinsic dimension of the manifold. Default is 1.0.
  /// </remarks>
  public float LocalConnectivity { get; init; } = 1.0f;

  /// <summary>
  /// Weighting applied to negative samples in low-dimensional embedding optimization.
  /// </summary>
  /// <remarks>
  /// Values higher than 1 result in greater weight being given to negative samples.
  /// Default is 1.0.
  /// </remarks>
  public float RepulsionStrength { get; init; } = 1.0f;

  /// <summary>
  /// The number of negative samples to select per positive sample in the optimization process.
  /// </summary>
  /// <remarks>
  /// Increasing this value results in greater repulsive force, greater optimization cost,
  /// but slightly more accuracy. Default is 5.
  /// </remarks>
  public int NegativeSampleRate { get; init; } = 5;

  /// <summary>
  /// Interpolate between fuzzy union and intersection as the set operation used to
  /// combine local fuzzy simplicial sets.
  /// </summary>
  /// <remarks>
  /// A value of 1.0 uses pure fuzzy union, while 0.0 uses pure fuzzy intersection.
  /// Default is 1.0.
  /// </remarks>
  public float SetOpMixRatio { get; init; } = 1.0f;

  /// <summary>
  /// Random seed for reproducibility. If null, a random seed is used.
  /// </summary>
  public int? RandomState { get; init; } = null;

  /// <summary>
  /// Verbosity level for progress reporting.
  /// </summary>
  /// <remarks>
  /// <para>0 = Silent (no progress output)</para>
  /// <para>1 = Minimal (major phases only)</para>
  /// <para>2 = Detailed (phase progress percentages)</para>
  /// <para>Default is 1.</para>
  /// </remarks>
  public int Verbosity { get; init; } = 1;

  /// <summary>
  /// Optional progress reporter for programmatic progress tracking.
  /// </summary>
  /// <remarks>
  /// Reports (Stage, Progress) where Stage is the current operation and Progress is 0.0-1.0.
  /// If null, progress is written to Console based on Verbosity setting.
  /// </remarks>
  public IProgress<(
    string Stage,
    float Progress,
    string? Details
  )>? ProgressReporter { get; init; } = null;

  /// <summary>
  /// Whether to use approximate nearest neighbors (ANN) instead of exact k-NN.
  /// </summary>
  /// <remarks>
  /// <para>
  /// For large datasets (>10k samples) in high dimensions (>50D), approximate nearest neighbors
  /// can provide 10-100x speedup with minimal accuracy loss.
  /// </para>
  /// <para>
  /// Uses Random Projection Trees (similar to Annoy) for efficient approximate search.
  /// Recommended for datasets with >10,000 samples and >50 dimensions.
  /// </para>
  /// <para>Default: null (auto-detect based on dataset size)</para>
  /// <list type="bullet">
  /// <item>null: Use ANN if samples > 10,000 AND dimensions > 50</item>
  /// <item>true: Always use ANN</item>
  /// <item>false: Always use exact k-NN</item>
  /// </list>
  /// </remarks>
  public bool? UseApproximateNearestNeighbors { get; init; } = null;

  /// <summary>
  /// Number of random projection trees to build for approximate nearest neighbors.
  /// </summary>
  /// <remarks>
  /// <para>
  /// More trees = better accuracy but slower queries and more memory.
  /// Only used when UseApproximateNearestNeighbors is true.
  /// </para>
  /// <para>Recommended values: 10-20 for most datasets. Default: 10.</para>
  /// </remarks>
  public int AnnNumTrees { get; init; } = 10;

  /// <summary>
  /// Maximum number of points in a leaf node for approximate nearest neighbors.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Smaller values = more accurate but deeper trees.
  /// Only used when UseApproximateNearestNeighbors is true.
  /// </para>
  /// <para>Recommended values: 5-20. Default: 10.</para>
  /// </remarks>
  public int AnnLeafSize { get; init; } = 10;

  /// <summary>
  /// Number of nodes to search in each tree for approximate nearest neighbors.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Higher values = more accurate but slower.
  /// If null (default), uses NumberOfNeighbors * AnnNumTrees.
  /// Only used when UseApproximateNearestNeighbors is true.
  /// </para>
  /// </remarks>
  public int? AnnSearchK { get; init; } = null;

  /// <summary>
  /// Validates the options and throws if any are invalid.
  /// </summary>
  public void Validate()
  {
    if (NumberOfNeighbors < 2)
    {
      throw new ArgumentException(
        "NumberOfNeighbors must be at least 2",
        nameof(NumberOfNeighbors)
      );
    }

    if (NumberOfComponents < 1)
    {
      throw new ArgumentException(
        "NumberOfComponents must be at least 1",
        nameof(NumberOfComponents)
      );
    }

    if (MinDist < 0 || MinDist > Spread)
    {
      throw new ArgumentException("MinDist must be between 0 and Spread", nameof(MinDist));
    }

    if (Spread <= 0)
    {
      throw new ArgumentException("Spread must be positive", nameof(Spread));
    }

    if (NumberOfEpochs.HasValue && NumberOfEpochs.Value < 0)
    {
      throw new ArgumentException("NumberOfEpochs must be non-negative", nameof(NumberOfEpochs));
    }

    if (LearningRate <= 0)
    {
      throw new ArgumentException("LearningRate must be positive", nameof(LearningRate));
    }

    if (LocalConnectivity < 0)
    {
      throw new ArgumentException(
        "LocalConnectivity must be non-negative",
        nameof(LocalConnectivity)
      );
    }

    if (RepulsionStrength < 0)
    {
      throw new ArgumentException(
        "RepulsionStrength must be non-negative",
        nameof(RepulsionStrength)
      );
    }

    if (NegativeSampleRate < 0)
    {
      throw new ArgumentException(
        "NegativeSampleRate must be non-negative",
        nameof(NegativeSampleRate)
      );
    }

    if (SetOpMixRatio < 0 || SetOpMixRatio > 1)
    {
      throw new ArgumentException("SetOpMixRatio must be between 0 and 1", nameof(SetOpMixRatio));
    }

    if (Verbosity < 0 || Verbosity > 2)
    {
      throw new ArgumentException(
        "Verbosity must be 0 (silent), 1 (minimal), or 2 (detailed)",
        nameof(Verbosity)
      );
    }

    if (AnnNumTrees < 1)
    {
      throw new ArgumentException("AnnNumTrees must be at least 1", nameof(AnnNumTrees));
    }

    if (AnnLeafSize < 1)
    {
      throw new ArgumentException("AnnLeafSize must be at least 1", nameof(AnnLeafSize));
    }

    if (AnnSearchK.HasValue && AnnSearchK.Value < 1)
    {
      throw new ArgumentException("AnnSearchK must be at least 1", nameof(AnnSearchK));
    }

    var supportedMetrics = new[] { "euclidean", "cosine", "correlation", "manhattan" };
    if (!supportedMetrics.Contains(Metric.ToLowerInvariant()))
    {
      throw new ArgumentException(
        $"Metric must be one of: {string.Join(", ", supportedMetrics)}",
        nameof(Metric)
      );
    }
  }
}
