using Flowthru.Data;
using MagicAtlas.Data._05_Diagnostics.Schemas;

namespace MagicAtlas.Data;

public partial class Catalog
{
  /// <summary>
  /// Nearest neighbor analysis results for sampled oracle cards.
  /// </summary>
  /// <remarks>
  /// <para>
  /// <strong>Transformation:</strong> (FilteredCardCoreData, FilteredCardMetadata, OracleTextEmbeddings) → NearestNeighborAnalysis
  /// </para>
  /// <para>
  /// Each entry represents a target card and its N nearest neighbors in embedding space,
  /// based on cosine similarity of ability embeddings (excluding full text embeddings).
  /// </para>
  /// <para>
  /// <strong>Storage:</strong> JSON format for human-readable diagnostic output
  /// </para>
  /// <para>
  /// <strong>Layer:</strong> 5 (Diagnostics - derived from embeddings and core data)
  /// </para>
  /// </remarks>
  public ICatalogEntry<IEnumerable<NearestNeighborAnalysis>> NearestNeighborAnalysis =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Json<NearestNeighborAnalysis>(
          label: "NearestNeighborAnalysis",
          filePath: $"{_basePath}/_05_Diagnostics/Datasets/nearest_neighbor_analysis.json"
        )
    );
}
