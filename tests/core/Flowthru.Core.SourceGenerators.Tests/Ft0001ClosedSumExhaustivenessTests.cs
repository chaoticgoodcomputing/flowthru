using Flowthru.Core.SourceGenerators.Algebra;
using Flowthru.Validation.Runtime;

namespace Flowthru.Core.SourceGenerators.Tests;

/// <summary>
/// Positive + negative tests for FT0001 — closed-sum exhaustiveness.
/// Phase 6 done-criterion: every active analyzer ID has at least
/// one positive (analyzer fires on bad code) and one negative
/// (analyzer silent on good code) test passing.
/// </summary>
[TestFixture]
public class Ft0001ClosedSumExhaustivenessTests
{
  // Sample sum used as a self-contained closed-sum input. Mirrors the
  // canonical Flowthru shape (abstract umbrella + private ctor + nested
  // sealed records) without dragging the real RuntimeError into the
  // test compilation, which keeps fixtures readable and bounded.
  private const string SampleSumDeclaration = """
    namespace Sample;

    public abstract record Outcome
    {
      private Outcome() { }
      public sealed record Win(int Amount) : Outcome;
      public sealed record Loss(string Reason) : Outcome;
      public sealed record Draw : Outcome;
    }
    """;

  // ── Positive — analyzer fires ──────────────────────────────────────────

  [Test]
  public async Task SwitchExpressionMissingACase_FiresFt0001()
  {
    var source = SampleSumDeclaration + """

      namespace Sample;

      public static class Consumer
      {
        public static int Score(Outcome o) => o switch
        {
          Outcome.Win w => w.Amount,
          Outcome.Loss l => 0,
          // Outcome.Draw deliberately omitted — FT0001 should fire.
        };
      }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new ClosedSumExhaustivenessAnalyzer(),
      source
    );
    Assert.That(diags.Where("FT0001").ToList(), Is.Not.Empty,
      "FT0001 should fire when Outcome.Draw is omitted from the switch expression. "
      + "Got: " + string.Join(", ", diags.Select(d => d.Id)));
  }

  // ── Negative — analyzer silent ─────────────────────────────────────────

  [Test]
  public async Task SwitchExpressionWithEveryCase_NoFt0001()
  {
    var source = SampleSumDeclaration + """

      namespace Sample;

      public static class Consumer
      {
        public static int Score(Outcome o) => o switch
        {
          Outcome.Win w => w.Amount,
          Outcome.Loss l => 0,
          Outcome.Draw => 1,
        };
      }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new ClosedSumExhaustivenessAnalyzer(),
      source
    );
    Assert.That(diags.Where("FT0001").ToList(), Is.Empty,
      "FT0001 should be silent when every nested sealed record case has an arm.");
  }

  [Test]
  public async Task SwitchExpressionWithDiscardArm_NoFt0001()
  {
    // A bare discard arm is the user's explicit "I know there are
    // unhandled cases and I'm OK with that." opt-out. The analyzer
    // must respect it.
    var source = SampleSumDeclaration + """

      namespace Sample;

      public static class Consumer
      {
        public static int Score(Outcome o) => o switch
        {
          Outcome.Win w => w.Amount,
          _ => 0,
        };
      }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new ClosedSumExhaustivenessAnalyzer(),
      source
    );
    Assert.That(diags.Where("FT0001").ToList(), Is.Empty,
      "FT0001 should be silent when the user opts out via a discard arm.");
  }

  [Test]
  public async Task SwitchOverNonClosedSum_NoFt0001()
  {
    // Generic int switch isn't a closed sum — no FT0001 should fire.
    var source = """
      namespace Sample;

      public static class Consumer
      {
        public static string Categorize(int x) => x switch
        {
          0 => "zero",
          > 0 => "positive",
          _ => "negative",
        };
      }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new ClosedSumExhaustivenessAnalyzer(),
      source
    );
    Assert.That(diags.Where("FT0001").ToList(), Is.Empty);
  }

  // ── Real Core closed sum ───────────────────────────────────────────────

  [Test]
  public async Task RuntimeErrorMissingExtensionVariant_FiresFt0001()
  {
    var source = """
      using Flowthru.Validation.Runtime;

      namespace Sample;

      public static class Consumer
      {
        public static string Render(RuntimeError error) => error switch
        {
          RuntimeError.External e => e.Message,
          RuntimeError.StepFailed s => s.Message,
          RuntimeError.Cancelled c => c.Message,
          RuntimeError.InvariantViolated v => v.Message,
          // RuntimeError.ExtensionError missing.
        };
      }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new ClosedSumExhaustivenessAnalyzer(),
      source,
      typeof(RuntimeError).Assembly
    );
    Assert.That(diags.Where("FT0001").ToList(), Is.Not.Empty,
      "Real-world consumer of RuntimeError that omits ExtensionError should trip FT0001.");
  }
}
