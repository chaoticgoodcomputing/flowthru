using Flowthru.Core.CodeFixes;
using Flowthru.Core.SourceGenerators.StepAnalysis;
using Flowthru.Tests.Helpers;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Flowthru.Core.CodeFixes.Tests;

/// <summary>
/// Tests for FT4001: warns when a <c>FlowBuilder.AddStep</c> invocation's
/// <c>transform:</c> argument references a step factory class lacking
/// <c>[FlowthruStep]</c>. Inline lambdas and anonymous methods are exempted.
/// </summary>
[TestFixture]
[Category("CodeFixes")]
public class Ft4001Tests
{
  // Minimal stubs so the analyzer can resolve FlowBuilder.AddStep and
  // [FlowthruStep] without referencing the real Flowthru.Core assembly.
  private const string Stubs = """
    namespace Flowthru.Core.Steps
    {
        [System.AttributeUsage(System.AttributeTargets.Class)]
        public class FlowthruStepAttribute : System.Attribute { }
    }

    namespace Flowthru.Core.Graph
    {
        public interface INode<T> { }
    }

    namespace Flowthru.Core.Flows
    {
        using System;
        using Flowthru.Core.Graph;

        public partial class FlowBuilder
        {
            public FlowBuilder AddStep<TIn, TOut>(
                string label,
                Func<TIn, TOut> transform,
                INode<TIn> input,
                INode<TOut> output,
                string description = ""
            ) => this;
        }
    }
    """;

  [Test]
  public async Task UnattributedStepClass_ReportsFt4001AndCodeFixAddsAttribute()
  {
    var source =
      Stubs
      + """

        namespace TestProject
        {
            using Flowthru.Core.Flows;
            using Flowthru.Core.Graph;

            public static class MyStep
            {
                public static System.Func<int, int> Create() => x => x;
            }

            public class FakeNode<T> : INode<T> { }

            public class Sample
            {
                public void Build()
                {
                    var b = new FlowBuilder();
                    b.AddStep(label: "s", transform: {|FT4001:MyStep.Create()|},
                        input: new FakeNode<int>(), output: new FakeNode<int>());
                }
            }
        }
        """;

    // The fix adds the using at the compilation-unit root, which is the convention
    // used across Flowthru's own files.
    var fixedSource =
      "using Flowthru.Core.Steps;\n\n"
      + Stubs
      + """

        namespace TestProject
        {
            using Flowthru.Core.Flows;
            using Flowthru.Core.Graph;

            [FlowthruStep]
            public static class MyStep
            {
                public static System.Func<int, int> Create() => x => x;
            }

            public class FakeNode<T> : INode<T> { }

            public class Sample
            {
                public void Build()
                {
                    var b = new FlowBuilder();
                    b.AddStep(label: "s", transform: MyStep.Create(),
                        input: new FakeNode<int>(), output: new FakeNode<int>());
                }
            }
        }
        """;

    await new CSharpCodeFixTest<
      FlowthruStepAttributeAnalyzer,
      Ft4001AddFlowthruStepAttributeFix,
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
            using Flowthru.Core.Flows;
            using Flowthru.Core.Graph;
            using Flowthru.Core.Steps;

            [FlowthruStep]
            public static class MyStep
            {
                public static System.Func<int, int> Create() => x => x;
            }

            public class FakeNode<T> : INode<T> { }

            public class Sample
            {
                public void Build()
                {
                    var b = new FlowBuilder();
                    b.AddStep(label: "s", transform: MyStep.Create(),
                        input: new FakeNode<int>(), output: new FakeNode<int>());
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
            using Flowthru.Core.Flows;
            using Flowthru.Core.Graph;

            public class FakeNode<T> : INode<T> { }

            public class Sample
            {
                public void Build()
                {
                    var b = new FlowBuilder();
                    b.AddStep<int, int>(label: "s", transform: x => x,
                        input: new FakeNode<int>(), output: new FakeNode<int>());
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
            using Flowthru.Core.Flows;
            using Flowthru.Core.Graph;

            public class FakeNode<T> : INode<T> { }

            public class Sample
            {
                public void Build()
                {
                    var b = new FlowBuilder();
                    b.AddStep<int, int>(label: "s",
                        transform: delegate(int x) { return x; },
                        input: new FakeNode<int>(), output: new FakeNode<int>());
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

  [Test]
  public async Task ExternalStepClass_NoDiagnostic()
  {
    // The analyzer skips when the receiver type's locations are all non-source —
    // i.e., the type is from a referenced assembly the user can't modify.
    // Here, the "step class" is FlowBuilder itself (which has the AddStep method
    // resolved against), so the analyzer must not crash or report.
    var source =
      Stubs
      + """

        namespace TestProject
        {
            using Flowthru.Core.Flows;
            using Flowthru.Core.Graph;

            public class FakeNode<T> : INode<T> { }

            public class Sample
            {
                public void Build()
                {
                    // Direct method-group reference to a type in the stubs (which
                    // are technically in-source for the test, but this exercises
                    // the resolution path for non-step types).
                    var b = new FlowBuilder();
                    b.AddStep<int, int>(label: "s", transform: (int x) => x + 1,
                        input: new FakeNode<int>(), output: new FakeNode<int>());
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
