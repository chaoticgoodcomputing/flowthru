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

public sealed class UmapPipelineBuilder<TMetric>
  where TMetric : IMetricMarker
{
  private readonly UmapParameters _parameters;
  private INeighborSearchStrategy<TMetric>? _neighborSearch;
  private ILocalMetricStrategy? _localMetric;
  private IMembershipStrengthStrategy? _membershipStrength;
  private IGraphRefinementStrategy? _graphRefinement;
  private ILayoutInitStrategy<TMetric>? _layoutInit;
  private ISamplingScheduleStrategy? _samplingSchedule;
  private ILayoutOptimizationStrategy? _layoutOptimization;

  internal UmapPipelineBuilder(UmapParameters parameters)
  {
    _parameters = parameters;
    _parameters.Validate();
  }

  public UmapPipelineBuilder<TNewMetric> WithMetric<TNewMetric>()
    where TNewMetric : IMetricMarker
  {
    return new UmapPipelineBuilder<TNewMetric>(_parameters);
  }

  public UmapPipelineBuilder<TMetric> WithNeighborSearch(INeighborSearchStrategy<TMetric> strategy)
  {
    _neighborSearch = strategy;
    return this;
  }

  public UmapPipelineBuilder<TMetric> WithLocalMetric(ILocalMetricStrategy strategy)
  {
    _localMetric = strategy;
    return this;
  }

  public UmapPipelineBuilder<TMetric> WithMembershipStrength(IMembershipStrengthStrategy strategy)
  {
    _membershipStrength = strategy;
    return this;
  }

  public UmapPipelineBuilder<TMetric> WithGraphRefinement(IGraphRefinementStrategy strategy)
  {
    _graphRefinement = strategy;
    return this;
  }

  public UmapPipelineBuilder<TMetric> WithLayoutInit(ILayoutInitStrategy<TMetric> strategy)
  {
    _layoutInit = strategy;
    return this;
  }

  public UmapPipelineBuilder<TMetric> WithSamplingSchedule(ISamplingScheduleStrategy strategy)
  {
    _samplingSchedule = strategy;
    return this;
  }

  public UmapPipelineBuilder<TMetric> WithLayoutOptimization(ILayoutOptimizationStrategy strategy)
  {
    _layoutOptimization = strategy;
    return this;
  }

  public Matrix<float> FitTransform(
    Matrix<float> data,
    Func<ReadOnlySpan<float>, ReadOnlySpan<float>, float>? metric = null
  )
  {
    var shape = new DataShape
    {
      Samples = data.RowCount,
      Features = data.ColumnCount,
      IsSparse = false,
      EstimatedMemoryBytes = data.RowCount * data.ColumnCount * sizeof(float),
    };

    var effectiveMetric = metric ?? GetDefaultMetric();

    var executor = new UmapPipelineExecutor<TMetric>(
      _parameters,
      neighborSearch: _neighborSearch
        ?? StrategyResolver.ResolveNeighborSearch<TMetric>(shape, _parameters.Verbosity),
      localMetric: _localMetric ?? StrategyResolver.ResolveLocalMetric(_parameters.Verbosity),
      membershipStrength: _membershipStrength
        ?? StrategyResolver.ResolveMembershipStrength(_parameters.Verbosity),
      graphRefinement: _graphRefinement
        ?? StrategyResolver.ResolveGraphRefinement(_parameters.Verbosity),
      layoutInit: _layoutInit ?? StrategyResolver.ResolveLayoutInit<TMetric>(_parameters.Verbosity),
      samplingSchedule: _samplingSchedule
        ?? StrategyResolver.ResolveSamplingSchedule(_parameters.Verbosity),
      layoutOptimization: _layoutOptimization
        ?? StrategyResolver.ResolveLayoutOptimization<TMetric>(_parameters.Verbosity)
    );

    var result = executor.FitTransform(data, effectiveMetric);
    return result.Embedding;
  }

  private static Func<ReadOnlySpan<float>, ReadOnlySpan<float>, float> GetDefaultMetric()
  {
    if (typeof(TMetric) == typeof(IEuclideanMetric))
    {
      return Metrics.Euclidean;
    }

    throw new NotSupportedException(
      $"No default metric available for {typeof(TMetric).Name}. "
        + "Please provide metric explicitly via FitTransform(data, metric)"
    );
  }
}
