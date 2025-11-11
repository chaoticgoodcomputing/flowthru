using Flowthru.Extensions.ML.UMAP.Core;
using Flowthru.Extensions.ML.UMAP.Core.Markers;
using Flowthru.Extensions.ML.UMAP.Strategies.GraphRefinement;
using Flowthru.Extensions.ML.UMAP.Strategies.LayoutInit;
using Flowthru.Extensions.ML.UMAP.Strategies.LayoutOptimization;
using Flowthru.Extensions.ML.UMAP.Strategies.LocalMetric;
using Flowthru.Extensions.ML.UMAP.Strategies.MembershipStrength;
using Flowthru.Extensions.ML.UMAP.Strategies.NeighborSearch;
using Flowthru.Extensions.ML.UMAP.Strategies.SamplingSchedule;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Single;

namespace Flowthru.Extensions.ML.UMAP.Core;

/// <summary>
/// Type-safe builder for constructing UMAP pipelines with compile-time validation.
/// Uses the type-state pattern to enforce configuration order and strategy compatibility.
/// </summary>
/// <typeparam name="TState">Current builder state (unconfigured, partially configured, or complete).</typeparam>
/// <typeparam name="TMetric">Input metric marker constraining strategy selection.</typeparam>
/// <remarks>
/// <para>
/// The builder enforces a fluent API where strategies must be configured in a specific order,
/// and only compatible strategies can be combined. Invalid combinations are rejected at
/// compile-time rather than runtime.
/// </para>
/// <para>
/// <b>Example usage:</b>
/// </para>
/// <code>
/// var pipeline = new UmapPipelineBuilder&lt;IUnconfigured, ISmallData, IEuclideanMetric&gt;()
///     .WithNeighborSearch(new BruteForceSearch&lt;IEuclideanMetric&gt;())
///     .WithLocalMetric(new BinarySearchSmoothing())
///     .WithMembershipStrength(new ExponentialKernel())
///     .Build();
/// </code>
/// </remarks>
public sealed class UmapPipelineBuilder<TState, TMetric>
  where TState : notnull
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

  internal UmapPipelineBuilder(UmapParameters? parameters = null)
  {
    _parameters = parameters ?? new UmapParameters();
    _parameters.Validate();
  }

  /// <summary>
  /// Configures the neighbor search strategy.
  /// Available in the unconfigured state.
  /// </summary>
  /// <typeparam name="TSearch">The concrete neighbor search strategy type.</typeparam>
  /// <param name="strategy">The neighbor search strategy instance.</param>
  /// <returns>Builder in the neighbor-search-configured state.</returns>
  public UmapPipelineBuilder<INeighborSearchConfigured, TMetric> WithNeighborSearch<TSearch>(
    TSearch strategy
  )
    where TSearch : INeighborSearchStrategy<TMetric>
  {
    if (this is not UmapPipelineBuilder<IUnconfigured, TMetric>)
    {
      throw new InvalidOperationException(
        "WithNeighborSearch can only be called on unconfigured builder"
      );
    }

    var next = new UmapPipelineBuilder<INeighborSearchConfigured, TMetric>(_parameters);
    next._neighborSearch = strategy;
    return next;
  }

  /// <summary>
  /// Configures the local metric strategy.
  /// Available after neighbor search is configured.
  /// </summary>
  /// <param name="strategy">The local metric strategy instance.</param>
  /// <returns>Builder in the local-metric-configured state.</returns>
  public UmapPipelineBuilder<ILocalMetricConfigured, TMetric> WithLocalMetric(
    ILocalMetricStrategy strategy
  )
  {
    if (this is not UmapPipelineBuilder<INeighborSearchConfigured, TMetric>)
    {
      throw new InvalidOperationException(
        "WithLocalMetric can only be called after WithNeighborSearch"
      );
    }

    var next = new UmapPipelineBuilder<ILocalMetricConfigured, TMetric>(_parameters);
    next._neighborSearch = _neighborSearch;
    next._localMetric = strategy;
    return next;
  }

  /// <summary>
  /// Configures the membership strength strategy.
  /// Available after local metric is configured.
  /// </summary>
  /// <param name="strategy">The membership strength strategy instance.</param>
  /// <returns>Builder in the complete state.</returns>
  public UmapPipelineBuilder<IComplete, TMetric> WithMembershipStrength(
    IMembershipStrengthStrategy strategy
  )
  {
    if (this is not UmapPipelineBuilder<ILocalMetricConfigured, TMetric>)
    {
      throw new InvalidOperationException(
        "WithMembershipStrength can only be called after WithLocalMetric"
      );
    }

    var next = new UmapPipelineBuilder<IComplete, TMetric>(_parameters);
    next._neighborSearch = _neighborSearch;
    next._localMetric = _localMetric;
    next._membershipStrength = strategy;
    next._graphRefinement = _graphRefinement;
    next._layoutInit = _layoutInit;
    return next;
  }

  /// <summary>
  /// Configures an optional graph refinement strategy.
  /// Can be called after membership strength is configured.
  /// </summary>
  public UmapPipelineBuilder<IComplete, TMetric> WithGraphRefinement(
    IGraphRefinementStrategy strategy
  )
  {
    if (this is not UmapPipelineBuilder<IComplete, TMetric>)
    {
      throw new InvalidOperationException(
        "WithGraphRefinement can only be called after WithMembershipStrength"
      );
    }

    var next = new UmapPipelineBuilder<IComplete, TMetric>(_parameters);
    next._neighborSearch = _neighborSearch;
    next._localMetric = _localMetric;
    next._membershipStrength = _membershipStrength;
    next._graphRefinement = strategy;
    next._layoutInit = _layoutInit;
    return next;
  }

  /// <summary>
  /// Configures an optional layout initialization strategy.
  /// Can be called after membership strength is configured.
  /// </summary>
  public UmapPipelineBuilder<IComplete, TMetric> WithLayoutInit(
    ILayoutInitStrategy<TMetric> strategy
  )
  {
    if (this is not UmapPipelineBuilder<IComplete, TMetric>)
    {
      throw new InvalidOperationException(
        "WithLayoutInit can only be called after WithMembershipStrength"
      );
    }

    var next = new UmapPipelineBuilder<IComplete, TMetric>(_parameters);
    next._neighborSearch = _neighborSearch;
    next._localMetric = _localMetric;
    next._membershipStrength = _membershipStrength;
    next._graphRefinement = _graphRefinement;
    next._layoutInit = strategy;
    next._samplingSchedule = _samplingSchedule;
    next._layoutOptimization = _layoutOptimization;
    return next;
  }

  /// <summary>
  /// Configures an optional sampling schedule strategy.
  /// Can be called after membership strength is configured.
  /// </summary>
  public UmapPipelineBuilder<IComplete, TMetric> WithSamplingSchedule(
    ISamplingScheduleStrategy strategy
  )
  {
    if (this is not UmapPipelineBuilder<IComplete, TMetric>)
    {
      throw new InvalidOperationException(
        "WithSamplingSchedule can only be called after WithMembershipStrength"
      );
    }

    var next = new UmapPipelineBuilder<IComplete, TMetric>(_parameters);
    next._neighborSearch = _neighborSearch;
    next._localMetric = _localMetric;
    next._membershipStrength = _membershipStrength;
    next._graphRefinement = _graphRefinement;
    next._layoutInit = _layoutInit;
    next._samplingSchedule = strategy;
    next._layoutOptimization = _layoutOptimization;
    return next;
  }

  /// <summary>
  /// Configures an optional layout optimization strategy.
  /// Can be called after membership strength is configured.
  /// </summary>
  public UmapPipelineBuilder<IComplete, TMetric> WithLayoutOptimization(
    ILayoutOptimizationStrategy strategy
  )
  {
    if (this is not UmapPipelineBuilder<IComplete, TMetric>)
    {
      throw new InvalidOperationException(
        "WithLayoutOptimization can only be called after WithMembershipStrength"
      );
    }

    var next = new UmapPipelineBuilder<IComplete, TMetric>(_parameters);
    next._neighborSearch = _neighborSearch;
    next._localMetric = _localMetric;
    next._membershipStrength = _membershipStrength;
    next._graphRefinement = _graphRefinement;
    next._layoutInit = _layoutInit;
    next._samplingSchedule = _samplingSchedule;
    next._layoutOptimization = strategy;
    return next;
  }

  /// <summary>
  /// Builds the configured UMAP pipeline.
  /// Only available when all required strategies are configured.
  /// </summary>
  /// <returns>A configured UMAP pipeline ready to process data.</returns>
  /// <exception cref="InvalidOperationException">Thrown if strategies are not properly configured (should not happen with proper type-state usage).</exception>
  public UmapPipeline<TMetric> Build()
  {
    if (this is not UmapPipelineBuilder<IComplete, TMetric>)
    {
      throw new InvalidOperationException(
        "Build can only be called when all strategies are configured"
      );
    }

    if (_neighborSearch == null)
    {
      throw new InvalidOperationException("Neighbor search strategy is not configured");
    }
    if (_localMetric == null)
    {
      throw new InvalidOperationException("Local metric strategy is not configured");
    }
    if (_membershipStrength == null)
    {
      throw new InvalidOperationException("Membership strength strategy is not configured");
    }

    return new UmapPipeline<TMetric>(
      _parameters,
      _neighborSearch,
      _localMetric,
      _membershipStrength,
      _graphRefinement,
      _layoutInit,
      _samplingSchedule,
      _layoutOptimization
    );
  }
}

/// <summary>
/// Builder state: No strategies configured yet.
/// </summary>
public interface IUnconfigured { }

/// <summary>
/// Builder state: Neighbor search strategy configured.
/// </summary>
public interface INeighborSearchConfigured { }

/// <summary>
/// Builder state: Local metric strategy configured.
/// </summary>
public interface ILocalMetricConfigured : INeighborSearchConfigured { }

/// <summary>
/// Builder state: Optional graph refinement configured.
/// </summary>
public interface IGraphRefinementConfigured : ILocalMetricConfigured { }

/// <summary>
/// Builder state: Optional layout initialization configured.
/// </summary>
public interface ILayoutInitConfigured : IGraphRefinementConfigured { }

/// <summary>
/// Builder state: All required strategies configured, ready to build.
/// </summary>
public interface IComplete : ILocalMetricConfigured { }
