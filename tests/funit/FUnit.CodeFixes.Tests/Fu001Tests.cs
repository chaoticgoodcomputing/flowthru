using System.Collections.Immutable;
using Flowthru.FUnit.CodeFixes;
using Flowthru.FUnit.SourceGenerators;
using Flowthru.Tests.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;

namespace FUnit.CodeFixes.Tests;

/// <summary>
/// Tests for FU001 — a <c>[FlowthruStep]</c>-annotated class with no
/// <c>[FUnitStepTest]</c> coverage — and the <see cref="Fu001ScaffoldTestsClassFix"/>
/// codefix that scaffolds an empty <c>Tests : FUnitContext</c> nested class
/// guarded by <c>#if FUNIT_ENABLED</c>.
/// </summary>
[TestFixture]
[Category("CodeFixes")]
public class Fu001Tests
{
  // Minimal stubs so the analyzer resolves [FlowthruStep], [FUnitStepTest],
  // and FUnitContext without referencing real Flowthru.Core / FUnit assemblies.
  // The analyzer keys off ToDisplayString() equality of full type names, so the
  // namespace/name layout is the only load-bearing part of these stubs.
  private const string Stubs = """
    namespace Flowthru.Step
    {
        [System.AttributeUsage(System.AttributeTargets.Class)]
        public class FlowthruStepAttribute : System.Attribute { }
    }

    namespace Flowthru.Step.Testing
    {
        [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true)]
        public class FUnitStepTestAttribute : System.Attribute
        {
            public FUnitStepTestAttribute(System.Type target) { }
        }

        public abstract class FUnitContext { }
    }
    """;

  // ---------- Analyzer behavior ----------

  [Test]
  public async Task FlowthruStepWithoutTests_ReportsFu001()
  {
    var source =
      Stubs
      + """

        namespace TestProject
        {
            using Flowthru.Step;

            [FlowthruStep]
            public class {|FU001:MyStep|}
            {
            }
        }
        """;

    await new CSharpAnalyzerTest<FUnitDiagnosticAnalyzer, NUnit4Verifier>
    {
      TestCode = source,
    }.RunAsync();
  }

  [Test]
  public async Task FlowthruStepWithMatchingTests_NoDiagnostic()
  {
    // An [FUnitStepTest(typeof(MyStep))]-decorated method elsewhere in the
    // project satisfies the analyzer's coverage check.
    var source =
      Stubs
      + """

        namespace TestProject
        {
            using Flowthru.Step;
            using Flowthru.Step.Testing;

            [FlowthruStep]
            public class MyStep
            {
            }

            public class TestsForMyStep
            {
                [FUnitStepTest(typeof(MyStep))]
                public void SomeTest() { }
            }
        }
        """;

    await new CSharpAnalyzerTest<FUnitDiagnosticAnalyzer, NUnit4Verifier>
    {
      TestCode = source,
    }.RunAsync();
  }

  [Test]
  public async Task NonStepClass_NoDiagnostic()
  {
    // A class without [FlowthruStep] is irrelevant to FU001 regardless of
    // whether anyone tests it.
    var source =
      Stubs
      + """

        namespace TestProject
        {
            public class JustAClass
            {
            }
        }
        """;

    await new CSharpAnalyzerTest<FUnitDiagnosticAnalyzer, NUnit4Verifier>
    {
      TestCode = source,
    }.RunAsync();
  }

  [Test]
  public async Task FlowthruStepWithTestForDifferentClass_StillReportsFu001()
  {
    // The analyzer keys [FUnitStepTest]'s target type via the constructor's
    // first argument; if the target is *another* class, the step under test
    // remains untested.
    var source =
      Stubs
      + """

        namespace TestProject
        {
            using Flowthru.Step;
            using Flowthru.Step.Testing;

            [FlowthruStep]
            public class {|FU001:MyStep|}
            {
            }

            [FlowthruStep]
            public class OtherStep
            {
            }

            public class TestsForOtherStep
            {
                [FUnitStepTest(typeof(OtherStep))]
                public void SomeTest() { }
            }
        }
        """;

    await new CSharpAnalyzerTest<FUnitDiagnosticAnalyzer, NUnit4Verifier>
    {
      TestCode = source,
    }.RunAsync();
  }

  // ---------- Codefix behavior ----------
  //
  // The Roslyn CSharpCodeFixTest harness runs the codefix iteratively until
  // the diagnostic clears. Fu001ScaffoldTestsClassFix only *scaffolds* an
  // empty Tests-class — it doesn't add [FUnitStepTest]-annotated methods, so
  // FU001 keeps firing post-fix, and the harness loops. To assert on the
  // codefix's *immediate* output (the actual surface area shipped today),
  // we drive the codefix once through Roslyn workspaces directly. This is
  // the same pattern used internally by CSharpCodeFixTest before its iterate
  // loop wraps it.

  [Test]
  public async Task CodeFix_RegistersScaffoldAction_ForFu001Diagnostic()
  {
    var source =
      Stubs
      + """

        namespace TestProject
        {
            using Flowthru.Step;

            [FlowthruStep]
            public class MyStep
            {
            }
        }
        """;

    var (document, diagnostic) = await BuildDocumentAndFu001DiagnosticAsync(source);

    Assert.That(diagnostic.Id, Is.EqualTo("FU001"));

    var fix = new Fu001ScaffoldTestsClassFix();
    Assert.That(fix.FixableDiagnosticIds, Does.Contain("FU001"),
      "FixableDiagnosticIds advertises FU001");

    var registered = new List<CodeAction>();
    var context = new CodeFixContext(
      document,
      diagnostic,
      (action, _) => registered.Add(action),
      CancellationToken.None
    );
    await fix.RegisterCodeFixesAsync(context);

    Assert.That(registered, Has.Count.EqualTo(1),
      "exactly one code action is registered for an FU001 diagnostic");
    Assert.That(registered[0].Title, Is.EqualTo("Scaffold inline FUnit Tests class"));
    Assert.That(registered[0].EquivalenceKey, Is.EqualTo(nameof(Fu001ScaffoldTestsClassFix)));
  }

  [Test]
  public async Task CodeFix_ScaffoldsTestsClassInsideStep_WithFUnitEnabledGuard()
  {
    var source =
      Stubs
      + """

        namespace TestProject
        {
            using Flowthru.Step;

            [FlowthruStep]
            public class MyStep
            {
            }
        }
        """;

    var fixedText = await ApplyFu001FixAsync(source);

    // Behavioural assertions on the codefix's output. We deliberately do not
    // assert on exact whitespace — only on the structural changes the fix is
    // contracted to make.
    Assert.That(fixedText, Does.Contain("#if FUNIT_ENABLED"),
      "the scaffolded Tests class is wrapped in #if FUNIT_ENABLED");
    Assert.That(fixedText, Does.Contain("#endif"),
      "the scaffolded Tests class is terminated by #endif");
    Assert.That(fixedText, Does.Contain("class Tests"),
      "the scaffolded type is named Tests");
    Assert.That(fixedText, Does.Contain("global::Flowthru.Step.Testing.FUnitContext"),
      "the scaffolded Tests class derives from FUnitContext (fully qualified)");

    // The Tests class is nested inside MyStep, not added as a sibling — i.e.
    // it appears after `class MyStep` opens but before `class MyStep` closes.
    var myStepIdx = fixedText.IndexOf("class MyStep", StringComparison.Ordinal);
    var testsIdx = fixedText.IndexOf("class Tests", StringComparison.Ordinal);
    Assert.That(myStepIdx, Is.GreaterThanOrEqualTo(0));
    Assert.That(testsIdx, Is.GreaterThan(myStepIdx),
      "Tests class is declared after MyStep — i.e. nested inside its body");
  }

  [Test]
  public async Task CodeFix_LeavesUnrelatedSource_Unchanged()
  {
    // A second non-flagged class in the same compilation must not be touched.
    var source =
      Stubs
      + """

        namespace TestProject
        {
            using Flowthru.Step;

            [FlowthruStep]
            public class MyStep
            {
            }

            public class OtherClass
            {
                public int Marker => 42;
            }
        }
        """;

    var fixedText = await ApplyFu001FixAsync(source);

    Assert.That(fixedText, Does.Contain("public class OtherClass"),
      "OtherClass declaration is preserved");
    Assert.That(fixedText, Does.Contain("public int Marker => 42;"),
      "OtherClass members are preserved");
  }

  [Test]
  public void CodeFix_GetFixAllProvider_ReturnsBatchFixer()
  {
    var fix = new Fu001ScaffoldTestsClassFix();
    var provider = fix.GetFixAllProvider();
    Assert.That(provider, Is.SameAs(WellKnownFixAllProviders.BatchFixer));
  }

  // ---------- Helpers ----------

  /// <summary>
  /// Builds an ad-hoc Roslyn workspace + document from <paramref name="source"/>,
  /// runs the FU001 analyzer against it, and returns the document plus the
  /// single FU001 diagnostic reported.
  /// </summary>
  private static async Task<(Document Document, Diagnostic Diagnostic)>
    BuildDocumentAndFu001DiagnosticAsync(string source)
  {
    var workspace = new AdhocWorkspace();
    var projectId = ProjectId.CreateNewId();
    var documentId = DocumentId.CreateNewId(projectId);

    // Reference the same core assemblies the production analyzer relies on.
    // We just need enough for the test compilation to bind System.* types —
    // the [FlowthruStep] / [FUnitStepTest] / FUnitContext shapes are defined
    // inline by Stubs.
    var references = new[]
    {
      MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(System.Attribute).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(System.Runtime.CompilerServices.RuntimeHelpers).Assembly.Location),
    };

    var solution = workspace.CurrentSolution
      .AddProject(projectId, "TestProject", "TestProject", LanguageNames.CSharp)
      .AddMetadataReferences(projectId, references)
      .WithProjectCompilationOptions(
        projectId,
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
      )
      .AddDocument(documentId, "Test.cs", SourceText.From(source));

    var project = solution.GetProject(projectId)!;
    var compilation = await project.GetCompilationAsync();

    var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new FUnitDiagnosticAnalyzer());
    var compilationWithAnalyzers = compilation!.WithAnalyzers(analyzers);
    var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();

    var fu001 = diagnostics.Single(d => d.Id == "FU001");
    return (project.GetDocument(documentId)!, fu001);
  }

  /// <summary>
  /// Runs <see cref="Fu001ScaffoldTestsClassFix"/> against <paramref name="source"/>
  /// exactly once and returns the fixed document's text.
  /// </summary>
  private static async Task<string> ApplyFu001FixAsync(string source)
  {
    var (document, diagnostic) = await BuildDocumentAndFu001DiagnosticAsync(source);

    var fix = new Fu001ScaffoldTestsClassFix();
    CodeAction? action = null;
    var context = new CodeFixContext(
      document,
      diagnostic,
      (a, _) => action = a,
      CancellationToken.None
    );
    await fix.RegisterCodeFixesAsync(context);

    Assert.That(action, Is.Not.Null, "the codefix registered an action");

    var operations = await action!.GetOperationsAsync(CancellationToken.None);
    var applyChanges = operations.OfType<ApplyChangesOperation>().Single();
    var changedDocument = applyChanges.ChangedSolution.GetDocument(document.Id)!;
    var changedText = await changedDocument.GetTextAsync();
    return changedText.ToString();
  }
}
