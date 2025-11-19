namespace MagicAST.Core.Keywords;

/// <summary>
/// Metadata for keyword abilities.
/// Includes reminder text and other keyword-specific information.
/// </summary>
public class KeywordData
{
  /// <summary>
  /// The keyword this data describes.
  /// </summary>
  public required Keyword Keyword { get; init; }

  /// <summary>
  /// Standard reminder text for this keyword.
  /// </summary>
  public required string ReminderText { get; init; }

  /// <summary>
  /// Gets the reminder text for a specific keyword.
  /// </summary>
  public static string GetReminderText(Keyword keyword)
  {
    return keyword switch
    {
      Keyword.Vigilance => "Attacking doesn't cause this creature to tap.",
      Keyword.Flying => "This creature can only be blocked by creatures with flying or reach.",
      Keyword.Haste => "This creature can attack and {T} as soon as it comes under your control.",
      Keyword.Deathtouch =>
        "Any amount of damage this deals to a creature is enough to destroy it.",
      Keyword.Lifelink => "Damage dealt by this creature also causes you to gain that much life.",
      Keyword.Trample =>
        "This creature can deal excess combat damage to the player or planeswalker it's attacking.",
      Keyword.FirstStrike =>
        "This creature deals combat damage before creatures without first strike.",
      Keyword.DoubleStrike => "This creature deals both first-strike and regular combat damage.",
      Keyword.Menace => "This creature can't be blocked except by two or more creatures.",
      Keyword.Defender => "This creature can't attack.",
      Keyword.Reach => "This creature can block creatures with flying.",
      Keyword.Hexproof =>
        "This creature can't be the target of spells or abilities your opponents control.",
      Keyword.Indestructible =>
        "Damage and effects that say \"destroy\" don't destroy this creature.",
      Keyword.Flash => "You may cast this spell any time you could cast an instant.",
      _ => string.Empty,
    };
  }
}
