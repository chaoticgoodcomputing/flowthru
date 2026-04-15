using Flowthru.Misc.ML.UMAP.Core.Markers;

namespace Flowthru.Misc.ML.UMAP.Core.Utils;

/// <summary>
/// Euclidean (L2) distance metric with gradient support.
/// </summary>
/// <remarks>
/// Euclidean distance is the straight-line distance in n-dimensional space:
/// d(x, y) = sqrt(sum((x[i] - y[i])^2))
///
/// This is the most common metric and has specialized optimizations in layout optimization.
/// </remarks>
public sealed class EuclideanMetric : IOutputMetric
{
  /// <summary>
  /// Singleton instance of Euclidean metric.
  /// Use this to avoid allocations.
  /// </summary>
  public static EuclideanMetric Instance { get; } = new();

  private EuclideanMetric() { }

  /// <summary>
  /// Name of the metric, used for logging and strategy selection. This is a simple identifier and does not affect behavior.
  /// It should be unique among built-in metrics to allow strategies to recognize it, but can be arbitrary for custom metrics.
  /// </summary>
  public string Name => "euclidean";

  /// <summary>
  /// UMAP's optimization can be unstable for very large distances. Setting a disconnection distance allows the algorithm to treat points beyond this distance as effectively disconnected, which can improve convergence and embedding quality for certain datasets.
  /// </summary>
  public float? DisconnectionDistance => null; // Unbounded

  /// <summary>
  /// Indicates whether this metric benefits from angular random projection forests. Euclidean distance does not benefit from angular RPs, so this returns false. Metrics that do benefit (e.g., cosine) should return true to enable the use of angular RP forests for neighbor search, which can improve performance and embedding quality.
  /// </summary>
  public bool SupportsAngularProjection => false;

  /// <summary>
  /// Compute Euclidean distance: sqrt(sum of squared differences).
  /// </summary>
  public float Distance(ReadOnlySpan<float> x, ReadOnlySpan<float> y)
  {
    float sumSq = 0f;
    for (int i = 0; i < x.Length; i++)
    {
      float diff = x[i] - y[i];
      sumSq += diff * diff;
    }
    return MathF.Sqrt(sumSq);
  }

  /// <summary>
  /// Compute Euclidean distance and its gradient: ∇d/∂x = (x - y) / ||x - y||
  /// </summary>
  public void DistanceWithGradient(
    ReadOnlySpan<float> x,
    ReadOnlySpan<float> y,
    out float distance,
    Span<float> gradient
  )
  {
    float sumSq = 0f;
    for (int i = 0; i < x.Length; i++)
    {
      float diff = x[i] - y[i];
      gradient[i] = diff;
      sumSq += diff * diff;
    }

    distance = MathF.Sqrt(sumSq);

    // Normalize gradient: (x - y) / ||x - y||
    float invDist = 1f / (distance + 1e-8f); // Avoid division by zero
    for (int i = 0; i < gradient.Length; i++)
    {
      gradient[i] *= invDist;
    }
  }
}

/// <summary>
/// Manhattan (L1) distance metric.
/// </summary>
/// <remarks>
/// Manhattan distance is the sum of absolute differences:
/// d(x, y) = sum(|x[i] - y[i]|)
///
/// Also known as taxicab or city block distance.
/// </remarks>
public sealed class ManhattanMetric : IMetric
{
  /// <summary>
  /// Singleton instance of Manhattan metric.
  /// Use this to avoid allocations.
  /// </summary>
  public static ManhattanMetric Instance { get; } = new();

  private ManhattanMetric() { }

  /// <summary>
  /// Name of the metric, used for logging and strategy selection. This is a simple identifier and does not affect behavior.
  /// </summary>
  public string Name => "manhattan";

  /// <summary>
  /// UMAP's optimization can be unstable for very large distances. Setting a disconnection distance allows the algorithm to treat points beyond this distance as effectively disconnected, which can improve convergence and embedding quality for certain datasets.
  /// </summary>
  public float? DisconnectionDistance => null; // Unbounded

  /// <summary>
  /// Indicates whether this metric benefits from angular random projection forests. Manhattan distance does not benefit from angular RPs, so this returns false. Metrics that do benefit (e.g., cosine) should return true to enable the use of angular RP forests for neighbor search, which can improve performance and embedding quality.
  /// </summary>
  public bool SupportsAngularProjection => false;

  /// <summary>
  /// Compute Manhattan distance: sum of absolute differences.
  /// </summary>
  public float Distance(ReadOnlySpan<float> x, ReadOnlySpan<float> y)
  {
    float sum = 0f;
    for (int i = 0; i < x.Length; i++)
    {
      sum += MathF.Abs(x[i] - y[i]);
    }
    return sum;
  }
}

/// <summary>
/// Cosine distance metric (angular distance).
/// </summary>
/// <remarks>
/// Cosine distance measures the angle between vectors:
/// d(x, y) = 1 - (x·y) / (||x|| ||y||)
///
/// Range: [0, 2] where 0 = identical direction, 1 = orthogonal, 2 = opposite direction.
/// Ignores magnitude, only considers direction.
/// </remarks>
public sealed class CosineMetric : IMetric
{
  /// <summary>
  /// Singleton instance of Cosine metric.
  /// Use this to avoid allocations.
  /// </summary>
  public static CosineMetric Instance { get; } = new();

  private CosineMetric() { }

  /// <summary>
  /// Name of the metric, used for logging and strategy selection. This is a simple identifier and does not affect behavior.
  /// </summary>
  public string Name => "cosine";

  /// <summary>
  /// UMAP's optimization can be unstable for very large distances. Setting a disconnection distance allows the algorithm to treat points beyond this distance as effectively disconnected, which can improve convergence and embedding quality for certain datasets. For cosine distance, a natural choice is 2.0, which corresponds to opposite direction.
  /// </summary>
  public float? DisconnectionDistance => 2.0f; // Maximum cosine distance

  /// <summary>
  /// Indicates whether this metric benefits from angular random projection forests. Cosine distance benefits from angular RPs, so this returns true. This allows the use of angular RP forests for neighbor search, which can improve performance and embedding quality when using cosine distance.
  /// </summary>
  public bool SupportsAngularProjection => true;

  /// <summary>
  /// Compute cosine distance: 1 - (dot product / product of norms).
  /// </summary>
  public float Distance(ReadOnlySpan<float> x, ReadOnlySpan<float> y)
  {
    float dotProduct = 0f;
    float normX = 0f;
    float normY = 0f;

    for (int i = 0; i < x.Length; i++)
    {
      dotProduct += x[i] * y[i];
      normX += x[i] * x[i];
      normY += y[i] * y[i];
    }

    float denominator = MathF.Sqrt(normX * normY);
    if (denominator < 1e-8f)
    {
      return 0f; // Treat zero vectors as identical
    }

    float cosineSimilarity = dotProduct / denominator;
    // Clamp to [-1, 1] to handle numerical errors
    cosineSimilarity = Math.Clamp(cosineSimilarity, -1f, 1f);
    return 1f - cosineSimilarity;
  }
}

/// <summary>
/// Custom metric wrapper for user-defined distance functions.
/// </summary>
/// <remarks>
/// Allows users to provide arbitrary distance functions while maintaining
/// the IMetric interface contract. Useful for experimentation and custom metrics.
/// </remarks>
public sealed class CustomMetric : IMetric
{
  private readonly Func<ReadOnlySpan<float>, ReadOnlySpan<float>, float> _distanceFunc;

  /// <summary>
  /// Creates a custom metric from a distance function.
  /// </summary>
  /// <param name="name">Human-readable name for the metric</param>
  /// <param name="distanceFunc">Function computing distance between two points</param>
  /// <param name="disconnectionDistance">Optional maximum distance for bounded metrics</param>
  /// <param name="supportsAngularProjection">Whether angular RP forests benefit this metric</param>
  public CustomMetric(
    string name,
    Func<ReadOnlySpan<float>, ReadOnlySpan<float>, float> distanceFunc,
    float? disconnectionDistance = null,
    bool supportsAngularProjection = false
  )
  {
    Name = name ?? throw new ArgumentNullException(nameof(name));
    _distanceFunc = distanceFunc ?? throw new ArgumentNullException(nameof(distanceFunc));
    DisconnectionDistance = disconnectionDistance;
    SupportsAngularProjection = supportsAngularProjection;
  }

  /// <summary>
  /// Name of the metric, used for logging and strategy selection. This is a simple identifier and does not affect behavior, but should be unique among built-in metrics to allow strategies to recognize it. For custom metrics, this can be arbitrary but should ideally describe the metric's behavior for clarity in logs and strategy selection.
  /// </summary>
  public string Name { get; }

  /// <summary>
  /// UMAP's optimization can be unstable for very large distances. Setting a disconnection distance allows the algorithm to treat points beyond this distance as effectively disconnected, which can improve convergence and embedding quality for certain datasets. If null, the metric is unbounded and all distances are treated as valid.
  /// </summary>
  public float? DisconnectionDistance { get; }

  /// <summary>
  /// Indicates whether this metric benefits from angular random projection forests. This should be set to true for metrics where angular RPs can improve neighbor search performance and embedding quality (e.g., cosine), and false for metrics where they do not provide a benefit (e.g., Euclidean). This allows the UMAP implementation to optimize neighbor search appropriately based on the metric's characteristics.
  /// </summary>
  public bool SupportsAngularProjection { get; }

  /// <summary>
  /// Compute distance using the provided distance function.
  /// </summary>
  /// <param name="x"></param>
  /// <param name="y"></param>
  /// <returns></returns>
  public float Distance(ReadOnlySpan<float> x, ReadOnlySpan<float> y) => _distanceFunc(x, y);
}
