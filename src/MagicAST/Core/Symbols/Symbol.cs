namespace MagicAST.Core.Symbols;

/// <summary>
/// Represents a parsed Magic: The Gathering symbol.
/// Symbols can appear in mana costs, oracle text, or other game contexts.
/// </summary>
public abstract class Symbol
{
  /// <summary>
  /// The original text representation of the symbol (e.g., "{T}", "{W/U}", "{2}").
  /// </summary>
  public required string SymbolText { get; set; }

  /// <summary>
  /// The category of this symbol.
  /// </summary>
  public abstract SymbolCategory Category { get; }

  /// <summary>
  /// Human-readable English description of what this symbol represents.
  /// </summary>
  public abstract string EnglishDescription { get; }

  /// <summary>
  /// Whether this symbol represents mana.
  /// </summary>
  public virtual bool RepresentsMana => false;

  /// <summary>
  /// Whether this symbol can appear in mana costs.
  /// </summary>
  public virtual bool AppearsInManaCosts => false;

  /// <summary>
  /// The mana value contributed by this symbol to converted mana cost.
  /// </summary>
  public virtual decimal ManaValue => 0;

  /// <summary>
  /// Whether this is a hybrid symbol (can be paid with multiple options).
  /// </summary>
  public virtual bool IsHybrid => false;

  /// <summary>
  /// Whether this is a Phyrexian symbol (can be paid with life).
  /// </summary>
  public virtual bool IsPhyrexian => false;
}

/// <summary>
/// Represents a mana symbol.
/// </summary>
public class ManaSymbol : Symbol
{
  /// <summary>
  /// The specific type of mana symbol.
  /// </summary>
  public required ManaSymbolType SymbolType { get; init; }

  /// <summary>
  /// For generic mana, the numeric amount (e.g., 2 for "{2}").
  /// For colored/hybrid mana, always 1.
  /// For variables (X, Y, Z), 0 (determined at cast time).
  /// </summary>
  public required decimal Amount { get; init; }

  public override SymbolCategory Category => SymbolCategory.Mana;

  public override bool RepresentsMana => true;

  public override bool AppearsInManaCosts => true;

  public override decimal ManaValue => Amount;

  public override bool IsHybrid =>
    SymbolType switch
    {
      ManaSymbolType.HybridWU
      or ManaSymbolType.HybridWB
      or ManaSymbolType.HybridUB
      or ManaSymbolType.HybridUR
      or ManaSymbolType.HybridBR
      or ManaSymbolType.HybridBG
      or ManaSymbolType.HybridRG
      or ManaSymbolType.HybridRW
      or ManaSymbolType.HybridGW
      or ManaSymbolType.HybridGU
      or ManaSymbolType.PhyrexianHybridWU
      or ManaSymbolType.PhyrexianHybridWB
      or ManaSymbolType.PhyrexianHybridUB
      or ManaSymbolType.PhyrexianHybridUR
      or ManaSymbolType.PhyrexianHybridBR
      or ManaSymbolType.PhyrexianHybridBG
      or ManaSymbolType.PhyrexianHybridRG
      or ManaSymbolType.PhyrexianHybridRW
      or ManaSymbolType.PhyrexianHybridGW
      or ManaSymbolType.PhyrexianHybridGU
      or ManaSymbolType.MonoHybridW
      or ManaSymbolType.MonoHybridU
      or ManaSymbolType.MonoHybridB
      or ManaSymbolType.MonoHybridR
      or ManaSymbolType.MonoHybridG
      or ManaSymbolType.ColorlessHybridW
      or ManaSymbolType.ColorlessHybridU
      or ManaSymbolType.ColorlessHybridB
      or ManaSymbolType.ColorlessHybridR
      or ManaSymbolType.ColorlessHybridG => true,
      _ => false,
    };

  public override bool IsPhyrexian =>
    SymbolType switch
    {
      ManaSymbolType.PhyrexianW
      or ManaSymbolType.PhyrexianU
      or ManaSymbolType.PhyrexianB
      or ManaSymbolType.PhyrexianR
      or ManaSymbolType.PhyrexianG
      or ManaSymbolType.PhyrexianC
      or ManaSymbolType.PhyrexianHybridWU
      or ManaSymbolType.PhyrexianHybridWB
      or ManaSymbolType.PhyrexianHybridUB
      or ManaSymbolType.PhyrexianHybridUR
      or ManaSymbolType.PhyrexianHybridBR
      or ManaSymbolType.PhyrexianHybridBG
      or ManaSymbolType.PhyrexianHybridRG
      or ManaSymbolType.PhyrexianHybridRW
      or ManaSymbolType.PhyrexianHybridGW
      or ManaSymbolType.PhyrexianHybridGU => true,
      _ => false,
    };

  public override string EnglishDescription =>
    SymbolType switch
    {
      // Basic colors
      ManaSymbolType.White => "one white mana",
      ManaSymbolType.Blue => "one blue mana",
      ManaSymbolType.Black => "one black mana",
      ManaSymbolType.Red => "one red mana",
      ManaSymbolType.Green => "one green mana",
      ManaSymbolType.Colorless => "one colorless mana",
      ManaSymbolType.Snow => "one snow mana",

      // Generic
      ManaSymbolType.Generic when Amount == 0 => "zero mana",
      ManaSymbolType.Generic when Amount == 0.5m => "one-half generic mana",
      ManaSymbolType.Generic when Amount == 1 => "one generic mana",
      ManaSymbolType.Generic => $"{FormatAmount(Amount)} generic mana",

      // Variables
      ManaSymbolType.X => "X generic mana",
      ManaSymbolType.Y => "Y generic mana",
      ManaSymbolType.Z => "Z generic mana",

      // Hybrid two-color
      ManaSymbolType.HybridWU => "one white or blue mana",
      ManaSymbolType.HybridWB => "one white or black mana",
      ManaSymbolType.HybridUB => "one blue or black mana",
      ManaSymbolType.HybridUR => "one blue or red mana",
      ManaSymbolType.HybridBR => "one black or red mana",
      ManaSymbolType.HybridBG => "one black or green mana",
      ManaSymbolType.HybridRG => "one red or green mana",
      ManaSymbolType.HybridRW => "one red or white mana",
      ManaSymbolType.HybridGW => "one green or white mana",
      ManaSymbolType.HybridGU => "one green or blue mana",

      // Phyrexian single-color
      ManaSymbolType.PhyrexianW => "one white mana or two life",
      ManaSymbolType.PhyrexianU => "one blue mana or two life",
      ManaSymbolType.PhyrexianB => "one black mana or two life",
      ManaSymbolType.PhyrexianR => "one red mana or two life",
      ManaSymbolType.PhyrexianG => "one green mana or two life",
      ManaSymbolType.PhyrexianC => "one colorless mana or two life",

      // Phyrexian hybrid
      ManaSymbolType.PhyrexianHybridWU => "one white mana, one blue mana, or 2 life",
      ManaSymbolType.PhyrexianHybridWB => "one white mana, one black mana, or 2 life",
      ManaSymbolType.PhyrexianHybridUB => "one blue mana, one black mana, or 2 life",
      ManaSymbolType.PhyrexianHybridUR => "one blue mana, one red mana, or 2 life",
      ManaSymbolType.PhyrexianHybridBR => "one black mana, one red mana, or 2 life",
      ManaSymbolType.PhyrexianHybridBG => "one black mana, one green mana, or 2 life",
      ManaSymbolType.PhyrexianHybridRG => "one red mana, one green mana, or 2 life",
      ManaSymbolType.PhyrexianHybridRW => "one red mana, one white mana, or 2 life",
      ManaSymbolType.PhyrexianHybridGW => "one green mana, one white mana, or 2 life",
      ManaSymbolType.PhyrexianHybridGU => "one green mana, one blue mana, or 2 life",

      // Monocolored hybrid
      ManaSymbolType.MonoHybridW => "two generic mana or one white mana",
      ManaSymbolType.MonoHybridU => "two generic mana or one blue mana",
      ManaSymbolType.MonoHybridB => "two generic mana or one black mana",
      ManaSymbolType.MonoHybridR => "two generic mana or one red mana",
      ManaSymbolType.MonoHybridG => "two generic mana or one green mana",

      // Colorless hybrid
      ManaSymbolType.ColorlessHybridW => "one colorless mana or one white mana",
      ManaSymbolType.ColorlessHybridU => "one colorless mana or one blue mana",
      ManaSymbolType.ColorlessHybridB => "one colorless mana or one black mana",
      ManaSymbolType.ColorlessHybridR => "one colorless mana or one red mana",
      ManaSymbolType.ColorlessHybridG => "one colorless mana or one green mana",

      // Special
      ManaSymbolType.HalfColorless => "one colored mana or two life",
      ManaSymbolType.HalfWhite => "one-half white mana",
      ManaSymbolType.HalfRed => "one-half red mana",
      ManaSymbolType.Legendary => "one mana from a legendary source",
      ManaSymbolType.LandDrop => "one potential land drop",
      ManaSymbolType.Infinite => "infinite generic mana",
      ManaSymbolType.HalfGeneric => "one-half generic mana",

      _ => $"unknown mana symbol: {SymbolText}",
    };

  private static string FormatAmount(decimal amount)
  {
    return amount switch
    {
      2 => "two",
      3 => "three",
      4 => "four",
      5 => "five",
      6 => "six",
      7 => "seven",
      8 => "eight",
      9 => "nine",
      10 => "ten",
      11 => "eleven",
      12 => "twelve",
      13 => "thirteen",
      14 => "fourteen",
      15 => "fifteen",
      16 => "sixteen",
      17 => "seventeen",
      18 => "eighteen",
      19 => "nineteen",
      20 => "twenty",
      100 => "one hundred",
      1000000 => "one million",
      _ => amount.ToString(),
    };
  }
}

/// <summary>
/// Represents a tap or untap symbol.
/// </summary>
public class TapUntapSymbol : Symbol
{
  /// <summary>
  /// The specific type of tap/untap symbol.
  /// </summary>
  public required AbilitySymbolType SymbolType { get; init; }

  public override SymbolCategory Category => SymbolCategory.TapUntap;

  public override string EnglishDescription =>
    SymbolType switch
    {
      AbilitySymbolType.Tap => "tap this permanent",
      AbilitySymbolType.Untap => "untap this permanent",
      _ => $"unknown tap/untap symbol: {SymbolText}",
    };
}

/// <summary>
/// Represents a counter symbol.
/// </summary>
public class CounterSymbol : Symbol
{
  /// <summary>
  /// The specific type of counter symbol.
  /// </summary>
  public required AbilitySymbolType SymbolType { get; init; }

  public override SymbolCategory Category => SymbolCategory.Counter;

  public override string EnglishDescription =>
    SymbolType switch
    {
      AbilitySymbolType.Energy => "an energy counter",
      AbilitySymbolType.Acorn => "an acorn counter",
      AbilitySymbolType.Ticket => "a ticket counter",
      _ => $"unknown counter symbol: {SymbolText}",
    };
}

/// <summary>
/// Represents a card type or special game symbol.
/// </summary>
public class SpecialSymbol : Symbol
{
  /// <summary>
  /// The specific type of special symbol.
  /// </summary>
  public required AbilitySymbolType SymbolType { get; init; }

  public override SymbolCategory Category => SymbolCategory.Special;

  public override string EnglishDescription =>
    SymbolType switch
    {
      AbilitySymbolType.Planeswalker => "planeswalker",
      AbilitySymbolType.Chaos => "chaos",
      AbilitySymbolType.Pawprint => "modal budget pawprint",
      _ => $"unknown special symbol: {SymbolText}",
    };
}
