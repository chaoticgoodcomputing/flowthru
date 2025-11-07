using Flowthru.Data;
using MagicAtlas.Data._07_ModelOutput.Schemas;
using MagicAtlas.Data._08_Reporting.Schemas;
using Plotly.NET;

namespace MagicAtlas.Data;

/// <summary>
/// Reporting data catalog entries (Layer 8).
/// Contains ad hoc descriptive analyses and diagnostic outputs.
/// </summary>
public partial class Catalog
{
  /// <summary>
  /// Nearest neighbor analysis results for sampled oracle cards.
  /// </summary>
  public ICatalogEntry<IEnumerable<NearestNeighborAnalysis>> NearestNeighborAnalysis =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Json<NearestNeighborAnalysis>(
          label: "NearestNeighborAnalysis",
          filePath: $"{_basePath}/_08_Reporting/Datasets/nearest_neighbor_analysis.json"
        )
    );

  /// <summary>
  /// Randomly sampled oracle text embeddings for distribution analysis.
  /// </summary>
  public ICatalogEntry<IEnumerable<OracleTextEmbedding>> SampledOracleTextEmbeddings =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Json<OracleTextEmbedding>(
          label: "SampledOracleTextEmbeddings",
          filePath: $"{_basePath}/_08_Reporting/Datasets/sampled_oracle_embeddings.json"
        )
    );

  /// <summary>
  /// In-memory chart showing distribution of embedding dimensions.
  /// </summary>
  public ICatalogEntry<GenericChart> EmbeddingDistributionChart =>
    GetOrCreateEntry(
      () => CatalogEntries.Single.Memory<GenericChart>(label: "EmbeddingDistributionChart")
    );

  /// <summary>
  /// PNG export of embedding distribution chart.
  /// </summary>
  public ICatalogEntry<byte[]> EmbeddingDistributionPlotPng =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Single.Binary(
          label: "EmbeddingDistributionPlotPng",
          filePath: $"{_basePath}/_08_Reporting/Datasets/embedding_distribution_plot.png"
        )
    );
}
