using Flowthru.Extensions.ML.UMAP.Core.Markers;
using Flowthru.Extensions.ML.UMAP.Core.Utils;
using Flowthru.Extensions.ML.UMAP.Strategies.GraphRefinement;
using Flowthru.Extensions.ML.UMAP.Strategies.LayoutInit;
using Flowthru.Extensions.ML.UMAP.Strategies.LayoutOptimization;
using Flowthru.Extensions.ML.UMAP.Strategies.LocalMetric;
using Flowthru.Extensions.ML.UMAP.Strategies.MembershipStrength;
using Flowthru.Extensions.ML.UMAP.Strategies.NeighborSearch;
using Flowthru.Extensions.ML.UMAP.Strategies.SamplingSchedule;
using MathNet.Numerics.LinearAlgebra;

namespace Flowthru.Extensions.ML.UMAP.Core;

public sealed class UmapPipelineBuilder
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

  internal UmapPipelineBuilder(UmapParameters parameters)
  {
    _parameters = parameters;
    _parameters.Validate();
  }

  public UmapPipelineBuilder WithInputMetric(IMetric metric)
  {
    _inputMetric = metric ?? throw new ArgumentNullException(nameof(metric));
    return this;
  }

  public UmapPipelineBuilder WithOutputMetric(IOutputMetric metric)
  {
    _outputMetric = metric;
    return this;
  }

  public UmapPipelineBuilder WithNeighborSearch(INeighborSearchStrategy strategy)
  {
    _neighborSearch = strategy;
    return this;
  }

  public UmapPipelineBuilder WithLocalMetric(ILocalMetricStrategy strategy)
  {
    _localMetric = strategy;
    return this;
  }

  public UmapPipelineBuilder WithMembershipStrength(IMembershipStrengthStrategy strategy)
  {
    _membershipStrength = strategy;
    return this;
  }

  public UmapPipelineBuilder WithGraphRefinement(IGraphRefinementStrategy strategy)
  {
    _graphRefinement = strategy;
    return this;
  }

  public UmapPipelineBuilder WithLayoutInit(ILayoutInitStrategy strategy)
  {
    _layoutInit = strategy;
    return this;
  }

  public UmapPipelineBuilder WithSamplingSchedule(ISamplingScheduleStrategy strategy)
  {
    _samplingSchedule = strategy;
    return this;
  }

  public UmapPipelineBuilder WithLayoutOptimization(ILayoutOptimizationStrategy strategy)
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
        ?? StrategyResolver.ResolveLayoutOptimization(_parameters.Verbosity)
    );

    return executor.FitTransform(data);
  }
}
