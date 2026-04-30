using Flowthru.FUnit.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.FUnit.Tests;

// ─────────────────────────────────────────────────────────────────────────
// Top-level fixtures (the runtime auto-registration walks the test
// assembly's types — keep stub containers visible at namespace scope).
// ─────────────────────────────────────────────────────────────────────────

public interface IFakeStubbedService
{
  string Greeting { get; }
}

public sealed class FakeStubbedService : IFakeStubbedService
{
  public string Greeting => "hello from container";
}

[FUnitStubContainer]
public static class TestStubsContainer
{
  public static void Configure(IServiceCollection services)
  {
    services.AddSingleton<IFakeStubbedService, FakeStubbedService>();
  }
}

/// <summary>
/// Verifies that <see cref="FunitContext"/> auto-registers stub containers from the
/// test assembly via reflection, and that the new <c>GetRequiredService&lt;T&gt;</c>
/// helper resolves them. Split into separate fixtures to avoid NUnit's per-fixture
/// instance reuse leaking <see cref="FunitContext.Services"/> mutations across tests.
/// </summary>
[TestFixture]
[Category("FUnit")]
[Category("StubContainer")]
#pragma warning disable FU002 // FU002 fires on FunitContext subclasses outside #if FUNIT_ENABLED
public class StubContainerAutoRegistrationTests : FunitContext
{
  [Test]
  public void StubContainer_AutoRegistration_ServiceResolvable()
  {
    var svc = GetRequiredService<IFakeStubbedService>();
    Assert.That(svc.Greeting, Is.EqualTo("hello from container"));
  }
}

[TestFixture]
[Category("FUnit")]
[Category("StubContainer")]
public class StubContainerOverrideTests : FunitContext
{
  [Test]
  public void StubContainer_AutoRegistration_RegistersBeforeUserCode()
  {
    // Per-test override registered AFTER construction wins (matches ASP.NET
    // WebApplicationFactory.ConfigureTestServices semantics).
    Services.AddSingleton<IFakeStubbedService>(new OverrideService());

    var svc = GetRequiredService<IFakeStubbedService>();
    Assert.That(svc.Greeting, Is.EqualTo("override"));
  }
}
#pragma warning restore FU002

internal sealed class OverrideService : IFakeStubbedService
{
  public string Greeting => "override";
}
