namespace MagicAST.DTOs;

/// <summary>
/// Input DTO representing a Magic: The Gathering card for AST generation.
/// </summary>
public record CardInputDto
{
  /// <summary>
  /// Card name. For split/multi-faced cards, contains both names separated by ␣//␣.
  /// </summary>
  /// <remarks>
  /// Maps to CardCoreData.Name.
  /// </remarks>
  public required string Name { get; init; }

  /// <summary>
  /// Mana cost string (e.g., "{2}{G}", "{1}{R}", "{X}{B}{B}").
  /// </summary>
  /// <remarks>
  /// Empty string "" for cards with no cost (different from {0}).
  /// Maps to CardCoreData.ManaCost.
  /// </remarks>
  public string? ManaCost { get; init; }

  /// <summary>
  /// Type line string (e.g., "Legendary Creature — Squirrel Warrior", "Instant").
  /// </summary>
  public required string TypeLine { get; init; }

  /// <summary>
  /// Oracle text (rules text) as a single string with line breaks.
  /// </summary>
  /// <remarks>
  /// Split on newlines during parsing to extract individual ability paragraphs.
  /// </remarks>
  public string? OracleText { get; init; }

  /// <summary>
  /// Power value for creatures (e.g., "3", "2", "*", "1+*").
  /// </summary>
  public string? Power { get; init; }

  /// <summary>
  /// Toughness value for creatures (e.g., "3", "2", "*", "1+*").
  /// </summary>
  public string? Toughness { get; init; }

  /// <summary>
  /// Loyalty value for planeswalkers (e.g., "3", "4", "X").
  /// </summary>
  public string? Loyalty { get; init; }
}
