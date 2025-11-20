using MagicAST.Core.AST.Nodes.Abilities;
using MagicAST.Core.AST.Nodes.Effects;
using MagicAST.Core.AST.Nodes.Expressions;
using MagicAST.Core.AST.Nodes.Triggers;
using MagicAST.Core.Parsing;
using Superpower;

namespace MagicAST.Tests.Unit.Parsing;

[TestFixture]
public class TriggeredAbilityTests
{
  private const string TestCardName = "Test Card";

  #region Diagnostic Tests

  [Test]
  public void Diagnostics_WhenEntersDraw_ShowsParserOutput()
  {
    var result = OracleTextParser.Parse("When this creature enters, draw a card.", TestCardName);

    Console.WriteLine($"Abilities parsed: {result.Abilities.Count}");
    Console.WriteLine($"Diagnostics: {result.Diagnostics.Count}");
    foreach (var diag in result.Diagnostics)
    {
      Console.WriteLine($"  - {diag.Descriptor.Id}: {diag.GetMessage()}");
    }

    if (result.Abilities.Count > 0)
    {
      Console.WriteLine($"First ability type: {result.Abilities[0].GetType().Name}");
    }
  }

  [Test]
  public void Diagnostics_DirectTriggerParser_ShowsOutput()
  {
    var result = TriggeredAbilityParser.Parse(
      "When this creature enters, draw a card.",
      TestCardName
    );

    Console.WriteLine($"Direct parser - Abilities: {result.Abilities.Count}");
    Console.WriteLine($"Direct parser - Diagnostics: {result.Diagnostics.Count}");
    foreach (var diag in result.Diagnostics)
    {
      Console.WriteLine($"  - {diag.Descriptor.Id}: {diag.GetMessage()}");
    }
  }

  [Test]
  public void Diagnostics_EffectParserDetails_ShowsWhereItFails()
  {
    // Test "card" vs "cards"
    var cardResult = EffectParser.Draw.TryParse("draw a card");
    Console.WriteLine($"'draw a card': HasValue={cardResult.HasValue}");
    if (!cardResult.HasValue)
    {
      Console.WriteLine($"  Position: {cardResult.ErrorPosition} (length={" draw a card".Length})");
      Console.WriteLine($"  Expectations: {string.Join(", ", cardResult.Expectations ?? [])}");
    }
    else
    {
      Console.WriteLine($"  SUCCESS - parsed 'draw a card'");
    }

    var cardsResult = EffectParser.Draw.TryParse("draw two cards");
    Console.WriteLine($"'draw two cards': HasValue={cardsResult.HasValue}");
    if (!cardsResult.HasValue)
    {
      Console.WriteLine($"  Position: {cardsResult.ErrorPosition}");
    }
    else
    {
      Console.WriteLine($"  SUCCESS - parsed 'draw two cards'");
    }
  }

  [Test]
  public void Diagnostics_CardNameTriggers_ShowPatterns()
  {
    // Test with card name
    var mullResult = TriggerParser.ETBTrigger.TryParse("When Mulldrifter enters the battlefield");
    Console.WriteLine($"'When Mulldrifter enters the battlefield': HasValue={mullResult.HasValue}");
    if (!mullResult.HasValue)
    {
      Console.WriteLine($"  Position: {mullResult.ErrorPosition}");
      Console.WriteLine($"  Expectations: {string.Join(", ", mullResult.Expectations ?? [])}");
    }

    // Test with "a creature"
    var creatureResult = TriggerParser.ETBTrigger.TryParse("Whenever a creature enters");
    Console.WriteLine($"'Whenever a creature enters': HasValue={creatureResult.HasValue}");
    if (!creatureResult.HasValue)
    {
      Console.WriteLine($"  Position: {creatureResult.ErrorPosition}");
    }

    // Test attack with "a creature"
    var attackResult = TriggerParser.AttackTrigger.TryParse("Whenever a creature attacks");
    Console.WriteLine($"'Whenever a creature attacks': HasValue={attackResult.HasValue}");
    if (!attackResult.HasValue)
    {
      Console.WriteLine($"  Position: {attackResult.ErrorPosition}");
    }
  }

  [Test]
  public void Diagnostics_RemainingFailures_ShowDetails()
  {
    // Test ETB trigger with different subjects to see if SubjectWords is working
    var etb1 = TriggerParser.ETBTrigger.TryParse("When a creature enters");
    Console.WriteLine($"ETB 'When a creature enters': HasValue={etb1.HasValue}");

    var etb2 = TriggerParser.ETBTrigger.TryParse("When this creature enters");
    Console.WriteLine($"ETB 'When this creature enters': HasValue={etb2.HasValue}");

    // Test death trigger
    var deathTrigger = TriggerParser.DeathTrigger.TryParse("Whenever a creature dies");
    Console.WriteLine(
      $"\\nDeath trigger 'Whenever a creature dies': HasValue={deathTrigger.HasValue}"
    );
    if (!deathTrigger.HasValue)
    {
      Console.WriteLine($"  Position: {deathTrigger.ErrorPosition}");
      Console.WriteLine($"  Expectations: {string.Join(", ", deathTrigger.Expectations ?? [])}");
    }

    // Also test with simpler subjects
    var death2 = TriggerParser.DeathTrigger.TryParse("When this creature dies");
    Console.WriteLine($"Death trigger 'When this creature dies': HasValue={death2.HasValue}");
    if (!death2.HasValue)
    {
      Console.WriteLine($"  Position: {death2.ErrorPosition}");
    }

    // Test life gain effect
    var lifeGain = EffectParser.GainLife.TryParse("you gain 2 life");
    Console.WriteLine($"Life gain: HasValue={lifeGain.HasValue}");

    // Test death trigger + life gain combined
    var deathLife = TriggeredAbilityParser.TriggeredAbility.TryParse(
      "Whenever a creature dies, you gain 2 life"
    );
    Console.WriteLine($"Combined death + life gain: HasValue={deathLife.HasValue}");
    if (!deathLife.HasValue)
    {
      Console.WriteLine($"  Position: {deathLife.ErrorPosition}");
      Console.WriteLine($"  Expectations: {string.Join(", ", deathLife.Expectations ?? [])}");
    }

    // Test LoseLife effect alone
    var loseLife = EffectParser.LoseLife.TryParse("each opponent loses 1 life");
    Console.WriteLine($"LoseLife 'each opponent loses 1 life': HasValue={loseLife.HasValue}");
    if (!loseLife.HasValue)
    {
      Console.WriteLine($"  Position: {loseLife.ErrorPosition}");
    }

    // Test Gray Merchant trigger alone
    var gmTrigger = TriggerParser.ETBTrigger.TryParse("When Gray Merchant of Asphodel enters");
    Console.WriteLine($"Gray Merchant trigger: HasValue={gmTrigger.HasValue}");

    // Test Gray Merchant combined
    var grayMerchant = TriggeredAbilityParser.TriggeredAbility.TryParse(
      "When Gray Merchant of Asphodel enters, each opponent loses 1 life"
    );
    Console.WriteLine($"Gray Merchant combined: HasValue={grayMerchant.HasValue}");
    if (!grayMerchant.HasValue)
    {
      Console.WriteLine($"  Position: {grayMerchant.ErrorPosition}");
      Console.WriteLine($"  Expectations: {string.Join(", ", grayMerchant.Expectations ?? [])}");
    }
  }

  [Test]
  public void Diagnostics_TriggerParserForACreature_ShowsDetails()
  {
    var trigger1 = TriggerParser.ETBTrigger.TryParse("Whenever a creature enters");
    Console.WriteLine($"'Whenever a creature enters': HasValue={trigger1.HasValue}");
    if (!trigger1.HasValue)
    {
      Console.WriteLine($"  Position: {trigger1.ErrorPosition}");
      Console.WriteLine($"  Expectations: {string.Join(", ", trigger1.Expectations ?? [])}");
    }

    var combined = TriggeredAbilityParser.TriggeredAbility.TryParse(
      "Whenever a creature enters, draw a card"
    );
    Console.WriteLine($"'Whenever a creature enters, draw a card': HasValue={combined.HasValue}");
    if (!combined.HasValue)
    {
      Console.WriteLine($"  Position: {combined.ErrorPosition}");
      Console.WriteLine($"  Expectations: {string.Join(", ", combined.Expectations ?? [])}");
    }
  }

  #endregion

  #region ETB Triggers

  [Test]
  public void Parse_WhenEntersDraw_ParsesETBDrawTrigger()
  {
    var result = OracleTextParser.Parse("When this creature enters, draw a card.", TestCardName);

    if (result.Diagnostics.Count > 0)
    {
      Console.WriteLine("Diagnostics found:");
      foreach (var diag in result.Diagnostics)
      {
        Console.WriteLine($"  - {diag.Descriptor.Id}: {diag.GetMessage()}");
      }
    }

    Assert.That(result.Abilities, Has.Count.EqualTo(1), "Expected 1 ability to be parsed");
    var ability = result.Abilities[0] as TriggeredAbilityNode;
    Assert.That(ability, Is.Not.Null);
    Assert.That(ability!.Trigger.Type, Is.EqualTo(EventType.Enters));
    Assert.That(ability.Effects, Has.Count.EqualTo(1));
    Assert.That(ability.Effects[0], Is.TypeOf<DrawEffect>());
    var drawEffect = (DrawEffect)ability.Effects[0];
    var staticValue = drawEffect.NumberOfCards as StaticValue;
    Assert.That(staticValue!.Value, Is.EqualTo(1));
  }

  [Test]
  public void Parse_WhenEntersGainLife_ParsesETBLifeGainTrigger()
  {
    var result = OracleTextParser.Parse(
      "When this creature enters, you gain 2 life.",
      TestCardName
    );

    Assert.That(result.Abilities, Has.Count.EqualTo(1));
    var ability = result.Abilities[0] as TriggeredAbilityNode;
    Assert.That(ability, Is.Not.Null);
    Assert.That(ability!.Trigger.Type, Is.EqualTo(EventType.Enters));
    Assert.That(ability.Effects, Has.Count.EqualTo(1));
    Assert.That(ability.Effects[0], Is.TypeOf<GainLifeEffect>());
    var lifeEffect = (GainLifeEffect)ability.Effects[0];
    var amount = lifeEffect.Amount as StaticValue;
    Assert.That(amount!.Value, Is.EqualTo(2));
    Assert.That(lifeEffect.Target, Is.EqualTo(LifeTarget.You));
  }

  [Test]
  public void Parse_WheneverEnters_ParsesETBTrigger()
  {
    var result = OracleTextParser.Parse("Whenever a creature enters, draw a card.", TestCardName);

    if (result.Diagnostics.Count > 0)
    {
      Console.WriteLine("Parse_WheneverEnters diagnostics:");
      foreach (var d in result.Diagnostics)
        Console.WriteLine($"  {d.GetMessage()}");
    }

    Assert.That(result.Abilities, Has.Count.EqualTo(1));
    var ability = result.Abilities[0] as TriggeredAbilityNode;
    Assert.That(ability, Is.Not.Null);
    Assert.That(ability!.Trigger.Type, Is.EqualTo(EventType.Enters));
  }

  [Test]
  public void Parse_WhenEntersBattlefield_ParsesWithFullText()
  {
    var result = OracleTextParser.Parse(
      "When this creature enters the battlefield, you gain 3 life.",
      TestCardName
    );

    Assert.That(result.Abilities, Has.Count.EqualTo(1));
    var ability = result.Abilities[0] as TriggeredAbilityNode;
    Assert.That(ability, Is.Not.Null);
    Assert.That(ability!.Trigger.Type, Is.EqualTo(EventType.Enters));
    var lifeEffect = ability.Effects[0] as GainLifeEffect;
    var amount = lifeEffect!.Amount as StaticValue;
    Assert.That(amount!.Value, Is.EqualTo(3));
  }

  #endregion

  #region Combat Triggers

  [Test]
  public void Parse_WheneverAttacks_ParsesAttackTrigger()
  {
    var result = OracleTextParser.Parse(
      "Whenever this creature attacks, you gain 1 life.",
      TestCardName
    );

    Assert.That(result.Abilities, Has.Count.EqualTo(1));
    var ability = result.Abilities[0] as TriggeredAbilityNode;
    Assert.That(ability, Is.Not.Null);
    Assert.That(ability!.Trigger.Type, Is.EqualTo(EventType.Attacks));
    Assert.That(ability.Effects[0], Is.TypeOf<GainLifeEffect>());
  }

  [Test]
  public void Parse_WhenAttacks_ParsesWithWhenKeyword()
  {
    var result = OracleTextParser.Parse("When this creature attacks, draw a card.", TestCardName);

    Assert.That(result.Abilities, Has.Count.EqualTo(1));
    var ability = result.Abilities[0] as TriggeredAbilityNode;
    Assert.That(ability, Is.Not.Null);
    Assert.That(ability!.Trigger.Type, Is.EqualTo(EventType.Attacks));
  }

  [Test]
  public void Parse_WheneverACreatureAttacks_ParsesUnspecifiedAttacker()
  {
    var result = OracleTextParser.Parse(
      "Whenever a creature attacks, you gain 1 life.",
      TestCardName
    );

    Assert.That(result.Abilities, Has.Count.EqualTo(1));
    var ability = result.Abilities[0] as TriggeredAbilityNode;
    Assert.That(ability, Is.Not.Null);
    Assert.That(ability!.Trigger.Type, Is.EqualTo(EventType.Attacks));
  }

  #endregion

  #region Death Triggers

  [Test]
  public void Parse_WhenDies_ParsesDeathTrigger()
  {
    var result = OracleTextParser.Parse("When this creature dies, draw a card.", TestCardName);

    Assert.That(result.Abilities, Has.Count.EqualTo(1));
    var ability = result.Abilities[0] as TriggeredAbilityNode;
    Assert.That(ability, Is.Not.Null);
    Assert.That(ability!.Trigger.Type, Is.EqualTo(EventType.Dies));
    Assert.That(ability.Effects[0], Is.TypeOf<DrawEffect>());
  }

  [Test]
  public void Parse_WheneverDies_ParsesWithWhenever()
  {
    var result = OracleTextParser.Parse("Whenever a creature dies, you gain 2 life.", TestCardName);

    Assert.That(result.Abilities, Has.Count.EqualTo(1));
    var ability = result.Abilities[0] as TriggeredAbilityNode;
    Assert.That(ability, Is.Not.Null);
    Assert.That(ability!.Trigger.Type, Is.EqualTo(EventType.Dies));
    var lifeEffect = ability.Effects[0] as GainLifeEffect;
    var amount = lifeEffect!.Amount as StaticValue;
    Assert.That(amount!.Value, Is.EqualTo(2));
  }

  #endregion

  #region Phase Triggers

  [Test]
  public void Parse_AtBeginningOfUpkeep_ParsesUpkeepTrigger()
  {
    var result = OracleTextParser.Parse(
      "At the beginning of your upkeep, draw a card.",
      TestCardName
    );

    Assert.That(result.Abilities, Has.Count.EqualTo(1));
    var ability = result.Abilities[0] as TriggeredAbilityNode;
    Assert.That(ability, Is.Not.Null);
    Assert.That(ability!.Trigger.Type, Is.EqualTo(EventType.PhaseBegin));
    Assert.That(ability.Effects[0], Is.TypeOf<DrawEffect>());
  }

  [Test]
  public void Parse_AtBeginningOfUpkeepLifeGain_ParsesUpkeepLifeEffect()
  {
    var result = OracleTextParser.Parse(
      "At the beginning of your upkeep, you gain 1 life.",
      TestCardName
    );

    Assert.That(result.Abilities, Has.Count.EqualTo(1));
    var ability = result.Abilities[0] as TriggeredAbilityNode;
    Assert.That(ability, Is.Not.Null);
    var lifeEffect = ability!.Effects[0] as GainLifeEffect;
    var amount = lifeEffect!.Amount as StaticValue;
    Assert.That(amount!.Value, Is.EqualTo(1));
  }

  #endregion

  #region Real Card Examples

  [Test]
  public void Diagnostics_MulldrifterVariations_ShowWhichFormWorks()
  {
    // Without "the battlefield"
    var short1 = TriggerParser.ETBTrigger.TryParse("When Mulldrifter enters");
    Console.WriteLine($"'When Mulldrifter enters': HasValue={short1.HasValue}");
    if (!short1.HasValue)
    {
      Console.WriteLine(
        $"  Position: {short1.ErrorPosition}, Expectations: {string.Join(", ", short1.Expectations ?? [])}"
      );
    }

    // With "the battlefield"
    var long1 = TriggerParser.ETBTrigger.TryParse("When Mulldrifter enters the battlefield");
    Console.WriteLine($"'When Mulldrifter enters the battlefield': HasValue={long1.HasValue}");
    if (!long1.HasValue)
    {
      Console.WriteLine(
        $"  Position: {long1.ErrorPosition}, Expectations: {string.Join(", ", long1.Expectations ?? [])}"
      );
    }

    // Full Mulldrifter text
    var combined = TriggeredAbilityParser.TriggeredAbility.TryParse(
      "When Mulldrifter enters the battlefield, draw two cards"
    );
    Console.WriteLine($"Full Mulldrifter: HasValue={combined.HasValue}");
    if (!combined.HasValue)
    {
      Console.WriteLine(
        $"  Position: {combined.ErrorPosition}, Expectations: {string.Join(", ", combined.Expectations ?? [])}"
      );
    }
  }

  [Test]
  public void Parse_Mulldrifter_ParsesETBDraw()
  {
    // Mulldrifter: "When Mulldrifter enters the battlefield, draw two cards."
    // TODO: Requires parsing card names as subjects, not just "this creature" or "a creature"
    var result = OracleTextParser.Parse(
      "When Mulldrifter enters the battlefield, draw two cards.",
      "Mulldrifter"
    );

    Assert.That(result.Abilities, Has.Count.EqualTo(1));
    var ability = result.Abilities[0] as TriggeredAbilityNode;
    Assert.That(ability, Is.Not.Null);
    Assert.That(ability!.Trigger.Type, Is.EqualTo(EventType.Enters));
    var drawEffect = ability.Effects[0] as DrawEffect;
    var staticValue = drawEffect!.NumberOfCards as StaticValue;
    Assert.That(staticValue!.Value, Is.EqualTo(2));
  }

  [Test]
  public void Parse_GrayMerchant_ParsesETBLifeLoss()
  {
    // Gray Merchant of Asphodel: "When Gray Merchant of Asphodel enters, each opponent loses X life..."
    var result = OracleTextParser.Parse(
      "When Gray Merchant of Asphodel enters, each opponent loses 1 life.",
      "Gray Merchant of Asphodel"
    );

    Assert.That(result.Abilities, Has.Count.EqualTo(1));
    var ability = result.Abilities[0] as TriggeredAbilityNode;
    Assert.That(ability, Is.Not.Null);
    Assert.That(ability!.Trigger.Type, Is.EqualTo(EventType.Enters));
    var loseEffect = ability.Effects[0] as LoseLifeEffect;
    Assert.That(loseEffect, Is.Not.Null);
    var amount = loseEffect!.Amount as StaticValue;
    Assert.That(amount!.Value, Is.EqualTo(1));
    Assert.That(loseEffect.Target, Is.EqualTo(LifeTarget.EachOpponent));
  }

  [Test]
  public void Parse_Bitterblossom_ParsesUpkeepTrigger()
  {
    // Simplified Bitterblossom trigger
    var result = OracleTextParser.Parse(
      "At the beginning of your upkeep, you lose 1 life.",
      "Bitterblossom"
    );

    Assert.That(result.Abilities, Has.Count.EqualTo(1));
    var ability = result.Abilities[0] as TriggeredAbilityNode;
    Assert.That(ability, Is.Not.Null);
    Assert.That(ability!.Trigger.Type, Is.EqualTo(EventType.PhaseBegin));
    var loseEffect = ability.Effects[0] as LoseLifeEffect;
    var amount = loseEffect!.Amount as StaticValue;
    Assert.That(amount!.Value, Is.EqualTo(1));
  }

  [Test]
  public void Parse_AjanisPridemate_ParsesGenericLifeGainTrigger()
  {
    // Ajani's Pridemate: "Whenever you gain life, put a +1/+1 counter on Ajani's Pridemate."
    // For now, test with a simpler effect - this won't parse yet
    var result = OracleTextParser.Parse(
      "Whenever you gain life, draw a card.",
      "Ajani's Pridemate"
    );

    // This won't parse yet - we haven't implemented "you gain life" as a trigger event
    // But we can still check for graceful failure
    Assert.That(result.Diagnostics, Is.Not.Empty);
  }

  #endregion

  #region Edge Cases

  [Test]
  public void Parse_EmptyString_ReturnsNoAbilities()
  {
    var result = OracleTextParser.Parse("", TestCardName);

    Assert.That(result.Abilities, Is.Empty);
    Assert.That(result.Diagnostics, Is.Empty);
  }

  [Test]
  public void Parse_InvalidTrigger_ReportsDiagnostic()
  {
    var result = OracleTextParser.Parse("When something weird happens, do stuff.", TestCardName);

    // Should fail to parse and report diagnostic
    Assert.That(result.Diagnostics, Is.Not.Empty);
  }

  [Test]
  public void Parse_TriggerWithoutComma_ReportsDiagnostic()
  {
    var result = OracleTextParser.Parse("When this creature enters draw a card.", TestCardName);

    // Missing comma should fail
    Assert.That(result.Diagnostics, Is.Not.Empty);
  }

  [Test]
  public void Parse_MultipleAbilities_ParsesEach()
  {
    var result = OracleTextParser.Parse(
      "When this creature enters, draw a card.\nWhenever this creature attacks, you gain 1 life.",
      TestCardName
    );

    Assert.That(result.Abilities, Has.Count.EqualTo(2));
    Assert.That(result.Abilities[0], Is.TypeOf<TriggeredAbilityNode>());
    Assert.That(result.Abilities[1], Is.TypeOf<TriggeredAbilityNode>());
  }

  #endregion

  #region Direct Parser Tests

  [Test]
  public void TriggeredAbilityParser_ETBDraw_ParsesDirectly()
  {
    var result = TriggeredAbilityParser.Parse(
      "When this creature enters, draw a card.",
      TestCardName
    );

    Assert.That(result.Abilities, Has.Count.EqualTo(1));
    var ability = result.Abilities[0] as TriggeredAbilityNode;
    Assert.That(ability, Is.Not.Null);
  }

  [Test]
  public void TriggeredAbilityParser_AttackLifeGain_ParsesDirectly()
  {
    var result = TriggeredAbilityParser.Parse(
      "Whenever this creature attacks, you gain 1 life.",
      TestCardName
    );

    Assert.That(result.Abilities, Has.Count.EqualTo(1));
    var ability = result.Abilities[0] as TriggeredAbilityNode;
    Assert.That(ability!.Trigger.Type, Is.EqualTo(EventType.Attacks));
    var lifeEffect = ability.Effects[0] as GainLifeEffect;
    var amount = lifeEffect!.Amount as StaticValue;
    Assert.That(amount!.Value, Is.EqualTo(1));
  }

  #endregion
}
