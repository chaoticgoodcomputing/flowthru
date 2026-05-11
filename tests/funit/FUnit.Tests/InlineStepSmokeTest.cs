using Flowthru.Step;
using Flowthru.Step.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.FUnit.Tests;

/// <summary>
/// Phase 5 done-criterion: a smoke test step file with inline
/// <see cref="FUnitStubContainerAttribute"/> + <see cref="FUnitStepTestAttribute"/>
/// fixtures passes via <c>dotnet test</c> — exercising
/// <see cref="FUnitContext"/> sample helpers, DI auto-registration
/// from a stub container, and the source-generated NUnit runner.
/// </summary>
/// <remarks>
/// The runner class is emitted by <c>FUnit.SourceGenerators</c> as
/// <c>InlineStepSmokeTest_FooStep_Tests_NUnitRunner</c> next to this
/// fixture; the NUnit framework picks it up automatically.
/// </remarks>
[FlowthruStep]
public static class FooStep
{
  public static Func<int, int> Create() => x => x + 1;

  public static Func<int, int> Create(IClock clock) => x => x + clock.Now;

#if FUNIT_ENABLED
  public class Tests : FUnitContext
  {
    [FUnitStepTest(typeof(FooStep))]
    public void NoServiceVariant_AddsOne()
    {
      var transform = Create();
      Assert.That(Invoke(transform, 41), Is.EqualTo(42));
    }

    [FUnitStepTest(typeof(FooStep))]
    public void ServiceVariant_PullsClockFromStubContainer()
    {
      var clock = GetRequiredService<IClock>();
      var transform = Create(clock);
      Assert.That(Invoke(transform, 0), Is.EqualTo(7),
        "Service variant should resolve the FixedClock(Now=7) from the stub container.");
    }

    [FUnitStepTest(typeof(FooStep))]
    public void Samples_GenerateDelegatesIndexInto()
    {
      var rows = Samples.Generate(5, i => i * 10).ToList();
      Assert.That(rows, Is.EqualTo(new[] { 0, 10, 20, 30, 40 }));
    }
  }
#endif
}

public interface IClock
{
  int Now { get; }
}

public sealed class FixedClock : IClock
{
  public int Now => 7;
}

[FUnitStubContainer]
public static class TestStubs
{
  public static void Configure(IServiceCollection services)
  {
    services.AddSingleton<IClock, FixedClock>();
  }
}
