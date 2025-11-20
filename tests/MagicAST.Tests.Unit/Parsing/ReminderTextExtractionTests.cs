using MagicAST.Core.Parsing;
using NUnit.Framework;

namespace MagicAST.Tests.Unit.Parsing;

/// <summary>
/// Unit tests for ExtractReminderText functionality in OracleTextParser.
/// Tests the extraction of reminder text from keyword ability strings.
/// </summary>
[TestFixture]
public class ReminderTextExtractionTests
{
  [Test]
  public void ExtractReminderText_SimpleKeywordWithReminder_ExtractsCorrectly()
  {
    var input = "Flying (This creature can't be blocked except by creatures with flying or reach.)";
    var result = OracleTextParser.Parse(input, "Test Card");

    Assert.That(result.Abilities, Has.Count.EqualTo(1));
    var ability = result.Abilities[0] as MagicAST.Core.AST.Nodes.Abilities.KeywordAbilityNode;
    Assert.That(ability, Is.Not.Null);
    Assert.That(
      ability!.ReminderText,
      Is.EqualTo("This creature can't be blocked except by creatures with flying or reach.")
    );
  }

  [Test]
  public void ExtractReminderText_KeywordWithoutReminder_NoReminderText()
  {
    var input = "Flying";
    var result = OracleTextParser.Parse(input, "Test Card");

    Assert.That(result.Abilities, Has.Count.EqualTo(1));
    var ability = result.Abilities[0] as MagicAST.Core.AST.Nodes.Abilities.KeywordAbilityNode;
    Assert.That(ability!.ReminderText, Is.Null);
  }

  [Test]
  public void ExtractReminderText_CyclingWithReminder_ExtractsManaCostAndReminder()
  {
    var input = "Cycling {2} ({2}, Discard this card: Draw a card.)";
    var result = OracleTextParser.Parse(input, "Test Card");

    Assert.That(result.Abilities, Has.Count.EqualTo(1));
    var ability = result.Abilities[0] as MagicAST.Core.AST.Nodes.Abilities.KeywordAbilityNode;
    Assert.That(ability, Is.Not.Null);
    Assert.That(ability!.Keyword, Is.EqualTo(MagicAST.Core.Keywords.Keyword.Cycling));
    Assert.That(ability.Cost, Is.Not.Null);
    Assert.That(ability.ReminderText, Is.EqualTo("{2}, Discard this card: Draw a card."));
  }

  [Test]
  public void ExtractReminderText_EquipWithReminder_ExtractsManaCostAndReminder()
  {
    var input = "Equip {2} ({2}: Attach to target creature you control. Equip only as a sorcery.)";
    var result = OracleTextParser.Parse(input, "Test Card");

    Assert.That(result.Abilities, Has.Count.EqualTo(1));
    var ability = result.Abilities[0] as MagicAST.Core.AST.Nodes.Abilities.KeywordAbilityNode;
    Assert.That(ability, Is.Not.Null);
    Assert.That(ability!.Keyword, Is.EqualTo(MagicAST.Core.Keywords.Keyword.Equip));
    Assert.That(ability.Cost, Is.Not.Null);
    Assert.That(ability.ReminderText, Is.Not.Null);
    Assert.That(ability.ReminderText, Does.Contain("Attach to target creature"));
  }

  [Test]
  public void ExtractReminderText_ProtectionWithReminder_ExtractsFilterAndReminder()
  {
    var input =
      "Protection from red (This creature can't be blocked, targeted, dealt damage, or enchanted by anything red.)";
    var result = OracleTextParser.Parse(input, "Test Card");

    Assert.That(result.Abilities, Has.Count.EqualTo(1));
    var ability = result.Abilities[0] as MagicAST.Core.AST.Nodes.Abilities.KeywordAbilityNode;
    Assert.That(ability, Is.Not.Null);
    Assert.That(ability!.Keyword, Is.EqualTo(MagicAST.Core.Keywords.Keyword.Protection));
    Assert.That(ability.Filter, Is.EqualTo("red"));
    Assert.That(ability.ReminderText, Is.Not.Null);
    Assert.That(ability.ReminderText, Does.Contain("can't be blocked"));
  }

  [Test]
  public void ExtractReminderText_MultipleKeywordsWithReminders_ExtractsAll()
  {
    var input = "Flying (Can't be blocked.), Vigilance (Doesn't tap.)";
    var result = OracleTextParser.Parse(input, "Test Card");

    Assert.That(result.Abilities, Has.Count.EqualTo(2));

    var flying = result.Abilities[0] as MagicAST.Core.AST.Nodes.Abilities.KeywordAbilityNode;
    Assert.That(flying!.ReminderText, Is.EqualTo("Can't be blocked."));

    var vigilance = result.Abilities[1] as MagicAST.Core.AST.Nodes.Abilities.KeywordAbilityNode;
    Assert.That(vigilance!.ReminderText, Is.EqualTo("Doesn't tap."));
  }

  [Test]
  public void ExtractReminderText_ReminderWithApostrophe_HandlesCorrectly()
  {
    var input = "Devoid (This card has no color.)";
    var result = OracleTextParser.Parse(input, "Test Card");

    Assert.That(result.Abilities, Has.Count.EqualTo(1));
    var ability = result.Abilities[0] as MagicAST.Core.AST.Nodes.Abilities.KeywordAbilityNode;
    Assert.That(ability!.ReminderText, Is.EqualTo("This card has no color."));
  }

  [Test]
  public void ExtractReminderText_ReminderWithCommas_HandlesCorrectly()
  {
    var input =
      "Absorb 3 (If a source would deal damage to this creature, prevent 3 of that damage.)";
    var result = OracleTextParser.Parse(input, "Test Card");

    Assert.That(result.Abilities, Has.Count.EqualTo(1));
    var ability = result.Abilities[0] as MagicAST.Core.AST.Nodes.Abilities.KeywordAbilityNode;
    Assert.That(ability!.ReminderText, Does.Contain("prevent 3"));
  }

  [Test]
  public void ExtractReminderText_NoClosingParen_TreatsAsNoReminder()
  {
    var input = "Flying (incomplete reminder";
    var result = OracleTextParser.Parse(input, "Test Card");

    // Should either parse or report diagnostic, but not crash
    Assert.That(result, Is.Not.Null);
  }
}
