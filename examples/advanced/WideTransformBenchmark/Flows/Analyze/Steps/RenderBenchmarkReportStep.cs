using System.Globalization;
using System.Text;
using Flowthru.Step;
using WideTransformBenchmark.Data._04_Reporting.Schemas;
#if FUNIT_ENABLED
using Flowthru.Step.Testing;
#endif

namespace WideTransformBenchmark.Flows.Analyze.Steps;

/// <summary>
/// Fill the Markdown template with the per-size comparison table and a
/// crossover verdict. Structure and prose live in the checked-in template (a
/// Raw Catalog Item); this Step builds the table rows and substitutes
/// <c>{{token}}</c> placeholders, so re-shaping the report is a template
/// edit, not a code change.
/// </summary>
[FlowthruStep]
public static class RenderBenchmarkReportStep
{
  public static Func<(IEnumerable<BenchmarkComparison>, string), byte[]> Create()
  {
    return inputs =>
    {
      var (comparisons, template) = inputs;
      var rows = comparisons.OrderBy(c => c.InputRows).ToList();
      var ci = CultureInfo.InvariantCulture;

      var table = new StringBuilder();
      foreach (var c in rows)
      {
        table.AppendLine(
          $"| {c.InputRows.ToString("N0", ci)} | {c.OutputRows.ToString("N0", ci)} "
          + $"| {c.EagerMs.ToString("N0", ci)} | {c.EngineMs.ToString("N0", ci)} "
          + $"| {c.SpeedupX.ToString("0.00", ci)}x "
          + $"| {c.EagerAllocatedMb.ToString("N1", ci)} | {c.EngineAllocatedMb.ToString("0.00", ci)} "
          + $"| {c.AllocationRatioX.ToString("0.0", ci)}x |");
      }

      var largest = rows[^1];
      var crossover = rows.FirstOrDefault(c => c.SpeedupX >= 1.0);
      var verdict = crossover is null
        ? "The eager Step won at every measured size — the engine's fixed startup cost "
          + "was never amortised. Re-run with larger sizes (see the README's env knob) "
          + "to find the crossover."
        : $"The engine pays off from {crossover.InputRows.ToString("N0", ci)} rows up; at "
          + $"{largest.InputRows.ToString("N0", ci)} rows it ran the optimize pass "
          + $"**{largest.SpeedupX.ToString("0.00", ci)}x** faster than the eager Step while "
          + $"allocating **{largest.AllocationRatioX.ToString("0.0", ci)}x** less managed memory.";

      var subs = new Dictionary<string, string>(StringComparer.Ordinal)
      {
        ["comparison_table_rows"] = table.ToString().TrimEnd(),
        ["sizes"] = string.Join(", ", rows.Select(c => c.InputRows.ToString("N0", ci))),
        ["verdict"] = verdict,
        ["generated_utc"] = DateTime.UtcNow.ToString("u", ci),
      };

      var rendered = template;
      foreach (var (token, value) in subs)
      {
        rendered = rendered.Replace("{{" + token + "}}", value);
      }

      return Encoding.UTF8.GetBytes(rendered);
    };
  }

#if FUNIT_ENABLED
  /// <summary>FUnit tests for <see cref="RenderBenchmarkReportStep"/>.</summary>
  public class Tests : FUnitContext
  {
    // A minimal template echoing each placeholder so tests assert on
    // substitution without depending on the production template's prose.
    private const string EchoTemplate =
      "sizes={{sizes}}\nverdict={{verdict}}\ntable:\n{{comparison_table_rows}}";

    private static BenchmarkComparison Row(int inputRows, double speedupX) =>
      new()
      {
        InputRows = inputRows,
        OutputRows = inputRows * 4 / 5,
        EagerMs = 100,
        EngineMs = 50,
        SpeedupX = speedupX,
        EagerAllocatedMb = 64.0,
        EngineAllocatedMb = 8.0,
        AllocationRatioX = 8.0,
      };

    [FUnitStepTest(typeof(RenderBenchmarkReportStep))]
    public void SubstitutesTokens_OneTableRowPerSize()
    {
      var comparisons = new[] { Row(10_000, 0.5), Row(40_000, 2.0) };

      var rendered = Encoding.UTF8.GetString(
        Invoke(RenderBenchmarkReportStep.Create(), (comparisons, EchoTemplate)));

      Assert.That(rendered, Does.Contain("sizes=10,000, 40,000"));
      Assert.That(rendered, Does.Not.Contain("{{"));
      Assert.That(rendered, Does.Contain("| 10,000 |"));
      Assert.That(rendered, Does.Contain("| 40,000 |"));
    }

    [FUnitStepTest(typeof(RenderBenchmarkReportStep))]
    public void Verdict_NamesTheCrossoverSize()
    {
      var comparisons = new[] { Row(10_000, 0.5), Row(40_000, 1.4), Row(160_000, 3.0) };

      var rendered = Encoding.UTF8.GetString(
        Invoke(RenderBenchmarkReportStep.Create(), (comparisons, EchoTemplate)));

      Assert.That(rendered, Does.Contain("pays off from 40,000 rows"));
      Assert.That(rendered, Does.Contain("**3.00x** faster"));
    }

    [FUnitStepTest(typeof(RenderBenchmarkReportStep))]
    public void Verdict_HandlesNoCrossoverHonestly()
    {
      var comparisons = new[] { Row(10_000, 0.4), Row(40_000, 0.8) };

      var rendered = Encoding.UTF8.GetString(
        Invoke(RenderBenchmarkReportStep.Create(), (comparisons, EchoTemplate)));

      Assert.That(rendered, Does.Contain("eager Step won at every measured size"));
    }
  }
#endif
}
