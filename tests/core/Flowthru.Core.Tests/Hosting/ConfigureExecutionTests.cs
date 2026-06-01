using Flowthru.Flow;
using Flowthru.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Flowthru.Core.Tests.Hosting;

/// <summary>
/// Tests for the <c>FlowthruServiceBuilder.ConfigureExecution(...)</c>
/// host surface — the builder hook that seeds host-level
/// <see cref="ExecutionDefaults"/> (e.g. <see cref="ExecutionDefaults.Parallelism"/>)
/// through the standard Options pipeline. The scheduler's behaviour at a
/// given Parallelism is covered separately by ParallelFlowSchedulerTests;
/// these cover the wiring (defaulting, composition, fail-fast validation).
/// </summary>
[TestFixture]
[Category("Hosting")]
public class ConfigureExecutionTests
{
  private static ExecutionDefaults ResolveDefaults(IServiceCollection services)
  {
    using var sp = services.BuildServiceProvider();
    return sp.GetRequiredService<IOptions<ExecutionDefaults>>().Value;
  }

  [Test]
  public void Default_WhenNotConfigured_IsSequential()
  {
    var services = new ServiceCollection();
    services.AddFlowthru(_ => { });

    Assert.That(ResolveDefaults(services).Parallelism, Is.EqualTo(1),
      "Absent ConfigureExecution, the host default must stay sequential (Parallelism = 1).");
  }

  [Test]
  public void ConfigureExecution_SetsParallelismDefault()
  {
    var services = new ServiceCollection();
    services.AddFlowthru(b => b.ConfigureExecution(o => o.Parallelism = 4));

    Assert.That(ResolveDefaults(services).Parallelism, Is.EqualTo(4),
      "ConfigureExecution must surface through IOptions<ExecutionDefaults>.");
  }

  [Test]
  public void ConfigureExecution_MultipleCalls_ComposeInOrder()
  {
    // Each call registers an ordered IConfigureOptions<ExecutionDefaults>;
    // the last one to touch a property wins.
    var services = new ServiceCollection();
    services.AddFlowthru(b =>
    {
      b.ConfigureExecution(o => o.Parallelism = 2);
      b.ConfigureExecution(o => o.Parallelism = 8);
    });

    Assert.That(ResolveDefaults(services).Parallelism, Is.EqualTo(8),
      "The most-recent ConfigureExecution assignment must win.");
  }

  [Test]
  public void ConfigureExecution_InvalidParallelism_FailsFast()
  {
    // Fail-fast: a nonsensical value surfaces when the options are
    // resolved (pre-flight), not silently clamped at runtime.
    var services = new ServiceCollection();
    services.AddFlowthru(b => b.ConfigureExecution(o => o.Parallelism = 0));

    using var sp = services.BuildServiceProvider();
    Assert.Throws<OptionsValidationException>(
      () => _ = sp.GetRequiredService<IOptions<ExecutionDefaults>>().Value,
      "Parallelism < 1 must fail validation rather than be silently corrected.");
  }

  [Test]
  public void ConfigureExecution_NullConfigure_Throws()
  {
    var services = new ServiceCollection();
    Assert.Throws<ArgumentNullException>(
      () => services.AddFlowthru(b => b.ConfigureExecution(null!)));
  }
}
