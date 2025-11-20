namespace MagicAST.Core.Keywords;

/// <summary>
/// Enumeration of Magic: The Gathering keywords.
/// Phase 0: Simple keywords without parameters.
/// Phase 1+: Will add parametric keywords (Equip, Cycling, etc.)
/// </summary>
public enum Keyword
{
  // Evasion abilities
  Flying,
  Menace,
  Fear,
  Intimidate,
  Shadow,
  Horsemanship,
  Skulk,
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
  Flanking,
  Banding,

  // Protection
  Hexproof,
  Shroud,
  Indestructible,
  Ward,
  TotemArmor,

  // Graveyard/Recursion
  Undying,
  Persist,
  Unearth,
  Flashback,
  Retrace,

  // Damage modification
  Wither,
  Infect,

  // Cost reduction / Casting
  Flash,
  Convoke,
  Delve,
  Affinity,
  Improvise,

  // Tribal / Type changing
  Changeling,
  Prowl,

  // Triggered keyword abilities
  Prowess,
  Evolve,
  Extort,
  Landfall,

  // Spell mechanics
  Rebound,
  SplitSecond,
  Storm,
  Cascade,
  Ripple,

  // Other
  Landwalk, // Generic - specific types (Forestwalk, etc.) can be subtypes

  // Phase 1: Parametric keywords
  // Keywords with mana costs
  Equip,
  Cycling,
  Ninjutsu,
  Madness,
  Transmute,
  Reinforce,
  Bloodrush,
  Scavenge,
  Bestow,
  Dash,
  Megamorph,
  Surge,
  Emerge,
  Escalate,
  Overload,
  Outlast,
  Unearth_Cost, // Unearth with cost parameter (distinct from graveyard mechanic)

  // Keywords with amount parameters
  Absorb,
  Afflict,
  Amplify,
  Annihilator,
  Bushido,
  Rampage,
  Fading,
  Vanishing,
  Modular,
  Graft,
  Soulshift,
  Dredge,
  Bloodthirst,
  Tribute,
  Renown,
  Crew,

  // Keywords with filters
  Protection,
  Forestwalk,
  Islandwalk,
  Mountainwalk,
  Plainswalk,
  Swampwalk,

  // Simple keywords added in Phase 1
  Devoid,
  Partner,
  PartnerWith,
  Companion,
  Mutate,
  Foretell,
  Boast,
  Daybound,
  Nightbound,
  Disturb,
  Decayed,
  Training,
  Reconfigure,
  Compleated,
  Toxic,
  ForMirrodin,
  Backup,
  Bargain,
  Craft,
}
