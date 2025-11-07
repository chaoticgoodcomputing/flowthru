using Microsoft.ML;
using Microsoft.ML.Runtime;

namespace Flowthru.Extensions.ML.UMAP;

/// <summary>
/// Extension methods for easy integration of UMAP with ML.NET's MLContext.
/// </summary>
/// <remarks>
/// Provides fluent API for applying UMAP dimensionality reduction in ML.NET pipelines.
/// Based on the UMAP Python implementation by Leland McInnes.
/// <para>
/// Citation: McInnes, L, Healy, J, "UMAP: Uniform Manifold Approximation and Projection
/// for Dimension Reduction", ArXiv e-prints 1802.03426, 2018
/// https://arxiv.org/abs/1802.03426
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var mlContext = new MLContext();
///
/// // Create UMAP trainer with custom options
/// var umapOptions = new UmapOptions
/// {
///     NumberOfNeighbors = 15,
///     NumberOfComponents = 2,
///     MinDist = 0.1f,
///     Metric = "euclidean"
/// };
///
/// var trainer = mlContext.CreateUmapTrainer(umapOptions);
/// var model = trainer.Fit(data);
/// var embedding = model.Embedding;
/// </code>
/// </example>
public static class UmapCatalog
{
  /// <summary>
  /// Creates a UMAP trainer with default options.
  /// </summary>
  /// <param name="context">The ML.NET context.</param>
  /// <returns>A new UMAP trainer instance.</returns>
  public static UmapTrainer CreateUmapTrainer(this MLContext context)
  {
    var env = new MLContext(seed: context.GetHashCode());
    return new UmapTrainer(env, null);
  }

  /// <summary>
  /// Creates a UMAP trainer with the specified options.
  /// </summary>
  /// <param name="context">The ML.NET context.</param>
  /// <param name="options">Configuration options for UMAP.</param>
  /// <returns>A new UMAP trainer instance.</returns>
  public static UmapTrainer CreateUmapTrainer(this MLContext context, UmapOptions options)
  {
    var env = new MLContext(seed: options.RandomState);
    return new UmapTrainer(env, options);
  }

  /// <summary>
  /// Creates a UMAP trainer with a configuration action.
  /// </summary>
  /// <param name="context">The ML.NET context.</param>
  /// <param name="configure">Action to configure UMAP options.</param>
  /// <returns>A new UMAP trainer instance.</returns>
  public static UmapTrainer CreateUmapTrainer(this MLContext context, Action<UmapOptions> configure)
  {
    var options = new UmapOptions();
    configure?.Invoke(options);

    var env = new MLContext(seed: options.RandomState);
    return new UmapTrainer(env, options);
  }

  /// <summary>
  /// Creates a UMAP transformer from trained model parameters.
  /// </summary>
  /// <param name="context">The ML.NET context.</param>
  /// <param name="model">Trained UMAP model parameters.</param>
  /// <returns>A new UMAP transformer instance.</returns>
  public static UmapTransformer CreateUmapTransformer(
    this MLContext context,
    UmapModelParameters model
  )
  {
    var env = new MLContext(seed: context.GetHashCode());
    return new UmapTransformer(env, model);
  }

  /// <summary>
  /// Creates a UMAP trainer with simplified parameters.
  /// </summary>
  /// <param name="context">The ML.NET context.</param>
  /// <param name="nNeighbors">Number of neighbors to consider.</param>
  /// <param name="nComponents">Target dimensionality of the embedding.</param>
  /// <param name="minDist">Minimum distance between embedded points.</param>
  /// <param name="metric">Distance metric to use ("euclidean", "cosine", etc.).</param>
  /// <returns>A new UMAP trainer instance.</returns>
  public static UmapTrainer CreateUmapTrainer(
    this MLContext context,
    int nNeighbors = 15,
    int nComponents = 2,
    float minDist = 0.1f,
    string metric = "euclidean"
  )
  {
    var options = new UmapOptions
    {
      NumberOfNeighbors = nNeighbors,
      NumberOfComponents = nComponents,
      MinDist = minDist,
      Metric = metric,
    };

    var env = new MLContext(seed: context.GetHashCode());
    return new UmapTrainer(env, options);
  }
}
