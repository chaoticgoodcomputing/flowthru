using Flowthru.FUnit.SourceGenerators;

namespace FUnit.SourceGenerators.Tests;

/// <summary>
/// Tests for FU100 in <see cref="FUnitDiagnosticAnalyzer"/>: when a
/// <c>[FUnitStepTest]</c> references a step whose service-typed
/// <c>Create(...)</c> parameter is not registered in any visible
/// <c>[FUnitStubContainer]</c>, the analyzer warns at the test method.
/// </summary>
[TestFixture]
public class FU100AnalyzerTests
{
  // Minimal stubs so the analyzer can resolve [FlowthruStep], [FUnitStepTest],
  // [FUnitStubContainer], FUnitContext, and IServiceCollection without
  // referencing the real Flowthru.Core / Flowthru.FUnit assemblies.
  private const string Stubs = """
    namespace Flowthru.Step
    {
        [System.AttributeUsage(System.AttributeTargets.Class)]
        public class FlowthruStepAttribute : System.Attribute
        {
            public bool IsIdempotent { get; set; }
            public bool HasSideEffects { get; set; }
        }
    }

    namespace Flowthru.Step.Testing
    {
        [System.AttributeUsage(System.AttributeTargets.Method)]
        public sealed class FUnitStepTestAttribute : System.Attribute
        {
            public FUnitStepTestAttribute(System.Type stepType) { StepType = stepType; }
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
    var consumer = """
      namespace TestProject
      {
          using Flowthru.Step;
          using Flowthru.Step.Testing;
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
              [FUnitStepTest(typeof(MyStep))]
              public void Works() { }
          }
      }
      """;

    var diags = await AnalyzerTestHarness.RunAnalyzerAsync(
      new FUnitDiagnosticAnalyzer(),
      new[] { Stubs, consumer }
    );
    Assert.That(diags.WithId("FU100").ToList(), Is.Empty,
      "FU100 should not fire when every service param has a stub registration. Got: "
      + string.Join(", ", diags.Select(d => d.Id)));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // (2) Single container, one service unstubbed → FU100
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task SingleContainer_ServiceUnstubbed_EmitsFU100()
  {
    var consumer = """
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
              public static void Configure(IServiceCollection services) { }
          }

      #pragma warning disable FU002
          public class MyTests : FUnitContext
          {
              [FUnitStepTest(typeof(MyStep))]
              public void Works() { }
          }
      }
      """;

    var diags = await AnalyzerTestHarness.RunAnalyzerAsync(
      new FUnitDiagnosticAnalyzer(),
      new[] { Stubs, consumer }
    );
    Assert.That(diags.WithId("FU100").ToList(), Is.Not.Empty,
      "FU100 should fire when a [FUnitStepTest]'s step has an unstubbed service dep. "
      + "Got: " + string.Join(", ", diags.Select(d => d.Id)));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // (3) Multiple containers, services split across them → silent
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task MultipleContainers_ServicesSplit_NoDiagnostic()
  {
    var consumer = """
      namespace TestProject
      {
          using Flowthru.Step;
          using Flowthru.Step.Testing;
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
              [FUnitStepTest(typeof(MyStep))]
              public void Works() { }
          }
      }
      """;

    var diags = await AnalyzerTestHarness.RunAnalyzerAsync(
      new FUnitDiagnosticAnalyzer(),
      new[] { Stubs, consumer }
    );
    Assert.That(diags.WithId("FU100").ToList(), Is.Empty,
      "FU100 should not fire when service registrations are split across multiple "
      + "[FUnitStubContainer] classes. Got: " + string.Join(", ", diags.Select(d => d.Id)));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // (4) Step has no service params → silent regardless of containers
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task StepWithoutServiceParams_NoDiagnostic()
  {
    var consumer = """
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

    var diags = await AnalyzerTestHarness.RunAnalyzerAsync(
      new FUnitDiagnosticAnalyzer(),
      new[] { Stubs, consumer }
    );
    Assert.That(diags.WithId("FU100").ToList(), Is.Empty,
      "FU100 should not fire for steps without service-typed Create() params.");
  }

  // ─────────────────────────────────────────────────────────────────────────
  // (5) No container, step has service params → FU100
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task NoContainer_StepNeedsService_EmitsFU100()
  {
    var consumer = """
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

    var diags = await AnalyzerTestHarness.RunAnalyzerAsync(
      new FUnitDiagnosticAnalyzer(),
      new[] { Stubs, consumer }
    );
    Assert.That(diags.WithId("FU100").ToList(), Is.Not.Empty,
      "FU100 should fire when no [FUnitStubContainer] exists and the step needs services.");
  }

  // ─────────────────────────────────────────────────────────────────────────
  // (6) Registration outside [FUnitStubContainer] → not counted, FU100 fires
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task RegistrationOutsideStubContainer_DoesNotCount_EmitsFU100()
  {
    var consumer = """
      namespace TestProject
      {
          using Flowthru.Step;
          using Flowthru.Step.Testing;
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
              [FUnitStepTest(typeof(MyStep))]
              public void Works() { }
          }
      }
      """;

    var diags = await AnalyzerTestHarness.RunAnalyzerAsync(
      new FUnitDiagnosticAnalyzer(),
      new[] { Stubs, consumer }
    );
    Assert.That(diags.WithId("FU100").ToList(), Is.Not.Empty,
      "FU100 should fire because the registration is outside any [FUnitStubContainer].");
  }
}
