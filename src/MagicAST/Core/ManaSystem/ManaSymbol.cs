namespace MagicAST.Core.ManaSystem;

/// <summary>
/// Represents the types of mana symbols that can appear in mana costs.
/// Includes colored mana, colorless mana, generic mana, and special symbols.
/// </summary>
public enum ManaSymbol
{
  /// <summary>
  /// White mana (W).
  /// </summary>
  White,

  /// <summary>
  /// Blue mana (U).
  /// </summary>
  Blue,

  /// <summary>
  /// Black mana (B).
  /// </summary>
  Black,

  /// <summary>
  /// Red mana (R).
  /// </summary>
  Red,

  /// <summary>
  /// Green mana (G).
  /// </summary>
  Green,

  /// <summary>
  /// Colorless mana (C).
  /// </summary>
  Colorless,

  /// <summary>
  /// Generic mana (can be paid with any type of mana).
  /// Represented by numbers in mana costs.
  /// </summary>
  Generic,

  /// <summary>
  /// Variable mana (X).
  /// Represents an amount chosen when the spell or ability is cast or activated.
  /// </summary>
  X,
}
