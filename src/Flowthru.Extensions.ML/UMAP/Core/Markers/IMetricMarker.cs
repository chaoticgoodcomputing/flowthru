namespace Flowthru.Extensions.ML.UMAP.Core.Markers;

/// <summary>
/// Phantom type marker for distance metrics.
/// Used to enforce compile-time constraints on strategy compatibility based on metric properties.
/// </summary>
/// <remarks>
/// This is a marker interface with no members - it exists purely for compile-time type checking.
/// Different metrics have different mathematical properties that affect which algorithms can be used.
/// </remarks>
public interface IMetricMarker { }

/// <summary>
/// Marker for Euclidean (L2) distance metric.
/// Euclidean distance enables specialized optimizations in layout and optimization strategies.
/// </summary>
public interface IEuclideanMetric : IMetricMarker { }

/// <summary>
/// Marker for Cosine distance metric (angular distance).
/// Cosine distance measures the angle between vectors, ignoring magnitude.
/// </summary>
public interface ICosineMetric : IMetricMarker { }

/// <summary>
/// Marker for Manhattan (L1) distance metric.
/// Manhattan distance is the sum of absolute differences between coordinates.
/// </summary>
public interface IManhattanMetric : IMetricMarker { }

/// <summary>
/// Marker for correlation distance metric.
/// Correlation distance measures similarity in patterns rather than absolute values.
/// </summary>
public interface ICorrelationMetric : IMetricMarker { }

/// <summary>
/// Marker for generic/custom distance metrics.
/// Used for user-defined metrics or metrics without specialized optimizations.
/// </summary>
public interface IGenericMetric : IMetricMarker { }

/// <summary>
/// Marker for precomputed distance matrices.
/// Indicates that distances have been pre-calculated and provided directly.
/// </summary>
public interface IPrecomputedMetric : IMetricMarker { }

/// <summary>
/// Marker indicating the metric supports angular (cosine-based) random projection forests.
/// Angular metrics benefit from specialized nearest neighbor search structures.
/// </summary>
public interface IAngularMetric : IMetricMarker { }
