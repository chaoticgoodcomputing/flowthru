namespace Flowthru.Misc.ML.UMAP.Core;

/// <summary>
/// Runtime performance report for UMAP execution.
/// </summary>
/// <remarks>
/// Generic schema capturing timing metrics for each UMAP algorithmic phase.
/// Does not include Flowthru serialization markers to keep it framework-agnostic.
/// </remarks>
public sealed record UmapRuntimeReport
{
    /// <summary>
    /// Timing measurements for each UMAP phase.
    /// Key is the stage name, value is elapsed time in milliseconds.
    /// </summary>
    /// <remarks>
    /// Expected stages:
    /// - "NeighborSearch" - Phase 1: k-NN graph construction
    /// - "LocalMetric" - Phase 2: Local metric parameter computation
    /// - "GraphConstruction" - Phase 3: Fuzzy simplicial set construction
    /// - "GraphRefinement" - Phase 4: Graph refinement (optional)
    /// - "LayoutInit" - Phase 5: Low-dimensional layout initialization
    /// - "SamplingSchedule" - Phase 6: Edge sampling schedule computation
    /// - "LayoutOptimization" - Phase 7: Stochastic gradient descent optimization
    /// </remarks>
    public Dictionary<string, int> Timings { get; init; } = new();

    /// <summary>
    /// Total elapsed time for the complete FitTransform operation, in milliseconds.
    /// </summary>
    /// <remarks>
    /// Sum of all individual phase timings. Useful for quick performance assessment.
    /// </remarks>
    public int TotalTimeMs { get; init; }
}
