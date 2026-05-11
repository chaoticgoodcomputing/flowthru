using Flowthru.Core.CodeFixes;
using Flowthru.Core.SourceGenerators.Step;
using Flowthru.Tests.Helpers;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Flowthru.Core.CodeFixes.Tests;

/// <summary>
/// Tests for FT1101: warns when a <c>FlowBuilder.AddStep</c> invocation's
/// <c>transform:</c> argument references a step factory class lacking
/// <c>[FlowthruStep]</c>. Inline lambdas and anonymous methods are exempted.
/// </summary>
[TestFixture]
[Category("CodeFixes")]
public class Ft1101Tests
{
  // Minimal stubs so the analyzer can resolve FlowBuilder.AddStep and
  // [FlowthruStep] without referencing the real Flowthru.Core assembly.
  // The analyzer only checks for: (a) the attribute by full name, and
  // (b) FlowBuilder.AddStep with a `transform:` parameter — so the
  // input/output types are placeholder-shaped (object) here.
  private const string Stubs = """
    namespace Flowthru.Step
    {
        [System.AttributeUsage(System.AttributeTargets.Class)]
        public class FlowthruStepAttribute : System.Attribute { }
    }

    namespace Flowthru.Flow
    {
        using System;

        public partial class FlowBuilder
        {
            public FlowBuilder AddStep<TIn, TOut>(
                string label,
                Func<TIn, TOut> transform,
                object input,
                object output
            ) => this;
        }
    }
    """;

  [Test]
  public async Task UnattributedStepClass_ReportsFt1101AndCodeFixAddsAttribute()
  {
    var source =
      Stubs
      + """

        namespace TestProject
        {
            using Flowthru.Flow;

            public static class MyStep
            {
                public static System.Func<int, int> Create() => x => x;
            }

            public class Sample
            {
                public void Build()
                {
                    var b = new FlowBuilder();
                    b.AddStep(label: "s", transform: {|FT1101:MyStep.Create()|},
                        input: new object(), output: new object());
                }
            }
        }
        """;

    // The fix adds the using at the compilation-unit root, which is the convention
    // used across Flowthru's own files.
    var fixedSource =
      "using Flowthru.Step;\n\n"
      + Stubs
      + """

        namespace TestProject
        {
            using Flowthru.Flow;

            [FlowthruStep]
            public static class MyStep
            {
                public static System.Func<int, int> Create() => x => x;
            }

            public class Sample
            {
                public void Build()
                {
                    var b = new FlowBuilder();
                    b.AddStep(label: "s", transform: MyStep.Create(),
                        input: new object(), output: new object());
                }
            }
        }
        """;

    await new CSharpCodeFixTest<
      FlowthruStepAttributeAnalyzer,
      Ft1101AddFlowthruStepAttributeFix,
      NUnit4Verifier
    >
    {
      TestCode = source,
      FixedCode = fixedSource,
    }.RunAsync();
  }

  [Test]
  public async Task AttributedStepClass_NoDiagnostic()
  {
    var source =
      Stubs
      + """

        namespace TestProject
        {
            using Flowthru.Flow;
            using Flowthru.Step;

            [FlowthruStep]
            public static class MyStep
            {
                public static System.Func<int, int> Create() => x => x;
            }

            public class Sample
            {
                public void Build()
                {
                    var b = new FlowBuilder();
                    b.AddStep(label: "s", transform: MyStep.Create(),
                        input: new object(), output: new object());
                }
            }
        }
        """;

    await new CSharpAnalyzerTest<FlowthruStepAttributeAnalyzer, NUnit4Verifier>
    {
      TestCode = source,
    }.RunAsync();
  }

  [Test]
  public async Task InlineLambda_NoDiagnostic()
  {
    var source =
      Stubs
      + """

        namespace TestProject
        {
            using Flowthru.Flow;

            public class Sample
            {
                public void Build()
                {
                    var b = new FlowBuilder();
                    b.AddStep<int, int>(label: "s", transform: x => x,
                        input: new object(), output: new object());
                }
            }
        }
        """;

    await new CSharpAnalyzerTest<FlowthruStepAttributeAnalyzer, NUnit4Verifier>
    {
      TestCode = source,
    }.RunAsync();
  }

  [Test]
  public async Task AnonymousMethod_NoDiagnostic()
  {
    var source =
      Stubs
      + """

        namespace TestProject
        {
            using Flowthru.Flow;

            public class Sample
            {
                public void Build()
                {
                    var b = new FlowBuilder();
                    b.AddStep<int, int>(label: "s",
                        transform: delegate(int x) { return x; },
                        input: new object(), output: new object());
                }
            }
        }
        """;

    await new CSharpAnalyzerTest<FlowthruStepAttributeAnalyzer, NUnit4Verifier>
    {
      TestCode = source,
    }.RunAsync();
  }

  [Test]
  public async Task AddStepOnNonFlowBuilderType_NoDiagnostic()
  {
    // Tests that the analyzer doesn't fire on AddStep methods of unrelated types.
    var source =
      Stubs
      + """

        namespace TestProject
        {
            public static class UnrelatedClass
            {
                public static System.Func<int, int> Create() => x => x;
            }

            public class NotAFlowBuilder
            {
                public void AddStep(string label, System.Func<int, int> transform) { }
            }

            public class Sample
            {
                public void Build()
                {
                    var b = new NotAFlowBuilder();
                    b.AddStep("s", UnrelatedClass.Create());
                }
            }
        }
        """;

    await new CSharpAnalyzerTest<FlowthruStepAttributeAnalyzer, NUnit4Verifier>
    {
      TestCode = source,
    }.RunAsync();
  }
}
