using MagicAST.Core.ManaSystem;
using MagicAST.Core.Parsing;
using NUnit.Framework;

namespace MagicAST.Tests.Unit.Parsing;

/// <summary>
/// Unit tests for ManaCostParser.
/// Tests parsing of mana cost strings into ManaCostNode objects.
/// </summary>
[TestFixture]
public class ManaCostParserTests
{
  #region Simple Generic Costs

  [TestCase("{0}", 0)]
  [TestCase("{1}", 1)]
  [TestCase("{2}", 2)]
  [TestCase("{3}", 3)]
  [TestCase("{10}", 10)]
  [TestCase("{15}", 15)]
  public void Parse_GenericManaCost_ParsesCorrectly(string costString, int expectedCMC)
  {
    var result = ManaCostParser.Parse(costString, "Test Card");

    Assert.That(result.Result, Is.Not.Null);
    Assert.That(result.Result!.Cost.ConvertedManaCost, Is.EqualTo(expectedCMC));
    Assert.That(result.Diagnostics, Is.Empty);
  }

  #endregion

  #region Colored Mana Costs

  [TestCase("{W}", 1)]
  [TestCase("{U}", 1)]
  [TestCase("{B}", 1)]
  [TestCase("{R}", 1)]
  [TestCase("{G}", 1)]
  [TestCase("{C}", 1)]
  public void Parse_SingleColoredMana_ParsesCorrectly(string costString, int expectedCMC)
  {
    var result = ManaCostParser.Parse(costString, "Test Card");

    Assert.That(result.Result, Is.Not.Null);
    Assert.That(result.Result!.Cost.ConvertedManaCost, Is.EqualTo(expectedCMC));
    Assert.That(result.Diagnostics, Is.Empty);
  }

  [TestCase("{W}{W}", 2)]
  [TestCase("{U}{U}", 2)]
  [TestCase("{B}{B}{B}", 3)]
  [TestCase("{R}{R}{R}{R}", 4)]
  [TestCase("{G}{G}{G}{G}{G}", 5)]
  public void Parse_MultipleColoredMana_ParsesCorrectly(string costString, int expectedCMC)
  {
    var result = ManaCostParser.Parse(costString, "Test Card");

    Assert.That(result.Result, Is.Not.Null);
    Assert.That(result.Result!.Cost.ConvertedManaCost, Is.EqualTo(expectedCMC));
    Assert.That(result.Diagnostics, Is.Empty);
  }

  #endregion

  #region Mixed Costs

  [TestCase("{1}{W}", 2)]
  [TestCase("{2}{U}{U}", 4)]
  [TestCase("{3}{B}{B}{B}", 6)]
  [TestCase("{1}{R}{G}", 3)]
  [TestCase("{4}{W}{U}{B}{R}{G}", 9)]
  public void Parse_MixedGenericAndColored_ParsesCorrectly(string costString, int expectedCMC)
  {
    var result = ManaCostParser.Parse(costString, "Test Card");

    Assert.That(result.Result, Is.Not.Null);
    Assert.That(result.Result!.Cost.ConvertedManaCost, Is.EqualTo(expectedCMC));
    Assert.That(result.Diagnostics, Is.Empty);
  }

  #endregion

  #region Variable Costs

  [TestCase("{X}")]
  [TestCase("{X}{G}")]
  [TestCase("{X}{X}")]
  [TestCase("{X}{U}{U}")]
  public void Parse_XCost_ParsesCorrectly(string costString)
  {
    var result = ManaCostParser.Parse(costString, "Test Card");

    Assert.That(result.Result, Is.Not.Null);
    Assert.That(result.Result!.Cost.ContainsX, Is.True);
    Assert.That(result.Diagnostics, Is.Empty);
  }

  #endregion

  #region Hybrid Costs

  [TestCase("{W/U}", 1)]
  [TestCase("{U/B}", 1)]
  [TestCase("{B/R}", 1)]
  [TestCase("{R/G}", 1)]
  [TestCase("{G/W}", 1)]
  public void Parse_HybridMana_ParsesCorrectly(string costString, int expectedCMC)
  {
    var result = ManaCostParser.Parse(costString, "Test Card");

    Assert.That(result.Result, Is.Not.Null);
    Assert.That(result.Result!.Cost.ConvertedManaCost, Is.EqualTo(expectedCMC));
    Assert.That(result.Diagnostics, Is.Empty);
  }

  [TestCase("{2/W}", 2)]
  [TestCase("{2/U}", 2)]
  [TestCase("{2/B}", 2)]
  [TestCase("{2/R}", 2)]
  [TestCase("{2/G}", 2)]
  public void Parse_MonocoloredHybrid_ParsesCorrectly(string costString, int expectedCMC)
  {
    var result = ManaCostParser.Parse(costString, "Test Card");

    Assert.That(result.Result, Is.Not.Null);
    Assert.That(result.Result!.Cost.ConvertedManaCost, Is.EqualTo(expectedCMC));
    Assert.That(result.Diagnostics, Is.Empty);
  }

  #endregion

  #region Phyrexian Costs

  [TestCase("{W/P}", 1)]
  [TestCase("{U/P}", 1)]
  [TestCase("{B/P}", 1)]
  [TestCase("{R/P}", 1)]
  [TestCase("{G/P}", 1)]
  public void Parse_PhyrexianMana_ParsesCorrectly(string costString, int expectedCMC)
  {
    var result = ManaCostParser.Parse(costString, "Test Card");

    Assert.That(result.Result, Is.Not.Null);
    Assert.That(result.Result!.Cost.ConvertedManaCost, Is.EqualTo(expectedCMC));
    Assert.That(result.Diagnostics, Is.Empty);
  }

  #endregion

  #region Edge Cases

  [Test]
  public void Parse_EmptyString_ReportsDiagnostic()
  {
    var result = ManaCostParser.Parse("", "Test Card");

    Assert.That(result.Result, Is.Null);
    Assert.That(result.Diagnostics, Is.Not.Empty);
    Assert.That(result.Diagnostics[0].Id, Is.EqualTo("MAST0001"));
  }

  [Test]
  public void Parse_NullString_ReportsDiagnostic()
  {
    var result = ManaCostParser.Parse(null!, "Test Card");

    Assert.That(result.Result, Is.Null);
    Assert.That(result.Diagnostics, Is.Not.Empty);
  }

  [Test]
  public void Parse_InvalidManaCost_ReportsDiagnostic()
  {
    var result = ManaCostParser.Parse("{INVALID}", "Test Card");

    // Should either parse with diagnostic or return null with diagnostic
    Assert.That(result.Diagnostics, Is.Not.Empty);
  }

  [Test]
  public void Parse_ComplexRealWorldCost_ParsesCorrectly()
  {
    // Progenitus: {W}{W}{U}{U}{B}{B}{R}{R}{G}{G}
    var result = ManaCostParser.Parse("{W}{W}{U}{U}{B}{B}{R}{R}{G}{G}", "Progenitus");

    Assert.That(result.Result, Is.Not.Null);
    Assert.That(result.Result!.Cost.ConvertedManaCost, Is.EqualTo(10));
    Assert.That(result.Diagnostics, Is.Empty);
  }

  [Test]
  public void Parse_FireballXCost_ParsesCorrectly()
  {
    // Fireball: {X}{R}
    var result = ManaCostParser.Parse("{X}{R}", "Fireball");

    Assert.That(result.Result, Is.Not.Null);
    Assert.That(result.Result!.Cost.ContainsX, Is.True);
    Assert.That(result.Diagnostics, Is.Empty);
  }

  #endregion

  #region ExtractManaCost Helper

  [TestCase("Cycling {2}", "{2}")]
  [TestCase("Equip {3}", "{3}")]
  [TestCase("Some text {1}{R} more text", "{1}{R}")]
  [TestCase("{T}: Add {G}.", "{T}")]
  [TestCase("Cost is {2}{U}{U} to cast", "{2}{U}{U}")]
  public void ExtractManaCost_FindsManaCostInText(string text, string expectedCost)
  {
    var result = ManaCostParser.ExtractManaCost(text);

    Assert.That(result, Is.Not.Null);
    Assert.That(result, Does.Contain(expectedCost));
  }

  [TestCase("No mana cost here")]
  [TestCase("")]
  [TestCase("Just regular text")]
  public void ExtractManaCost_NoManaCost_ReturnsNull(string text)
  {
    var result = ManaCostParser.ExtractManaCost(text);

    Assert.That(result, Is.Null);
  }

  #endregion
}
