namespace Flowthru.Extensions.MLPure.UMAP;

/// <summary>
/// Configuration options for UMAP dimensionality reduction.
/// </summary>
/// <remarks>
/// Pure implementation - direct port from Python UMAP by Leland McInnes.
/// This version prioritizes algorithmic correctness over performance.
/// <para>
/// Citation: McInnes, L, Healy, J, "UMAP: Uniform Manifold Approximation and Projection
/// for Dimension Reduction", ArXiv e-prints 1802.03426, 2018
/// https://arxiv.org/abs/1802.03426
/// </para>
/// </remarks>
public sealed class UmapOptions
{
  // Core parameters matching Python UMAP __init__
  public int NumberOfNeighbors { get; init; } = 15; // n_neighbors
  public int NumberOfComponents { get; init; } = 2; // n_components
  public string Metric { get; init; } = "euclidean"; // metric
  public float MinDist { get; init; } = 0.1f; // min_dist
  public float Spread { get; init; } = 1.0f; // spread
  public int? NumberOfEpochs { get; init; } = null; // n_epochs
  public float LearningRate { get; init; } = 1.0f; // learning_rate
  public float LocalConnectivity { get; init; } = 1.0f; // local_connectivity
  public float RepulsionStrength { get; init; } = 1.0f; // repulsion_strength
  public int NegativeSampleRate { get; init; } = 5; // negative_sample_rate
  public float SetOpMixRatio { get; init; } = 1.0f; // set_op_mix_ratio
  public int? RandomState { get; init; } = null; // random_state
  public int Verbosity { get; init; } = 1; // verbose

  // Parameters for curve fitting (a, b parameters)
  public float? A { get; init; } = null; // a
  public float? B { get; init; } = null; // b

  // Init method for embedding initialization
  public string Init { get; init; } = "spectral"; // init: "spectral", "random", "pca"

  /// <summary>
  /// Optional progress reporter for programmatic progress tracking.
  /// </summary>
  public IProgress<(string Stage, float Progress, string? Details)>? ProgressReporter { get; init; } = null;

  // Note: Pure implementation always uses exact k-NN (no approximation)
  // This matches the Python reference behavior without pynndescent optimizations

  /// <summary>
  /// Validates the options and throws if any are invalid.
  /// </summary>
  public void Validate()
  {
    if (NumberOfNeighbors < 2)
      throw new ArgumentException("NumberOfNeighbors must be at least 2", nameof(NumberOfNeighbors));
    
    if (NumberOfComponents < 1)
      throw new ArgumentException("NumberOfComponents must be at least 1", nameof(NumberOfComponents));
    
    if (MinDist < 0 || MinDist > Spread)
      throw new ArgumentException("MinDist must be between 0 and Spread", nameof(MinDist));
    
    if (Spread <= 0)
      throw new ArgumentException("Spread must be positive", nameof(Spread));
    
    if (NumberOfEpochs.HasValue && NumberOfEpochs.Value < 0)
      throw new ArgumentException("NumberOfEpochs must be non-negative", nameof(NumberOfEpochs));
    
    if (LearningRate <= 0)
      throw new ArgumentException("LearningRate must be positive", nameof(LearningRate));
    
    if (LocalConnectivity < 0)
      throw new ArgumentException("LocalConnectivity must be non-negative", nameof(LocalConnectivity));
    
    if (RepulsionStrength < 0)
      throw new ArgumentException("RepulsionStrength must be non-negative", nameof(RepulsionStrength));
    
    if (NegativeSampleRate < 0)
      throw new ArgumentException("NegativeSampleRate must be non-negative", nameof(NegativeSampleRate));
    
    if (SetOpMixRatio < 0 || SetOpMixRatio > 1)
      throw new ArgumentException("SetOpMixRatio must be between 0 and 1", nameof(SetOpMixRatio));
    
    if (Verbosity < 0 || Verbosity > 2)
      throw new ArgumentException("Verbosity must be 0, 1, or 2", nameof(Verbosity));

    var supportedMetrics = new[] { "euclidean", "cosine", "correlation", "manhattan" };
    if (!supportedMetrics.Contains(Metric.ToLowerInvariant()))
      throw new ArgumentException($"Metric must be one of: {string.Join(", ", supportedMetrics)}", nameof(Metric));
    
    var supportedInits = new[] { "spectral", "random", "pca" };
    if (!supportedInits.Contains(Init.ToLowerInvariant()))
      throw new ArgumentException($"Init must be one of: {string.Join(", ", supportedInits)}", nameof(Init));
  }
}
