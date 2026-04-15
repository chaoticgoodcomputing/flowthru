using Flowthru.Misc.ML.UMAP.Core.Markers;
using Flowthru.Misc.ML.UMAP.Core.Utils;
using Flowthru.Misc.ML.UMAP.Strategies.GraphRefinement;
using Flowthru.Misc.ML.UMAP.Strategies.LayoutInit;
using Flowthru.Misc.ML.UMAP.Strategies.LayoutOptimization;
using Flowthru.Misc.ML.UMAP.Strategies.LocalMetric;
using Flowthru.Misc.ML.UMAP.Strategies.MembershipStrength;
using Flowthru.Misc.ML.UMAP.Strategies.NeighborSearch;
using Flowthru.Misc.ML.UMAP.Strategies.SamplingSchedule;
using MathNet.Numerics.LinearAlgebra;

namespace Flowthru.Misc.ML.UMAP.Core;

/// <summary>
/// Builder for configuring and executing UMAP dimensionality reduction with flexible strategy selection.
/// </summary>
public sealed class UmapFlowBuilder
{
    private readonly UmapParameters _parameters;
    private IMetric _inputMetric = EuclideanMetric.Instance;
    private IOutputMetric? _outputMetric;
    private INeighborSearchStrategy? _neighborSearch;
    private ILocalMetricStrategy? _localMetric;
    private IMembershipStrengthStrategy? _membershipStrength;
    private IGraphRefinementStrategy? _graphRefinement;
    private ILayoutInitStrategy? _layoutInit;
    private ISamplingScheduleStrategy? _samplingSchedule;
    private ILayoutOptimizationStrategy? _layoutOptimization;

    internal UmapFlowBuilder(UmapParameters parameters)
    {
        _parameters = parameters;
        _parameters.Validate();
    }

    /// <summary>
    /// Sets the input distance metric for UMAP. Defaults to Euclidean if not set.
    /// </summary>
    /// <param name="metric"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public UmapFlowBuilder WithInputMetric(IMetric metric)
    {
        _inputMetric = metric ?? throw new ArgumentNullException(nameof(metric));
        return this;
    }

    /// <summary>
    /// Sets the output metric for evaluating embedding quality. Optional, as many strategies do not require it.
    /// If not set, strategies that can utilize an output metric will default to a standard choice (e.g., KNN preservation).
    /// This allows users to benefit from output-aware strategies without needing to specify a metric if they don't have a specific one in mind.
    /// Providing an output metric can enable more sophisticated strategies that optimize for that metric, but is not required for basic UMAP functionality.
    /// </summary>
    /// <param name="metric"></param>
    /// <returns></returns>
    public UmapFlowBuilder WithOutputMetric(IOutputMetric metric)
    {
        _outputMetric = metric;
        return this;
    }

    /// <summary>
    /// Sets the neighbor search strategy for UMAP. If not set, a strategy will be auto-selected based on data size and dimensionality.
    /// </summary>
    /// <param name="strategy"></param>
    /// <returns></returns>
    public UmapFlowBuilder WithNeighborSearch(INeighborSearchStrategy strategy)
    {
        _neighborSearch = strategy;
        return this;
    }

    /// <summary>
    /// Sets the local metric strategy for UMAP. If not set, a strategy will be auto-selected based on data characteristics and verbosity level.
    /// Local metric strategies determine how distances are computed in the high-dimensional space and can significantly impact the quality of the embedding.
    /// By allowing users to specify a local metric strategy, we enable them to tailor UMAP to their specific data and use case, while still providing sensible defaults for those who do not wish to configure this aspect.
    /// This flexibility is important for accommodating the wide variety of datasets and requirements that users may have when applying UMAP.
    /// </summary>
    /// <param name="strategy"></param>
    /// <returns></returns>
    public UmapFlowBuilder WithLocalMetric(ILocalMetricStrategy strategy)
    {
        _localMetric = strategy;
        return this;
    }

    /// <summary>
    /// Sets the membership strength strategy for UMAP. If not set, a strategy will be auto-selected based on data characteristics and verbosity level.
    /// </summary>
    /// <param name="strategy"></param>
    /// <returns></returns>
    public UmapFlowBuilder WithMembershipStrength(IMembershipStrengthStrategy strategy)
    {
        _membershipStrength = strategy;
        return this;
    }

    /// <summary>
    /// Sets the graph refinement strategy for UMAP. If not set, a strategy will be auto-selected based on data characteristics and verbosity level.
    /// Graph refinement strategies modify the initial k-nearest neighbor graph to improve the quality of the embedding. Examples include:
    /// - Mutual kNN: Retains only edges where both points are in each other's kNN
    /// - Local connectivity: Ensures each point is connected to at least one neighbor
    /// - Edge weighting: Adjusts edge weights based on distance or local density
    /// By allowing users to specify a graph refinement strategy, we enable them to enhance UMAP's performance on their specific dataset, while still providing sensible defaults for those who do not wish to configure this aspect.
    /// This flexibility is important for accommodating the wide variety of datasets and requirements that users may have when applying UMAP.
    /// </summary>
    /// <param name="strategy"></param>
    /// <returns></returns>
    public UmapFlowBuilder WithGraphRefinement(IGraphRefinementStrategy strategy)
    {
        _graphRefinement = strategy;
        return this;
    }

    /// <summary>
    /// Sets the layout initialization strategy for UMAP. If not set, a strategy will be auto-selected based on data characteristics and verbosity level.
    /// Layout initialization strategies determine how the initial low-dimensional embedding is generated before optimization. Examples include:
    /// - Spectral embedding: Uses eigenvectors of the graph Laplacian for initialization
    /// - Random initialization: Assigns random coordinates to each point
    /// - PCA-based initialization: Uses the top principal components for initialization
    /// By allowing users to specify a layout initialization strategy, we enable them to improve convergence and
    /// </summary>
    /// <param name="strategy"></param>
    /// <returns></returns>
    public UmapFlowBuilder WithLayoutInit(ILayoutInitStrategy strategy)
    {
        _layoutInit = strategy;
        return this;
    }

    /// <summary>
    /// Sets the sampling schedule strategy for UMAP. If not set, a strategy will be auto-selected based on data characteristics and verbosity level.
    /// Sampling schedule strategies determine how data points are sampled during the optimization process, which can impact convergence speed and embedding quality. Examples include:
    /// - Uniform sampling: Samples points uniformly at random
    /// - Density-based sampling: Samples points based on local density to ensure underrepresented regions are adequately sampled
    /// - Adaptive sampling: Adjusts sampling probabilities based on optimization progress to focus on points that are not yet well-embedded
    /// By allowing users to specify a sampling schedule strategy, we enable them to improve convergence and embedding quality for their specific dataset, while still providing sensible defaults
    /// </summary>
    /// <param name="strategy"></param>
    /// <returns></returns>
    public UmapFlowBuilder WithSamplingSchedule(ISamplingScheduleStrategy strategy)
    {
        _samplingSchedule = strategy;
        return this;
    }

    /// <summary>
    /// Sets the layout optimization strategy for UMAP. If not set, a strategy will be auto-selected based on data characteristics and verbosity level.
    /// </summary>
    /// <param name="strategy"></param>
    /// <returns></returns>
    public UmapFlowBuilder WithLayoutOptimization(ILayoutOptimizationStrategy strategy)
    {
        _layoutOptimization = strategy;
        return this;
    }

    /// <summary>
    /// Fits UMAP and transforms data in one step.
    /// Auto-selects strategies based on data characteristics if not explicitly set.
    /// </summary>
    /// <param name="data">Input data as jagged array (n_samples, n_features)</param>
    /// <returns>Low-dimensional embedding (n_samples, n_components)</returns>
    public float[][] FitTransform(float[][] data)
    {
        // Convert to Matrix for internal processing
        var matrix = MathNet.Numerics.LinearAlgebra.Single.DenseMatrix.OfRowArrays(data);
        var fitResult = FitTransformWithReport(matrix);

        // Convert back to jagged array
        var result = new float[fitResult.Embedding.RowCount][];
        for (int i = 0; i < fitResult.Embedding.RowCount; i++)
        {
            result[i] = fitResult.Embedding.Row(i).ToArray();
        }

        return result;
    }

    /// <summary>
    /// Fits UMAP and transforms data in one step.
    /// Auto-selects strategies based on data characteristics if not explicitly set.
    /// </summary>
    /// <param name="data">Input data matrix (n_samples, n_features)</param>
    /// <returns>Low-dimensional embedding (n_samples, n_components)</returns>
    /// <remarks>
    /// TODO: Consider deprecating this overload. Matrix&lt;float&gt; adds virtual call overhead
    /// and intermediate allocations compared to float[][]. Only kept for compatibility with
    /// SpectralInit which uses Math.Net for eigenvalue decomposition.
    /// </remarks>
    public Matrix<float> FitTransform(Matrix<float> data)
    {
        var result = FitTransformWithReport(data);
        return result.Embedding;
    }

    /// <summary>
    /// Fits UMAP and transforms data in one step, returning full result including runtime report.
    /// Auto-selects strategies based on data characteristics if not explicitly set.
    /// </summary>
    /// <param name="data">Input data matrix (n_samples, n_features)</param>
    /// <returns>Complete UMAP result including embedding and runtime metrics</returns>
    public UmapFitResult FitTransformWithReport(Matrix<float> data)
    {
        var shape = new DataShape
        {
            Samples = data.RowCount,
            Features = data.ColumnCount,
            IsSparse = false,
            EstimatedMemoryBytes = data.RowCount * data.ColumnCount * sizeof(float),
        };

        var executor = new UmapPipelineExecutor(
          _parameters,
          _inputMetric,
          neighborSearch: _neighborSearch
            ?? StrategyResolver.ResolveNeighborSearch(shape, _parameters.Verbosity),
          localMetric: _localMetric ?? StrategyResolver.ResolveLocalMetric(_parameters.Verbosity),
          membershipStrength: _membershipStrength
            ?? StrategyResolver.ResolveMembershipStrength(_parameters.Verbosity),
          graphRefinement: _graphRefinement
            ?? StrategyResolver.ResolveGraphRefinement(shape, _parameters.Verbosity),
          layoutInit: _layoutInit ?? StrategyResolver.ResolveLayoutInit(_parameters.Verbosity),
          samplingSchedule: _samplingSchedule
            ?? StrategyResolver.ResolveSamplingSchedule(_parameters.Verbosity),
          layoutOptimization: _layoutOptimization
            ?? StrategyResolver.ResolveLayoutOptimization(shape, _parameters.Verbosity)
        );

        return executor.FitTransform(data);
    }
}
