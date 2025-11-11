namespace Flowthru.Extensions.ML.UMAP.Core.Markers;

/// <summary>
/// Phantom type marker for strategy compatibility constraints.
/// Used to track which combinations of strategies are mathematically valid.
/// </summary>
/// <remarks>
/// Compatibility markers prevent invalid combinations like using density-preserving
/// optimization with non-Euclidean output metrics, or supervised learning with
/// precomputed distance matrices.
/// </remarks>
public interface ICompatibilityMarker { }

/// <summary>
/// Marker indicating support for supervised dimensionality reduction.
/// Supervised strategies can incorporate target labels during graph construction.
/// </summary>
public interface ISupervisedCompatible : ICompatibilityMarker { }

/// <summary>
/// Marker indicating support for density preservation (densMAP).
/// Density-preserving strategies attempt to maintain local density information from the original space.
/// </summary>
public interface IDensityPreservingCompatible : ICompatibilityMarker { }

/// <summary>
/// Marker indicating support for sparse input data.
/// Sparse-compatible strategies can efficiently handle sparse matrices without densification.
/// </summary>
public interface ISparseCompatible : ICompatibilityMarker { }

/// <summary>
/// Marker indicating support for out-of-sample transformation.
/// Transform-compatible strategies can embed new data points into an existing embedding.
/// </summary>
public interface ITransformCompatible : ICompatibilityMarker { }

/// <summary>
/// Marker indicating the strategy produces a search index for future queries.
/// Indexable strategies build data structures that enable efficient nearest neighbor queries.
/// </summary>
public interface IIndexable : ICompatibilityMarker { }
