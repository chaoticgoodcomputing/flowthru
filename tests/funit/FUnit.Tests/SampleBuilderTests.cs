using Flowthru.Step.Testing;
using Flowthru.FUnit.Tests.Fixtures;

namespace Flowthru.FUnit.Tests;

[TestFixture]
[Category("FUnit")]
public class SampleBuilderTests
{
  private readonly SampleBuilder _samples = new();

  // ===========================================================================
  // Of
  // ===========================================================================

  [Test]
  public void Of_ReturnsAllProvidedItems()
  {
    var result = _samples.Of(new NumberRow(1.0), new NumberRow(2.0), new NumberRow(3.0)).ToList();

    Assert.That(result, Has.Count.EqualTo(3));
    Assert.That(result[0].Value, Is.EqualTo(1.0));
    Assert.That(result[2].Value, Is.EqualTo(3.0));
  }

  [Test]
  public void Of_WithNoArgs_ReturnsEmpty()
  {
    var result = _samples.Of<NumberRow>().ToList();

    Assert.That(result, Is.Empty);
  }

  // ===========================================================================
  // Generate
  // ===========================================================================

  [Test]
  public void Generate_ProducesCorrectCount()
  {
    var result = _samples.Generate(5, i => new NumberRow(i)).ToList();

    Assert.That(result, Has.Count.EqualTo(5));
  }

  [Test]
  public void Generate_FactoryReceivesZeroBasedIndex()
  {
    var result = _samples.Generate(3, i => new NumberRow(i * 10.0)).ToList();

    Assert.That(result[0].Value, Is.EqualTo(0.0));
    Assert.That(result[1].Value, Is.EqualTo(10.0));
    Assert.That(result[2].Value, Is.EqualTo(20.0));
  }

  [Test]
  public void Generate_WithZeroCount_ReturnsEmpty()
  {
    var result = _samples.Generate(0, i => new NumberRow(i)).ToList();

    Assert.That(result, Is.Empty);
  }

  // ===========================================================================
  // FromCsv
  // ===========================================================================

  [Test]
  public void FromCsv_ThrowsWhenResourceNotFound()
  {
    Assert.Throws<InvalidOperationException>(
      () => _samples.FromCsv<CsvSampleRow>("Flowthru.FUnit.Tests.NonExistent.csv").ToList()
    );
  }
}

/// <summary>Minimal row type used only for CSV round-trip testing.</summary>
file class CsvSampleRow
{
  public double Value { get; set; }
  public string Text { get; set; } = "";
}
