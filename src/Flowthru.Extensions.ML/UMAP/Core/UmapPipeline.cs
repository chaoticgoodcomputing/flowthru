using Flowthru.Extensions.ML.UMAP.Core.Markers;
using Flowthru.Extensions.ML.UMAP.Strategies.GraphRefinement;
using Flowthru.Extensions.ML.UMAP.Strategies.GraphRefinement.Implementations;
using Flowthru.Extensions.ML.UMAP.Strategies.LayoutInit;
using Flowthru.Extensions.ML.UMAP.Strategies.LayoutInit.Implementations;
using Flowthru.Extensions.ML.UMAP.Strategies.LayoutOptimization;
using Flowthru.Extensions.ML.UMAP.Strategies.LayoutOptimization.Implementations;
using Flowthru.Extensions.ML.UMAP.Strategies.LocalMetric;
using Flowthru.Extensions.ML.UMAP.Strategies.LocalMetric.Implementations;
using Flowthru.Extensions.ML.UMAP.Strategies.MembershipStrength;
using Flowthru.Extensions.ML.UMAP.Strategies.MembershipStrength.Implementations;
using Flowthru.Extensions.ML.UMAP.Strategies.NeighborSearch;
using Flowthru.Extensions.ML.UMAP.Strategies.NeighborSearch.Implementations;
using Flowthru.Extensions.ML.UMAP.Strategies.SamplingSchedule;
using Flowthru.Extensions.ML.UMAP.Strategies.SamplingSchedule.Implementations;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Single;

namespace Flowthru.Extensions.ML.UMAP.Core;

/// <summary>
/// Fluent builder for UMAP pipelines with automatic strategy selection.
/// </summary>
/// <remarks>
/// <para>
/// This provides a low skill floor (simple defaults) with a high skill ceiling (full customization).
/// Strategies are resolved lazily at FitTransform() time based on data characteristics.
/// </para>
/// <para>
/// <b>Usage patterns:</b>
/// </para>
/// <code>
/// // Beginner: Full auto-configuration
/// var result = UmapPipeline.Create().FitTransform(data);
///
/// // Intermediate: Custom metric
/// var result = UmapPipeline.Create&lt;ICosineMetric&gt;()
///   .FitTransform(data, cosineDistance);
///
/// // Advanced: Custom strategies for testing/benchmarking
/// var result = UmapPipeline.Create()
///   .WithNeighborSearch(new NNDescentSearch&lt;IEuclideanMetric&gt; { MaxIterations = 50 })
///   .FitTransform(data);
/// </code>
/// </remarks>
public static class UmapPipeline
{
  /// <summary>
  /// Creates a new UMAP pipeline with default Euclidean metric.
  /// Strategies will be auto-selected based on data shape at FitTransform() time.
  /// </summary>
  /// <param name="parameters">
  /// UMAP hyperparameters (n_neighbors, min_dist, etc.).
  /// If null, uses defaults appropriate for the data.
  /// </param>
  public static UmapPipelineBuilder<IEuclideanMetric> Create(UmapParameters? parameters = null)
  {
    return new UmapPipelineBuilder<IEuclideanMetric>(parameters ?? new UmapParameters());
  }

  /// <summary>
  /// Creates a new UMAP pipeline with a specific metric type.
  /// Useful when you need non-Euclidean metrics (cosine, Manhattan, etc.).
  /// </summary>
  public static UmapPipelineBuilder<TMetric> Create<TMetric>(UmapParameters? parameters = null)
    where TMetric : IMetricMarker
  {
    return new UmapPipelineBuilder<TMetric>(parameters ?? new UmapParameters());
  }

  /// <summary>
  /// Euclidean distance metric (L2 norm).
  /// </summary>
  public static float EuclideanDistance(ReadOnlySpan<float> x, ReadOnlySpan<float> y)
  {
    float sum = 0f;
    for (int i = 0; i < x.Length; i++)
    {
      float diff = x[i] - y[i];
      sum += diff * diff;
    }
    return MathF.Sqrt(sum);
  }
}

/// <summary>
/// Internal executor that runs the UMAP algorithm with resolved strategies.
/// </summary>
internal sealed class UmapPipelineExecutor<TMetric>
  where TMetric : IMetricMarker
{
  private readonly UmapParameters _parameters;
  private readonly INeighborSearchStrategy<TMetric> _neighborSearch;
  private readonly ILocalMetricStrategy _localMetric;
  private readonly IMembershipStrengthStrategy _membershipStrength;
  private readonly IGraphRefinementStrategy? _graphRefinement;
  private readonly ILayoutInitStrategy<TMetric>? _layoutInit;
  private readonly ISamplingScheduleStrategy? _samplingSchedule;
  private readonly ILayoutOptimizationStrategy? _layoutOptimization;

  internal UmapPipelineExecutor(
    UmapParameters parameters,
    INeighborSearchStrategy<TMetric> neighborSearch,
    ILocalMetricStrategy localMetric,
    IMembershipStrengthStrategy membershipStrength,
    IGraphRefinementStrategy? graphRefinement = null,
    ILayoutInitStrategy<TMetric>? layoutInit = null,
    ISamplingScheduleStrategy? samplingSchedule = null,
    ILayoutOptimizationStrategy? layoutOptimization = null
  )
  {
    _parameters = parameters;
    _neighborSearch = neighborSearch;
    _localMetric = localMetric;
    _membershipStrength = membershipStrength;
    _graphRefinement = graphRefinement;
    _layoutInit = layoutInit;
    _samplingSchedule = samplingSchedule;
    _layoutOptimization = layoutOptimization;
  }

  /// <summary>
  /// Computes the fuzzy simplicial set graph from input data.
  /// This executes phases 1-3 of the UMAP algorithm.
  /// </summary>
  /// <param name="data">
  /// Input data matrix where rows are samples and columns are features.
  /// Shape: (n_samples, n_features)
  /// </param>
  /// <param name="metric">
  /// Distance metric function for computing pairwise distances.
  /// </param>
  /// <returns>
  /// A result containing the fuzzy simplicial set graph and intermediate data structures.
  /// </returns>
  public UmapGraphResult ComputeGraph(
    Matrix<float> data,
    Func<ReadOnlySpan<float>, ReadOnlySpan<float>, float> metric
  )
  {
    var random = _parameters.RandomSeed.HasValue
      ? new Random(_parameters.RandomSeed.Value)
      : new Random();

    // Phase 1: Nearest Neighbor Search
    ReportProgress("Neighbor Search", 0.0f, "Finding k-nearest neighbors");

    var neighborResult = _neighborSearch.Search(
      data,
      _parameters.NumberOfNeighbors,
      metric,
      random
    );

    ReportProgress("Neighbor Search", 1.0f, $"Found neighbors for {data.RowCount} points");

    // Phase 2: Local Metric Computation
    ReportProgress("Local Metric", 0.0f, "Computing local metric parameters");

    var localMetricResult = _localMetric.ComputeLocalMetrics(
      neighborResult.Distances,
      _parameters.NumberOfNeighbors,
      _parameters.LocalConnectivity
    );

    ReportProgress("Local Metric", 1.0f, "Local metrics computed");

    // Phase 3: Membership Strength Computation
    ReportProgress("Graph Construction", 0.0f, "Building fuzzy simplicial set");

    var graph = _membershipStrength.ComputeMembershipStrengths(
      neighborResult.Indices,
      neighborResult.Distances,
      localMetricResult.Sigmas,
      localMetricResult.Rhos,
      _parameters.SetOpMixRatio
    );

    ReportProgress(
      "Graph Construction",
      1.0f,
      $"Graph constructed with {graph.NonZerosCount} edges"
    );

    // Phase 4: Optional Graph Refinement
    if (_graphRefinement != null)
    {
      ReportProgress("Graph Refinement", 0.0f, "Refining graph (pruning weak edges)");

      var nEpochs = _parameters.NumberOfEpochs ?? (graph.RowCount <= 10000 ? 500 : 200);

      var refinementResult = _graphRefinement.RefineGraph(graph, nEpochs);
      graph = refinementResult.RefinedGraph;

      ReportProgress(
        "Graph Refinement",
        1.0f,
        $"Refined graph; removed {refinementResult.EdgesRemoved} edges (threshold {refinementResult.MinEdgeWeight})"
      );
    }

    return new UmapGraphResult(
      Graph: graph,
      KnnIndices: neighborResult.Indices,
      KnnDistances: neighborResult.Distances,
      Sigmas: localMetricResult.Sigmas,
      Rhos: localMetricResult.Rhos,
      SearchIndex: neighborResult.SearchIndex
    );
  }

  /// <summary>
  /// Initializes low-dimensional layout using the configured layout initialization strategy.
  /// Falls back to a simple random initialization if none is configured.
  /// </summary>
  public LayoutInitResult InitializeLayout(Matrix<float>? data, SparseMatrix graph)
  {
    var random = _parameters.RandomSeed.HasValue
      ? new Random(_parameters.RandomSeed.Value)
      : new Random();

    var nComponents = _parameters.NumberOfComponents;

    if (_layoutInit != null)
    {
      return _layoutInit.InitializeLayout(data, graph, nComponents, random);
    }

    // Default fallback: random initialization using the pipeline's output metric type
    var fallback = new Strategies.LayoutInit.Implementations.RandomInit<TMetric>();
    return fallback.InitializeLayout(data, graph, nComponents, random);
  }

  private void ReportProgress(string stage, float progress, string? details = null)
  {
    if (_parameters.Verbosity >= 1 && progress >= 1.0f)
    {
      Console.WriteLine($"[{stage}] {details ?? "Complete"}");
    }

    _parameters.ProgressReporter?.Report(
      new UmapProgress
      {
        Stage = stage,
        Progress = progress,
        Details = details,
      }
    );
  }

  /// <summary>
  /// Executes the complete UMAP algorithm: graph construction, refinement,
  /// layout initialization, and optimization.
  /// </summary>
  /// <param name="data">Input data matrix where rows are samples and columns are features.</param>
  /// <param name="metric">Distance metric function for computing pairwise distances.</param>
  /// <returns>A result containing the final embedding and intermediate artifacts.</returns>
  /// <exception cref="InvalidOperationException">Thrown if required strategies are not configured.</exception>
  public UmapFitResult FitTransform(
    Matrix<float> data,
    Func<ReadOnlySpan<float>, ReadOnlySpan<float>, float> metric
  )
  {
    // Phase 1-4: Compute and refine graph
    var graphResult = ComputeGraph(data, metric);

    // Phase 5: Initialize layout
    if (_layoutInit == null)
    {
      throw new InvalidOperationException(
        "Layout initialization strategy is required for FitTransform. Call WithLayoutInit() on the builder."
      );
    }
    var layoutResult = InitializeLayout(data, graphResult.Graph);

    // Determine number of epochs using Python UMAP heuristic if not specified
    var nEpochs = _parameters.NumberOfEpochs ?? (data.RowCount <= 10000 ? 500 : 200);

    if (_parameters.Verbosity >= 1)
    {
      Console.WriteLine($"[UMAP] Using {nEpochs} epochs for {data.RowCount} samples");
    }

    // Phase 6: Compute sampling schedule
    if (_samplingSchedule == null)
    {
      throw new InvalidOperationException(
        "Sampling schedule strategy is required for FitTransform. Call WithSamplingSchedule() on the builder."
      );
    }

    var edges = ConvertGraphToEdges(graphResult.Graph);
    var edgeWeights = edges.Select(e => e.Weight).ToArray();

    var scheduleResult = _samplingSchedule.ComputeSchedule(edgeWeights, nEpochs);

    // Phase 7: Optimize layout
    if (_layoutOptimization == null)
    {
      throw new InvalidOperationException(
        "Layout optimization strategy is required for FitTransform. Call WithLayoutOptimization() on the builder."
      );
    }

    var optimizationParams = new OptimizationParameters
    {
      A = _parameters.GetA(),
      B = _parameters.GetB(),
      InitialAlpha = _parameters.LearningRate,
      Gamma = _parameters.RepulsionStrength,
      NegativeSampleRate = _parameters.NegativeSampleRate,
      Verbosity = _parameters.Verbosity,
      ProgressReporter = _parameters.ProgressReporter,
    };

    if (_parameters.Verbosity >= 1)
    {
      Console.WriteLine(
        $"[UMAP] Optimization parameters: A={optimizationParams.A:F4}, B={optimizationParams.B:F4}, "
          + $"LearningRate={optimizationParams.InitialAlpha:F3}, Gamma={optimizationParams.Gamma:F2}, "
          + $"NegativeSampleRate={optimizationParams.NegativeSampleRate}"
      );
      Console.WriteLine(
        $"[UMAP] Source parameters: MinDist={_parameters.MinDist:F4}, Spread={_parameters.Spread:F2}"
      );
    }

    var random = _parameters.RandomSeed.HasValue
      ? new Random(_parameters.RandomSeed.Value)
      : new Random();

    var optimizationResult = _layoutOptimization.Optimize(
      initialEmbedding: layoutResult.Embedding,
      graphEdges: edges,
      samplingSchedule: scheduleResult.EpochsPerSample,
      nEpochs: nEpochs,
      parameters: optimizationParams,
      random: random
    );

    return new UmapFitResult(
      Embedding: optimizationResult.OptimizedEmbedding,
      GraphResult: graphResult,
      LayoutInitResult: layoutResult,
      SamplingScheduleResult: scheduleResult,
      OptimizationResult: optimizationResult
    );
  }

  /// <summary>
  /// Converts a sparse graph matrix to an array of edges for optimization.
  /// </summary>
  private static GraphEdge[] ConvertGraphToEdges(SparseMatrix graph)
  {
    var edges = new List<GraphEdge>();
    var enumerator = graph.EnumerateIndexed(Zeros.AllowSkip);

    foreach (var (row, col, weight) in enumerator)
    {
      if (row < col && weight > 0.0f)
      {
        edges.Add(new GraphEdge(Head: row, Tail: col, Weight: weight));
      }
    }

    return edges.ToArray();
  }
}

/// <summary>
/// Result of computing the UMAP graph (phases 1-3).
/// </summary>
/// <param name="Graph">Fuzzy simplicial set as a sparse symmetric matrix.</param>
/// <param name="KnnIndices">K-nearest neighbor indices for each point.</param>
/// <param name="KnnDistances">K-nearest neighbor distances for each point.</param>
/// <param name="Sigmas">Bandwidth parameters from local metric computation.</param>
/// <param name="Rhos">Local connectivity distances from local metric computation.</param>
/// <param name="SearchIndex">Optional search index for transform operations.</param>
public sealed record UmapGraphResult(
  SparseMatrix Graph,
  int[][] KnnIndices,
  float[][] KnnDistances,
  float[] Sigmas,
  float[] Rhos,
  object? SearchIndex
);

/// <summary>
/// Result of the complete UMAP FitTransform operation.
/// Contains the final embedding and all intermediate results.
/// </summary>
/// <param name="Embedding">Final optimized low-dimensional embedding. Shape: (n_samples, n_components)</param>
/// <param name="GraphResult">Intermediate result from graph construction (Phases 1-4).</param>
/// <param name="LayoutInitResult">Intermediate result from layout initialization (Phase 5).</param>
/// <param name="SamplingScheduleResult">Intermediate result from sampling schedule computation (Phase 6).</param>
/// <param name="OptimizationResult">Result from layout optimization (Phase 7).</param>
public sealed record UmapFitResult(
  Matrix<float> Embedding,
  UmapGraphResult GraphResult,
  LayoutInitResult LayoutInitResult,
  SamplingScheduleResult SamplingScheduleResult,
  LayoutOptimizationResult OptimizationResult
);
