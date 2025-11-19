using System.Text;

namespace MagicAST.Core.ManaSystem;

/// <summary>
/// Represents a complete mana cost for a spell or ability.
/// Includes both the specific mana symbols and the total converted mana cost.
/// </summary>
public class ManaCost
{
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
  /// </summary>
  /// <param name="costString">The mana cost string to parse.</param>
  /// <returns>A ManaCost object representing the parsed cost.</returns>
  /// <exception cref="ArgumentException">Thrown when the cost string is invalid.</exception>
  public static ManaCost Parse(string costString)
  {
    var symbols = new List<ManaSymbolInstance>();
    int cmc = 0;

    // Remove spaces and split by braces
    costString = costString.Trim();

    if (string.IsNullOrEmpty(costString))
    {
      return new ManaCost { Symbols = symbols, ConvertedManaCost = 0 };
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

        // Add to CMC (X doesn't count)
        if (symbolInstance.Symbol == ManaSymbol.Generic)
        {
          cmc += symbolInstance.Amount;
        }
        else if (symbolInstance.Symbol != ManaSymbol.X)
        {
          cmc += 1;
        }

        i = closeBrace + 1;
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
  /// </summary>
  private static ManaSymbolInstance ParseSymbol(string symbolText)
  {
    return symbolText.ToUpper() switch
    {
      "W" => new ManaSymbolInstance { Symbol = ManaSymbol.White, Amount = 1 },
      "U" => new ManaSymbolInstance { Symbol = ManaSymbol.Blue, Amount = 1 },
      "B" => new ManaSymbolInstance { Symbol = ManaSymbol.Black, Amount = 1 },
      "R" => new ManaSymbolInstance { Symbol = ManaSymbol.Red, Amount = 1 },
      "G" => new ManaSymbolInstance { Symbol = ManaSymbol.Green, Amount = 1 },
      "C" => new ManaSymbolInstance { Symbol = ManaSymbol.Colorless, Amount = 1 },
      "X" => new ManaSymbolInstance { Symbol = ManaSymbol.X, Amount = 0 },
      _ when int.TryParse(symbolText, out int amount) => new ManaSymbolInstance
      {
        Symbol = ManaSymbol.Generic,
        Amount = amount,
      },
      _ => throw new ArgumentException($"Unknown mana symbol: {symbolText}"),
    };
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
      sb.Append(
        symbol.Symbol switch
        {
          ManaSymbol.White => "W",
          ManaSymbol.Blue => "U",
          ManaSymbol.Black => "B",
          ManaSymbol.Red => "R",
          ManaSymbol.Green => "G",
          ManaSymbol.Colorless => "C",
          ManaSymbol.X => "X",
          ManaSymbol.Generic => symbol.Amount.ToString(),
          _ => "?",
        }
      );
      sb.Append('}');
    }
    return sb.ToString();
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
