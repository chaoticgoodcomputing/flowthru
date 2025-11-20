using MagicAST.Core.AST.Nodes.Costs;
using MagicAST.Core.Diagnostics;
using Superpower;
using Superpower.Model;
using Superpower.Parsers;

namespace MagicAST.Core.Parsing;

/// <summary>
/// Parses cost expressions for activated abilities.
/// Phase 2: Handles tap costs, mana costs, and compound costs.
/// Future phases: Sacrifice, discard, pay life, etc.
/// </summary>
public static class CostParser
{
  /// <summary>
  /// Parses a tap cost symbol: {T}
  /// </summary>
  public static TextParser<TapCostNode> TapCost =>
    from open in Character.EqualTo('{')
    from t in Character.In('T', 't')
    from close in Character.EqualTo('}')
    select new TapCostNode();

  /// <summary>
  /// Parses a mana cost expression: {2}, {G}, {2}{R}{R}, etc.
  /// Reuses the existing ManaCostParser infrastructure.
  /// </summary>
  public static TextParser<ManaCostNode> ManaCost =>
    from cost in Character
      .EqualTo('{')
      .IgnoreThen(Character.ExceptIn('}').AtLeastOnce())
      .Then(chars => Character.EqualTo('}').Value(new string(chars.ToArray())))
      .Many()
      .Select(parts => string.Concat(parts.Select(p => "{" + p + "}")))
    select ParseManaCost(cost);

  /// <summary>
  /// Helper to parse mana cost string using existing infrastructure.
  /// </summary>
  private static ManaCostNode ParseManaCost(string costString)
  {
    var result = ManaCostParser.Parse(costString, "activated ability");
    if (result.Result != null)
    {
      return result.Result;
    }
    throw new ParseException($"Failed to parse mana cost: {costString}");
  }

  /// <summary>
  /// Parses any single cost (tap or mana).
  /// </summary>
  public static TextParser<CostNode> AnyCost =>
    TapCost.Select(c => (CostNode)c).Try().Or(ManaCost.Select(c => (CostNode)c));

  /// <summary>
  /// Parses compound costs separated by commas: {2}, {T}
  /// </summary>
  public static TextParser<List<CostNode>> CompoundCosts =>
    from first in AnyCost
    from rest in (
      from comma in Character.EqualTo(',')
      from ws in Character.WhiteSpace.Many()
      from cost in AnyCost
      select cost
    ).Many()
    select new List<CostNode> { first }
      .Concat(rest)
      .ToList();

  /// <summary>
  /// Parses costs (single or compound).
  /// Returns a list of cost nodes.
  /// </summary>
  public static TextParser<List<CostNode>> Costs =>
    CompoundCosts.Try().Or(AnyCost.Select(c => new List<CostNode> { c }));
}
