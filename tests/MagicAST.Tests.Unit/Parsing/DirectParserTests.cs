using MagicAST.Core.Parsing;
using NUnit.Framework;
using Superpower;

namespace MagicAST.Tests.Unit.Parsing;

/// <summary>
/// Direct parser unit tests for debugging Phase 2 parsers.
/// </summary>
[TestFixture]
[Category("Phase2")]
public class DirectParserTests
{
  [Test]
  public void DirectEffectParser_DrawACard_Success()
  {
    var input = "Draw a card.";
    var result = EffectParser.Draw.TryParse(input);

    Assert.That(
      result.HasValue,
      Is.True,
      $"Failed to parse '{input}'. Error: {(result.HasValue ? "none" : result.ToString())}"
    );
  }

  [Test]
  public void DirectEffectParser_DrawACard_NoPeriod_Success()
  {
    var input = "Draw a card";
    var result = EffectParser.Draw.TryParse(input);

    Assert.That(result.HasValue, Is.True, $"Failed to parse '{input}'");
  }

  [Test]
  public void DirectEffectParser_DrawTwoCards_Success()
  {
    var input = "Draw two cards.";
    var result = EffectParser.Draw.TryParse(input);

    Assert.That(result.HasValue, Is.True, $"Failed to parse '{input}'");
  }

  [Test]
  public void DirectEffectParser_AddGreen_Success()
  {
    var input = "Add {G}.";
    var result = EffectParser.AddMana.TryParse(input);

    Assert.That(result.HasValue, Is.True, $"Failed to parse '{input}'");
  }

  [Test]
  public void DirectCostParser_TapCost_Success()
  {
    var input = "{T}";
    var result = CostParser.TapCost.TryParse(input);

    Assert.That(result.HasValue, Is.True, $"Failed to parse '{input}'");
  }

  [Test]
  public void DirectCostParser_ManaCost_Success()
  {
    var input = "{2}";
    var result = CostParser.ManaCost.TryParse(input);

    Assert.That(result.HasValue, Is.True, $"Failed to parse '{input}'");
  }

  [Test]
  public void DirectCostParser_CompoundCost_Success()
  {
    var input = "{2}, {T}";
    var result = CostParser.Costs.TryParse(input);

    Assert.That(result.HasValue, Is.True, $"Failed to parse '{input}'");
    if (result.HasValue)
    {
      Assert.That(result.Value, Has.Count.EqualTo(2));
    }
  }

  [Test]
  public void DirectActivatedAbilityParser_Simple_Success()
  {
    var input = "{T}: Add {G}.";
    var result = ActivatedAbilityParser.ActivatedAbility.TryParse(input);

    Assert.That(result.HasValue, Is.True, $"Failed to parse '{input}'");
  }

  [Test]
  public void DirectActivatedAbilityParser_CompoundCost_Success()
  {
    var input = "{2}, {T}: Draw a card.";
    var result = ActivatedAbilityParser.ActivatedAbility.TryParse(input);

    Assert.That(result.HasValue, Is.True, $"Failed to parse '{input}'");
  }
}
