using Flowthru.Data;
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
}
