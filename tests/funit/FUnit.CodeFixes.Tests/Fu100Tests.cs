using Flowthru.FUnit.CodeFixes;
using Flowthru.FUnit.SourceGenerators;
using Flowthru.Tests.Helpers;

namespace Flowthru.FUnit.CodeFixes.Tests;

/// <summary>
/// Tests for the FU100 codefix (<see cref="Fu100AddStubRegistrationFix"/>):
/// scaffolds a new <c>[FUnitStubContainer]</c> when none exists, or appends
/// a registration line to an existing one.
/// </summary>
/// <remarks>
/// Uses the manual <see cref="CodeFixTestHelper"/> harness rather than the
/// standard <c>CSharpCodeFixTest</c> because FU100 is tagged
/// <c>WellKnownDiagnosticTags.CompilationEnd</c> (required — it cross-references
/// stub registrations against test methods across the whole compilation), and
/// the standard harness rejects codefixes on non-local diagnostics.
/// </remarks>
[TestFixture]
[Category("CodeFixes")]
public class Fu100Tests
{
  // Minimal in-source stubs for the analyzer's full-name lookups. Matches the
  // pattern used by FU100AnalyzerTests in the SourceGenerators.Tests project.
  private const string Stubs = """
    namespace Flowthru.Step
    {
        [System.AttributeUsage(System.AttributeTargets.Class)]
        public class FlowthruStepAttribute : System.Attribute { }
    }

    namespace Flowthru.Step.Testing
    {
        [System.AttributeUsage(System.AttributeTargets.Method)]
        public sealed class FUnitStepTestAttribute : System.Attribute
        {
            public FUnitStepTestAttribute(System.Type stepType) { }
        }

        [System.AttributeUsage(System.AttributeTargets.Class)]
        public sealed class FUnitStubContainerAttribute : System.Attribute { }

        public abstract class FUnitContext { }
    }

    namespace Microsoft.Extensions.DependencyInjection
    {
        public interface IServiceCollection { }

        public static class ServiceCollectionExtensions
        {
            public static IServiceCollection AddSingleton<TService, TImpl>(this IServiceCollection s)
                where TService : class where TImpl : class, TService => s;
        }
    }
    """;

  [Test]
  public async Task NoExistingContainer_ScaffoldsNewOne()
  {
    var source =
      Stubs
      + """

        namespace TestProject
        {
            using Flowthru.Step;
            using Flowthru.Step.Testing;

            public interface IMyService { }

            [FlowthruStep]
            public static class MyStep
            {
                public static System.Func<int, int> Create(IMyService svc) => x => x;
            }

        #pragma warning disable FU002
            public class MyTests : FUnitContext
            {
                [FUnitStepTest(typeof(MyStep))]
                public void Works() { }
            }
        }
        """;

    var result = await CodeFixTestHelper.ApplyCodeFixAsync(
      analyzer: new FUnitDiagnosticAnalyzer(),
      codeFix: new Fu100AddStubRegistrationFix(),
      sourceCode: source
    );

    // The analyzer fires FU100 because IMyService is unregistered.
    Assert.That(
      result.InitialDiagnostics.Where(d => d.Id == "FU100").ToList(),
      Is.Not.Empty,
      "FU100 should fire on unstubbed service. Got: "
        + string.Join(", ", result.InitialDiagnostics.Select(d => d.Id))
    );

    // The codefix registers exactly the "scaffold new container" action.
    Assert.That(result.RegisteredCodeFixTitles.Length, Is.EqualTo(1));
    Assert.That(
      result.RegisteredCodeFixTitles[0],
      Does.Contain("Create [FUnitStubContainer]")
    );

    // Applying the fix mutates exactly one document (the source itself —
    // there's no other doc since no container existed).
    Assert.That(result.ChangedDocuments, Has.Count.EqualTo(1));
    var fixedText = result.ChangedDocuments.Single().Value;
    Assert.That(fixedText, Does.Contain("[global::Flowthru.Step.Testing.FUnitStubContainer]"));
    Assert.That(fixedText, Does.Contain("internal static class TestStubs"));
    Assert.That(fixedText, Does.Contain("services.AddSingleton<global::TestProject.IMyService, TODO_StubImpl>()"));
  }

  [Test]
  public async Task ExistingContainer_AppendsRegistrationLine()
  {
    var source =
      Stubs
      + """

        namespace TestProject
        {
            using Flowthru.Step;
            using Flowthru.Step.Testing;
            using Microsoft.Extensions.DependencyInjection;

            public interface IMyService { }

            [FlowthruStep]
            public static class MyStep
            {
                public static System.Func<int, int> Create(IMyService svc) => x => x;
            }

            [FUnitStubContainer]
            public static class TestStubs
            {
                public static void Configure(IServiceCollection services)
                {
                }
            }

        #pragma warning disable FU002
            public class MyTests : FUnitContext
            {
                [FUnitStepTest(typeof(MyStep))]
                public void Works() { }
            }
        }
        """;

    var result = await CodeFixTestHelper.ApplyCodeFixAsync(
      analyzer: new FUnitDiagnosticAnalyzer(),
      codeFix: new Fu100AddStubRegistrationFix(),
      sourceCode: source
    );

    Assert.That(
      result.InitialDiagnostics.Where(d => d.Id == "FU100").ToList(),
      Is.Not.Empty
    );

    Assert.That(
      result.RegisteredCodeFixTitles[0],
      Does.Contain("Add").And.Contain("registration to existing [FUnitStubContainer]")
    );

    Assert.That(result.ChangedDocuments, Has.Count.EqualTo(1));
    var fixedText = result.ChangedDocuments.Single().Value;
    Assert.That(
      fixedText,
      Does.Contain("services.AddSingleton<global::TestProject.IMyService, TODO_StubImpl>()")
    );
    // The registration must end up inside the existing Configure body, not as
    // a new container scaffold.
    Assert.That(fixedText, Does.Not.Contain("internal static class TestStubs"),
      "Should append into the existing TestStubs, not scaffold a new container.");
  }

  [Test]
  public async Task StepWithoutServiceDeps_RegistersNoAction()
  {
    var source =
      Stubs
      + """

        namespace TestProject
        {
            using Flowthru.Step;
            using Flowthru.Step.Testing;

            [FlowthruStep]
            public static class PureStep
            {
                public static System.Func<int, int> Create() => x => x;
            }

        #pragma warning disable FU002
            public class MyTests : FUnitContext
            {
                [FUnitStepTest(typeof(PureStep))]
                public void Works() { }
            }
        }
        """;

    var result = await CodeFixTestHelper.ApplyCodeFixAsync(
      analyzer: new FUnitDiagnosticAnalyzer(),
      codeFix: new Fu100AddStubRegistrationFix(),
      sourceCode: source
    );

    Assert.That(
      result.InitialDiagnostics.Where(d => d.Id == "FU100").ToList(),
      Is.Empty,
      "FU100 should not fire when the step has no service deps."
    );
    Assert.That(result.RegisteredCodeFixTitles.Length, Is.EqualTo(0));
    Assert.That(result.ChangedDocuments, Is.Empty);
  }
}
