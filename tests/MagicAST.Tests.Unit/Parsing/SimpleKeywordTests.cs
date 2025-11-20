using MagicAST.Core.AST.Nodes.Abilities;
using MagicAST.Core.Keywords;
using MagicAST.Core.Parsing;
using NUnit.Framework;

namespace MagicAST.Tests.Unit.Parsing;

/// <summary>
/// Unit tests for simple keyword parsing (Phase 0 and Phase 1 simple keywords).
/// Tests keywords without parameters like Flying, Haste, Devoid, etc.
/// </summary>
[TestFixture]
public class SimpleKeywordTests
{
  #region Phase 0 Evasion Keywords

  [TestCase("Flying", Keyword.Flying)]
  [TestCase("Menace", Keyword.Menace)]
  [TestCase("Fear", Keyword.Fear)]
  [TestCase("Intimidate", Keyword.Intimidate)]
  [TestCase("Shadow", Keyword.Shadow)]
  [TestCase("Horsemanship", Keyword.Horsemanship)]
  [TestCase("Skulk", Keyword.Skulk)]
  [TestCase("Unblockable", Keyword.Unblockable)]
  public void Parse_EvasionKeywords_ParseCorrectly(string input, Keyword expected)
  {
    var result = OracleTextParser.Parse(input, "Test Card");

    Assert.That(result.Abilities, Has.Count.EqualTo(1));
    var ability = result.Abilities[0] as KeywordAbilityNode;
    Assert.That(ability, Is.Not.Null);
    Assert.That(ability!.Keyword, Is.EqualTo(expected));
  }

  #endregion

  #region Phase 0 Combat Keywords

  [TestCase("Vigilance", Keyword.Vigilance)]
  [TestCase("Haste", Keyword.Haste)]
  [TestCase("First strike", Keyword.FirstStrike)]
  [TestCase("Double strike", Keyword.DoubleStrike)]
  [TestCase("Deathtouch", Keyword.Deathtouch)]
  [TestCase("Lifelink", Keyword.Lifelink)]
  [TestCase("Trample", Keyword.Trample)]
  [TestCase("Defender", Keyword.Defender)]
  [TestCase("Reach", Keyword.Reach)]
  [TestCase("Flanking", Keyword.Flanking)]
  [TestCase("Banding", Keyword.Banding)]
  public void Parse_CombatKeywords_ParseCorrectly(string input, Keyword expected)
  {
    var result = OracleTextParser.Parse(input, "Test Card");

    Assert.That(result.Abilities, Has.Count.EqualTo(1));
    var ability = result.Abilities[0] as KeywordAbilityNode;
    Assert.That(ability!.Keyword, Is.EqualTo(expected));
  }

  #endregion

  #region Phase 0 Protection Keywords

  [TestCase("Hexproof", Keyword.Hexproof)]
  [TestCase("Shroud", Keyword.Shroud)]
  [TestCase("Indestructible", Keyword.Indestructible)]
  [TestCase("Ward", Keyword.Ward)]
  [TestCase("Totem armor", Keyword.TotemArmor)]
  public void Parse_ProtectionKeywords_ParseCorrectly(string input, Keyword expected)
  {
    var result = OracleTextParser.Parse(input, "Test Card");

    Assert.That(result.Abilities, Has.Count.EqualTo(1));
    var ability = result.Abilities[0] as KeywordAbilityNode;
    Assert.That(ability!.Keyword, Is.EqualTo(expected));
  }

  #endregion

  #region Phase 0 Graveyard Keywords

  [TestCase("Undying", Keyword.Undying)]
  [TestCase("Persist", Keyword.Persist)]
  [TestCase("Unearth", Keyword.Unearth)]
  [TestCase("Flashback", Keyword.Flashback)]
  [TestCase("Retrace", Keyword.Retrace)]
  public void Parse_GraveyardKeywords_ParseCorrectly(string input, Keyword expected)
  {
    var result = OracleTextParser.Parse(input, "Test Card");

    Assert.That(result.Abilities, Has.Count.EqualTo(1));
    var ability = result.Abilities[0] as KeywordAbilityNode;
    Assert.That(ability!.Keyword, Is.EqualTo(expected));
  }

  #endregion

  #region Phase 0 Other Keywords

  [TestCase("Wither", Keyword.Wither)]
  [TestCase("Infect", Keyword.Infect)]
  [TestCase("Flash", Keyword.Flash)]
  [TestCase("Convoke", Keyword.Convoke)]
  [TestCase("Delve", Keyword.Delve)]
  [TestCase("Affinity", Keyword.Affinity)]
  [TestCase("Improvise", Keyword.Improvise)]
  [TestCase("Changeling", Keyword.Changeling)]
  [TestCase("Prowl", Keyword.Prowl)]
  [TestCase("Prowess", Keyword.Prowess)]
  [TestCase("Evolve", Keyword.Evolve)]
  [TestCase("Extort", Keyword.Extort)]
  [TestCase("Landfall", Keyword.Landfall)]
  [TestCase("Rebound", Keyword.Rebound)]
  [TestCase("Split second", Keyword.SplitSecond)]
  [TestCase("Storm", Keyword.Storm)]
  [TestCase("Cascade", Keyword.Cascade)]
  [TestCase("Ripple", Keyword.Ripple)]
  public void Parse_Phase0OtherKeywords_ParseCorrectly(string input, Keyword expected)
  {
    var result = OracleTextParser.Parse(input, "Test Card");

    Assert.That(result.Abilities, Has.Count.EqualTo(1));
    var ability = result.Abilities[0] as KeywordAbilityNode;
    Assert.That(ability!.Keyword, Is.EqualTo(expected));
  }

  #endregion

  #region Phase 1 Simple Keywords

  [TestCase("Devoid", Keyword.Devoid)]
  [TestCase("Partner", Keyword.Partner)]
  [TestCase("Companion", Keyword.Companion)]
  [TestCase("Mutate", Keyword.Mutate)]
  [TestCase("Foretell", Keyword.Foretell)]
  [TestCase("Boast", Keyword.Boast)]
  [TestCase("Disturb", Keyword.Disturb)]
  [TestCase("Decayed", Keyword.Decayed)]
  [TestCase("Training", Keyword.Training)]
  [TestCase("Reconfigure", Keyword.Reconfigure)]
  [TestCase("Toxic", Keyword.Toxic)]
  [TestCase("Backup", Keyword.Backup)]
  [TestCase("Bargain", Keyword.Bargain)]
  public void Parse_Phase1SimpleKeywords_ParseCorrectly(string input, Keyword expected)
  {
    var result = OracleTextParser.Parse(input, "Test Card");

    Assert.That(result.Abilities, Has.Count.EqualTo(1));
    var ability = result.Abilities[0] as KeywordAbilityNode;
    Assert.That(ability, Is.Not.Null);
    Assert.That(ability!.Keyword, Is.EqualTo(expected));
    Assert.That(ability.Amount, Is.Null);
    Assert.That(ability.Cost, Is.Null);
    Assert.That(ability.Filter, Is.Null);
  }

  #endregion

  #region Multiple Keywords

  [Test]
  public void Parse_MultipleKeywords_CommaSeparated()
  {
    var result = OracleTextParser.Parse("Flying, vigilance, haste", "Test Card");

    Assert.That(result.Abilities, Has.Count.EqualTo(3));
    Assert.That((result.Abilities[0] as KeywordAbilityNode)!.Keyword, Is.EqualTo(Keyword.Flying));
    Assert.That(
      (result.Abilities[1] as KeywordAbilityNode)!.Keyword,
      Is.EqualTo(Keyword.Vigilance)
    );
    Assert.That((result.Abilities[2] as KeywordAbilityNode)!.Keyword, Is.EqualTo(Keyword.Haste));
  }

  [Test]
  public void Parse_MultipleKeywords_NewlineSeparated()
  {
    var input = "Flying\nVigilance\nHaste";
    var result = OracleTextParser.Parse(input, "Test Card");

    Assert.That(result.Abilities, Has.Count.EqualTo(3));
    Assert.That((result.Abilities[0] as KeywordAbilityNode)!.Keyword, Is.EqualTo(Keyword.Flying));
    Assert.That(
      (result.Abilities[1] as KeywordAbilityNode)!.Keyword,
      Is.EqualTo(Keyword.Vigilance)
    );
    Assert.That((result.Abilities[2] as KeywordAbilityNode)!.Keyword, Is.EqualTo(Keyword.Haste));
  }

  [Test]
  public void Parse_ManyKeywords_AllParse()
  {
    var input = "Flying, menace, vigilance, haste, deathtouch, lifelink, trample";
    var result = OracleTextParser.Parse(input, "Test Card");

    Assert.That(result.Abilities, Has.Count.EqualTo(7));
    foreach (var ability in result.Abilities)
    {
      Assert.That(ability, Is.InstanceOf<KeywordAbilityNode>());
    }
  }

  #endregion

  #region Case Sensitivity

  [TestCase("flying")]
  [TestCase("FLYING")]
  [TestCase("Flying")]
  [TestCase("FLyInG")]
  public void Parse_CaseInsensitive_ParsesCorrectly(string input)
  {
    var result = OracleTextParser.Parse(input, "Test Card");

    var ability = result.Abilities[0] as KeywordAbilityNode;
    Assert.That(ability!.Keyword, Is.EqualTo(Keyword.Flying));
  }

  [TestCase("first strike")]
  [TestCase("First Strike")]
  [TestCase("FIRST STRIKE")]
  [TestCase("FiRsT sTrIkE")]
  public void Parse_MultiWordKeyword_CaseInsensitive(string input)
  {
    var result = OracleTextParser.Parse(input, "Test Card");

    var ability = result.Abilities[0] as KeywordAbilityNode;
    Assert.That(ability!.Keyword, Is.EqualTo(Keyword.FirstStrike));
  }

  #endregion

  #region Empty and Invalid Inputs

  [Test]
  public void Parse_EmptyString_ReturnsEmpty()
  {
    var result = OracleTextParser.Parse("", "Test Card");

    Assert.That(result.Abilities, Is.Empty);
    Assert.That(result.Diagnostics, Is.Empty);
  }

  [Test]
  public void Parse_WhitespaceOnly_ReturnsEmpty()
  {
    var result = OracleTextParser.Parse("   \n\t  ", "Test Card");

    Assert.That(result.Abilities, Is.Empty);
  }

  [Test]
  public void Parse_UnknownKeyword_ReportsDiagnostic()
  {
    var result = OracleTextParser.Parse("Flibbertygibbet", "Test Card");

    Assert.That(result.Abilities, Is.Empty);
    Assert.That(result.Diagnostics, Is.Not.Empty);
  }

  #endregion

  #region Non-Keyword Abilities

  [Test]
  public void Parse_StaticAbility_DoesNotParseAsKeyword()
  {
    var input = "Creatures you control get +1/+1.";
    var result = OracleTextParser.Parse(input, "Test Card");

    Assert.That(result.Abilities, Is.Empty);
    Assert.That(result.Diagnostics, Is.Not.Empty);
  }

  [Test]
  public void Parse_ActivatedAbility_DoesNotParseAsKeyword()
  {
    var input = "{T}: Add {G}.";
    var result = OracleTextParser.Parse(input, "Test Card");

    // Phase 2: Activated abilities should now parse successfully
    Assert.That(result.Abilities, Is.Not.Empty);
    Assert.That(result.Abilities[0], Is.TypeOf<ActivatedAbilityNode>());
  }

  [Test]
  public void Parse_TriggeredAbility_DoesNotParseAsKeyword()
  {
    // Phase 3: Triggered abilities are now supported, so this text should parse
    // as a triggered ability, not remain unparsed and generate diagnostics.
    var input = "When this creature enters, draw a card.";
    var result = OracleTextParser.Parse(input, "Test Card");

    Assert.That(result.Abilities, Has.Count.EqualTo(1));
    Assert.That(result.Abilities[0], Is.TypeOf<TriggeredAbilityNode>());
    Assert.That(result.Diagnostics, Is.Empty);
  }

  #endregion
}
