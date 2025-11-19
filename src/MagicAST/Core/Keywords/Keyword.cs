namespace MagicAST.Core.Keywords;

/// <summary>
/// Enumeration of Magic: The Gathering keywords.
/// This is a subset for Phase 1 - will be expanded.
/// </summary>
public enum Keyword
{
  // Evasion abilities
  Flying,
  Menace,
  Unblockable,

  // Combat abilities
  Vigilance,
  FirstStrike,
  DoubleStrike,
  Deathtouch,
  Lifelink,
  Trample,
  Haste,
  Defender,
  Reach,

  // Protection
  Hexproof,
  Shroud,
  Indestructible,
  Ward,

  // Other
  Flash,
  Landwalk, // Generic - specific types (Forestwalk, etc.) can be subtypes
}
