using Flowthru.Core.Cli;
using Flowthru.Core.Flows;

namespace Flowthru.Core.Tests.Cli;

/// <summary>
/// Tests for <see cref="ArgumentParser"/> focusing on the <c>--parallelism</c> flag.
/// </summary>
[TestFixture]
[Category("Cli")]
[Category("Parallel")]
public class ArgumentParserParallelismTests
{
  // Dummy flow names — ArgumentParser validates slicing flags against these;
  // none of the parallelism tests use named flows so the list can be empty.
  private static readonly IEnumerable<string> NoFlows = [];

  // ─────────────────────────────────────────────────────────────────────────
  // Integer values
  // ─────────────────────────────────────────────────────────────────────────

  [TestCase(1)]
  [TestCase(2)]
  [TestCase(4)]
  [TestCase(16)]
  public void Parse_ParallelismInteger_SetsMaxDegreeOfParallelism(int n)
  {
    var parsed = ArgumentParser.Parse(["--parallelism", n.ToString()], NoFlows);

    Assert.That(parsed.Options!.MaxDegreeOfParallelism, Is.EqualTo(n));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // "auto" keyword
  // ─────────────────────────────────────────────────────────────────────────

  [TestCase("auto")]
  [TestCase("AUTO")]
  [TestCase("Auto")]
  public void Parse_ParallelismAuto_SetsMaxDegreeToProcessorCount(string token)
  {
    var parsed = ArgumentParser.Parse(["--parallelism", token], NoFlows);

    Assert.That(
      parsed.Options!.MaxDegreeOfParallelism,
      Is.EqualTo(Environment.ProcessorCount),
      "'auto' should map to Environment.ProcessorCount"
    );
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Default (flag absent)
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void Parse_NoParallelismFlag_LeavesNull()
  {
    var parsed = ArgumentParser.Parse([], NoFlows);

    // Null means "unspecified" — the priority chain in FlowthruService resolves it
    // to the service-level default, or 1 if none is configured.
    Assert.That(parsed.Options!.MaxDegreeOfParallelism, Is.Null);
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Invalid values
  // ─────────────────────────────────────────────────────────────────────────

  [TestCase("0")]
  [TestCase("-1")]
  [TestCase("abc")]
  [TestCase("3.5")]
  public void Parse_ParallelismInvalidValue_Throws(string bad)
  {
    Assert.Throws<ArgumentException>(
      () => ArgumentParser.Parse(["--parallelism", bad], NoFlows),
      $"'--parallelism {bad}' should throw ArgumentException"
    );
  }

  [Test]
  public void Parse_ParallelismMissingValue_Throws()
  {
    Assert.Throws<ArgumentException>(
      () => ArgumentParser.Parse(["--parallelism"], NoFlows),
      "'--parallelism' without a value should throw ArgumentException"
    );
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Composed with other flags
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void Parse_ParallelismWithDryRun_BothOptionsSet()
  {
    var parsed = ArgumentParser.Parse(["--dry-run", "--parallelism", "4"], NoFlows);

    Assert.That(parsed.Options!.MaxDegreeOfParallelism, Is.EqualTo(4));
    Assert.That(parsed.Options.DryRun.Enabled, Is.True);
  }
}
