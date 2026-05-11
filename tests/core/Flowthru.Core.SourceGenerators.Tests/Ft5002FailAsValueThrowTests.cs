using Flowthru.Core.SourceGenerators.Validation;

namespace Flowthru.Core.SourceGenerators.Tests;

/// <summary>
/// Positive + negative tests for FT5002 — fail-as-value throw discipline.
/// Each Flowthru fail-as-value carrier (<c>Validated&lt;,&gt;</c>,
/// <c>FlowIO&lt;&gt;</c>, <c>EffResult&lt;&gt;</c>, <c>ValidationResult</c>)
/// gets a positive case (throw inside it fires) and the documented escape
/// hatches (<c>"Unreachable: …"</c> idiom; non-fail-as-value return type)
/// get negative cases.
/// </summary>
[TestFixture]
public class Ft5002FailAsValueThrowTests
{
  // Sample carrier types live in the Flowthru namespace so the analyzer's
  // namespace guard recognises them. Real Validated/FlowIO/EffResult have
  // far more surface — these self-contained versions exist only so the
  // analyzer has a return type to recognise.
  private const string SampleCarriers = """
    namespace Flowthru.Sample;

    public abstract record Validated<TError, TValue>;
    public sealed record FlowIO<T>;
    public sealed record EffResult<T>;
    public sealed record ValidationResult;
    """;

  // ── Positive: throws fire ──────────────────────────────────────────────

  [Test]
  public async Task ThrowInValidationResultReturningMethod_FiresFt5002()
  {
    var source = SampleCarriers + """

      namespace Flowthru.Sample;

      public static class Probe
      {
        public static ValidationResult IsWritable(string path)
        {
          throw new System.InvalidOperationException("probe is in an inconsistent state");
        }
      }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(new FailAsValueThrowAnalyzer(), source);
    Assert.That(diags.Where("FT5002").ToList(), Is.Not.Empty,
      "FT5002 should fire on throws inside methods returning ValidationResult. "
      + "Got: " + string.Join(", ", diags.Select(d => d.Id)));
  }

  [Test]
  public async Task ThrowInFlowIOReturningMethod_FiresFt5002()
  {
    var source = SampleCarriers + """

      namespace Flowthru.Sample;

      public static class Probe
      {
        public static FlowIO<int> LoadAndDouble(string source)
        {
          throw new System.InvalidOperationException("source must not be null");
        }
      }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(new FailAsValueThrowAnalyzer(), source);
    Assert.That(diags.Where("FT5002").ToList(), Is.Not.Empty);
  }

  [Test]
  public async Task ThrowInValidatedReturningMethod_FiresFt5002()
  {
    var source = SampleCarriers + """

      namespace Flowthru.Sample;

      public static class Probe
      {
        public static Validated<string, int> Compute(int x)
        {
          throw new System.InvalidOperationException("compute failed mid-flight");
        }
      }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(new FailAsValueThrowAnalyzer(), source);
    Assert.That(diags.Where("FT5002").ToList(), Is.Not.Empty);
  }

  [Test]
  public async Task ThrowInTaskWrappedValidationResult_FiresFt5002()
  {
    var source = SampleCarriers + """

      namespace Flowthru.Sample;

      public static class Probe
      {
        public static async System.Threading.Tasks.Task<ValidationResult> ProbeAsync(string path)
        {
          await System.Threading.Tasks.Task.Yield();
          throw new System.InvalidOperationException("probe path is unreachable");
        }
      }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(new FailAsValueThrowAnalyzer(), source);
    Assert.That(diags.Where("FT5002").ToList(), Is.Not.Empty,
      "Task-wrapped fail-as-value carriers must also be analyzed — the wrapping is incidental.");
  }

  [Test]
  public async Task ThrowExpressionInValidationResultMethod_FiresFt5002()
  {
    // C# throw-expressions (e.g. `x ?? throw new …`) are equivalent to a
    // throw statement for the analyzer's purposes.
    var source = SampleCarriers + """

      namespace Flowthru.Sample;

      public static class Probe
      {
        public static ValidationResult Validate(string? input)
            => input is null
              ? throw new System.InvalidOperationException("input was unexpectedly null")
              : new ValidationResult();
      }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(new FailAsValueThrowAnalyzer(), source);
    Assert.That(diags.Where("FT5002").ToList(), Is.Not.Empty,
      "Throw-expressions must be analyzed identically to throw statements.");
  }

  // ── Negative: analyzer silent ─────────────────────────────────────────

  [Test]
  public async Task UnreachableClosedSumFallthrough_NoFt5002()
  {
    // The documented idiom for closed-sum exhaustiveness fallthroughs.
    // FT0001 separately enforces that the switch covers every case;
    // FT5002 must not double-flag this paired pattern.
    var source = SampleCarriers + """

      namespace Flowthru.Sample;

      public static class Consumer
      {
        public static ValidationResult Handle(int kind) => kind switch
        {
          1 => new ValidationResult(),
          2 => new ValidationResult(),
          _ => throw new System.InvalidOperationException("Unreachable: kind is a closed sum"),
        };
      }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(new FailAsValueThrowAnalyzer(), source);
    Assert.That(diags.Where("FT5002").ToList(), Is.Empty,
      "The 'Unreachable: …' idiom is the documented escape hatch for closed-sum fallthroughs. "
      + "Got: " + string.Join(", ", diags.Select(d => d.Id + " @ " + d.Location)));
  }

  [Test]
  public async Task ThrowInNonFailAsValueReturningMethod_NoFt5002()
  {
    // Methods returning `int`, `string`, `void`, or anything outside the
    // Flowthru carriers are not analyzed — the discipline applies only at
    // the fail-as-value boundary.
    var source = """
      namespace Flowthru.Sample;

      public static class NonFailAsValueAPI
      {
        public static int Parse(string s)
        {
          if (s is null) throw new System.ArgumentNullException(nameof(s));
          return int.Parse(s);
        }
      }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(new FailAsValueThrowAnalyzer(), source);
    Assert.That(diags.Where("FT5002").ToList(), Is.Empty,
      "FT5002 should not fire on methods outside the fail-as-value surface.");
  }

  [Test]
  public async Task ThrowInNonFlowthruValidatedType_NoFt5002()
  {
    // A `Validated` from a different namespace must not be confused with
    // Flowthru's carrier — the namespace guard prevents false positives.
    var source = """
      namespace ThirdParty;

      public sealed record Validated<TError, TValue>;

      public static class Probe
      {
        public static Validated<string, int> Run()
        {
          throw new System.ArgumentException("not flowthru's Validated");
        }
      }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(new FailAsValueThrowAnalyzer(), source);
    Assert.That(diags.Where("FT5002").ToList(), Is.Empty,
      "FT5002 must only fire when the carrier is in the Flowthru namespace.");
  }

  [Test]
  public async Task ArgumentPreconditionGuards_NoFt5002()
  {
    // ArgumentNullException / ArgumentOutOfRangeException / ArgumentException
    // are the .NET-idiomatic way to signal "the caller violated the contract".
    // These are programming errors (a bug at the call site), not operational
    // failures the pre-flight pipeline should aggregate — so the analyzer
    // allows them.
    var source = SampleCarriers + """

      namespace Flowthru.Sample;

      public static class Probe
      {
        public static ValidationResult Run(string path, int max)
        {
          if (path is null) throw new System.ArgumentNullException(nameof(path));
          if (max < 1) throw new System.ArgumentOutOfRangeException(nameof(max));
          if (string.IsNullOrWhiteSpace(path))
            throw new System.ArgumentException("path is empty", nameof(path));
          return new ValidationResult();
        }
      }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(new FailAsValueThrowAnalyzer(), source);
    Assert.That(diags.Where("FT5002").ToList(), Is.Empty,
      "Argument-precondition guards are the documented escape hatch — they signal "
      + "caller-contract violations, not operational failures. "
      + "Got: " + string.Join(", ", diags.Select(d => d.Id + " @ " + d.Location)));
  }

  [Test]
  public async Task ThrowInsideLambdaBoundary_NoFt5002()
  {
    // Throws inside lambdas (the FlowIO.Lift / Bind / etc. lifting pattern)
    // are caught by the lifting boundary and translated to typed failures.
    // The analyzer must not flag these — they're the documented idiom for
    // bridging exception-throwing .NET APIs into the FlowIO surface.
    var source = SampleCarriers + """

      namespace Flowthru.Sample;

      public static class FlowIOLifter
      {
        public static FlowIO<T> Lift<T>(System.Func<T> thunk) => new FlowIO<T>();
      }

      public static class Adapter
      {
        public static FlowIO<int> Load() => FlowIOLifter.Lift<int>(() =>
        {
          throw new System.InvalidOperationException("no data");
        });
      }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(new FailAsValueThrowAnalyzer(), source);
    Assert.That(diags.Where("FT5002").ToList(), Is.Empty,
      "Throws inside lambda boundaries (FlowIO.Lift idiom) must not fire FT5002. "
      + "Got: " + string.Join(", ", diags.Select(d => d.Id + " @ " + d.Location)));
  }

  [Test]
  public async Task BareRethrowInsideCatch_NoFt5002()
  {
    // Bare `throw;` inside a catch block doesn't introduce a new exception
    // path — it's a no-op the analyzer ignores. The original throw (if any)
    // is the discipline boundary.
    var source = SampleCarriers + """

      namespace Flowthru.Sample;

      public static class Probe
      {
        public static ValidationResult MaybeRethrow()
        {
          try
          {
            return new ValidationResult();
          }
          catch
          {
            throw;
          }
        }
      }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(new FailAsValueThrowAnalyzer(), source);
    Assert.That(diags.Where("FT5002").ToList(), Is.Empty,
      "Bare `throw;` rethrows in catch blocks are not the discipline target.");
  }
}
