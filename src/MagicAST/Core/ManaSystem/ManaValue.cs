namespace MagicAST.Core.ManaSystem;

/// <summary>
/// Represents an amount of mana produced or available.
/// Tracks specific quantities of each mana type.
/// </summary>
public class ManaValue
{
  /// <summary>
  /// Amount of white mana.
  /// </summary>
  public int White { get; init; }

  /// <summary>
  /// Amount of blue mana.
  /// </summary>
  public int Blue { get; init; }

  /// <summary>
  /// Amount of black mana.
  /// </summary>
  public int Black { get; init; }

  /// <summary>
  /// Amount of red mana.
  /// </summary>
  public int Red { get; init; }

  /// <summary>
  /// Amount of green mana.
  /// </summary>
  public int Green { get; init; }

  /// <summary>
  /// Amount of colorless mana.
  /// </summary>
  public int Colorless { get; init; }

  /// <summary>
  /// Creates a ManaValue with a single type of mana.
  /// </summary>
  public static ManaValue Of(ManaSymbol symbol, int amount)
  {
    return symbol switch
    {
      ManaSymbol.White => new ManaValue { White = amount },
      ManaSymbol.Blue => new ManaValue { Blue = amount },
      ManaSymbol.Black => new ManaValue { Black = amount },
      ManaSymbol.Red => new ManaValue { Red = amount },
      ManaSymbol.Green => new ManaValue { Green = amount },
      ManaSymbol.Colorless => new ManaValue { Colorless = amount },
      _ => throw new ArgumentException($"Cannot create ManaValue from {symbol}"),
    };
  }

  /// <summary>
  /// Returns the total amount of mana across all colors and colorless.
  /// </summary>
  public int Total => White + Blue + Black + Red + Green + Colorless;

  /// <summary>
  /// Returns a string representation of this mana value.
  /// </summary>
  public override string ToString()
  {
    var parts = new List<string>();
    if (White > 0)
    {
      parts.Add($"{White}W");
    }
    if (Blue > 0)
    {
      parts.Add($"{Blue}U");
    }
    if (Black > 0)
    {
      parts.Add($"{Black}B");
    }
    if (Red > 0)
    {
      parts.Add($"{Red}R");
    }
    if (Green > 0)
    {
      parts.Add($"{Green}G");
    }
    if (Colorless > 0)
    {
      parts.Add($"{Colorless}C");
    }

    return parts.Count > 0 ? string.Join(", ", parts) : "0";
  }
}
