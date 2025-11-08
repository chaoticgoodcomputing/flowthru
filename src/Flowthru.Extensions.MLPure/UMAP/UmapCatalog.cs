using Microsoft.ML;

namespace Flowthru.Extensions.MLPure.UMAP;

/// <summary>
/// Extension methods for ML.NET integration - pure implementation.
/// </summary>
public static class UmapCatalog
{
  public static UmapTrainer CreateUmapTrainer(this MLContext context)
  {
    var env = new MLContext(seed: context.GetHashCode());
    return new UmapTrainer(env, null);
  }

  public static UmapTrainer CreateUmapTrainer(this MLContext context, UmapOptions options)
  {
    var env = new MLContext(seed: options.RandomState);
    return new UmapTrainer(env, options);
  }

  public static UmapTrainer CreateUmapTrainer(this MLContext context, Action<UmapOptions> configure)
  {
    var options = new UmapOptions();
    configure?.Invoke(options);

    var env = new MLContext(seed: options.RandomState);
    return new UmapTrainer(env, options);
  }

  public static UmapTransformer CreateUmapTransformer(
    this MLContext context,
    UmapModelParameters model
  )
  {
    var env = new MLContext(seed: context.GetHashCode());
    return new UmapTransformer(env, model);
  }

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
