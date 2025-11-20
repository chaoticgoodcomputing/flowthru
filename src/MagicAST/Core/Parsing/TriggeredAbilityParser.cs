using MagicAST.Core.AST.Nodes.Abilities;
using MagicAST.Core.Diagnostics;
using Superpower;
using Superpower.Parsers;

namespace MagicAST.Core.Parsing;

/// <summary>
/// Parses triggered abilities: When/Whenever/At [trigger], [effect]
/// Phase 3: ETB, combat, phase, and death triggers.
/// Example: "When this creature enters, draw a card", "Whenever ~ attacks, you gain 1 life"
/// </summary>
public static class TriggeredAbilityParser
{
  /// <summary>
  /// Parses a complete triggered ability.
  /// Pattern: Trigger , Effect
  /// </summary>
  public static TextParser<TriggeredAbilityNode> TriggeredAbility =>
    from trigger in TriggerParser.AnyTrigger
    from ws1 in Character.WhiteSpace.Many()
    from comma in Character.EqualTo(',')
    from ws2 in Character.WhiteSpace.Many()
    from effect in EffectParser.AnyEffect
    select new TriggeredAbilityNode
    {
      Trigger = trigger,
      Effects = new List<AST.Nodes.Effects.EffectNode> { effect },
    };

  /// <summary>
  /// Parses a triggered ability from a string.
  /// </summary>
  /// <param name="abilityText">The ability text to parse.</param>
  /// <param name="cardName">Card name for diagnostics.</param>
  /// <returns>Parse result with ability and diagnostics.</returns>
  public static ParseResult Parse(string abilityText, string cardName)
  {
    var diagnostics = new DiagnosticBag();
    var sourceText = SourceText.From(abilityText);
    var abilities = new List<AbilityNode>();

    if (string.IsNullOrWhiteSpace(abilityText))
    {
      return new ParseResult(abilities, diagnostics.ToImmutableArray().ToList());
    }

    try
    {
      var result = TriggeredAbility.AtEnd().TryParse(abilityText);

      if (result.HasValue)
      {
        abilities.Add(result.Value);
      }
      else
      {
        // Failed to parse - report error
        var location = Location.Create(sourceText, new TextSpan(0, abilityText.Length), cardName);
        diagnostics.Report(Descriptors.UnknownAbilityPattern, location, abilityText);
      }
    }
    catch (Exception ex)
    {
      var location = Location.Create(sourceText, new TextSpan(0, abilityText.Length), cardName);
      diagnostics.Report(
        Descriptors.UnknownAbilityPattern,
        location,
        $"{abilityText} ({ex.Message})"
      );
    }

    return new ParseResult(abilities, diagnostics.ToImmutableArray().ToList());
  }
}
