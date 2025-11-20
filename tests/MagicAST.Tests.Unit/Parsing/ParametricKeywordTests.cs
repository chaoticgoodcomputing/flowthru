using MagicAST.Core.AST.Nodes.Abilities;
using MagicAST.Core.Keywords;
using MagicAST.Core.Parsing;
using NUnit.Framework;

namespace MagicAST.Tests.Unit.Parsing;

/// <summary>
/// Unit tests for parametric keyword parsing.
/// Tests Phase 1 functionality: Cycling {N}, Equip {N}, Absorb N, Protection from X.
/// </summary>
[TestFixture]
public class ParametricKeywordTests
{
  #region Cycling Tests

  [TestCase("Cycling {1}", 1)]
  [TestCase("Cycling {2}", 2)]
  [TestCase("Cycling {3}", 3)]
  [TestCase("Cycling {1}{G}", 2)]
  [TestCase("Cycling {2}{R}", 3)]
  public void Parse_Cycling_CorrectManaCost(string input, int expectedCMC)
  {
    var result = OracleTextParser.Parse(input, "Test Card");

    Assert.That(result.Abilities, Has.Count.EqualTo(1));
    var ability = result.Abilities[0] as KeywordAbilityNode;
    Assert.That(ability, Is.Not.Null);
    Assert.That(ability!.Keyword, Is.EqualTo(Keyword.Cycling));
    Assert.That(ability.Cost, Is.Not.Null);
    Assert.That(ability.Cost!.Cost.ConvertedManaCost, Is.EqualTo(expectedCMC));
  }

  [Test]
  public void Parse_CyclingWithX_ContainsX()
  {
    var result = OracleTextParser.Parse("Cycling {X}", "Test Card");

    var ability = result.Abilities[0] as KeywordAbilityNode;
    Assert.That(ability!.Cost!.Cost.ContainsX, Is.True);
  }

  #endregion

  #region Equip Tests

  [TestCase("Equip {1}", 1)]
  [TestCase("Equip {2}", 2)]
  [TestCase("Equip {3}", 3)]
  [TestCase("Equip {4}", 4)]
  [TestCase("Equip {W}{W}", 2)]
  public void Parse_Equip_CorrectManaCost(string input, int expectedCMC)
  {
    var result = OracleTextParser.Parse(input, "Test Card");

    Assert.That(result.Abilities, Has.Count.EqualTo(1));
    var ability = result.Abilities[0] as KeywordAbilityNode;
    Assert.That(ability, Is.Not.Null);
    Assert.That(ability!.Keyword, Is.EqualTo(Keyword.Equip));
    Assert.That(ability.Cost, Is.Not.Null);
    Assert.That(ability.Cost!.Cost.ConvertedManaCost, Is.EqualTo(expectedCMC));
  }

  #endregion

  #region Other Mana Cost Keywords

  [TestCase("Ninjutsu {U}{B}", Keyword.Ninjutsu, 2)]
  [TestCase("Madness {1}{R}", Keyword.Madness, 2)]
  [TestCase("Transmute {1}{U}{U}", Keyword.Transmute, 3)]
  public void Parse_OtherManaCostKeywords_ParseCorrectly(
    string input,
    Keyword expectedKeyword,
    int expectedCMC
  )
  {
    var result = OracleTextParser.Parse(input, "Test Card");

    Assert.That(result.Abilities, Has.Count.EqualTo(1));
    var ability = result.Abilities[0] as KeywordAbilityNode;
    Assert.That(ability, Is.Not.Null);
    Assert.That(ability!.Keyword, Is.EqualTo(expectedKeyword));
    Assert.That(ability.Cost!.Cost.ConvertedManaCost, Is.EqualTo(expectedCMC));
  }

  #endregion

  #region Amount Keywords

  [TestCase("Absorb 1", Keyword.Absorb, 1)]
  [TestCase("Absorb 3", Keyword.Absorb, 3)]
  [TestCase("Afflict 2", Keyword.Afflict, 2)]
  [TestCase("Afflict 4", Keyword.Afflict, 4)]
  [TestCase("Annihilator 2", Keyword.Annihilator, 2)]
  [TestCase("Annihilator 4", Keyword.Annihilator, 4)]
  [TestCase("Bushido 1", Keyword.Bushido, 1)]
  [TestCase("Bushido 2", Keyword.Bushido, 2)]
  [TestCase("Rampage 1", Keyword.Rampage, 1)]
  [TestCase("Fading 3", Keyword.Fading, 3)]
  [TestCase("Vanishing 2", Keyword.Vanishing, 2)]
  [TestCase("Modular 1", Keyword.Modular, 1)]
  [TestCase("Crew 3", Keyword.Crew, 3)]
  public void Parse_AmountKeywords_ParseCorrectly(
    string input,
    Keyword expectedKeyword,
    int expectedAmount
  )
  {
    var result = OracleTextParser.Parse(input, "Test Card");

    Assert.That(result.Abilities, Has.Count.EqualTo(1));
    var ability = result.Abilities[0] as KeywordAbilityNode;
    Assert.That(ability, Is.Not.Null);
    Assert.That(ability!.Keyword, Is.EqualTo(expectedKeyword));
    Assert.That(ability.Amount, Is.EqualTo(expectedAmount));
    Assert.That(ability.Cost, Is.Null);
    Assert.That(ability.Filter, Is.Null);
  }

  #endregion

  #region Protection Tests

  [TestCase("Protection from red", "red")]
  [TestCase("Protection from blue", "blue")]
  [TestCase("Protection from white", "white")]
  [TestCase("Protection from black", "black")]
  [TestCase("Protection from green", "green")]
  [TestCase("Protection from artifacts", "artifacts")]
  [TestCase("Protection from creatures", "creatures")]
  [TestCase("Protection from enchantments", "enchantments")]
  [TestCase("Protection from Demons", "Demons")]
  public void Parse_Protection_CorrectFilter(string input, string expectedFilter)
  {
    var result = OracleTextParser.Parse(input, "Test Card");

    Assert.That(result.Abilities, Has.Count.EqualTo(1));
    var ability = result.Abilities[0] as KeywordAbilityNode;
    Assert.That(ability, Is.Not.Null);
    Assert.That(ability!.Keyword, Is.EqualTo(Keyword.Protection));
    Assert.That(ability.Filter, Is.EqualTo(expectedFilter));
    Assert.That(ability.Amount, Is.Null);
    Assert.That(ability.Cost, Is.Null);
  }

  #endregion

  #region Case Sensitivity

  [TestCase("cycling {2}")]
  [TestCase("CYCLING {2}")]
  [TestCase("CyCLiNg {2}")]
  [TestCase("Cycling {2}")]
  public void Parse_CyclingCaseInsensitive_ParsesCorrectly(string input)
  {
    var result = OracleTextParser.Parse(input, "Test Card");

    var ability = result.Abilities[0] as KeywordAbilityNode;
    Assert.That(ability!.Keyword, Is.EqualTo(Keyword.Cycling));
  }

  [TestCase("equip {2}")]
  [TestCase("EQUIP {2}")]
  [TestCase("EqUiP {2}")]
  public void Parse_EquipCaseInsensitive_ParsesCorrectly(string input)
  {
    var result = OracleTextParser.Parse(input, "Test Card");

    var ability = result.Abilities[0] as KeywordAbilityNode;
    Assert.That(ability!.Keyword, Is.EqualTo(Keyword.Equip));
  }

  [TestCase("absorb 3")]
  [TestCase("ABSORB 3")]
  [TestCase("AbSoRb 3")]
  public void Parse_AbsorbCaseInsensitive_ParsesCorrectly(string input)
  {
    var result = OracleTextParser.Parse(input, "Test Card");

    var ability = result.Abilities[0] as KeywordAbilityNode;
    Assert.That(ability!.Keyword, Is.EqualTo(Keyword.Absorb));
    Assert.That(ability.Amount, Is.EqualTo(3));
  }

  [TestCase("protection from red")]
  [TestCase("PROTECTION FROM RED")]
  [TestCase("Protection From Red")]
  public void Parse_ProtectionCaseInsensitive_ParsesCorrectly(string input)
  {
    var result = OracleTextParser.Parse(input, "Test Card");

    var ability = result.Abilities[0] as KeywordAbilityNode;
    Assert.That(ability!.Keyword, Is.EqualTo(Keyword.Protection));
    Assert.That(ability.Filter, Does.Match("(?i)red")); // Case-insensitive match
  }

  #endregion

  #region Mixed Keywords

  [Test]
  public void Parse_MixedSimpleAndParametric_CommaSeparated()
  {
    var result = OracleTextParser.Parse("Flying, Equip {2}", "Test Card");

    Assert.That(result.Abilities, Has.Count.EqualTo(2));

    var flying = result.Abilities[0] as KeywordAbilityNode;
    Assert.That(flying!.Keyword, Is.EqualTo(Keyword.Flying));
    Assert.That(flying.Cost, Is.Null);

    var equip = result.Abilities[1] as KeywordAbilityNode;
    Assert.That(equip!.Keyword, Is.EqualTo(Keyword.Equip));
    Assert.That(equip.Cost, Is.Not.Null);
  }

  [Test]
  public void Parse_MixedParametricKeywords_NewlineSeparated()
  {
    var input = "Devoid\nAnnihilator 2";
    var result = OracleTextParser.Parse(input, "Test Card");

    Assert.That(result.Abilities, Has.Count.EqualTo(2));

    var devoid = result.Abilities[0] as KeywordAbilityNode;
    Assert.That(devoid!.Keyword, Is.EqualTo(Keyword.Devoid));

    var annihilator = result.Abilities[1] as KeywordAbilityNode;
    Assert.That(annihilator!.Keyword, Is.EqualTo(Keyword.Annihilator));
    Assert.That(annihilator.Amount, Is.EqualTo(2));
  }

  [Test]
  public void Parse_ThreeParametricKeywords_ParsesAll()
  {
    var input = "Cycling {2}, Absorb 3, Protection from red";
    var result = OracleTextParser.Parse(input, "Test Card");

    Assert.That(result.Abilities, Has.Count.EqualTo(3));

    var cycling = result.Abilities[0] as KeywordAbilityNode;
    Assert.That(cycling!.Keyword, Is.EqualTo(Keyword.Cycling));

    var absorb = result.Abilities[1] as KeywordAbilityNode;
    Assert.That(absorb!.Keyword, Is.EqualTo(Keyword.Absorb));

    var protection = result.Abilities[2] as KeywordAbilityNode;
    Assert.That(protection!.Keyword, Is.EqualTo(Keyword.Protection));
  }

  #endregion

  #region Validation Tests

  [Test]
  public void Parse_CyclingWithoutCost_DoesNotParse()
  {
    var result = OracleTextParser.Parse("Cycling", "Test Card");

    // Should either not parse or report diagnostic
    if (result.Abilities.Count > 0)
    {
      var ability = result.Abilities[0] as KeywordAbilityNode;
      Assert.That(ability!.Keyword, Is.Not.EqualTo(Keyword.Cycling));
    }
  }

  [Test]
  public void Parse_EquipWithoutCost_DoesNotParse()
  {
    var result = OracleTextParser.Parse("Equip", "Test Card");

    // Should either not parse or report diagnostic
    if (result.Abilities.Count > 0)
    {
      var ability = result.Abilities[0] as KeywordAbilityNode;
      Assert.That(ability!.Keyword, Is.Not.EqualTo(Keyword.Equip));
    }
  }

  [Test]
  public void Parse_AbsorbWithoutAmount_DoesNotParse()
  {
    var result = OracleTextParser.Parse("Absorb", "Test Card");

    // Should either not parse or report diagnostic
    if (result.Abilities.Count > 0)
    {
      var ability = result.Abilities[0] as KeywordAbilityNode;
      Assert.That(ability!.Keyword, Is.Not.EqualTo(Keyword.Absorb));
    }
  }

  #endregion
}
