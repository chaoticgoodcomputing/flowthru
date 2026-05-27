using Flowthru.Step.Python;

namespace Flowthru.Extensions.Python.SourceGenerators.Tests;

/// <summary>
/// Tests for <see cref="PythonRequirementsAnalyzer"/> — the design-time
/// half of the Python requirements algebra. Exercises:
/// <list type="bullet">
///   <item>The "no consumer references" / "no uv.lock" silent paths.</item>
///   <item>Base requirements satisfied / missing / wrong-version cases.</item>
///   <item>Attribute-discovered capability requirements folding into the closure.</item>
/// </list>
/// </summary>
[TestFixture]
public class PythonRequirementsAnalyzerTests
{
  // ── Silent paths ─────────────────────────────────────────────────────

  [Test]
  public async Task NoFlowthruReference_AnalyzerStaysSilent()
  {
    var source = """
      namespace Sample;
      public class Bare { }
      """;
    // No reference to Flowthru.Extensions.Python → the analyzer
    // can't resolve IPythonCapability and must not fire.
    var diagnostics = await AnalyzerTestHarness.RunAsync(
      new PythonRequirementsAnalyzer(),
      source,
      additionalFiles: new[] { UvLock("pyarrow", "15.0.0") }
    );

    Assert.That(diagnostics.Where("FTPY1501"), Is.Empty);
    Assert.That(diagnostics.Where("FTPY1502"), Is.Empty);
  }

  [Test]
  public async Task NoUvLock_AnalyzerStaysSilent()
  {
    var source = "namespace Sample; public class Bare { }";
    var diagnostics = await AnalyzerTestHarness.RunAsync(
      new PythonRequirementsAnalyzer(),
      source,
      additionalFiles: null,
      extraReferences: typeof(IPythonCapability).Assembly
    );

    Assert.That(diagnostics.Where("FTPY1501"), Is.Empty);
    Assert.That(diagnostics.Where("FTPY1502"), Is.Empty);
  }

  // ── Base requirements ────────────────────────────────────────────────

  [Test]
  public async Task BaseRequirementsSatisfied_NoDiagnostic()
  {
    var diagnostics = await RunWithReferences(
      source: "namespace Sample; public class Bare { }",
      uvLock: UvLock(("pyarrow", "15.0.0"), ("flowthru", "0.20.0"))
    );

    Assert.That(diagnostics.Where("FTPY1501"), Is.Empty);
    Assert.That(diagnostics.Where("FTPY1502"), Is.Empty);
  }

  [Test]
  public async Task BasePyarrowMissing_FiresFtpy1501()
  {
    var diagnostics = await RunWithReferences(
      source: "namespace Sample; public class Bare { }",
      uvLock: UvLock(("flowthru", "0.20.0"))  // no pyarrow
    );

    var ftpy1501s = diagnostics.Where("FTPY1501").ToList();
    Assert.That(ftpy1501s, Has.Count.EqualTo(1));
    var msg = ftpy1501s[0].GetMessage();
    Assert.That(msg, Does.Contain("pyarrow"));
    Assert.That(msg, Does.Contain(">=14"));
    Assert.That(msg, Does.Contain("uv add pyarrow>=14"));
  }

  [Test]
  public async Task BasePyarrowTooOld_FiresFtpy1502()
  {
    var diagnostics = await RunWithReferences(
      source: "namespace Sample; public class Bare { }",
      uvLock: UvLock(("pyarrow", "13.0.0"), ("flowthru", "0.20.0"))
    );

    var ftpy1502s = diagnostics.Where("FTPY1502").ToList();
    Assert.That(ftpy1502s, Has.Count.EqualTo(1));
    var msg = ftpy1502s[0].GetMessage();
    Assert.That(msg, Does.Contain("pyarrow"));
    Assert.That(msg, Does.Contain("13.0.0"));
    Assert.That(msg, Does.Contain(">=14"));
  }

  // ── Attribute-discovered capabilities ────────────────────────────────

  [Test]
  public async Task UserCapabilityAttribute_FoldsIntoClosure_MissingFiresFtpy1501()
  {
    var source = """
      using Flowthru.Step.Python;
      namespace Sample;

      [PythonPackageRequirement("accelerate", ">=0.30", "Required by MyLauncher")]
      public sealed class MyLauncher : IPythonCapability
      {
        public IReadOnlyList<PythonPackageRequirement> Requirements { get; } =
          new[] { new PythonPackageRequirement("accelerate", ">=0.30", "Required by MyLauncher") };
      }
      """;
    var diagnostics = await RunWithReferences(
      source,
      uvLock: UvLock(("pyarrow", "15.0.0"), ("flowthru", "0.20.0"))  // no accelerate
    );

    var ftpy1501s = diagnostics.Where("FTPY1501").ToList();
    Assert.That(ftpy1501s.Any(d => d.GetMessage().Contains("accelerate")), Is.True,
      "FTPY1501 must fire for the attribute-declared accelerate dep when uv.lock doesn't have it.");
    Assert.That(ftpy1501s.Any(d => d.GetMessage().Contains("MyLauncher")), Is.True,
      "Diagnostic message must name the declaring capability.");
  }

  [Test]
  public async Task UserCapabilityAttribute_FoldsIntoClosure_SatisfiedIsSilent()
  {
    var source = """
      using Flowthru.Step.Python;
      namespace Sample;

      [PythonPackageRequirement("accelerate", ">=0.30", "Required by MyLauncher")]
      public sealed class MyLauncher : IPythonCapability
      {
        public IReadOnlyList<PythonPackageRequirement> Requirements { get; } =
          new[] { new PythonPackageRequirement("accelerate", ">=0.30", "Required by MyLauncher") };
      }
      """;
    var diagnostics = await RunWithReferences(
      source,
      uvLock: UvLock(
        ("pyarrow", "15.0.0"),
        ("flowthru", "0.20.0"),
        ("accelerate", "0.31.0")
      )
    );

    Assert.That(diagnostics.Where("FTPY1501"), Is.Empty);
    Assert.That(diagnostics.Where("FTPY1502"), Is.Empty);
  }

  [Test]
  public async Task ConflictingAttributes_FoldedConstraintNamesAllDeclarers()
  {
    var source = """
      using Flowthru.Step.Python;
      namespace Sample;

      [PythonPackageRequirement("pyarrow", "<14", "Required by BadLauncher")]
      public sealed class BadLauncher : IPythonCapability
      {
        public IReadOnlyList<PythonPackageRequirement> Requirements { get; } =
          new[] { new PythonPackageRequirement("pyarrow", "<14", "Required by BadLauncher") };
      }
      """;
    var diagnostics = await RunWithReferences(
      source,
      uvLock: UvLock(("pyarrow", "15.0.0"), ("flowthru", "0.20.0"))
    );

    // pyarrow=15.0 vs intersected constraint (>=14 AND <14) — fails
    // 1502. Both declarers must appear in the message.
    var ftpy1502s = diagnostics.Where("FTPY1502").ToList();
    Assert.That(ftpy1502s, Has.Count.EqualTo(1));
    var msg = ftpy1502s[0].GetMessage();
    Assert.That(msg, Does.Contain("PythonStepExtension"),
      "Base declarer (PythonStepExtension Arrow IPC) must be named.");
    Assert.That(msg, Does.Contain("BadLauncher"),
      "User declarer must also be named so the conflict is diagnosable.");
  }

  // ── Reference-chain capabilities — only folded when used ────────────

  [Test]
  public async Task LauncherInReferenceChain_NotInstantiated_DoesNotFold()
  {
    // Regression: pre-fix the analyzer walked every referenced
    // assembly's namespace tree for [PythonPackageRequirement] and
    // folded AccelerateLauncher's `accelerate>=0.30` declaration even
    // when the consumer never used the launcher. The fix walks source
    // syntax for type references; if no `new AccelerateLauncher()` or
    // generic-arg reference appears in source, the launcher's
    // requirement is not folded.
    var source = """
      namespace Sample;
      public static class Pipeline { }
      """;
    var diagnostics = await RunWithReferences(
      source,
      uvLock: UvLock(("pyarrow", "15.0.0"))  // no accelerate, no torch — only base
    );

    Assert.That(
      diagnostics.Where("FTPY1501").Any(d => d.GetMessage().Contains("accelerate")),
      Is.False,
      "Untouched launchers in the reference chain must not contribute their requirements."
    );
    Assert.That(
      diagnostics.Where("FTPY1501").Any(d => d.GetMessage().Contains("torch")),
      Is.False,
      "TorchrunLauncher in the reference chain must not contribute either."
    );
  }

  [Test]
  public async Task LauncherInstantiated_FoldsAttributeRequirements()
  {
    // Positive case for the new syntax-walk: `new AccelerateLauncher()`
    // in source is the reference the analyzer needs to see in order
    // to fold its declared `accelerate>=0.30` requirement.
    var source = """
      using Flowthru.Step.Python;
      namespace Sample;

      public static class Pipeline
      {
        public static IPythonLauncher Build() => new AccelerateLauncher();
      }
      """;
    var diagnostics = await RunWithReferences(
      source,
      uvLock: UvLock(("pyarrow", "15.0.0"))  // accelerate missing
    );

    Assert.That(
      diagnostics.Where("FTPY1501").Any(d => d.GetMessage().Contains("accelerate")),
      Is.True,
      "AccelerateLauncher referenced in source must contribute its accelerate dep."
    );
  }

  [Test]
  public async Task LauncherReferencedViaGenericArg_FoldsAttributeRequirements()
  {
    // Second positive case: DI registration via type arg, not `new`.
    // `services.AddSingleton<IPythonLauncher, AccelerateLauncher>()`
    // names AccelerateLauncher only as a generic type argument; the
    // analyzer's NameSyntax binding must still discover it.
    var source = """
      using Flowthru.Step.Python;
      using Microsoft.Extensions.DependencyInjection;
      namespace Sample;

      public static class Pipeline
      {
        public static void Register(IServiceCollection services) =>
          services.AddSingleton<IPythonLauncher, AccelerateLauncher>();
      }
      """;
    var diagnostics = await AnalyzerTestHarness.RunAsync(
      new PythonRequirementsAnalyzer(),
      source,
      additionalFiles: new[] { UvLock(("pyarrow", "15.0.0")) },
      extraReferences: new[]
      {
        typeof(IPythonCapability).Assembly,
        typeof(Microsoft.Extensions.DependencyInjection.IServiceCollection).Assembly,
        typeof(Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions).Assembly,
      }
    );

    Assert.That(
      diagnostics.Where("FTPY1501").Any(d => d.GetMessage().Contains("accelerate")),
      Is.True,
      "AccelerateLauncher referenced as a generic type argument must still contribute its accelerate dep."
    );
  }

  // ── Helpers ──────────────────────────────────────────────────────────

  private static Task<System.Collections.Immutable.ImmutableArray<Microsoft.CodeAnalysis.Diagnostic>>
    RunWithReferences(string source, AnalyzerTestHarness.InMemoryAdditionalText uvLock) =>
    AnalyzerTestHarness.RunAsync(
      new PythonRequirementsAnalyzer(),
      source,
      additionalFiles: new[] { uvLock },
      extraReferences: typeof(IPythonCapability).Assembly
    );

  private static AnalyzerTestHarness.InMemoryAdditionalText UvLock(string package, string version) =>
    UvLock((package, version));

  private static AnalyzerTestHarness.InMemoryAdditionalText UvLock(
    params (string Package, string Version)[] packages
  )
  {
    var sb = new System.Text.StringBuilder();
    sb.AppendLine("version = 1");
    sb.AppendLine();
    foreach (var (pkg, ver) in packages)
    {
      sb.AppendLine("[[package]]");
      sb.AppendLine($"name = \"{pkg}\"");
      sb.AppendLine($"version = \"{ver}\"");
      sb.AppendLine();
    }
    return new AnalyzerTestHarness.InMemoryAdditionalText("uv.lock", sb.ToString());
  }
}
