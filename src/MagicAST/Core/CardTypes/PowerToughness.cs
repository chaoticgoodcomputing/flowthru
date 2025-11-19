namespace MagicAST.Core.CardTypes;

/// <summary>
/// Represents the power and toughness of a creature.
/// Can be static values, variables (X), or special values (*).
/// </summary>
public class PowerToughness
{
  /// <summary>
  /// The power value (combat damage dealt).
  /// </summary>
  public required PTValue Power { get; init; }

  /// <summary>
  /// The toughness value (damage needed to destroy).
  /// </summary>
  public required PTValue Toughness { get; init; }

  /// <summary>
  /// Parses a power/toughness string (e.g., "3/3", "2/1", "*/", "*+1/*").
  /// </summary>
  /// <param name="ptString">The power/toughness string to parse.</param>
  /// <returns>A PowerToughness object representing the parsed values.</returns>
  public static PowerToughness Parse(string ptString)
  {
    string[] parts = ptString.Split('/', StringSplitOptions.TrimEntries);
    if (parts.Length != 2)
    {
      throw new ArgumentException($"Invalid power/toughness format: {ptString}");
    }

    return new PowerToughness
    {
      Power = PTValue.Parse(parts[0]),
      Toughness = PTValue.Parse(parts[1]),
    };
  }

  /// <summary>
  /// Returns the string representation of this power/toughness.
  /// </summary>
  public override string ToString()
  {
    return $"{Power}/{Toughness}";
  }
}

/// <summary>
/// Represents a single power or toughness value.
/// Can be a static number, a variable (X), or a special value (*).
/// </summary>
public class PTValue
{
  /// <summary>
  /// The type of P/T value.
  /// </summary>
  public required PTValueType Type { get; init; }

  /// <summary>
  /// The base value (for static or modified values).
  /// </summary>
  public int BaseValue { get; init; }

  /// <summary>
  /// Optional modifier applied to the value (e.g., "+1" in "*+1").
  /// </summary>
  public int Modifier { get; init; }

  /// <summary>
  /// Parses a single P/T value string.
  /// </summary>
  public static PTValue Parse(string valueString)
  {
    valueString = valueString.Trim();

    // Check for X
    if (valueString.Equals("X", StringComparison.OrdinalIgnoreCase))
    {
      return new PTValue
      {
        Type = PTValueType.Variable,
        BaseValue = 0,
        Modifier = 0,
      };
    }

    // Check for * (characteristic-defining)
    if (valueString.Contains('*'))
    {
      // Parse modifier if present (e.g., "*+1", "*-2")
      if (valueString.Length > 1)
      {
        string modifierPart = valueString.Substring(1);
        if (int.TryParse(modifierPart, out int modifier))
        {
          return new PTValue
          {
            Type = PTValueType.CharacteristicDefining,
            BaseValue = 0,
            Modifier = modifier,
          };
        }
      }
      return new PTValue
      {
        Type = PTValueType.CharacteristicDefining,
        BaseValue = 0,
        Modifier = 0,
      };
    }

    // Static value
    if (int.TryParse(valueString, out int value))
    {
      return new PTValue
      {
        Type = PTValueType.Static,
        BaseValue = value,
        Modifier = 0,
      };
    }

    throw new ArgumentException($"Invalid P/T value: {valueString}");
  }

  /// <summary>
  /// Returns the string representation of this P/T value.
  /// </summary>
  public override string ToString()
  {
    return Type switch
    {
      PTValueType.Static => BaseValue.ToString(),
      PTValueType.Variable => "X",
      PTValueType.CharacteristicDefining when Modifier == 0 => "*",
      PTValueType.CharacteristicDefining => $"*{Modifier:+#;-#;+0}",
      _ => "?",
    };
  }
}

/// <summary>
/// The type of power/toughness value.
/// </summary>
public enum PTValueType
{
  /// <summary>
  /// Static numeric value (e.g., 2, 5).
  /// </summary>
  Static,

  /// <summary>
  /// Variable value (X).
  /// </summary>
  Variable,

  /// <summary>
  /// Characteristic-defining value (*).
  /// Set by a characteristic-defining ability.
  /// </summary>
  CharacteristicDefining,
}
