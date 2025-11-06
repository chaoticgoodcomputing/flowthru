using Flowthru.Data;
using MagicAtlas.Data._08_Reporting.Schemas;

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
}
