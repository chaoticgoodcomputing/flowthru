using Flowthru.Data;
using MagicAtlas.Data._02_Processed.Schemas;

namespace MagicAtlas.Data;

public partial class Catalog
{
  /// <summary>
  /// Processed card symbols with strong types.
  /// Persisted to disk as JSON.
  /// </summary>
  public ICatalogEntry<CardSymbolDictionary> ProcessedCardSymbols =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Single.Json<CardSymbolDictionary>(
          label: "ProcessedCardSymbols",
          filePath: $"{_basePath}/_02_Processed/Datasets/card-symbols.json"
        )
    );

  /// <summary>
  /// Processed cards with strong types.
  /// Stored in memory only (not persisted to disk due to size).
  /// Contains 35,000+ card objects with full type safety.
  /// </summary>
  public ICatalogEntry<CardCollection> ProcessedCards =>
    GetOrCreateEntry(() => CatalogEntries.Single.Memory<CardCollection>(label: "ProcessedCards"));
}
