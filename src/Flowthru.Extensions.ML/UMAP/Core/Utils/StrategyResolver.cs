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

namespace Flowthru.Extensions.ML.UMAP.Core.Utils;

/// <summary>
/// Resolves strategy instances based on data characteristics.
/// Implements the auto-selection logic for UMAP pipeline strategies.
/// </summary>
internal static class StrategyResolver
{
  /// <summary>
  /// Auto-selects neighbor search strategy based on data size.
  /// </summary>
  /// <param name="shape">Data characteristics for selection</param>
  /// <param name="verbosity">Verbosity level for logging</param>
  /// <returns>Appropriate neighbor search strategy</returns>
  public static INeighborSearchStrategy ResolveNeighborSearch(
    DataShape shape,
    int verbosity
  )
  {
    // Use NN-Descent for larger datasets, brute force for smaller
    var strategy =
      shape.Samples < 4096
        ? (INeighborSearchStrategy)new BruteForceSearch()
        : new NNDescentSearch { Verbose = verbosity >= 2 };

    if (verbosity >= 1)
    {
      var strategyName = shape.Samples < 4096 ? "BruteForceSearch" : "NNDescentSearch";
      Console.WriteLine(
        $"[UMAP] Auto-selected {strategyName} for {shape.Samples} samples (threshold: 4096)"
      );
    }

    return strategy;
  }

  /// <summary>
  /// Auto-selects local metric strategy.
  /// Currently always returns BinarySearchSmoothing (Python UMAP default).
  /// </summary>
  public static ILocalMetricStrategy ResolveLocalMetric(int verbosity)
  {
    if (verbosity >= 2)
    {
      Console.WriteLine("[UMAP] Using BinarySearchSmoothing for local metric");
    }

    return new BinarySearchSmoothing();
  }

  /// <summary>
  /// Auto-selects membership strength strategy.
  /// Currently always returns ExponentialKernel (Python UMAP default).
  /// </summary>
  public static IMembershipStrengthStrategy ResolveMembershipStrength(int verbosity)
  {
    if (verbosity >= 2)
    {
      Console.WriteLine("[UMAP] Using ExponentialKernel for membership strength");
    }

    return new ExponentialKernel();
  }

  /// <summary>
  /// Auto-selects graph refinement strategy.
  /// Currently always returns AdaptiveThresholding (Python UMAP default).
  /// </summary>
  public static IGraphRefinementStrategy ResolveGraphRefinement(int verbosity)
  {
    if (verbosity >= 2)
    {
      Console.WriteLine("[UMAP] Using AdaptiveThresholding for graph refinement");
    }

    return new AdaptiveThresholding();
  }

  /// <summary>
  /// Auto-selects layout initialization strategy.
  /// Defaults to spectral initialization.
  /// </summary>
  /// <param name="verbosity">Verbosity level for logging</param>
  /// <returns>Appropriate layout initialization strategy</returns>
  public static ILayoutInitStrategy ResolveLayoutInit(int verbosity)
  {
    if (verbosity >= 1)
    {
      Console.WriteLine("[UMAP] Using SpectralInit for layout initialization");
    }

    return new SpectralInit();
  }

  /// <summary>
  /// Auto-selects sampling schedule strategy.
  /// Currently always returns ProportionalSampling (Python UMAP default).
  /// </summary>
  public static ISamplingScheduleStrategy ResolveSamplingSchedule(int verbosity)
  {
    if (verbosity >= 2)
    {
      Console.WriteLine("[UMAP] Using ProportionalSampling for sampling schedule");
    }

    return new ProportionalSampling();
  }

  /// <summary>
  /// Auto-selects layout optimization strategy.
  /// Currently always returns EuclideanSGD.
  /// </summary>
  /// <param name="verbosity">Verbosity level for logging</param>
  /// <returns>Appropriate layout optimization strategy</returns>
  public static ILayoutOptimizationStrategy ResolveLayoutOptimization(int verbosity)
  {
    if (verbosity >= 1)
    {
      Console.WriteLine("[UMAP] Using EuclideanSGD for layout optimization");
    }

    return new EuclideanSGD();
  }
}
