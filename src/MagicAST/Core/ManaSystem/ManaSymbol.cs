namespace MagicAST.Core.ManaSystem;

/// <summary>
/// Represents the types of mana symbols that can appear in mana costs.
/// Includes colored mana, colorless mana, generic mana, and special symbols.
/// </summary>
public enum ManaSymbol
{
  // ============================================================================
  // Basic colored mana
  // ============================================================================

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

  // ============================================================================
  // Colorless and special mana
  // ============================================================================

  /// <summary>
  /// Colorless mana (C).
  /// </summary>
  Colorless,

  /// <summary>
  /// Snow mana (S).
  /// </summary>
  Snow,

  // ============================================================================
  // Generic mana and variables
  // ============================================================================

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

  /// <summary>
  /// Variable mana (Y).
  /// </summary>
  Y,

  /// <summary>
  /// Variable mana (Z).
  /// </summary>
  Z,

  // ============================================================================
  // Hybrid mana (two-color)
  // ============================================================================

  /// <summary>
  /// Hybrid white/blue mana (W/U).
  /// </summary>
  HybridWU,

  /// <summary>
  /// Hybrid white/black mana (W/B).
  /// </summary>
  HybridWB,

  /// <summary>
  /// Hybrid blue/black mana (U/B).
  /// </summary>
  HybridUB,

  /// <summary>
  /// Hybrid blue/red mana (U/R).
  /// </summary>
  HybridUR,

  /// <summary>
  /// Hybrid black/red mana (B/R).
  /// </summary>
  HybridBR,

  /// <summary>
  /// Hybrid black/green mana (B/G).
  /// </summary>
  HybridBG,

  /// <summary>
  /// Hybrid red/green mana (R/G).
  /// </summary>
  HybridRG,

  /// <summary>
  /// Hybrid red/white mana (R/W).
  /// </summary>
  HybridRW,

  /// <summary>
  /// Hybrid green/white mana (G/W).
  /// </summary>
  HybridGW,

  /// <summary>
  /// Hybrid green/blue mana (G/U).
  /// </summary>
  HybridGU,

  // ============================================================================
  // Phyrexian mana (single color)
  // ============================================================================

  /// <summary>
  /// Phyrexian white mana (W/P) - can be paid with one white mana or 2 life.
  /// </summary>
  PhyrexianW,

  /// <summary>
  /// Phyrexian blue mana (U/P) - can be paid with one blue mana or 2 life.
  /// </summary>
  PhyrexianU,

  /// <summary>
  /// Phyrexian black mana (B/P) - can be paid with one black mana or 2 life.
  /// </summary>
  PhyrexianB,

  /// <summary>
  /// Phyrexian red mana (R/P) - can be paid with one red mana or 2 life.
  /// </summary>
  PhyrexianR,

  /// <summary>
  /// Phyrexian green mana (G/P) - can be paid with one green mana or 2 life.
  /// </summary>
  PhyrexianG,

  /// <summary>
  /// Phyrexian colorless mana (C/P) - can be paid with one colorless mana or 2 life.
  /// </summary>
  PhyrexianC,

  // ============================================================================
  // Phyrexian hybrid (two-color)
  // ============================================================================

  /// <summary>
  /// Phyrexian hybrid white/blue mana (W/U/P).
  /// </summary>
  PhyrexianHybridWU,

  /// <summary>
  /// Phyrexian hybrid white/black mana (W/B/P).
  /// </summary>
  PhyrexianHybridWB,

  /// <summary>
  /// Phyrexian hybrid blue/black mana (U/B/P).
  /// </summary>
  PhyrexianHybridUB,

  /// <summary>
  /// Phyrexian hybrid blue/red mana (U/R/P).
  /// </summary>
  PhyrexianHybridUR,

  /// <summary>
  /// Phyrexian hybrid black/red mana (B/R/P).
  /// </summary>
  PhyrexianHybridBR,

  /// <summary>
  /// Phyrexian hybrid black/green mana (B/G/P).
  /// </summary>
  PhyrexianHybridBG,

  /// <summary>
  /// Phyrexian hybrid red/green mana (R/G/P).
  /// </summary>
  PhyrexianHybridRG,

  /// <summary>
  /// Phyrexian hybrid red/white mana (R/W/P).
  /// </summary>
  PhyrexianHybridRW,

  /// <summary>
  /// Phyrexian hybrid green/white mana (G/W/P).
  /// </summary>
  PhyrexianHybridGW,

  /// <summary>
  /// Phyrexian hybrid green/blue mana (G/U/P).
  /// </summary>
  PhyrexianHybridGU,

  // ============================================================================
  // Monocolored hybrid (2/Color)
  // ============================================================================

  /// <summary>
  /// Monocolored hybrid white (2/W) - can be paid with 2 generic or one white.
  /// </summary>
  MonoHybridW,

  /// <summary>
  /// Monocolored hybrid blue (2/U) - can be paid with 2 generic or one blue.
  /// </summary>
  MonoHybridU,

  /// <summary>
  /// Monocolored hybrid black (2/B) - can be paid with 2 generic or one black.
  /// </summary>
  MonoHybridB,

  /// <summary>
  /// Monocolored hybrid red (2/R) - can be paid with 2 generic or one red.
  /// </summary>
  MonoHybridR,

  /// <summary>
  /// Monocolored hybrid green (2/G) - can be paid with 2 generic or one green.
  /// </summary>
  MonoHybridG,

  // ============================================================================
  // Colorless hybrid
  // ============================================================================

  /// <summary>
  /// Colorless hybrid white (C/W).
  /// </summary>
  ColorlessHybridW,

  /// <summary>
  /// Colorless hybrid blue (C/U).
  /// </summary>
  ColorlessHybridU,

  /// <summary>
  /// Colorless hybrid black (C/B).
  /// </summary>
  ColorlessHybridB,

  /// <summary>
  /// Colorless hybrid red (C/R).
  /// </summary>
  ColorlessHybridR,

  /// <summary>
  /// Colorless hybrid green (C/G).
  /// </summary>
  ColorlessHybridG,

  // ============================================================================
  // Special mana symbols
  // ============================================================================

  /// <summary>
  /// Half colorless ({H}) - one colored mana or two life.
  /// </summary>
  HalfColorless,

  /// <summary>
  /// Half white ({HW}).
  /// </summary>
  HalfWhite,

  /// <summary>
  /// Half red ({HR}).
  /// </summary>
  HalfRed,

  /// <summary>
  /// Legendary ({L}) - one mana from a legendary source.
  /// </summary>
  Legendary,

  /// <summary>
  /// Land drop ({D}) - one potential land drop.
  /// </summary>
  LandDrop,

  /// <summary>
  /// Infinite ({∞}).
  /// </summary>
  Infinite,

  /// <summary>
  /// Half generic ({½}).
  /// </summary>
  HalfGeneric,
}
