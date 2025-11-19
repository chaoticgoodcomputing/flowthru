namespace MagicAST.Core.CardTypes;

/// <summary>
/// Represents the main card types in Magic: The Gathering.
/// A card must have at least one card type.
/// </summary>
public enum CardType
{
  /// <summary>
  /// Artifact card type.
  /// </summary>
  Artifact,

  /// <summary>
  /// Creature card type.
  /// </summary>
  Creature,

  /// <summary>
  /// Enchantment card type.
  /// </summary>
  Enchantment,

  /// <summary>
  /// Instant card type.
  /// </summary>
  Instant,

  /// <summary>
  /// Land card type.
  /// </summary>
  Land,

  /// <summary>
  /// Planeswalker card type.
  /// </summary>
  Planeswalker,

  /// <summary>
  /// Sorcery card type.
  /// </summary>
  Sorcery,

  /// <summary>
  /// Tribal card type (deprecated but still valid).
  /// </summary>
  Tribal,

  /// <summary>
  /// Battle card type.
  /// </summary>
  Battle,
}
