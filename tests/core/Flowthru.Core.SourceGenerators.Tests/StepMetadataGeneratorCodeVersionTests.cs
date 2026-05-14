using System.Text.RegularExpressions;
using Flowthru.Core.SourceGenerators.Step;
using Flowthru.Step;

namespace Flowthru.Core.SourceGenerators.Tests;

/// <summary>
/// Behavioural tests for the <c>CodeVersion</c> identity emitted by
/// <see cref="StepMetadataGenerator"/> as part of the
/// <c>{ClassName}_Metadata</c> companion. The generator computes a
/// short SHA-256 prefix over the step class's normalized source text
/// (whitespace and comment trivia stripped via Roslyn's syntax tree)
/// so cosmetic edits do not invalidate downstream caches. An explicit
/// <c>[FlowthruStep(CodeVersion = "...")]</c> override replaces the
/// computed value verbatim — the escape hatch for users that need
/// stable cross-machine identities.
/// </summary>
[TestFixture]
public class StepMetadataGeneratorCodeVersionTests
{
  // ── Shape ──────────────────────────────────────────────────────────────

  [Test]
  public void EmittedMetadata_ContainsCodeVersionConstant()
  {
    var source = """
      using Flowthru.Step;
      namespace Sample;

      [FlowthruStep]
      public static class FooStep
      {
        public static System.Func<int, int> Create() => x => x + 1;
      }
      """;

    var emitted = EmitFirstSource(source);
    Assert.That(emitted, Does.Match(@"public const string CodeVersion = ""[A-Fa-f0-9]+"";"),
      "Companion should declare a hex CodeVersion constant.");
  }

  [Test]
  public void EmittedCodeVersion_IsSha256HexPrefix()
  {
    var source = """
      using Flowthru.Step;
      namespace Sample;

      [FlowthruStep]
      public static class FooStep
      {
        public static System.Func<int, int> Create() => x => x + 1;
      }
      """;

    var emitted = EmitFirstSource(source);
    var version = ExtractCodeVersion(emitted);
    Assert.That(version, Is.Not.Null);
    Assert.That(version!.Length, Is.GreaterThanOrEqualTo(8),
      "CodeVersion should be at least 8 hex characters (32 bits of SHA256 prefix).");
    Assert.That(version, Does.Match("^[0-9a-f]+$"),
      "CodeVersion should be lowercase hex.");
  }

  // ── Override ───────────────────────────────────────────────────────────

  [Test]
  public void ExplicitCodeVersionOverride_IsUsedVerbatim()
  {
    var source = """
      using Flowthru.Step;
      namespace Sample;

      [FlowthruStep(CodeVersion = "v2")]
      public static class FooStep
      {
        public static System.Func<int, int> Create() => x => x + 1;
      }
      """;

    var emitted = EmitFirstSource(source);
    Assert.That(emitted, Does.Contain("public const string CodeVersion = \"v2\";"),
      "An explicit CodeVersion override should replace the computed hash verbatim.");
  }

  // ── Stability under cosmetic edits ────────────────────────────────────

  [Test]
  public void WhitespaceOnlyChange_DoesNotChangeCodeVersion()
  {
    // Same logical content, different whitespace and indentation.
    var sourceA = """
      using Flowthru.Step;
      namespace Sample;

      [FlowthruStep]
      public static class FooStep
      {
        public static System.Func<int, int> Create() => x => x + 1;
      }
      """;

    var sourceB = """
      using Flowthru.Step;
      namespace Sample;

      [FlowthruStep]
              public  static   class   FooStep    {



        public  static  System.Func<int,int>  Create()=>  x=>x+1;
      }
      """;

    var versionA = ExtractCodeVersion(EmitFirstSource(sourceA));
    var versionB = ExtractCodeVersion(EmitFirstSource(sourceB));

    Assert.That(versionB, Is.EqualTo(versionA),
      "Whitespace-only differences must not change CodeVersion: the source "
      + "generator strips trivia before hashing.");
  }

  [Test]
  public void CommentOnlyChange_DoesNotChangeCodeVersion()
  {
    var sourceA = """
      using Flowthru.Step;
      namespace Sample;

      [FlowthruStep]
      public static class FooStep
      {
        public static System.Func<int, int> Create() => x => x + 1;
      }
      """;

    var sourceB = """
      using Flowthru.Step;
      namespace Sample;

      // A leading documentation comment that has no bearing on behaviour.
      [FlowthruStep]
      public static class FooStep
      {
        /* block comment */ public static System.Func<int, int> Create() => x => x + 1;
        // trailing comment
      }
      """;

    var versionA = ExtractCodeVersion(EmitFirstSource(sourceA));
    var versionB = ExtractCodeVersion(EmitFirstSource(sourceB));

    Assert.That(versionB, Is.EqualTo(versionA),
      "Comment-only differences must not change CodeVersion.");
  }

  // ── Sensitivity to body changes ───────────────────────────────────────

  [Test]
  public void BodyChange_ChangesCodeVersion()
  {
    var sourceA = """
      using Flowthru.Step;
      namespace Sample;

      [FlowthruStep]
      public static class FooStep
      {
        public static System.Func<int, int> Create() => x => x + 1;
      }
      """;

    var sourceB = """
      using Flowthru.Step;
      namespace Sample;

      [FlowthruStep]
      public static class FooStep
      {
        public static System.Func<int, int> Create() => x => x + 2;
      }
      """;

    var versionA = ExtractCodeVersion(EmitFirstSource(sourceA));
    var versionB = ExtractCodeVersion(EmitFirstSource(sourceB));

    Assert.That(versionB, Is.Not.EqualTo(versionA),
      "Body changes (transform logic differs) must produce a different CodeVersion.");
  }

  [Test]
  public void DifferentStepsInSameCompilation_HaveDifferentCodeVersions()
  {
    var source = """
      using Flowthru.Step;
      namespace Sample;

      [FlowthruStep]
      public static class FooStep
      {
        public static System.Func<int, int> Create() => x => x + 1;
      }

      [FlowthruStep]
      public static class BarStep
      {
        public static System.Func<int, int> Create() => x => x * 2;
      }
      """;

    var result = AnalyzerTestHarness.RunGenerator(
      new StepMetadataGenerator(),
      source,
      typeof(FlowthruStepAttribute).Assembly
    );

    var emissions = result.GeneratedSources.Where(g => g.HintName.EndsWith("_Metadata.g.cs")).ToList();
    Assert.That(emissions, Has.Count.EqualTo(2));

    var versions = emissions.Select(e => ExtractCodeVersion(e.Source)).ToList();
    Assert.That(versions.Distinct().Count(), Is.EqualTo(2),
      "Distinct step classes with different bodies must yield distinct CodeVersions.");
  }

  // ── helpers ───────────────────────────────────────────────────────────

  private static string EmitFirstSource(string source)
  {
    var result = AnalyzerTestHarness.RunGenerator(
      new StepMetadataGenerator(),
      source,
      typeof(FlowthruStepAttribute).Assembly
    );
    Assert.That(result.GeneratedSources, Is.Not.Empty,
      "Expected at least one generated metadata companion. Diagnostics: "
      + string.Join("; ", result.Diagnostics.Select(d => d.GetMessage())));
    return result.GeneratedSources[0].Source;
  }

  private static string? ExtractCodeVersion(string emitted)
  {
    var match = Regex.Match(emitted, @"public const string CodeVersion = ""([^""]+)"";");
    return match.Success ? match.Groups[1].Value : null;
  }
}
