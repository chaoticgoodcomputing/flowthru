namespace MagicAST.Core.Symbols;

/// <summary>
/// Categorizes symbols by their semantic role in Magic: The Gathering.
/// </summary>
public enum SymbolCategory
{
  /// <summary>
  /// Mana symbols that can appear in costs or effects.
  /// </summary>
  Mana,

  /// <summary>
  /// Tap/untap symbols for abilities.
  /// </summary>
  TapUntap,

  /// <summary>
  /// Counter type symbols (energy, acorn, ticket).
  /// </summary>
  Counter,

  /// <summary>
  /// Card type symbols (planeswalker, chaos).
  /// </summary>
  CardType,

  /// <summary>
  /// Special symbols (half mana, legendary source, land drop).
  /// </summary>
  Special,

  /// <summary>
  /// Unknown or unrecognized symbol.
  /// </summary>
  Unknown,
}

/// <summary>
/// Represents the type of mana symbol.
/// </summary>
public enum ManaSymbolType
{
  // Basic colored mana
  White,
  Blue,
  Black,
  Red,
  Green,

  // Colorless and special mana
  Colorless,
  Snow,

  // Generic mana (numbers)
  Generic,

  // Variables
  X,
  Y,
  Z,

  // Hybrid mana (two-color)
  HybridWU,
  HybridWB,
  HybridUB,
  HybridUR,
  HybridBR,
  HybridBG,
  HybridRG,
  HybridRW,
  HybridGW,
  HybridGU,

  // Phyrexian mana (single color)
  PhyrexianW,
  PhyrexianU,
  PhyrexianB,
  PhyrexianR,
  PhyrexianG,
  PhyrexianC,

  // Phyrexian hybrid (two-color)
  PhyrexianHybridWU,
  PhyrexianHybridWB,
  PhyrexianHybridUB,
  PhyrexianHybridUR,
  PhyrexianHybridBR,
  PhyrexianHybridBG,
  PhyrexianHybridRG,
  PhyrexianHybridRW,
  PhyrexianHybridGW,
  PhyrexianHybridGU,

  // Monocolored hybrid (2/Color)
  MonoHybridW,
  MonoHybridU,
  MonoHybridB,
  MonoHybridR,
  MonoHybridG,

  // Colorless hybrid
  ColorlessHybridW,
  ColorlessHybridU,
  ColorlessHybridB,
  ColorlessHybridR,
  ColorlessHybridG,

  // Special mana symbols
  HalfColorless, // {H} - one colored mana or two life
  HalfWhite, // {HW}
  HalfRed, // {HR}
  Legendary, // {L} - from a legendary source
  LandDrop, // {D} - potential land drop

  // Infinite/unusual
  Infinite, // {∞}
  HalfGeneric // {½}
  ,
}

/// <summary>
/// Represents non-mana symbols that appear in oracle text.
/// </summary>
public enum AbilitySymbolType
{
  /// <summary>
  /// {T} - Tap this permanent.
  /// </summary>
  Tap,

  /// <summary>
  /// {Q} - Untap this permanent.
  /// </summary>
  Untap,

  /// <summary>
  /// {E} - Energy counter.
  /// </summary>
  Energy,

  /// <summary>
  /// {A} - Acorn counter.
  /// </summary>
  Acorn,

  /// <summary>
  /// {TK} - Ticket counter.
  /// </summary>
  Ticket,

  /// <summary>
  /// {P} - Modal budget pawprint.
  /// </summary>
  Pawprint,

  /// <summary>
  /// {PW} - Planeswalker.
  /// </summary>
  Planeswalker,

  /// <summary>
  /// {CHAOS} - Chaos symbol (planechase).
  /// </summary>
  Chaos,
}
