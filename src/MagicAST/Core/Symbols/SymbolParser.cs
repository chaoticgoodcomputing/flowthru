using System.Text.RegularExpressions;

namespace MagicAST.Core.Symbols;

/// <summary>
/// Parses Magic: The Gathering symbols from their text representation.
/// Handles both mana symbols (for costs) and ability symbols (for oracle text).
/// </summary>
public static partial class SymbolParser
{
  // Regex to match symbols in curly braces: {T}, {2/W}, {B/G/P}, etc.
  [GeneratedRegex(@"\{([^}]+)\}", RegexOptions.Compiled)]
  private static partial Regex SymbolPattern();

  /// <summary>
  /// Parses all symbols found in the given text.
  /// </summary>
  /// <param name="text">Text containing symbols in {symbol} format.</param>
  /// <returns>List of parsed symbols in order of appearance.</returns>
  public static List<Symbol> ParseSymbols(string text)
  {
    var symbols = new List<Symbol>();
    var matches = SymbolPattern().Matches(text);

    foreach (Match match in matches)
    {
      var symbolText = match.Value; // Full text including braces
      var symbolContent = match.Groups[1].Value; // Content without braces

      try
      {
        var symbol = ParseSymbol(symbolContent);
        symbol.SymbolText = symbolText;
        symbols.Add(symbol);
      }
      catch (ArgumentException)
      {
        // Unknown symbol - skip or log warning
        continue;
      }
    }

    return symbols;
  }

  /// <summary>
  /// Parses a single symbol from its content (without braces).
  /// </summary>
  /// <param name="symbolContent">Symbol content, e.g., "T", "2/W", "B/G/P"</param>
  /// <returns>Parsed Symbol object.</returns>
  /// <exception cref="ArgumentException">Thrown if symbol is not recognized.</exception>
  public static Symbol ParseSymbol(string symbolContent)
  {
    var content = symbolContent.Trim().ToUpper();

    // Try to parse as ability symbol first (tap, untap, counters, etc.)
    if (TryParseAbilitySymbol(content, out var abilitySymbol))
    {
      return abilitySymbol!;
    }

    // Try to parse as mana symbol
    if (TryParseManaSymbol(content, out var manaSymbol))
    {
      return manaSymbol!;
    }

    throw new ArgumentException($"Unknown symbol: {symbolContent}");
  }

  /// <summary>
  /// Attempts to parse an ability symbol (tap, untap, counters, special).
  /// </summary>
  private static bool TryParseAbilitySymbol(string content, out Symbol? symbol)
  {
    symbol = null;

    switch (content)
    {
      // Tap/Untap
      case "T":
        symbol = new TapUntapSymbol
        {
          SymbolText = $"{{{content}}}",
          SymbolType = AbilitySymbolType.Tap,
        };
        return true;

      case "Q":
        symbol = new TapUntapSymbol
        {
          SymbolText = $"{{{content}}}",
          SymbolType = AbilitySymbolType.Untap,
        };
        return true;

      // Counters
      case "E":
        symbol = new CounterSymbol
        {
          SymbolText = $"{{{content}}}",
          SymbolType = AbilitySymbolType.Energy,
        };
        return true;

      case "A":
        symbol = new CounterSymbol
        {
          SymbolText = $"{{{content}}}",
          SymbolType = AbilitySymbolType.Acorn,
        };
        return true;

      case "TK":
        symbol = new CounterSymbol
        {
          SymbolText = $"{{{content}}}",
          SymbolType = AbilitySymbolType.Ticket,
        };
        return true;

      // Special
      case "P":
        symbol = new SpecialSymbol
        {
          SymbolText = $"{{{content}}}",
          SymbolType = AbilitySymbolType.Pawprint,
        };
        return true;

      case "PW":
        symbol = new SpecialSymbol
        {
          SymbolText = $"{{{content}}}",
          SymbolType = AbilitySymbolType.Planeswalker,
        };
        return true;

      case "CHAOS":
        symbol = new SpecialSymbol
        {
          SymbolText = $"{{{content}}}",
          SymbolType = AbilitySymbolType.Chaos,
        };
        return true;

      default:
        return false;
    }
  }

  /// <summary>
  /// Attempts to parse a mana symbol.
  /// </summary>
  private static bool TryParseManaSymbol(string content, out Symbol? symbol)
  {
    symbol = null;

    // Basic single-color mana
    switch (content)
    {
      case "W":
        symbol = CreateManaSymbol(content, ManaSymbolType.White, 1);
        return true;
      case "U":
        symbol = CreateManaSymbol(content, ManaSymbolType.Blue, 1);
        return true;
      case "B":
        symbol = CreateManaSymbol(content, ManaSymbolType.Black, 1);
        return true;
      case "R":
        symbol = CreateManaSymbol(content, ManaSymbolType.Red, 1);
        return true;
      case "G":
        symbol = CreateManaSymbol(content, ManaSymbolType.Green, 1);
        return true;
      case "C":
        symbol = CreateManaSymbol(content, ManaSymbolType.Colorless, 1);
        return true;
      case "S":
        symbol = CreateManaSymbol(content, ManaSymbolType.Snow, 1);
        return true;
    }

    // Variables
    switch (content)
    {
      case "X":
        symbol = CreateManaSymbol(content, ManaSymbolType.X, 0);
        return true;
      case "Y":
        symbol = CreateManaSymbol(content, ManaSymbolType.Y, 0);
        return true;
      case "Z":
        symbol = CreateManaSymbol(content, ManaSymbolType.Z, 0);
        return true;
    }

    // Hybrid two-color mana
    switch (content)
    {
      case "W/U"
      or "U/W":
        symbol = CreateManaSymbol(content, ManaSymbolType.HybridWU, 1);
        return true;
      case "W/B"
      or "B/W":
        symbol = CreateManaSymbol(content, ManaSymbolType.HybridWB, 1);
        return true;
      case "U/B"
      or "B/U":
        symbol = CreateManaSymbol(content, ManaSymbolType.HybridUB, 1);
        return true;
      case "U/R"
      or "R/U":
        symbol = CreateManaSymbol(content, ManaSymbolType.HybridUR, 1);
        return true;
      case "B/R"
      or "R/B":
        symbol = CreateManaSymbol(content, ManaSymbolType.HybridBR, 1);
        return true;
      case "B/G"
      or "G/B":
        symbol = CreateManaSymbol(content, ManaSymbolType.HybridBG, 1);
        return true;
      case "R/G"
      or "G/R":
        symbol = CreateManaSymbol(content, ManaSymbolType.HybridRG, 1);
        return true;
      case "R/W"
      or "W/R":
        symbol = CreateManaSymbol(content, ManaSymbolType.HybridRW, 1);
        return true;
      case "G/W"
      or "W/G":
        symbol = CreateManaSymbol(content, ManaSymbolType.HybridGW, 1);
        return true;
      case "G/U"
      or "U/G":
        symbol = CreateManaSymbol(content, ManaSymbolType.HybridGU, 1);
        return true;
    }

    // Phyrexian single-color
    switch (content)
    {
      case "W/P":
        symbol = CreateManaSymbol(content, ManaSymbolType.PhyrexianW, 1);
        return true;
      case "U/P":
        symbol = CreateManaSymbol(content, ManaSymbolType.PhyrexianU, 1);
        return true;
      case "B/P":
        symbol = CreateManaSymbol(content, ManaSymbolType.PhyrexianB, 1);
        return true;
      case "R/P":
        symbol = CreateManaSymbol(content, ManaSymbolType.PhyrexianR, 1);
        return true;
      case "G/P":
        symbol = CreateManaSymbol(content, ManaSymbolType.PhyrexianG, 1);
        return true;
      case "C/P":
        symbol = CreateManaSymbol(content, ManaSymbolType.PhyrexianC, 1);
        return true;
    }

    // Phyrexian hybrid (two-color)
    switch (content)
    {
      case "W/U/P"
      or "U/W/P":
        symbol = CreateManaSymbol(content, ManaSymbolType.PhyrexianHybridWU, 1);
        return true;
      case "W/B/P"
      or "B/W/P":
        symbol = CreateManaSymbol(content, ManaSymbolType.PhyrexianHybridWB, 1);
        return true;
      case "U/B/P"
      or "B/U/P":
        symbol = CreateManaSymbol(content, ManaSymbolType.PhyrexianHybridUB, 1);
        return true;
      case "U/R/P"
      or "R/U/P":
        symbol = CreateManaSymbol(content, ManaSymbolType.PhyrexianHybridUR, 1);
        return true;
      case "B/R/P"
      or "R/B/P":
        symbol = CreateManaSymbol(content, ManaSymbolType.PhyrexianHybridBR, 1);
        return true;
      case "B/G/P"
      or "G/B/P":
        symbol = CreateManaSymbol(content, ManaSymbolType.PhyrexianHybridBG, 1);
        return true;
      case "R/G/P"
      or "G/R/P":
        symbol = CreateManaSymbol(content, ManaSymbolType.PhyrexianHybridRG, 1);
        return true;
      case "R/W/P"
      or "W/R/P":
        symbol = CreateManaSymbol(content, ManaSymbolType.PhyrexianHybridRW, 1);
        return true;
      case "G/W/P"
      or "W/G/P":
        symbol = CreateManaSymbol(content, ManaSymbolType.PhyrexianHybridGW, 1);
        return true;
      case "G/U/P"
      or "U/G/P":
        symbol = CreateManaSymbol(content, ManaSymbolType.PhyrexianHybridGU, 1);
        return true;
    }

    // Monocolored hybrid
    switch (content)
    {
      case "2/W":
        symbol = CreateManaSymbol(content, ManaSymbolType.MonoHybridW, 2);
        return true;
      case "2/U":
        symbol = CreateManaSymbol(content, ManaSymbolType.MonoHybridU, 2);
        return true;
      case "2/B":
        symbol = CreateManaSymbol(content, ManaSymbolType.MonoHybridB, 2);
        return true;
      case "2/R":
        symbol = CreateManaSymbol(content, ManaSymbolType.MonoHybridR, 2);
        return true;
      case "2/G":
        symbol = CreateManaSymbol(content, ManaSymbolType.MonoHybridG, 2);
        return true;
    }

    // Colorless hybrid
    switch (content)
    {
      case "C/W":
        symbol = CreateManaSymbol(content, ManaSymbolType.ColorlessHybridW, 1);
        return true;
      case "C/U":
        symbol = CreateManaSymbol(content, ManaSymbolType.ColorlessHybridU, 1);
        return true;
      case "C/B":
        symbol = CreateManaSymbol(content, ManaSymbolType.ColorlessHybridB, 1);
        return true;
      case "C/R":
        symbol = CreateManaSymbol(content, ManaSymbolType.ColorlessHybridR, 1);
        return true;
      case "C/G":
        symbol = CreateManaSymbol(content, ManaSymbolType.ColorlessHybridG, 1);
        return true;
    }

    // Special symbols
    switch (content)
    {
      case "H":
        symbol = CreateManaSymbol(content, ManaSymbolType.HalfColorless, 1);
        return true;
      case "HW":
        symbol = CreateManaSymbol(content, ManaSymbolType.HalfWhite, 0.5m);
        return true;
      case "HR":
        symbol = CreateManaSymbol(content, ManaSymbolType.HalfRed, 0.5m);
        return true;
      case "L":
        symbol = CreateManaSymbol(content, ManaSymbolType.Legendary, 1);
        return true;
      case "D":
        symbol = CreateManaSymbol(content, ManaSymbolType.LandDrop, 0);
        return true;
      case "∞":
        symbol = CreateManaSymbol(content, ManaSymbolType.Infinite, decimal.MaxValue);
        return true;
      case "½":
        symbol = CreateManaSymbol(content, ManaSymbolType.HalfGeneric, 0.5m);
        return true;
    }

    // Try to parse as generic numeric mana
    if (int.TryParse(content, out int amount))
    {
      symbol = CreateManaSymbol(content, ManaSymbolType.Generic, amount);
      return true;
    }

    return false;
  }

  /// <summary>
  /// Helper to create a ManaSymbol instance.
  /// </summary>
  private static ManaSymbol CreateManaSymbol(string content, ManaSymbolType type, decimal amount)
  {
    return new ManaSymbol
    {
      SymbolText = $"{{{content}}}",
      SymbolType = type,
      Amount = amount,
    };
  }

  /// <summary>
  /// Replaces all symbols in text with their English descriptions.
  /// </summary>
  /// <param name="text">Text containing symbols.</param>
  /// <returns>Text with symbols replaced by English descriptions.</returns>
  public static string ReplaceSymbolsWithEnglish(string text)
  {
    return SymbolPattern()
      .Replace(
        text,
        match =>
        {
          var symbolContent = match.Groups[1].Value;
          try
          {
            var symbol = ParseSymbol(symbolContent);
            return symbol.EnglishDescription;
          }
          catch (ArgumentException)
          {
            // Keep original if unrecognized
            return match.Value;
          }
        }
      );
  }
}
