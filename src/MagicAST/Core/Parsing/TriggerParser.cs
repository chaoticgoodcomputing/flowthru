using MagicAST.Core.AST.Nodes.Triggers;
using Superpower;
using Superpower.Parsers;

namespace MagicAST.Core.Parsing;

/// <summary>
/// Parses trigger events for triggered abilities.
/// Phase 3: Handles ETB, combat, phase, and death triggers.
/// </summary>
public static class TriggerParser
{
  /// <summary>
  /// Case-insensitive string parser helper.
  /// </summary>
  private static TextParser<string> Text(string text) =>
    Span.EqualToIgnoreCase(text).Select(s => s.ToStringValue());

  /// <summary>
  /// Parses a single word (letters, digits, hyphens, apostrophes, commas)
  /// </summary>
  private static TextParser<string> Word =>
    from chars in Character
      .LetterOrDigit.Or(Character.EqualTo('-'))
      .Or(Character.EqualTo('\''))
      .Or(Character.EqualTo(','))
      .AtLeastOnce()
    select new string(chars.ToArray());

  /// <summary>
  /// Parses "enters" or "enters the battlefield"
  /// </summary>
  private static TextParser<string> EntersTheBattlefield =>
    Text("enters the battlefield").Try().Or(Text("enters"));

  /// <summary>
  /// Parses words until we hit a keyword like "enters", "dies", or "attacks"
  /// This captures card names (like "Mulldrifter", "Gray Merchant of Asphodel"),
  /// pronouns ("this creature"), or generic subjects ("a creature")
  /// Strategy: Consume word + space pairs, checking after each word if the next thing is a keyword
  /// </summary>
  internal static TextParser<string> SubjectWords =>
    from firstWord in Word
    from ws in Character.WhiteSpace.AtLeastOnce()
    from rest in (
      from peek in Parse.Not(Text("enters").Or(Text("dies")).Or(Text("attacks"))).Value("")
      from w in Word
      from sp in Character.WhiteSpace.AtLeastOnce()
      select w + " "
    ).Many()
    select (firstWord + " " + string.Concat(rest)).Trim();

  /// <summary>
  /// Parses ETB trigger: "When/Whenever X enters (the battlefield)"
  /// Handles: "When this creature enters", "When Mulldrifter enters", "Whenever a creature enters"
  /// Strategy: Parse everything up to "enters" to ensure we're looking at the right trigger type
  /// </summary>
  public static TextParser<TriggerEvent> ETBTrigger =>
    from when in Text("Whenever").Try().Or(Text("When"))
    from ws1 in Character.WhiteSpace.AtLeastOnce()
    from subject in SubjectWords
    from _ in Parse.Not(Text("attacks").Or(Text("dies"))).Value("")
    from enters in EntersTheBattlefield
    select new TriggerEvent { Type = EventType.Enters, Filter = null };

  /// <summary>
  /// Parses attack trigger: "When/Whenever X attacks"
  /// Handles: "When this creature attacks", "Whenever a creature attacks", "Whenever Acererak attacks"
  /// Strategy: Explicitly require "attacks" keyword to distinguish from enters/dies
  /// </summary>
  public static TextParser<TriggerEvent> AttackTrigger =>
    from when in Text("Whenever").Try().Or(Text("When"))
    from ws1 in Character.WhiteSpace.AtLeastOnce()
    from subject in SubjectWords
    from _ in Parse.Not(Text("enters").Or(Text("dies"))).Value("")
    from attacks in Text("attacks")
    select new TriggerEvent { Type = EventType.Attacks, Filter = null };

  /// <summary>
  /// Parses death trigger: "When/Whenever X dies"
  /// Handles: "When this creature dies", "Whenever a creature dies", "When Acererak dies"
  /// Strategy: Explicitly require "dies" keyword to distinguish from enters/attacks
  /// </summary>
  public static TextParser<TriggerEvent> DeathTrigger =>
    from when in Text("Whenever").Try().Or(Text("When"))
    from ws1 in Character.WhiteSpace.AtLeastOnce()
    from subject in SubjectWords
    from _ in Parse.Not(Text("enters").Or(Text("attacks"))).Value("")
    from dies in Text("dies")
    select new TriggerEvent { Type = EventType.Dies, Filter = null };

  /// <summary>
  /// Parses upkeep trigger: "At the beginning of your upkeep"
  /// </summary>
  public static TextParser<TriggerEvent> UpkeepTrigger =>
    from at in Text("At")
    from ws1 in Character.WhiteSpace.AtLeastOnce()
    from the1 in Text("the")
    from ws2 in Character.WhiteSpace.AtLeastOnce()
    from beginning in Text("beginning")
    from ws3 in Character.WhiteSpace.AtLeastOnce()
    from of in Text("of")
    from ws4 in Character.WhiteSpace.AtLeastOnce()
    from your in Text("your").Or(Text("each"))
    from ws5 in Character.WhiteSpace.AtLeastOnce()
    from upkeep in Text("upkeep")
    select new TriggerEvent { Type = EventType.PhaseBegin, Filter = null };

  /// <summary>
  /// Parses any trigger event.
  /// Try more specific patterns first (upkeep has unique "At" start),
  /// then try the "When/Whenever" patterns. All must use Try() for proper backtracking.
  /// </summary>
  public static TextParser<TriggerEvent> AnyTrigger =>
    UpkeepTrigger.Try().Or(DeathTrigger.Try()).Or(AttackTrigger.Try()).Or(ETBTrigger);
}
