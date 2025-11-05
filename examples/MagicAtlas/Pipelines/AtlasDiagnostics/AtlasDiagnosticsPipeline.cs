using Flowthru.Pipelines;
using MagicAtlas.Data;
using MagicAtlas.Pipelines.AtlasDiagnostics.Nodes;

namespace MagicAtlas.Pipelines.AtlasDiagnostics;

/// <summary>
/// Diagnostic pipeline for analyzing card embeddings through nearest neighbor search.
/// </summary>
public static class AtlasDiagnosticsPipeline
{
  /// <summary>
  /// Configuration parameters for the diagnostics pipeline.
  /// </summary>
  public record Params
  {
    /// <summary>
    /// Configuration options for the nearest neighbor sampling node.
    /// </summary>
    public SampleOracleNearestNeighborsNode.Options NodeOptions { get; init; } = new();
  }

  /// <summary>
  /// Creates the diagnostics pipeline.
  /// </summary>
  /// <param name="catalog">The data catalog containing input and output entries.</param>
  /// <param name="parameters">Configuration parameters for the pipeline.</param>
  /// <returns>
  /// A configured pipeline that samples cards and finds their nearest neighbors in embedding space.
  /// </returns>
  public static Pipeline Create(Catalog catalog, Params parameters)
  {
    return PipelineBuilder.CreatePipeline(pipeline =>
    {
      pipeline.AddNode(
        label: "SampleOracleNearestNeighbors",
        description: """
          Samples oracle cards and finds their nearest neighbors in embedding space based
          on ability similarity.
        """,
        transform: SampleOracleNearestNeighborsNode.Create(parameters.NodeOptions),
        input: (
          catalog.FilteredCardCoreData,
          catalog.FilteredCardMetadata,
          catalog.OracleTextEmbeddings
        ),
        output: catalog.NearestNeighborAnalysis
      );
    });
  }
}
