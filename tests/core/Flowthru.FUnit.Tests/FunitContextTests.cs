using Flowthru.FUnit.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.FUnit.Tests;

[TestFixture]
[Category("FUnit")]
#pragma warning disable FU002 // FU002 is suppressed here, as these are the actual unit tests for this error.
public class FUnitContextTests : FUnitContext
{
  // ===========================================================================
  // Invoke (sync)
  // ===========================================================================

  [Test]
  public void Invoke_Sync_ReturnsTransformedOutput()
  {
    var input = Samples.Of(new NumberRow(1.0), new NumberRow(2.0));

    var result = Invoke(DoubleStep.Create(), input).ToList();

    Assert.That(result, Has.Count.EqualTo(2));
    Assert.That(result[0].Value, Is.EqualTo(2.0));
    Assert.That(result[1].Value, Is.EqualTo(4.0));
  }

  [Test]
  public void Invoke_Sync_WithEmptyInput_ReturnsEmpty()
  {
    var result = Invoke(DoubleStep.Create(), Samples.Of<NumberRow>()).ToList();

    Assert.That(result, Is.Empty);
  }

  // ===========================================================================
  // InvokeAsync
  // ===========================================================================

  [Test]
  public async Task InvokeAsync_ReturnsTransformedOutput()
  {
    var input = Samples.Of(new NumberRow(3.0), new NumberRow(4.0));

    var result = (await InvokeAsync(AsyncDoubleStep.Create(), input)).ToList();

    Assert.That(result[0].Value, Is.EqualTo(6.0));
    Assert.That(result[1].Value, Is.EqualTo(8.0));
  }

  [Test]
  public async Task InvokeAsync_WithCancellationToken_ReturnsTransformedOutput()
  {
    var input = Samples.Of(new NumberRow(5.0));
    using var cts = new CancellationTokenSource();

    var result = (await InvokeAsync(CancellableDoubleStep.Create(), input, cts.Token)).ToList();

    Assert.That(result[0].Value, Is.EqualTo(10.0));
  }

  [Test]
  public void InvokeAsync_WithCancelledToken_ThrowsOperationCancelled()
  {
    var input = Samples.Of(new NumberRow(1.0));
    using var cts = new CancellationTokenSource();
    cts.Cancel();

    var ex = Assert.CatchAsync(() => InvokeAsync(CancellableDoubleStep.Create(), input, cts.Token));
    Assert.That(ex, Is.InstanceOf<OperationCanceledException>());
  }

  // ===========================================================================
  // Services / DI
  // ===========================================================================

  [Test]
  public void Services_AllowsRegistrationBeforeFirstAccess()
  {
    using var ctx = new FUnitContextAccessor();
    ctx.Services.AddSingleton<string>("hello");

    var value = ctx.Get().GetRequiredService<string>();

    Assert.That(value, Is.EqualTo("hello"));
  }
}

// Helper to expose the protected ServiceProvider for the DI test.
file sealed class FUnitContextAccessor : FUnitContext
{
  public IServiceProvider Get() => ServiceProvider;
}
#pragma warning restore FU002 // FUnitContext subclass not guarded by #if FUNIT_ENABLED
