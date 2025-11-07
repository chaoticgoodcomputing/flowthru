using Flowthru.Pipelines;
using MagicAtlas.Data;
using MagicAtlas.Helpers.Nodes;
using MagicAtlas.Pipelines.EmbeddingReductions.Nodes;

namespace MagicAtlas.Pipelines.EmbeddingReductions;

/// <summary>
/// Pipeline for performing dimensionality reduction on oracle text embeddings using PCA.
/// </summary>
/// <remarks>
/// <para>
/// Reduces 384-dimensional sentence embeddings to a configurable lower-dimensional
/// representation using Principal Component Analysis. This preserves the most
/// important variance while enabling visualization and reducing computational costs.
/// </para>
/// </remarks>
public static class EmbeddingReductions
{
  /// <summary>
  /// Configuration parameters for the embedding reductions pipeline.
  /// </summary>
  public record Params
  {
    /// <summary>
    /// Configuration options for the PCA reduction node.
    /// </summary>
    public PcaReductionNode.Params PcaOptions { get; init; } = new();

    /// <summary>
    /// Configuration options for the PCA scatter plot visualization.
    /// </summary>
    public GeneratePcaScatterPlotNode.Params ScatterPlotOptions { get; init; } = new();
  }

  /// <summary>
  /// Creates the embedding reductions pipeline.
  /// </summary>
  /// <param name="catalog">The data catalog containing input and output entries.</param>
  /// <param name="parameters">Configuration parameters for the pipeline.</param>
  /// <returns>
  /// A configured pipeline that performs PCA dimensionality reduction on embeddings.
  /// </returns>
  public static Pipeline Create(Catalog catalog, Params? parameters = null)
  {
    var opts = parameters ?? new Params();

    return PipelineBuilder.CreatePipeline(pipeline =>
    {
      pipeline.AddNode(
        label: "PcaReduction",
        description: """
          Performs PCA dimensionality reduction on oracle text embeddings.
          Reduces 384-dimensional vectors to a lower-dimensional representation.
        """,
        transform: PcaReductionNode.Create(opts.PcaOptions),
        input: catalog.OracleTextEmbeddings,
        output: catalog.OraclePcaEmbeddings
      );

      pipeline.AddNode(
        label: "GeneratePcaScatterPlot",
        description: """
          Generates a 2D scatter plot of the first two PCA components.
          Filters out Full text entries and colors points by oracle text type.
        """,
        transform: GeneratePcaScatterPlotNode.Create(opts.ScatterPlotOptions),
        input: catalog.OraclePcaEmbeddings,
        output: catalog.PcaScatterPlotChart
      );

      pipeline.AddNode(
        label: "ExportPcaScatterPlot",
        description: """
          Exports the PCA scatter plot to PNG format for viewing and sharing.
        """,
        transform: PlotlyImageExportNode.Create(),
        input: catalog.PcaScatterPlotChart,
        output: catalog.PcaScatterPlotPng
      );
    });
  }
}
