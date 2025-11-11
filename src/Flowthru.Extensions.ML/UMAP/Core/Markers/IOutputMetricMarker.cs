namespace Flowthru.Extensions.ML.UMAP.Core.Markers;

/// <summary>
/// Phantom type marker for output space distance metrics.
/// Used to constrain layout optimization strategies based on the target embedding space metric.
/// </summary>
/// <remarks>
/// Output metrics define how distances are measured in the low-dimensional embedding space.
/// Some optimization strategies are specialized for specific output metrics (e.g., Euclidean).
/// </remarks>
public interface IOutputMetricMarker { }

/// <summary>
/// Marker for Euclidean output space.
/// Enables the use of highly-optimized Euclidean SGD layout optimization.
/// </summary>
public interface IEuclideanOutput : IOutputMetricMarker { }

/// <summary>
/// Marker for generic output space metrics.
/// Requires the generic layout optimization strategy with explicit distance gradients.
/// </summary>
public interface IGenericOutput : IOutputMetricMarker { }
