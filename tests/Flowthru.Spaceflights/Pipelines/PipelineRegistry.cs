using Flowthru.Pipelines;
using Flowthru.Spaceflights.Data;
using Flowthru.Spaceflights.Pipelines.DataProcessing;
using Flowthru.Spaceflights.Pipelines.DataScience;
using Flowthru.Spaceflights.Pipelines.DataScience.Parameters;

namespace Flowthru.Spaceflights.Pipelines;

/// <summary>
/// Central registry for all pipelines in the Spaceflights project.
/// Provides named access to configured pipelines.
/// </summary>
public static class PipelineRegistry
{
  /// <summary>
  /// Registers and returns all available pipelines.
  /// </summary>
  /// <param name="catalog">The data catalog to use for pipeline construction</param>
  /// <param name="modelOptions">Optional model training parameters</param>
  /// <returns>Dictionary of pipeline names to pipeline instances</returns>
  public static Dictionary<string, Pipeline> RegisterPipelines(
      DataCatalog catalog,
      ModelOptions? modelOptions = null)
  {
    var pipelines = new Dictionary<string, Pipeline>
    {
      ["data_processing"] = DataProcessingPipeline.Create(catalog),
      ["data_science"] = DataSciencePipeline.Create(catalog, modelOptions),
    };

    // Create default pipeline that runs all pipelines in sequence
    pipelines["__default__"] = CombinePipelines(catalog, pipelines.Values);

    return pipelines;
  }

  /// <summary>
  /// Combines multiple pipelines into a single sequential pipeline.
  /// </summary>
  private static Pipeline CombinePipelines(DataCatalog catalog, IEnumerable<Pipeline> pipelines)
  {
    return PipelineBuilder.CreatePipeline(catalog, pipeline =>
    {
      // In a full implementation, this would merge node graphs
      // For now, this is a placeholder showing the intended API
      foreach (var p in pipelines)
      {
        // pipeline.Merge(p);
      }
    });
  }
}
