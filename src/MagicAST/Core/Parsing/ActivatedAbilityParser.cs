using MagicAST.Core.AST.Nodes.Abilities;
using MagicAST.Core.Diagnostics;
using Superpower;
using Superpower.Parsers;

namespace MagicAST.Core.Parsing;

/// <summary>
/// Parses activated abilities: Cost(s) : Effect(s)
/// Phase 2: Simple mana abilities, draw abilities, and pump abilities.
/// Example: "{T}: Add {C}", "{2}, {T}: Draw a card"
/// </summary>
public static class ActivatedAbilityParser
{
  /// <summary>
  /// Parses a complete activated ability.
  /// Pattern: Cost(s) : Effect(s)
  /// </summary>
  public static TextParser<ActivatedAbilityNode> ActivatedAbility =>
    from costs in CostParser.Costs
    from ws1 in Character.WhiteSpace.Many()
    from colon in Character.EqualTo(':')
    from ws2 in Character.WhiteSpace.Many()
    from effect in EffectParser.AnyEffect
    select new ActivatedAbilityNode
    {
      Costs = costs,
      Effects = new List<AST.Nodes.Effects.EffectNode> { effect },
    };

  /// <summary>
  /// Parses an activated ability from a string.
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
      var result = ActivatedAbility.AtEnd().TryParse(abilityText);

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
