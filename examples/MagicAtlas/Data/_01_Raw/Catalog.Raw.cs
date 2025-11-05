using Flowthru.Data;

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
}
