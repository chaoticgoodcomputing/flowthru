using Flowthru.Data;
using MagicAtlas.Data._02_Processed.Schemas;
using MagicAtlas.Data._03_Structured.Schemas;

namespace MagicAtlas.Data;

public partial class Catalog
{
  /// <summary>
  /// Parsed hierarchical rules structure.
  /// </summary>
  public ICatalogEntry<RulesStructure> ParsedRules =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Single.Json<RulesStructure>(
          label: "ParsedRules",
          filePath: $"{_basePath}/_03_Structured/Datasets/rules-structure.json"
        )
    );

  /// <summary>
  /// Parsed glossary as term-definition pairs.
  /// </summary>
  public ICatalogEntry<GlossaryEntries> ParsedGlossary =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Single.Json<GlossaryEntries>(
          label: "ParsedGlossary",
          filePath: $"{_basePath}/_03_Structured/Datasets/glossary.json"
        )
    );

  /// <summary>
  /// Filtered card core data (analysis-relevant fields).
  /// Persisted to disk as JSON.
  /// </summary>
  public ICatalogEntry<IEnumerable<CardCoreData>> FilteredCardCoreData =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Json<CardCoreData>(
          label: "FilteredCardCoreData",
          filePath: $"{_basePath}/_03_Structured/Datasets/filtered-cards-core.json"
        )
    );

  /// <summary>
  /// Filtered card metadata (non-analysis fields).
  /// Persisted to disk as JSON (metadata is not flat tabular data).
  /// </summary>
  public ICatalogEntry<IEnumerable<CardMetadata>> FilteredCardMetadata =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Json<CardMetadata>(
          label: "FilteredCardMetadata",
          filePath: $"{_basePath}/_03_Structured/Datasets/filtered-cards-metadata.json"
        )
    );

  /// <summary>
  /// Refined oracle text with expanded symbols and categorized abilities.
  /// Persisted to disk as JSON.
  /// </summary>
  public ICatalogEntry<IEnumerable<RefinedOracleText>> RefinedOracleText =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Json<RefinedOracleText>(
          label: "RefinedOracleText",
          filePath: $"{_basePath}/_03_Structured/Datasets/refined-oracle-text.json"
        )
    );

  /// <summary>
  /// Flattened oracle text entries for embedding model input.
  /// Each card produces multiple entries (full text + individual abilities).
  /// </summary>
  public ICatalogEntry<IEnumerable<EmbeddingModelOracleInput>> EmbeddingModelOracleInput =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Csv<EmbeddingModelOracleInput>(
          label: "EmbeddingModelOracleInput",
          filePath: $"{_basePath}/_03_Structured/Datasets/embedding-model-oracle-input.csv"
        )
    );
}
