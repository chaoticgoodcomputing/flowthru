using Flowthru.Abstractions;
using MagicAtlas.Data.Enums.Card;

namespace MagicAtlas.Data._02_Intermediate.Schemas;

/// <summary>
/// Processed card symbol with strong types.
/// </summary>
public record CardSymbol : IStructuredSerializable
{
  public string Symbol { get; init; } = "";
  public string? LooseVariant { get; init; }
  public string English { get; init; } = "";
  public string? SvgUri { get; init; }
  public bool Transposable { get; init; }
  public bool RepresentsMana { get; init; }
  public bool AppearsInManaCosts { get; init; }
  public decimal? ManaValue { get; init; }
  public bool Hybrid { get; init; }
  public bool Phyrexian { get; init; }
  public decimal Cmc { get; init; } // Defaults to 0 in processing
  public bool Funny { get; init; }
  public List<ManaColor>? Colors { get; init; }
  public List<string>? GathererAlternates { get; init; }
}

/// <summary>
/// Collection of card symbols.
/// </summary>
public record CardSymbolDictionary : IStructuredSerializable
{
  public Dictionary<string, CardSymbol> Symbols { get; init; } = new();
}
