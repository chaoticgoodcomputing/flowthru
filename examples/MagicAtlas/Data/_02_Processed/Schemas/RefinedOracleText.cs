using Flowthru.Abstractions;
using MagicAtlas.Data.Enums.Card;

namespace MagicAtlas.Data._02_Processed.Schemas;

/// <summary>
/// Opinionated core data for a Magic: The Gathering card. This represents data that I've
/// personally determined might be material for analysis.
/// </summary>
public record RefinedOracleText : IStructuredSerializable
{
  // =====================
  // MARK: IDENTIFIERS
  // =====================

  /// <summary>
  /// A unique ID for this card in Scryfall's database.
  /// </summary>
  public Guid Id { get; init; }

  // =====================
  // MARK: CONTENT
  // =====================

  /// <summary>
  /// The name of this card.
  /// </summary>
  /// <remarks>
  /// If this card has multiple faces, this field will contain both names separated by ␣//␣.
  /// </remarks>
  public string Name { get; init; } = "";

  /// <summary>
  /// Full oracle text for the card.
  /// </summary>
  public string RawText { get; init; } = "";

  /// <summary>
  /// Full oracle text for the card.
  /// </summary>
  public string RefinedText { get; init; } = "";

  /// <summary>
  /// List of keyword abilities found in the oracle text.
  ///
  /// For example, "Landfall — Whenever a land you control enters, you may exile target nonland
  /// permanent other than this creature."
  /// </summary>
  public List<KeywordAbility> KeywordAbilities { get; init; } = new();

  /// <summary>
  /// List of activated abilities found in the oracle text.
  ///
  /// For example: "{2}, {T}, Sacrifice an artifact: You get {E}{E} and draw a card.
  /// </summary>
  public List<ActivatedAbility> ActivatedAbilities { get; init; } = new();

  /// <summary>
  /// List of triggered abilities found in the oracle text.
  ///
  /// For example: "When Greta enters, create a Food token."
  /// </summary>
  public List<TriggeredAbility> TriggeredAbilities { get; init; } = new();

  /// <summary>
  /// List of passive abilities found in the oracle text. These are any abilities that are not
  /// keyword, activated, or triggered abilities.
  ///
  /// Technically, includes spell abilities, static abilities, and keywords such as "flying" or "trample".
  /// </summary>
  public List<PassiveAbility> PassiveAbilities { get; init; } = new();
}

/// <summary>
/// A passive ability found in a card's oracle text.
/// </summary>
public class PassiveAbility
{
  public string Effect { get; init; } = "";
}

/// <summary>
/// An activated ability found in a card's oracle text.
/// </summary>
public class ActivatedAbility
{
  public string RawText { get; init; } = "";
  public List<string> Costs { get; init; } = new();
  public string Effect { get; init; } = "";
}

/// <summary>
/// A triggered ability found in a card's oracle text.
/// </summary>
public class TriggeredAbility
{
  public string RawText { get; init; } = "";
  public string Trigger { get; init; } = "";
  public string Effect { get; init; } = "";
}

/// <summary>
/// A keyword ability found in a card's oracle text.
/// </summary>
public class KeywordAbility
{
  public string RawText { get; init; } = "";
  public string Keyword { get; init; } = "";
  public string Effect { get; init; } = "";
}
