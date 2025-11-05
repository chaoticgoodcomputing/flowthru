using Flowthru.Data;

namespace MagicAtlas.Data;

public partial class Catalog
{
  /// <summary>
  /// Introduction section.
  /// </summary>
  public ICatalogEntry<string> Intro =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Single.Text(
          label: "Intro",
          filePath: $"{_basePath}/_02_Sections/Datasets/intro.txt"
        )
    );

  /// <summary>
  /// Table of contents section.
  /// </summary>
  public ICatalogEntry<string> TableOfContents =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Single.Text(
          label: "TableOfContents",
          filePath: $"{_basePath}/_02_Sections/Datasets/toc.txt"
        )
    );

  /// <summary>
  /// Rules section (numbered rules only).
  /// </summary>
  public ICatalogEntry<string> RulesText =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Single.Text(
          label: "RulesText",
          filePath: $"{_basePath}/_02_Sections/Datasets/rules.txt"
        )
    );

  /// <summary>
  /// Glossary section.
  /// </summary>
  public ICatalogEntry<string> GlossaryText =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Single.Text(
          label: "GlossaryText",
          filePath: $"{_basePath}/_02_Sections/Datasets/glossary.txt"
        )
    );

  /// <summary>
  /// Credits section.
  /// </summary>
  public ICatalogEntry<string> Credits =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Single.Text(
          label: "Credits",
          filePath: $"{_basePath}/_02_Sections/Datasets/credits.txt"
        )
    );
}
