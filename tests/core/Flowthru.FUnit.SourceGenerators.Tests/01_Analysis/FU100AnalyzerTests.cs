using Flowthru.FUnit.SourceGenerators;
using Flowthru.Tests.Helpers;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Flowthru.FUnit.SourceGenerators.Tests.Analysis;

/// <summary>
/// Tests for FU100 in <see cref="FUnitDiagnosticAnalyzer"/>: when a <c>[StepTest]</c>
/// references a step whose service-typed <c>Create(...)</c> parameter is not registered
/// in any visible <c>[FUnitStubContainer]</c>, the analyzer warns at the test method.
/// </summary>
[TestFixture]
[Category("Analyzers")]
public class FU100AnalyzerTests
{
  // Minimal stubs so the analyzer can resolve [FlowthruStep], [StepTest],
  // [FUnitStubContainer], FUnitContext, and IServiceCollection without referencing
  // the full Flowthru.Core / Flowthru.FUnit assemblies.
  private const string Stubs = """
    namespace Flowthru.Core.Steps
    {
        [System.AttributeUsage(System.AttributeTargets.Class)]
        public class FlowthruStepAttribute : System.Attribute
        {
            public bool IsIdempotent { get; set; }
            public bool HasSideEffects { get; set; }
        }
    }

    namespace Flowthru.FUnit
    {
        [System.AttributeUsage(System.AttributeTargets.Method)]
        public sealed class StepTestAttribute : System.Attribute
        {
            public StepTestAttribute(System.Type stepType) { StepType = stepType; }
            public System.Type StepType { get; }
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
            public static IServiceCollection AddSingleton<TService>(this IServiceCollection s)
                where TService : class => s;
        }
    }
    """;

  // ─────────────────────────────────────────────────────────────────────────
  // (1) Single container, all services stubbed → silent
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task SingleContainer_AllServicesStubbed_NoDiagnostic()
  {
    var source =
      Stubs
      + """

        namespace TestProject
        {
            using Flowthru.Core.Steps;
            using Flowthru.FUnit;
            using Microsoft.Extensions.DependencyInjection;

            public interface IMyService { }
            public sealed class FakeMyService : IMyService { }

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
                    services.AddSingleton<IMyService, FakeMyService>();
                }
            }

        #pragma warning disable FU002
            public class MyTests : FUnitContext
            {
                [StepTest(typeof(MyStep))]
                public void Works() { }
            }
        }
        """;

    await new CSharpAnalyzerTest<FUnitDiagnosticAnalyzer, NUnit4Verifier>
    {
      TestCode = source,
    }.RunAsync();
  }

  // ─────────────────────────────────────────────────────────────────────────
  // (2) Single container, one service unstubbed → FU100
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task SingleContainer_ServiceUnstubbed_EmitsFU100()
  {
    var source =
      Stubs
      + """

        namespace TestProject
        {
            using Flowthru.Core.Steps;
            using Flowthru.FUnit;
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
                public static void Configure(IServiceCollection services) { }
            }

        #pragma warning disable FU002
            public class MyTests : FUnitContext
            {
                [StepTest(typeof(MyStep))]
                public void {|FU100:Works|}() { }
            }
        }
        """;

    await new CSharpAnalyzerTest<FUnitDiagnosticAnalyzer, NUnit4Verifier>
    {
      TestCode = source,
    }.RunAsync();
  }

  // ─────────────────────────────────────────────────────────────────────────
  // (3) Multiple containers, services split across them → silent
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task MultipleContainers_ServicesSplit_NoDiagnostic()
  {
    var source =
      Stubs
      + """

        namespace TestProject
        {
            using Flowthru.Core.Steps;
            using Flowthru.FUnit;
            using Microsoft.Extensions.DependencyInjection;

            public interface IServiceA { }
            public interface IServiceB { }
            public sealed class FakeA : IServiceA { }
            public sealed class FakeB : IServiceB { }

            [FlowthruStep]
            public static class MyStep
            {
                public static System.Func<int, int> Create(IServiceA a, IServiceB b) => x => x;
            }

            [FUnitStubContainer]
            public static class StubsForA
            {
                public static void Configure(IServiceCollection services)
                {
                    services.AddSingleton<IServiceA, FakeA>();
                }
            }

            [FUnitStubContainer]
            public static class StubsForB
            {
                public static void Configure(IServiceCollection services)
                {
                    services.AddSingleton<IServiceB, FakeB>();
                }
            }

        #pragma warning disable FU002
            public class MyTests : FUnitContext
            {
                [StepTest(typeof(MyStep))]
                public void Works() { }
            }
        }
        """;

    await new CSharpAnalyzerTest<FUnitDiagnosticAnalyzer, NUnit4Verifier>
    {
      TestCode = source,
    }.RunAsync();
  }

  // ─────────────────────────────────────────────────────────────────────────
  // (4) Step has no service params → silent regardless of containers
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task StepWithoutServiceParams_NoDiagnostic()
  {
    var source =
      Stubs
      + """

        namespace TestProject
        {
            using Flowthru.Core.Steps;
            using Flowthru.FUnit;

            [FlowthruStep]
            public static class PureStep
            {
                public static System.Func<int, int> Create() => x => x;
            }

        #pragma warning disable FU002
            public class MyTests : FUnitContext
            {
                [StepTest(typeof(PureStep))]
                public void Works() { }
            }
        }
        """;

    await new CSharpAnalyzerTest<FUnitDiagnosticAnalyzer, NUnit4Verifier>
    {
      TestCode = source,
    }.RunAsync();
  }

  // ─────────────────────────────────────────────────────────────────────────
  // (5) No container, step has service params → FU100
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task NoContainer_StepNeedsService_EmitsFU100()
  {
    var source =
      Stubs
      + """

        namespace TestProject
        {
            using Flowthru.Core.Steps;
            using Flowthru.FUnit;

            public interface IMyService { }

            [FlowthruStep]
            public static class MyStep
            {
                public static System.Func<int, int> Create(IMyService svc) => x => x;
            }

        #pragma warning disable FU002
            public class MyTests : FUnitContext
            {
                [StepTest(typeof(MyStep))]
                public void {|FU100:Works|}() { }
            }
        }
        """;

    await new CSharpAnalyzerTest<FUnitDiagnosticAnalyzer, NUnit4Verifier>
    {
      TestCode = source,
    }.RunAsync();
  }

  // ─────────────────────────────────────────────────────────────────────────
  // (6) Registration outside [FUnitStubContainer] → not counted, FU100 fires
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task RegistrationOutsideStubContainer_DoesNotCount_EmitsFU100()
  {
    var source =
      Stubs
      + """

        namespace TestProject
        {
            using Flowthru.Core.Steps;
            using Flowthru.FUnit;
            using Microsoft.Extensions.DependencyInjection;

            public interface IMyService { }
            public sealed class FakeMyService : IMyService { }

            [FlowthruStep]
            public static class MyStep
            {
                public static System.Func<int, int> Create(IMyService svc) => x => x;
            }

            // Registration in a non-stub-container class — analyzer ignores.
            public static class NotAStubContainer
            {
                public static void Setup(IServiceCollection services)
                {
                    services.AddSingleton<IMyService, FakeMyService>();
                }
            }

        #pragma warning disable FU002
            public class MyTests : FUnitContext
            {
                [StepTest(typeof(MyStep))]
                public void {|FU100:Works|}() { }
            }
        }
        """;

    await new CSharpAnalyzerTest<FUnitDiagnosticAnalyzer, NUnit4Verifier>
    {
      TestCode = source,
    }.RunAsync();
  }
}
