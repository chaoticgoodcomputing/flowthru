namespace Flowthru.Extensions.ML.UMAP.Core;

/// <summary>
/// Core parameters for UMAP algorithm configuration.
/// These parameters control the mathematical behavior of the algorithm across all strategies.
/// </summary>
/// <remarks>
/// This record contains the fundamental UMAP hyperparameters that affect the global
/// structure of the embedding. Strategy-specific parameters are configured on individual
/// strategy instances.
/// </remarks>
public sealed record UmapParameters
{
  /// <summary>
  /// Number of nearest neighbors to consider for manifold approximation.
  /// Larger values result in more global structure, smaller values preserve local details.
  /// </summary>
  /// <remarks>
  /// Default: 15. Range: [2, ∞). Typical values: 5-50.
  /// - Small values (5-10): Emphasize local structure, fine details
  /// - Medium values (15-30): Balanced local and global structure
  /// - Large values (50+): Emphasize global structure, may lose fine details
  /// </remarks>
  public int NumberOfNeighbors { get; init; } = 15;

  /// <summary>
  /// Dimensionality of the target embedding space.
  /// </summary>
  /// <remarks>
  /// Default: 2 (for visualization). Range: [1, ∞). Typical values: 2-100.
  /// - 2D: Visualization and exploratory analysis
  /// - 3D: Interactive 3D visualization
  /// - Higher: Feature extraction, downstream ML tasks
  /// </remarks>
  public int NumberOfComponents { get; init; } = 2;

  /// <summary>
  /// Number of optimization epochs (training iterations).
  /// If null, automatically determined based on dataset size.
  /// </summary>
  /// <remarks>
  /// Default: null (auto). If set, range: [0, ∞). Auto values: 500 (small data), 200 (large data).
  /// More epochs = better convergence but longer training time.
  /// </remarks>
  public int? NumberOfEpochs { get; init; } = null;

  /// <summary>
  /// Effective minimum distance between embedded points.
  /// Controls how tightly points are packed in clusters.
  /// </summary>
  /// <remarks>
  /// Default: 0.1. Range: [0, spread]. Typical values: 0.0-0.5.
  /// - 0.0: Dense, tightly packed clusters
  /// - 0.1: Balanced (default)
  /// - 0.3-0.5: More spread out, emphasizes separation
  /// </remarks>
  public float MinDist { get; init; } = 0.1f;

  /// <summary>
  /// Effective scale of embedded points.
  /// Works with <see cref="MinDist"/> to control clustering vs. dispersion.
  /// </summary>
  /// <remarks>
  /// Default: 1.0. Range: (0, ∞). Typical values: 0.5-2.0.
  /// Controls the overall scale at which embedded points spread out.
  /// </remarks>
  public float Spread { get; init; } = 1.0f;

  /// <summary>
  /// Local connectivity required at the manifold level.
  /// Number of nearest neighbors assumed to be connected locally.
  /// </summary>
  /// <remarks>
  /// Default: 1.0. Range: [1, numberOfNeighbors]. Typical values: 1.0-5.0.
  /// Higher values increase local connectivity, making the manifold more connected.
  /// Should not exceed the local intrinsic dimension of the manifold.
  /// </remarks>
  public float LocalConnectivity { get; init; } = 1.0f;

  /// <summary>
  /// Initial learning rate for stochastic gradient descent.
  /// </summary>
  /// <remarks>
  /// Default: 1.0. Range: (0, ∞). Typical values: 0.5-2.0.
  /// Learning rate decays linearly to 0 over training epochs.
  /// </remarks>
  public float LearningRate { get; init; } = 1.0f;

  /// <summary>
  /// Weight applied to negative samples in optimization.
  /// Controls repulsive force between non-neighboring points.
  /// </summary>
  /// <remarks>
  /// Default: 1.0. Range: [0, ∞). Typical values: 0.5-2.0.
  /// - Lower values: Less repulsion, denser embedding
  /// - Higher values: More repulsion, more spread out
  /// </remarks>
  public float RepulsionStrength { get; init; } = 1.0f;

  /// <summary>
  /// Number of negative samples per positive sample during optimization.
  /// </summary>
  /// <remarks>
  /// Default: 5. Range: [1, ∞). Typical values: 5-20.
  /// Higher values = stronger repulsive force but slower training.
  /// </remarks>
  public int NegativeSampleRate { get; init; } = 5;

  /// <summary>
  /// Interpolation between fuzzy union and intersection for combining local simplicial sets.
  /// </summary>
  /// <remarks>
  /// Default: 1.0 (pure fuzzy union). Range: [0, 1].
  /// - 1.0: Pure fuzzy union (standard UMAP)
  /// - 0.0: Pure fuzzy intersection (more conservative connectivity)
  /// - 0.5: Balanced between union and intersection
  /// </remarks>
  public float SetOpMixRatio { get; init; } = 1.0f;

  /// <summary>
  /// Curve fitting parameter 'a' for the low-dimensional fuzzy simplicial set.
  /// If null, automatically computed from <see cref="Spread"/> and <see cref="MinDist"/>.
  /// </summary>
  /// <remarks>
  /// Default: null (auto-compute). Manual setting is for advanced use only.
  /// This parameter controls the attractive force curve in the embedding space.
  /// </remarks>
  public float? A { get; init; } = null;

  /// <summary>
  /// Curve fitting parameter 'b' for the low-dimensional fuzzy simplicial set.
  /// If null, automatically computed from <see cref="Spread"/> and <see cref="MinDist"/>.
  /// </summary>
  /// <remarks>
  /// Default: null (auto-compute). Manual setting is for advanced use only.
  /// This parameter controls the attractive force curve in the embedding space.
  /// </remarks>
  public float? B { get; init; } = null;

  /// <summary>
  /// Random seed for reproducible results.
  /// If null, uses non-deterministic randomization.
  /// </summary>
  public int? RandomSeed { get; init; } = null;

  /// <summary>
  /// Verbosity level for progress reporting.
  /// </summary>
  /// <remarks>
  /// 0 = Silent, 1 = Basic progress, 2 = Detailed progress
  /// </remarks>
  public int Verbosity { get; init; } = 0;

  /// <summary>
  /// Optional progress reporter for programmatic progress tracking.
  /// </summary>
  public IProgress<UmapProgress>? ProgressReporter { get; init; } = null;

  /// <summary>
  /// Validates the parameters and throws if any are invalid.
  /// </summary>
  /// <exception cref="ArgumentException">Thrown when parameters are out of valid ranges.</exception>
  public void Validate()
  {
    if (NumberOfNeighbors < 2)
      throw new ArgumentException(
        "NumberOfNeighbors must be at least 2",
        nameof(NumberOfNeighbors)
      );

    if (NumberOfComponents < 1)
      throw new ArgumentException(
        "NumberOfComponents must be at least 1",
        nameof(NumberOfComponents)
      );

    if (MinDist < 0 || MinDist > Spread)
      throw new ArgumentException(
        $"MinDist ({MinDist}) must be between 0 and Spread ({Spread})",
        nameof(MinDist)
      );

    if (Spread <= 0)
      throw new ArgumentException("Spread must be positive", nameof(Spread));

    if (NumberOfEpochs.HasValue && NumberOfEpochs.Value < 0)
      throw new ArgumentException("NumberOfEpochs must be non-negative", nameof(NumberOfEpochs));

    if (LearningRate <= 0)
      throw new ArgumentException("LearningRate must be positive", nameof(LearningRate));

    if (LocalConnectivity < 1)
      throw new ArgumentException(
        "LocalConnectivity must be at least 1",
        nameof(LocalConnectivity)
      );

    if (RepulsionStrength < 0)
      throw new ArgumentException(
        "RepulsionStrength must be non-negative",
        nameof(RepulsionStrength)
      );

    if (NegativeSampleRate < 1)
      throw new ArgumentException(
        "NegativeSampleRate must be at least 1",
        nameof(NegativeSampleRate)
      );

    if (SetOpMixRatio < 0 || SetOpMixRatio > 1)
      throw new ArgumentException(
        $"SetOpMixRatio ({SetOpMixRatio}) must be between 0 and 1",
        nameof(SetOpMixRatio)
      );

    if (Verbosity < 0 || Verbosity > 2)
      throw new ArgumentException("Verbosity must be 0, 1, or 2", nameof(Verbosity));
  }

  /// <summary>
  /// Gets the curve parameter 'a', computing it from spread and min_dist if not explicitly set.
  /// </summary>
  public float GetA()
  {
    if (A.HasValue)
    {
      return A.Value;
    }

    var (a, _) = CurveFitting.FindABParams(Spread, MinDist);
    return a;
  }

  /// <summary>
  /// Gets the curve parameter 'b', computing it from spread and min_dist if not explicitly set.
  /// </summary>
  public float GetB()
  {
    if (B.HasValue)
    {
      return B.Value;
    }

    var (_, b) = CurveFitting.FindABParams(Spread, MinDist);
    return b;
  }
}

/// <summary>
/// Progress information reported during UMAP execution.
/// </summary>
public sealed record UmapProgress
{
  /// <summary>
  /// Name of the current pipeline stage (e.g., "K-NN", "Graph Construction", "Optimization").
  /// </summary>
  public required string Stage { get; init; }

  /// <summary>
  /// Progress within the current stage as a fraction [0.0, 1.0].
  /// </summary>
  public required float Progress { get; init; }

  /// <summary>
  /// Optional detailed status message.
  /// </summary>
  public string? Details { get; init; }

  /// <summary>
  /// Current epoch number, if applicable (during optimization).
  /// </summary>
  public int? CurrentEpoch { get; init; }

  /// <summary>
  /// Total number of epochs, if applicable (during optimization).
  /// </summary>
  public int? TotalEpochs { get; init; }
}
