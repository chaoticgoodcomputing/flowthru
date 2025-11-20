using MagicAST.Core.AST.Nodes.Abilities;
using MagicAST.Core.AST.Nodes.Costs;
using MagicAST.Core.AST.Nodes.Effects;
using MagicAST.Core.AST.Nodes.Expressions;
using MagicAST.Core.Diagnostics;
using MagicAST.Core.ManaSystem;
using MagicAST.Core.Parsing;
using NUnit.Framework;

namespace MagicAST.Tests.Unit.Parsing;

/// <summary>
/// Unit tests for Phase 2: Activated ability parsing.
/// Tests mana abilities, compound costs, draw effects, and pump effects.
/// </summary>
[TestFixture]
[Category("Phase2")]
public class ActivatedAbilityTests
{
  #region Simple Mana Abilities

  [Test]
  public void Parse_TapAddColorless_Success()
  {
    var result = OracleTextParser.Parse("{T}: Add {C}.", "Test Card");

    Assert.That(result.Abilities, Has.Count.EqualTo(1));
    var ability = result.Abilities[0] as ActivatedAbilityNode;
    Assert.That(ability, Is.Not.Null);
    Assert.That(ability!.Costs, Has.Count.EqualTo(1));
    Assert.That(ability.Costs[0], Is.TypeOf<TapCostNode>());
    Assert.That(ability.Effects, Has.Count.EqualTo(1));
    Assert.That(ability.Effects[0], Is.TypeOf<AddManaEffect>());

    var effect = ability.Effects[0] as AddManaEffect;
    Assert.That(effect!.ManaToAdd.Colorless, Is.EqualTo(1));
  }

  [Test]
  public void Parse_TapAddGreen_Success()
  {
    var result = OracleTextParser.Parse("{T}: Add {G}.", "Test Card");

    var ability = result.Abilities[0] as ActivatedAbilityNode;
    Assert.That(ability, Is.Not.Null);

    var effect = ability!.Effects[0] as AddManaEffect;
    Assert.That(effect!.ManaToAdd.Green, Is.EqualTo(1));
  }

  [TestCase("{T}: Add {W}.", 1, 0, 0, 0, 0, 0)]
  [TestCase("{T}: Add {U}.", 0, 1, 0, 0, 0, 0)]
  [TestCase("{T}: Add {B}.", 0, 0, 1, 0, 0, 0)]
  [TestCase("{T}: Add {R}.", 0, 0, 0, 1, 0, 0)]
  [TestCase("{T}: Add {G}.", 0, 0, 0, 0, 1, 0)]
  [TestCase("{T}: Add {C}.", 0, 0, 0, 0, 0, 1)]
  public void Parse_TapAddSingleColor_CorrectMana(
    string input,
    int w,
    int u,
    int b,
    int r,
    int g,
    int c
  )
  {
    var result = OracleTextParser.Parse(input, "Test Card");
    var ability = result.Abilities[0] as ActivatedAbilityNode;
    var effect = ability!.Effects[0] as AddManaEffect;

    Assert.That(effect!.ManaToAdd.White, Is.EqualTo(w));
    Assert.That(effect.ManaToAdd.Blue, Is.EqualTo(u));
    Assert.That(effect.ManaToAdd.Black, Is.EqualTo(b));
    Assert.That(effect.ManaToAdd.Red, Is.EqualTo(r));
    Assert.That(effect.ManaToAdd.Green, Is.EqualTo(g));
    Assert.That(effect.ManaToAdd.Colorless, Is.EqualTo(c));
  }

  [Test]
  public void Parse_TapAddMultipleMana_Success()
  {
    var result = OracleTextParser.Parse("{T}: Add {G}{G}.", "Test Card");

    var ability = result.Abilities[0] as ActivatedAbilityNode;
    var effect = ability!.Effects[0] as AddManaEffect;

    Assert.That(effect!.ManaToAdd.Green, Is.EqualTo(2));
  }

  [Test]
  public void Parse_TapAddAnyColor_Success()
  {
    var result = OracleTextParser.Parse("{T}: Add one mana of any color.", "Test Card");

    Assert.That(result.Abilities, Has.Count.EqualTo(1));
    var ability = result.Abilities[0] as ActivatedAbilityNode;
    Assert.That(ability, Is.Not.Null);
    Assert.That(ability!.Effects[0], Is.TypeOf<AddManaEffect>());
  }

  #endregion

  #region Compound Cost Abilities

  [Test]
  public void Parse_ManaTapDraw_Success()
  {
    var result = OracleTextParser.Parse("{2}, {T}: Draw a card.", "Test Card");

    Assert.That(result.Abilities, Has.Count.EqualTo(1));
    var ability = result.Abilities[0] as ActivatedAbilityNode;
    Assert.That(ability, Is.Not.Null);
    Assert.That(ability!.Costs, Has.Count.EqualTo(2));
    Assert.That(ability.Costs[0], Is.TypeOf<ManaCostNode>());
    Assert.That(ability.Costs[1], Is.TypeOf<TapCostNode>());

    var manaCost = ability.Costs[0] as ManaCostNode;
    Assert.That(manaCost!.Cost.ConvertedManaCost, Is.EqualTo(2));

    Assert.That(ability.Effects, Has.Count.EqualTo(1));
    Assert.That(ability.Effects[0], Is.TypeOf<DrawEffect>());
  }

  [Test]
  public void Parse_ColoredManaTapDraw_Success()
  {
    var result = OracleTextParser.Parse("{1}{U}, {T}: Draw a card.", "Test Card");

    var ability = result.Abilities[0] as ActivatedAbilityNode;
    Assert.That(ability!.Costs, Has.Count.EqualTo(2));

    var manaCost = ability.Costs[0] as ManaCostNode;
    Assert.That(manaCost!.Cost.ConvertedManaCost, Is.EqualTo(2));
    var blueSymbols = manaCost.Cost.Symbols.Count(s => s.Symbol == ManaSymbol.Blue);
    Assert.That(blueSymbols, Is.EqualTo(1));
  }

  #endregion

  #region Draw Effects

  [Test]
  public void Parse_DrawACard_Success()
  {
    var result = OracleTextParser.Parse("{T}: Draw a card.", "Test Card");

    var ability = result.Abilities[0] as ActivatedAbilityNode;
    var effect = ability!.Effects[0] as DrawEffect;

    Assert.That(effect, Is.Not.Null);
    Assert.That(effect!.NumberOfCards, Is.TypeOf<StaticValue>());
    var value = effect.NumberOfCards as StaticValue;
    Assert.That(value!.Value, Is.EqualTo(1));
    Assert.That(effect.Player, Is.EqualTo(DrawTarget.You));
  }

  [Test]
  public void Parse_DrawTwoCards_Success()
  {
    var result = OracleTextParser.Parse("{3}: Draw two cards.", "Test Card");

    Assert.That(
      result.Abilities,
      Has.Count.EqualTo(1),
      $"Expected 1 ability but got {result.Abilities.Count}. Diagnostics: {string.Join(", ", result.Diagnostics.Select(d => d.GetMessage()))}"
    );
    var ability = result.Abilities[0] as ActivatedAbilityNode;
    Assert.That(ability, Is.Not.Null);
    var effect = ability!.Effects[0] as DrawEffect;
    Assert.That(effect, Is.Not.Null);

    var value = effect!.NumberOfCards as StaticValue;
    Assert.That(value!.Value, Is.EqualTo(2));
  }

  [Test]
  public void Parse_DrawThreeCards_Success()
  {
    var result = OracleTextParser.Parse("{5}: Draw three cards.", "Test Card");

    Assert.That(
      result.Abilities,
      Has.Count.EqualTo(1),
      $"Expected 1 ability but got {result.Abilities.Count}. Diagnostics: {string.Join(", ", result.Diagnostics.Select(d => d.GetMessage()))}"
    );
    var ability = result.Abilities[0] as ActivatedAbilityNode;
    Assert.That(ability, Is.Not.Null);
    var effect = ability!.Effects[0] as DrawEffect;
    Assert.That(effect, Is.Not.Null);

    var value = effect!.NumberOfCards as StaticValue;
    Assert.That(value!.Value, Is.EqualTo(3));
  }

  #endregion

  #region PT Modification (Pump) Effects

  [Test]
  public void Parse_PumpPowerOnly_Success()
  {
    var result = OracleTextParser.Parse(
      "{R}: This creature gets +1/+0 until end of turn.",
      "Test Card"
    );

    Assert.That(result.Abilities, Has.Count.EqualTo(1));
    var ability = result.Abilities[0] as ActivatedAbilityNode;
    Assert.That(ability, Is.Not.Null);

    var effect = ability!.Effects[0] as PTModificationEffect;
    Assert.That(effect, Is.Not.Null);

    var powerMod = effect!.PowerModification as StaticValue;
    var toughnessMod = effect.ToughnessModification as StaticValue;

    Assert.That(powerMod!.Value, Is.EqualTo(1));
    Assert.That(toughnessMod!.Value, Is.EqualTo(0));
    Assert.That(effect.Duration, Is.EqualTo(Duration.UntilEndOfTurn));
  }

  [Test]
  public void Parse_PumpBothStats_Success()
  {
    var result = OracleTextParser.Parse(
      "{1}: This creature gets +2/+2 until end of turn.",
      "Test Card"
    );

    var ability = result.Abilities[0] as ActivatedAbilityNode;
    var effect = ability!.Effects[0] as PTModificationEffect;

    var powerMod = effect!.PowerModification as StaticValue;
    var toughnessMod = effect.ToughnessModification as StaticValue;

    Assert.That(powerMod!.Value, Is.EqualTo(2));
    Assert.That(toughnessMod!.Value, Is.EqualTo(2));
  }

  [Test]
  public void Parse_PumpNegativeStats_Success()
  {
    var result = OracleTextParser.Parse(
      "{B}: This creature gets -1/-0 until end of turn.",
      "Test Card"
    );

    var ability = result.Abilities[0] as ActivatedAbilityNode;
    var effect = ability!.Effects[0] as PTModificationEffect;

    var powerMod = effect!.PowerModification as StaticValue;
    var toughnessMod = effect.ToughnessModification as StaticValue;

    Assert.That(powerMod!.Value, Is.EqualTo(-1));
    Assert.That(toughnessMod!.Value, Is.EqualTo(0));
  }

  #endregion

  #region Real Card Examples

  [Test]
  public void Parse_LlanowarElves_Success()
  {
    // Classic mana dork
    var result = OracleTextParser.Parse("{T}: Add {G}.", "Llanowar Elves");

    Assert.That(result.Abilities, Has.Count.EqualTo(1));
    Assert.That(result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error), Is.Empty);
  }

  [Test]
  public void Parse_SolRing_Success()
  {
    var result = OracleTextParser.Parse("{T}: Add {C}{C}.", "Sol Ring");

    var ability = result.Abilities[0] as ActivatedAbilityNode;
    var effect = ability!.Effects[0] as AddManaEffect;

    Assert.That(effect!.ManaToAdd.Colorless, Is.EqualTo(2));
  }

  [Test]
  public void Parse_ThoughtVessel_Success()
  {
    // Card with multiple abilities - we should parse the activated one
    var result = OracleTextParser.Parse(
      "You have no maximum hand size.\n{T}: Add {C}.",
      "Thought Vessel"
    );

    // Should have 2 abilities: static (unparsed) and activated (parsed)
    var activatedAbilities = result.Abilities.OfType<ActivatedAbilityNode>().ToList();
    Assert.That(activatedAbilities, Has.Count.EqualTo(1));
  }

  #endregion

  #region Edge Cases

  [Test]
  public void Parse_NoTrailingPeriod_Success()
  {
    // Some cards don't have trailing periods
    var result = OracleTextParser.Parse("{T}: Add {G}", "Test Card");

    Assert.That(result.Abilities, Has.Count.EqualTo(1));
  }

  [Test]
  public void Parse_WithoutThisCreature_Success()
  {
    // "gets" without explicit subject
    var result = OracleTextParser.Parse("{R}: gets +1/+0 until end of turn.", "Test Card");

    var ability = result.Abilities[0] as ActivatedAbilityNode;
    Assert.That(ability!.Effects[0], Is.TypeOf<PTModificationEffect>());
  }

  #endregion
}
