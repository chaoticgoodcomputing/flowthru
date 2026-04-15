namespace Flowthru.Misc.ML.UMAP.Core.Markers;

/// <summary>
/// Base interface for distance metrics used in UMAP.
/// Provides the fundamental distance computation between points in high-dimensional space.
/// </summary>
/// <remarks>
/// <para>
/// Metrics define how distances are measured in the input space during k-NN search
/// and graph construction. Different metrics capture different notions of similarity.
/// </para>
/// <para>
/// Common implementations: Euclidean (L2), Manhattan (L1), Cosine (angular).
/// </para>
/// </remarks>
public interface IMetric
{
  /// <summary>
  /// Human-readable name of the metric (e.g., "euclidean", "cosine").
  /// Used for logging and serialization.
  /// </summary>
  string Name { get; }

  /// <summary>
  /// Maximum meaningful distance for bounded metrics, or null for unbounded metrics.
  /// Used to handle disconnected components in the k-NN graph.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Examples:
  /// - Euclidean: null (unbounded)
  /// - Cosine: 2.0 (ranges from 0 to 2)
  /// - Jaccard: 1.0 (ranges from 0 to 1)
  /// </para>
  /// <para>
  /// When set, distances at or beyond this value indicate maximally dissimilar points
  /// that should be treated as disconnected in the manifold approximation.
  /// </para>
  /// </remarks>
  float? DisconnectionDistance { get; }

  /// <summary>
  /// Whether this metric benefits from angular (cosine-based) random projection forests.
  /// Angular metrics (cosine, correlation) use different RP tree splits than Euclidean metrics.
  /// </summary>
  bool SupportsAngularProjection { get; }

  /// <summary>
  /// Compute the distance between two points.
  /// </summary>
  /// <param name="x">First point</param>
  /// <param name="y">Second point</param>
  /// <returns>Distance value (non-negative)</returns>
  /// <remarks>
  /// Must satisfy metric properties:
  /// - Non-negativity: Distance(x, y) ≥ 0
  /// - Identity: Distance(x, x) = 0
  /// - Symmetry: Distance(x, y) = Distance(y, x)
  /// - Triangle inequality: Distance(x, z) ≤ Distance(x, y) + Distance(y, z)
  /// </remarks>
  float Distance(ReadOnlySpan<float> x, ReadOnlySpan<float> y);
}

/// <summary>
/// Output space metric that provides distance gradients for layout optimization.
/// Required for embedding into non-Euclidean spaces (spherical, hyperbolic, toroidal, etc.).
/// </summary>
/// <remarks>
/// <para>
/// During layout optimization (SGD phase), UMAP needs both the distance and its gradient
/// to update point positions. Standard Euclidean SGD has a specialized, highly optimized
/// implementation. For other output spaces, the generic SGD implementation requires gradients.
/// </para>
/// <para>
/// Examples of non-Euclidean output spaces:
/// - Spherical (haversine distance): Embeddings constrained to sphere surface
/// - Hyperbolic (Poincaré/hyperboloid): For hierarchical data
/// - Toroidal (wrap-around): For periodic data
/// </para>
/// </remarks>
public interface IOutputMetric : IMetric
{
  /// <summary>
  /// Compute distance and its gradient with respect to the first argument.
  /// Used during stochastic gradient descent to optimize the embedding layout.
  /// </summary>
  /// <param name="x">First point (the point being optimized)</param>
  /// <param name="y">Second point (reference/anchor point)</param>
  /// <param name="distance">Output: distance between x and y</param>
  /// <param name="gradient">
  /// Output: gradient of distance with respect to x (∂distance/∂x).
  /// Must be pre-allocated by caller with length equal to dimensionality.
  /// </param>
  /// <remarks>
  /// <para>
  /// The gradient represents the direction and magnitude of steepest increase in distance
  /// when moving x. During SGD, we use this to either attract or repel points.
  /// </para>
  /// <para>
  /// For Euclidean distance d = ||x - y||:
  /// - ∇d/∂x = (x - y) / ||x - y||
  /// </para>
  /// </remarks>
  void DistanceWithGradient(
    ReadOnlySpan<float> x,
    ReadOnlySpan<float> y,
    out float distance,
    Span<float> gradient
  );
}
