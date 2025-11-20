namespace MagicAST.Core.Parsing;

/// <summary>
/// Token types for lexical analysis of Magic oracle text.
/// Phase 0: Simple keyword tokens only.
/// Phase 1+: Will expand to include operators, literals, and complex structures.
/// </summary>
public enum MTGToken
{
  // Punctuation
  Comma,
  Colon,
  Period,
  LeftParen,
  RightParen,

  // Simple keyword abilities (Phase 0 - no parameters)
  Flying,
  Menace,
  Vigilance,
  Haste,
  FirstStrike,
  DoubleStrike,
  Deathtouch,
  Lifelink,
  Trample,
  Defender,
  Reach,
  Hexproof,
  Shroud,
  Indestructible,
  Flash,
  Ward,

  // Additional Phase 0 keywords
  Fear,
  Intimidate,
  Shadow,
  Horsemanship,
  Skulk,
  Prowess,
  Changeling,
  Rebound,
  SplitSecond,
  Storm,
  Cascade,
  Evolve,
  Extort,
  Undying,
  Persist,
  Wither,
  Infect,
  Flanking,
  Banding,
  Convoke,
  Delve,
  Prowl,
  TotemArmor,

  // Phase 1 keywords (with parameters) - placeholders for future
  Equip,
  Cycling,
  Protection,
  Landwalk,
  Absorb,
  Afflict,
  Amplify,
  Annihilator,
  Bushido,
  Rampage,
  Fading,
  Vanishing,
  Ninjutsu,

  // Literals (Phase 1+)
  Number,
  Word,
  ManaCost,

  // Phase 2: Activated ability components
  Tap, // {T}
  Untap, // {Q}

  // Phase 2: Effect keywords
  Add, // "Add {G}"
  Draw, // "Draw a card"
  Gets, // "gets +1/+0"
  Until, // "until end of turn"

  // Phase 2: Common words
  One, // "one"
  A, // "a"
  Of, // "of"
  Any, // "any"
  Color, // "color"
  Mana, // "mana"
  Card, // "card"
  Cards, // "cards"
  This, // "this"
  Creature, // "creature"
  End, // "end"
  Turn, // "turn"

  // Phase 3: Triggered ability keywords
  When, // "When"
  Whenever, // "Whenever"
  At, // "At"
  Beginning, // "beginning"
  Your, // "your"
  Upkeep, // "upkeep"
  Enters, // "enters"
  The, // "the"
  Battlefield, // "battlefield"
  Attacks, // "attacks"
  Dies, // "dies"
  Leaves, // "leaves"
  You, // "you"
  Gain, // "gain"
  Life, // "life"
  Lose, // "lose"
  Each, // "each"
  Opponent, // "opponent"
  Control, // "control"
}
