using Flowthru.Data;
using MagicAtlas.Data._01_Raw.Schemas;

namespace MagicAtlas.Data;

public partial class Catalog
{
  /// <summary>
  /// Raw MTG comprehensive rules text file.
  ///
  /// This isn't included in the project itself, but can be downloaded from
  /// https://magic.wizards.com/en/rules
  /// </summary>
  public ICatalogEntry<string> RawRules =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Single.Text(
          label: "RawRules",
          filePath: $"{_basePath}/_01_Raw/Datasets/mtg-rules.txt"
        )
    );

  /// <summary>
  /// Raw Scryfall oracle card symbols JSON.
  /// </summary>
  public ICatalogEntry<RawScryfallCardSymbolList> RawCardSymbols =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Single.Json<RawScryfallCardSymbolList>(
          label: "RawCardSymbols",
          filePath: $"{_basePath}/_01_Raw/Datasets/oracle-card-symbols.json"
        )
    );

  /// <summary>
  /// Raw Scryfall oracle cards JSON.
  /// Contains all Magic: The Gathering cards in the Oracle database.
  /// This file is very large (50+ MB) and contains 35,000+ cards.
  /// The file is a JSON array of card objects.
  /// </summary>
  public ICatalogEntry<IEnumerable<RawScryfallCard>> RawCards =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Json<RawScryfallCard>(
          label: "RawCards",
          filePath: $"{_basePath}/_01_Raw/Datasets/oracle-cards.json"
        )
    );
}
