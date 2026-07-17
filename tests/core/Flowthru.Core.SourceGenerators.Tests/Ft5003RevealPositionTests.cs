using Flowthru.Core.SourceGenerators.Security;

namespace Flowthru.Core.SourceGenerators.Tests;

/// <summary>
/// Positive + negative tests for FT5003 — the syntactic
/// <c>SecretText.Reveal()</c>-position analyzer. Reveal() interpolated or passed
/// to a logging / console / format sink fires; Reveal() assigned to a local or
/// passed to a non-logging consumer (the legitimate reveal-site use) does not.
/// The negative cases pin the deliberate non-taint-tracking boundary (ADR-0026).
/// </summary>
[TestFixture]
public class Ft5003RevealPositionTests
{
  // Minimal SecretText the analyzer's namespace + name guard recognises.
  private const string SecretTextStub = """
    namespace Flowthru.Data.Storage;

    public sealed class SecretText
    {
      public SecretText(string value) { }
      public string Reveal() => "";
    }
    """;

  // ── Positive: disclosure-prone positions fire ─────────────────────────────

  [Test]
  public async Task RevealInStringInterpolation_FiresFt5003()
  {
    var consumer = """
      using Flowthru.Data.Storage;
      namespace Consumer;

      public static class Probe
      {
        public static string Leak(SecretText s) => $"key={s.Reveal()}";
      }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new RevealInSensitivePositionAnalyzer(), new[] { SecretTextStub, consumer });

    Assert.That(diags.Where("FT5003").ToList(), Is.Not.Empty,
      "Reveal() inside a string interpolation must fire FT5003. Got: "
      + string.Join(", ", diags.Select(d => d.Id)));
  }

  [Test]
  public async Task RevealAsConsoleWriteLineArgument_FiresFt5003()
  {
    var consumer = """
      using Flowthru.Data.Storage;
      namespace Consumer;

      public static class Probe
      {
        public static void Leak(SecretText s) => System.Console.WriteLine(s.Reveal());
      }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new RevealInSensitivePositionAnalyzer(), new[] { SecretTextStub, consumer });

    Assert.That(diags.Where("FT5003").ToList(), Is.Not.Empty);
  }

  [Test]
  public async Task RevealAsStringFormatArgument_FiresFt5003()
  {
    var consumer = """
      using Flowthru.Data.Storage;
      namespace Consumer;

      public static class Probe
      {
        public static string Leak(SecretText s) => string.Format("{0}", s.Reveal());
      }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new RevealInSensitivePositionAnalyzer(), new[] { SecretTextStub, consumer });

    Assert.That(diags.Where("FT5003").ToList(), Is.Not.Empty);
  }

  [Test]
  public async Task RevealAsBareLogCallArgument_FiresFt5003()
  {
    // A bare `Log(...)` call (identifier sink, not member access) is also flagged.
    var consumer = """
      using Flowthru.Data.Storage;
      namespace Consumer;

      public static class Probe
      {
        public static void Leak(SecretText s) => Log(s.Reveal());
        private static void Log(string message) { }
      }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new RevealInSensitivePositionAnalyzer(), new[] { SecretTextStub, consumer });

    Assert.That(diags.Where("FT5003").ToList(), Is.Not.Empty);
  }

  // ── Negative: legitimate reveal-site uses do not fire ─────────────────────

  [Test]
  public async Task RevealAssignedToLocal_DoesNotFire()
  {
    var consumer = """
      using Flowthru.Data.Storage;
      namespace Consumer;

      public static class Probe
      {
        public static string Use(SecretText s)
        {
          var value = s.Reveal();
          return Quote(value);
        }
        private static string Quote(string v) => "'" + v + "'";
      }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new RevealInSensitivePositionAnalyzer(), new[] { SecretTextStub, consumer });

    Assert.That(diags.Where("FT5003").ToList(), Is.Empty,
      "A Reveal() assigned to a local (not interpolated/logged in place) is the "
      + "documented non-taint-tracking boundary — must not fire.");
  }

  [Test]
  public async Task RevealPassedToNonLoggingConsumer_DoesNotFire()
  {
    var consumer = """
      using Flowthru.Data.Storage;
      namespace Consumer;

      public static class Probe
      {
        public static string Build(SecretText s) => Quote(s.Reveal());
        private static string Quote(string v) => "'" + v + "'";
      }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new RevealInSensitivePositionAnalyzer(), new[] { SecretTextStub, consumer });

    Assert.That(diags.Where("FT5003").ToList(), Is.Empty,
      "Passing Reveal() to a non-logging consumer (e.g. SQL quoting) is the legitimate "
      + "reveal-site use — must not fire.");
  }
}
