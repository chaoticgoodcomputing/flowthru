using Flowthru.FUnit.SourceGenerators;

namespace FUnit.SourceGenerators.Tests;

/// <summary>
/// Tests for <see cref="StepTestRegistryGenerator"/>. The generator
/// has two output shapes that must be covered:
/// <list type="number">
///   <item>The <c>StepTestRegistry</c> mapping step types to test
///     counts — emitted only if at least one <c>[FlowthruStep]</c>
///     class is found.</item>
///   <item>Per-test-class runner classes for the detected test
///     framework (NUnit / xUnit / MSTest) — emitted only if at least
///     one test framework is referenced by the compilation.</item>
/// </list>
/// </summary>
[TestFixture]
public class StepTestRegistryGeneratorTests
{
  // ── Stubs ──────────────────────────────────────────────────────────────
  //
  // The generator keys on these exact fully-qualified names; stub
  // declarations let fixtures stay self-contained without dragging
  // the production runtime assemblies into the test compilation.

  private const string AttributeStubs = """
    namespace Flowthru.Step
    {
      [System.AttributeUsage(System.AttributeTargets.Class)]
      public sealed class FlowthruStepAttribute : System.Attribute { }
    }

    namespace Flowthru.Step.Testing
    {
      [System.AttributeUsage(System.AttributeTargets.Method)]
      public sealed class FUnitStepTestAttribute : System.Attribute
      {
        public FUnitStepTestAttribute(System.Type stepType) { }
      }

      public class FUnitContext { }
    }
    """;

  /// <summary>
  /// Stand-in for the NUnit framework reference. The generator only
  /// reads <c>compilation.ReferencedAssemblyNames</c>, so any
  /// reference whose manifest name is <c>nunit.framework</c> is
  /// enough to flip the framework-detection branch to NUnit. We emit
  /// an empty C# compilation to disk under that name and reference
  /// the resulting DLL.
  /// </summary>
  private static Microsoft.CodeAnalysis.MetadataReference NUnitStubReference { get; } =
    BuildNamedStubReference("nunit.framework");

  private static Microsoft.CodeAnalysis.MetadataReference BuildNamedStubReference(
    string assemblyName
  )
  {
    var compilation = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create(
      assemblyName,
      syntaxTrees: new[] { Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText("") },
      references: new[]
      {
        Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
      },
      options: new Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(
        Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary
      )
    );

    using var stream = new System.IO.MemoryStream();
    var emit = compilation.Emit(stream);
    if (!emit.Success)
    {
      throw new System.InvalidOperationException(
        "Failed to emit stub assembly: "
          + string.Join(
            "; ",
            emit.Diagnostics.Select(d => d.GetMessage())
          )
      );
    }
    stream.Position = 0;
    return Microsoft.CodeAnalysis.MetadataReference.CreateFromStream(stream);
  }

  // ── Registry emission ──────────────────────────────────────────────────

  [Test]
  public void NoFlowthruStepClasses_EmitsNoRegistry()
  {
    var consumer = """
      namespace Sample;
      public class Boring { }
      """;

    var result = AnalyzerTestHarness.RunGenerator(
      new StepTestRegistryGenerator(),
      new[] { AttributeStubs, consumer }
    );

    Assert.That(
      result.GeneratedSources.Any(g => g.HintName.Contains("StepTestRegistry")),
      Is.False,
      "When no [FlowthruStep] class exists the generator must short-circuit before AddSource. "
      + "Generated: " + string.Join(", ", result.GeneratedSources.Select(g => g.HintName))
    );
  }

  [Test]
  public void SingleFlowthruStepWithoutTests_EmitsRegistryWithZeroCount()
  {
    var consumer = """
      namespace Sample
      {
        [Flowthru.Step.FlowthruStepAttribute]
        public class FooStep { }
      }
      """;

    var result = AnalyzerTestHarness.RunGenerator(
      new StepTestRegistryGenerator(),
      new[] { AttributeStubs, consumer }
    );

    var registry = result.GeneratedSources
      .FirstOrDefault(g => g.HintName == "StepTestRegistry.g.cs");
    Assert.That(registry.Source, Is.Not.Null,
      "StepTestRegistry.g.cs must be emitted when a [FlowthruStep] class is present. "
      + "Generated: " + string.Join(", ", result.GeneratedSources.Select(g => g.HintName)));

    Assert.That(registry.Source, Does.Contain("StepTestRegistry"));
    Assert.That(registry.Source, Does.Contain("Sample.FooStep"),
      "Registry must reference the fully-qualified step type. Got:\n" + registry.Source);
    Assert.That(registry.Source, Does.Contain("typeof(Sample.FooStep)] = 0"),
      "A step with no [FUnitStepTest] methods should map to 0. Got:\n" + registry.Source);
  }

  [Test]
  public void StepWithTwoTests_EmitsRegistryWithCountTwo()
  {
    var consumer = """
      namespace Sample
      {
        [Flowthru.Step.FlowthruStepAttribute]
        public class FooStep { }

        public class FooStepTests
        {
          [Flowthru.Step.Testing.FUnitStepTestAttribute(typeof(FooStep))]
          public void A() { }

          [Flowthru.Step.Testing.FUnitStepTestAttribute(typeof(FooStep))]
          public void B() { }
        }
      }
      """;

    var result = AnalyzerTestHarness.RunGenerator(
      new StepTestRegistryGenerator(),
      new[] { AttributeStubs, consumer }
    );

    var registry = result.GeneratedSources
      .FirstOrDefault(g => g.HintName == "StepTestRegistry.g.cs");
    Assert.That(registry.Source, Is.Not.Null);
    Assert.That(registry.Source, Does.Contain("typeof(Sample.FooStep)] = 2"),
      "Two [FUnitStepTest] methods targeting FooStep should yield a count of 2. Got:\n"
        + registry.Source);
  }

  [Test]
  public void MultipleSteps_AllAppearInRegistry()
  {
    var consumer = """
      namespace Sample
      {
        [Flowthru.Step.FlowthruStepAttribute]
        public class StepA { }

        [Flowthru.Step.FlowthruStepAttribute]
        public class StepB { }

        public class TestClass
        {
          [Flowthru.Step.Testing.FUnitStepTestAttribute(typeof(StepA))]
          public void TestForA() { }
        }
      }
      """;

    var result = AnalyzerTestHarness.RunGenerator(
      new StepTestRegistryGenerator(),
      new[] { AttributeStubs, consumer }
    );

    var registry = result.GeneratedSources
      .FirstOrDefault(g => g.HintName == "StepTestRegistry.g.cs");
    Assert.That(registry.Source, Is.Not.Null);
    Assert.That(registry.Source, Does.Contain("typeof(Sample.StepA)] = 1"));
    Assert.That(registry.Source, Does.Contain("typeof(Sample.StepB)] = 0"));
  }

  // ── Runner emission ────────────────────────────────────────────────────

  [Test]
  public void NoTestFrameworkReferenced_NoRunnersEmitted()
  {
    // No nunit.framework / xunit / MSTest assembly is referenced —
    // the framework detector returns None and the generator emits
    // zero runner classes regardless of [FUnitStepTest] presence.
    var consumer = """
      namespace Sample
      {
        [Flowthru.Step.FlowthruStepAttribute]
        public class FooStep { }

        public class FooStepTests
        {
          [Flowthru.Step.Testing.FUnitStepTestAttribute(typeof(FooStep))]
          public void Sanity() { }
        }
      }
      """;

    var result = AnalyzerTestHarness.RunGenerator(
      new StepTestRegistryGenerator(),
      new[] { AttributeStubs, consumer }
    );

    Assert.That(
      result.GeneratedSources.Any(g => g.HintName.Contains("Runner")),
      Is.False,
      "Without a test-framework reference the runner branch must be silent. "
      + "Generated: " + string.Join(", ", result.GeneratedSources.Select(g => g.HintName))
    );
  }

  [Test]
  public void NUnitReferenced_EmitsNUnitRunnerWithTestFixtureAttribute()
  {
    var consumer = """
      namespace Sample
      {
        [Flowthru.Step.FlowthruStepAttribute]
        public class FooStep { }

        public class FooStepTests
        {
          [Flowthru.Step.Testing.FUnitStepTestAttribute(typeof(FooStep))]
          public void Sanity() { }
        }
      }
      """;

    var result = AnalyzerTestHarness.RunGenerator(
      new StepTestRegistryGenerator(),
      new[] { AttributeStubs, consumer },
      System.Array.Empty<System.Reflection.Assembly>(),
      new[] { NUnitStubReference }
    );

    var runner = result.GeneratedSources
      .FirstOrDefault(g => g.HintName.Contains("NUnitRunner"));
    Assert.That(runner.Source, Is.Not.Null,
      "An NUnit-framework reference should emit a per-test-class NUnitRunner. "
      + "Generated: " + string.Join(", ", result.GeneratedSources.Select(g => g.HintName)));

    Assert.That(runner.Source, Does.Contain("[NUnit.Framework.TestFixture]"),
      "NUnit runner should be tagged with [TestFixture]. Got:\n" + runner.Source);
    Assert.That(runner.Source, Does.Contain("[NUnit.Framework.Test]"),
      "Each forwarded method should carry [Test]. Got:\n" + runner.Source);
    Assert.That(runner.Source, Does.Contain("public new void Sanity() => base.Sanity();"),
      "Forwarding method shape should preserve the user-test method name. Got:\n"
        + runner.Source);
    Assert.That(runner.Source, Does.Contain("namespace Sample"),
      "Runner should sit in the same namespace as the source test class. Got:\n"
        + runner.Source);
    Assert.That(runner.Source, Does.Contain(": FooStepTests"),
      "Runner should inherit from the user's test class so [FUnitStepTest] methods are "
      + "re-exposed under framework attributes. Got:\n" + runner.Source);
  }

  [Test]
  public void NUnitReferenced_NoStepTestMethods_NoRunnersEmitted()
  {
    // A [FlowthruStep] class plus an NUnit reference but no
    // [FUnitStepTest] method anywhere — there's nothing to forward,
    // so the runner branch must stay silent.
    var consumer = """
      namespace Sample
      {
        [Flowthru.Step.FlowthruStepAttribute]
        public class LonelyStep { }
      }
      """;

    var result = AnalyzerTestHarness.RunGenerator(
      new StepTestRegistryGenerator(),
      new[] { AttributeStubs, consumer },
      System.Array.Empty<System.Reflection.Assembly>(),
      new[] { NUnitStubReference }
    );

    Assert.That(
      result.GeneratedSources.Any(g => g.HintName.Contains("Runner")),
      Is.False,
      "No [FUnitStepTest] methods means no runner to emit. Generated: "
        + string.Join(", ", result.GeneratedSources.Select(g => g.HintName))
    );
  }

  [Test]
  public void MultipleStepTestMethodsOnOneClass_EmitsOneRunnerWithAllForwards()
  {
    var consumer = """
      namespace Sample
      {
        [Flowthru.Step.FlowthruStepAttribute]
        public class FooStep { }

        public class FooStepTests
        {
          [Flowthru.Step.Testing.FUnitStepTestAttribute(typeof(FooStep))]
          public void First() { }

          [Flowthru.Step.Testing.FUnitStepTestAttribute(typeof(FooStep))]
          public void Second() { }
        }
      }
      """;

    var result = AnalyzerTestHarness.RunGenerator(
      new StepTestRegistryGenerator(),
      new[] { AttributeStubs, consumer },
      System.Array.Empty<System.Reflection.Assembly>(),
      new[] { NUnitStubReference }
    );

    var runners = result.GeneratedSources
      .Where(g => g.HintName.Contains("NUnitRunner"))
      .ToList();
    Assert.That(runners, Has.Count.EqualTo(1),
      "All [FUnitStepTest] methods on one test class collapse into a single runner. "
      + "Got: " + string.Join(", ", runners.Select(g => g.HintName)));

    var src = runners[0].Source;
    Assert.That(src, Does.Contain("public new void First() => base.First();"));
    Assert.That(src, Does.Contain("public new void Second() => base.Second();"));
  }

  [Test]
  public void TestClassInGlobalNamespace_EmitsRunnerWithoutNamespace()
  {
    var consumer = """
      [Flowthru.Step.FlowthruStepAttribute]
      public class GlobalStep { }

      public class GlobalStepTests
      {
        [Flowthru.Step.Testing.FUnitStepTestAttribute(typeof(GlobalStep))]
        public void Smoke() { }
      }
      """;

    var result = AnalyzerTestHarness.RunGenerator(
      new StepTestRegistryGenerator(),
      new[] { AttributeStubs, consumer },
      System.Array.Empty<System.Reflection.Assembly>(),
      new[] { NUnitStubReference }
    );

    var runner = result.GeneratedSources
      .FirstOrDefault(g => g.HintName.Contains("NUnitRunner"));
    Assert.That(runner.Source, Is.Not.Null,
      "Runner should still emit for a global-namespace test class.");
    Assert.That(runner.Source, Does.Not.Contain("namespace "),
      "A global-namespace test class should not yield a `namespace` block. Got:\n"
        + runner.Source);
  }
}
