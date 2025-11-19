using System.Collections.Frozen;
using System.Text;

namespace MagicAST.Core.ManaSystem;

/// <summary>
/// Represents a complete mana cost for a spell or ability.
/// Includes both the specific mana symbols and the total converted mana cost.
/// </summary>
public class ManaCost
{
  /// <summary>
  /// Lookup table for mana symbols. Maps symbol text to (ManaSymbol, Amount).
  /// Initialized once and frozen for performance.
  /// </summary>
  private static readonly FrozenDictionary<string, (ManaSymbol Symbol, int Amount)> SymbolLookup =
    CreateSymbolLookup();

  /// <summary>
  /// Creates the symbol lookup table with all known mana symbols.
  /// </summary>
  private static FrozenDictionary<string, (ManaSymbol, int)> CreateSymbolLookup()
  {
    var lookup = new Dictionary<string, (ManaSymbol, int)>(StringComparer.OrdinalIgnoreCase);

    // Basic colors (CMC = 1)
    lookup["W"] = (ManaSymbol.White, 1);
    lookup["U"] = (ManaSymbol.Blue, 1);
    lookup["B"] = (ManaSymbol.Black, 1);
    lookup["R"] = (ManaSymbol.Red, 1);
    lookup["G"] = (ManaSymbol.Green, 1);
    lookup["C"] = (ManaSymbol.Colorless, 1);
    lookup["S"] = (ManaSymbol.Snow, 1);

    // Variables (CMC = 0)
    lookup["X"] = (ManaSymbol.X, 0);
    lookup["Y"] = (ManaSymbol.Y, 0);
    lookup["Z"] = (ManaSymbol.Z, 0);

    // Hybrid two-color (CMC = 1) - both orderings
    AddBidirectional(lookup, "W", "U", ManaSymbol.HybridWU, 1);
    AddBidirectional(lookup, "W", "B", ManaSymbol.HybridWB, 1);
    AddBidirectional(lookup, "U", "B", ManaSymbol.HybridUB, 1);
    AddBidirectional(lookup, "U", "R", ManaSymbol.HybridUR, 1);
    AddBidirectional(lookup, "B", "R", ManaSymbol.HybridBR, 1);
    AddBidirectional(lookup, "B", "G", ManaSymbol.HybridBG, 1);
    AddBidirectional(lookup, "R", "G", ManaSymbol.HybridRG, 1);
    AddBidirectional(lookup, "R", "W", ManaSymbol.HybridRW, 1);
    AddBidirectional(lookup, "G", "W", ManaSymbol.HybridGW, 1);
    AddBidirectional(lookup, "G", "U", ManaSymbol.HybridGU, 1);

    // Phyrexian single-color (CMC = 1)
    lookup["W/P"] = (ManaSymbol.PhyrexianW, 1);
    lookup["U/P"] = (ManaSymbol.PhyrexianU, 1);
    lookup["B/P"] = (ManaSymbol.PhyrexianB, 1);
    lookup["R/P"] = (ManaSymbol.PhyrexianR, 1);
    lookup["G/P"] = (ManaSymbol.PhyrexianG, 1);
    lookup["C/P"] = (ManaSymbol.PhyrexianC, 1);

    // Phyrexian hybrid (CMC = 1) - both orderings
    AddBidirectionalPhyrexian(lookup, "W", "U", ManaSymbol.PhyrexianHybridWU, 1);
    AddBidirectionalPhyrexian(lookup, "W", "B", ManaSymbol.PhyrexianHybridWB, 1);
    AddBidirectionalPhyrexian(lookup, "U", "B", ManaSymbol.PhyrexianHybridUB, 1);
    AddBidirectionalPhyrexian(lookup, "U", "R", ManaSymbol.PhyrexianHybridUR, 1);
    AddBidirectionalPhyrexian(lookup, "B", "R", ManaSymbol.PhyrexianHybridBR, 1);
    AddBidirectionalPhyrexian(lookup, "B", "G", ManaSymbol.PhyrexianHybridBG, 1);
    AddBidirectionalPhyrexian(lookup, "R", "G", ManaSymbol.PhyrexianHybridRG, 1);
    AddBidirectionalPhyrexian(lookup, "R", "W", ManaSymbol.PhyrexianHybridRW, 1);
    AddBidirectionalPhyrexian(lookup, "G", "W", ManaSymbol.PhyrexianHybridGW, 1);
    AddBidirectionalPhyrexian(lookup, "G", "U", ManaSymbol.PhyrexianHybridGU, 1);

    // Monocolored hybrid (CMC = 2)
    lookup["2/W"] = (ManaSymbol.MonoHybridW, 2);
    lookup["2/U"] = (ManaSymbol.MonoHybridU, 2);
    lookup["2/B"] = (ManaSymbol.MonoHybridB, 2);
    lookup["2/R"] = (ManaSymbol.MonoHybridR, 2);
    lookup["2/G"] = (ManaSymbol.MonoHybridG, 2);

    // Colorless hybrid (CMC = 1)
    lookup["C/W"] = (ManaSymbol.ColorlessHybridW, 1);
    lookup["C/U"] = (ManaSymbol.ColorlessHybridU, 1);
    lookup["C/B"] = (ManaSymbol.ColorlessHybridB, 1);
    lookup["C/R"] = (ManaSymbol.ColorlessHybridR, 1);
    lookup["C/G"] = (ManaSymbol.ColorlessHybridG, 1);

    // Special symbols
    lookup["H"] = (ManaSymbol.HalfColorless, 1);
    lookup["HW"] = (ManaSymbol.HalfWhite, 0);
    lookup["HR"] = (ManaSymbol.HalfRed, 0);
    lookup["L"] = (ManaSymbol.Legendary, 1);
    lookup["D"] = (ManaSymbol.LandDrop, 0);
    lookup["∞"] = (ManaSymbol.Infinite, int.MaxValue);
    lookup["½"] = (ManaSymbol.HalfGeneric, 0);

    return lookup.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
  }

  /// <summary>
  /// Helper to add hybrid symbols in both orderings (e.g., W/U and U/W).
  /// </summary>
  private static void AddBidirectional(
    Dictionary<string, (ManaSymbol, int)> lookup,
    string color1,
    string color2,
    ManaSymbol symbol,
    int amount
  )
  {
    lookup[$"{color1}/{color2}"] = (symbol, amount);
    lookup[$"{color2}/{color1}"] = (symbol, amount);
  }

  /// <summary>
  /// Helper to add Phyrexian hybrid symbols in both orderings (e.g., W/U/P and U/W/P).
  /// </summary>
  private static void AddBidirectionalPhyrexian(
    Dictionary<string, (ManaSymbol, int)> lookup,
    string color1,
    string color2,
    ManaSymbol symbol,
    int amount
  )
  {
    lookup[$"{color1}/{color2}/P"] = (symbol, amount);
    lookup[$"{color2}/{color1}/P"] = (symbol, amount);
  }

  /// <summary>
  /// The individual mana symbols in this cost, in order.
  /// </summary>
  public List<ManaSymbolInstance> Symbols { get; init; } = new();

  /// <summary>
  /// The converted mana cost (total cost ignoring color requirements).
  /// For costs with X, this is calculated without X.
  /// </summary>
  public int ConvertedManaCost { get; init; }

  /// <summary>
  /// Whether this cost contains X.
  /// </summary>
  public bool ContainsX => Symbols.Any(s => s.Symbol == ManaSymbol.X);

  /// <summary>
  /// Parses a mana cost string (e.g., "{1}{R}", "{2}{B}{B}", "{X}{G}") into a ManaCost object.
  /// For split cards with "//" separator, parses only the first face.
  /// </summary>
  /// <param name="costString">The mana cost string to parse.</param>
  /// <returns>A ManaCost object representing the parsed cost.</returns>
  /// <exception cref="ArgumentException">Thrown when the cost string is invalid.</exception>
  public static ManaCost Parse(string costString)
  {
    var symbols = new List<ManaSymbolInstance>();
    int cmc = 0;

    // Remove spaces
    costString = costString.Trim();

    if (string.IsNullOrEmpty(costString))
    {
      return new ManaCost { Symbols = symbols, ConvertedManaCost = 0 };
    }

    // Handle split cards - parse first face only
    // Example: "{X}{G} // {X}{R}{R}" -> "{X}{G}"
    if (costString.Contains(" // "))
    {
      var faces = costString.Split(" // ", StringSplitOptions.TrimEntries);
      costString = faces[0];
    }

    // Parse each symbol in {symbol} format
    int i = 0;
    while (i < costString.Length)
    {
      if (costString[i] == '{')
      {
        int closeBrace = costString.IndexOf('}', i);
        if (closeBrace == -1)
        {
          throw new ArgumentException($"Unclosed brace in mana cost: {costString}");
        }

        string symbolText = costString.Substring(i + 1, closeBrace - i - 1);
        var symbolInstance = ParseSymbol(symbolText);
        symbols.Add(symbolInstance);

        // Add to CMC
        cmc += symbolInstance.Amount;

        i = closeBrace + 1;
      }
      else if (char.IsWhiteSpace(costString[i]))
      {
        // Skip whitespace
        i++;
      }
      else
      {
        throw new ArgumentException($"Invalid character in mana cost: {costString[i]}");
      }
    }

    return new ManaCost { Symbols = symbols, ConvertedManaCost = cmc };
  }

  /// <summary>
  /// Parses a single mana symbol from its string representation.
  /// Uses a lookup table for O(1) performance.
  /// </summary>
  private static ManaSymbolInstance ParseSymbol(string symbolText)
  {
    var content = symbolText.Trim();

    // Try lookup table first
    if (SymbolLookup.TryGetValue(content, out var symbolData))
    {
      return new ManaSymbolInstance { Symbol = symbolData.Symbol, Amount = symbolData.Amount };
    }

    // Try to parse as generic numeric mana
    if (int.TryParse(content, out int amount))
    {
      return new ManaSymbolInstance { Symbol = ManaSymbol.Generic, Amount = amount };
    }

    throw new ArgumentException($"Unknown mana symbol: {symbolText}");
  }

  /// <summary>
  /// Returns the string representation of this mana cost in standard format.
  /// </summary>
  public override string ToString()
  {
    if (Symbols.Count == 0)
    {
      return "{0}";
    }

    var sb = new StringBuilder();
    foreach (var symbol in Symbols)
    {
      sb.Append('{');
      sb.Append(GetSymbolString(symbol));
      sb.Append('}');
    }
    return sb.ToString();
  }

  /// <summary>
  /// Gets the string representation of a mana symbol (without braces).
  /// </summary>
  private static string GetSymbolString(ManaSymbolInstance symbol)
  {
    return symbol.Symbol switch
    {
      // Basic colors
      ManaSymbol.White => "W",
      ManaSymbol.Blue => "U",
      ManaSymbol.Black => "B",
      ManaSymbol.Red => "R",
      ManaSymbol.Green => "G",
      ManaSymbol.Colorless => "C",
      ManaSymbol.Snow => "S",

      // Variables
      ManaSymbol.X => "X",
      ManaSymbol.Y => "Y",
      ManaSymbol.Z => "Z",

      // Generic
      ManaSymbol.Generic => symbol.Amount.ToString(),

      // Hybrid two-color
      ManaSymbol.HybridWU => "W/U",
      ManaSymbol.HybridWB => "W/B",
      ManaSymbol.HybridUB => "U/B",
      ManaSymbol.HybridUR => "U/R",
      ManaSymbol.HybridBR => "B/R",
      ManaSymbol.HybridBG => "B/G",
      ManaSymbol.HybridRG => "R/G",
      ManaSymbol.HybridRW => "R/W",
      ManaSymbol.HybridGW => "G/W",
      ManaSymbol.HybridGU => "G/U",

      // Phyrexian single-color
      ManaSymbol.PhyrexianW => "W/P",
      ManaSymbol.PhyrexianU => "U/P",
      ManaSymbol.PhyrexianB => "B/P",
      ManaSymbol.PhyrexianR => "R/P",
      ManaSymbol.PhyrexianG => "G/P",
      ManaSymbol.PhyrexianC => "C/P",

      // Phyrexian hybrid
      ManaSymbol.PhyrexianHybridWU => "W/U/P",
      ManaSymbol.PhyrexianHybridWB => "W/B/P",
      ManaSymbol.PhyrexianHybridUB => "U/B/P",
      ManaSymbol.PhyrexianHybridUR => "U/R/P",
      ManaSymbol.PhyrexianHybridBR => "B/R/P",
      ManaSymbol.PhyrexianHybridBG => "B/G/P",
      ManaSymbol.PhyrexianHybridRG => "R/G/P",
      ManaSymbol.PhyrexianHybridRW => "R/W/P",
      ManaSymbol.PhyrexianHybridGW => "G/W/P",
      ManaSymbol.PhyrexianHybridGU => "G/U/P",

      // Monocolored hybrid
      ManaSymbol.MonoHybridW => "2/W",
      ManaSymbol.MonoHybridU => "2/U",
      ManaSymbol.MonoHybridB => "2/B",
      ManaSymbol.MonoHybridR => "2/R",
      ManaSymbol.MonoHybridG => "2/G",

      // Colorless hybrid
      ManaSymbol.ColorlessHybridW => "C/W",
      ManaSymbol.ColorlessHybridU => "C/U",
      ManaSymbol.ColorlessHybridB => "C/B",
      ManaSymbol.ColorlessHybridR => "C/R",
      ManaSymbol.ColorlessHybridG => "C/G",

      // Special
      ManaSymbol.HalfColorless => "H",
      ManaSymbol.HalfWhite => "HW",
      ManaSymbol.HalfRed => "HR",
      ManaSymbol.Legendary => "L",
      ManaSymbol.LandDrop => "D",
      ManaSymbol.Infinite => "∞",
      ManaSymbol.HalfGeneric => "½",

      _ => "?",
    };
  }
}

/// <summary>
/// Represents a single mana symbol with its amount.
/// For colored mana, amount is always 1.
/// For generic mana, amount is the number value.
/// </summary>
public class ManaSymbolInstance
{
  /// <summary>
  /// The type of mana symbol.
  /// </summary>
  public required ManaSymbol Symbol { get; init; }

  /// <summary>
  /// The amount of mana. For colored mana this is 1.
  /// For generic mana this is the numeric value.
  /// For X this is 0 (determined at cast time).
  /// </summary>
  public required int Amount { get; init; }
}
