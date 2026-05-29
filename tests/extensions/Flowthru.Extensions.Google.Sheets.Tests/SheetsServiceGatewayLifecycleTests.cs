using Flowthru.Data.Storage.Sheets;
using Flowthru.Prelude;
using Google.Apis.Sheets.v4;

namespace Flowthru.Extensions.Google.Sheets.Tests;

internal static class FlowIoTestExtensions
{
  /// <summary>Run an effect and unwrap its success value (failing the test on error).</summary>
  public static A RunValue<A>(this FlowIO<A> io) =>
    io.Run().GetAwaiter().GetResult().Match(
      onSuccess: v => v,
      onFailure: e => throw new AssertionException($"Effect failed: {e}"));

  /// <summary>Run an effect and return whether it succeeded.</summary>
  public static bool RunSucceeds<A>(this FlowIO<A> io) =>
    io.Run().GetAwaiter().GetResult().IsSuccess;
}

/// <summary>
/// Construction-mode and <see cref="IFlowResource"/> lifecycle tests for the
/// production gateway. These never make a network call — they exercise the
/// two construction modes and the factory-mode acquire/release bracket.
/// </summary>
[TestFixture]
public sealed class SheetsServiceGatewayLifecycleTests
{
  [Test]
  public void InjectedMode_ExposesNoFlowResource()
  {
    using var service = new SheetsService();
    var gateway = new SheetsServiceGateway(service);

    // The container owns the injected client; there is nothing to bracket.
    Assert.That(gateway.FlowResource, Is.Null);
  }

  [Test]
  public void FactoryMode_ExposesAFlowResource()
  {
    var gateway = new SheetsServiceGateway(() => new SheetsService());
    Assert.That(gateway.FlowResource, Is.Not.Null);
  }

  [Test]
  public void FactoryMode_AcquireCreatesClient_ReleaseDisposesIt()
  {
    var created = 0;
    SheetsService? last = null;
    var gateway = new SheetsServiceGateway(() =>
    {
      created++;
      last = new SheetsService();
      return last;
    });

    var resource = gateway.FlowResource!;

    // Acquire: factory invoked exactly once.
    var acquired = resource.AcquireUntyped().RunValue();
    Assert.That(ReferenceEquals(acquired, last), Is.True, "acquire returns the factory-built client");
    Assert.That(created, Is.EqualTo(1));

    // Release: disposes the acquired client and the release effect succeeds.
    Assert.That(resource.ReleaseUntyped(acquired, bodyError: null).RunSucceeds(), Is.True);
  }

  [Test]
  public void FactoryMode_EachFlowResourceAccess_AcquiresAFreshClient()
  {
    var created = 0;
    var gateway = new SheetsServiceGateway(() =>
    {
      created++;
      return new SheetsService();
    });

    var r1 = gateway.FlowResource!;
    var first = r1.AcquireUntyped().RunValue();
    r1.ReleaseUntyped(first, null).RunValue();

    var r2 = gateway.FlowResource!;
    r2.AcquireUntyped().RunValue();

    Assert.That(created, Is.EqualTo(2), "one client per flow run");
  }

  [Test]
  public void FactoryMode_GatewayCall_WithoutAcquire_Fails()
  {
    var gateway = new SheetsServiceGateway(() => new SheetsService());

    // No resource acquired: the gateway has no active client to delegate to.
    var ex = Assert.ThrowsAsync<InvalidOperationException>(
      async () => await gateway.ResolveTable("spreadsheet-id", "AnyTable", CancellationToken.None));
    Assert.That(ex!.Message, Does.Contain("no active SheetsService"));
  }

  [Test]
  public void Constructors_RejectNullArguments()
  {
    Assert.Throws<ArgumentNullException>(() => new SheetsServiceGateway((SheetsService)null!));
    Assert.Throws<ArgumentNullException>(() => new SheetsServiceGateway((Func<SheetsService>)null!));
  }
}
