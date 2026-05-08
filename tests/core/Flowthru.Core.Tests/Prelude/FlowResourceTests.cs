using Flowthru.Prelude;
using Flowthru.Validation.Runtime;

namespace Flowthru.Core.Tests.Prelude;

/// <summary>
/// Tests for the <see cref="FlowResource{TScope}"/> bracket type —
/// reactivates the legacy <c>FlowResourceTests</c> contract suite.
/// </summary>
[TestFixture]
public class FlowResourceTests
{
  [Test]
  public async Task Use_AcquireBodyRelease_RunsInOrder()
  {
    var sequence = new List<string>();
    var resource = FlowResource.Make(
      acquire: FlowIO.Lift(() => { sequence.Add("acquire"); return 42; }),
      release: (scope, _) => FlowIO.Lift(() =>
      {
        sequence.Add($"release({scope})");
        return FlowUnit.Default;
      })
    );

    var result = await resource.Use(scope =>
      FlowIO.Lift(() => { sequence.Add($"body({scope})"); return scope * 2; })
    ).Run();

    Assert.That(result, Is.InstanceOf<EffResult<int>.Success>());
    Assert.That(((EffResult<int>.Success)result).Value, Is.EqualTo(84));
    Assert.That(sequence, Is.EqualTo(new[] { "acquire", "body(42)", "release(42)" }));
  }

  [Test]
  public async Task Use_BodySucceeds_ReleaseSeesNullError()
  {
    RuntimeError? capturedError = "sentinel" is var _ ? new RuntimeError.Cancelled("sentinel") : null;
    var resource = FlowResource.Make(
      acquire: FlowIO.Pure(1),
      release: (_, error) => FlowIO.Lift(() => { capturedError = error; return FlowUnit.Default; })
    );

    await resource.Use(scope => FlowIO.Pure(scope + 1)).Run();
    Assert.That(capturedError, Is.Null,
      "Body succeeded → release receives null bodyError per the contract."
    );
  }

  [Test]
  public async Task Use_BodyFails_ReleaseSeesBodyError()
  {
    RuntimeError? captured = null;
    var resource = FlowResource.Make(
      acquire: FlowIO.Pure(1),
      release: (_, error) => FlowIO.Lift(() => { captured = error; return FlowUnit.Default; })
    );

    var bodyError = new RuntimeError.External("test", new Exception("boom"));
    var result = await resource.Use<int>(_ => FlowIO.Fail<int>(bodyError)).Run();

    Assert.That(result, Is.InstanceOf<EffResult<int>.Failure>());
    Assert.That(((EffResult<int>.Failure)result).Error, Is.EqualTo(bodyError));
    Assert.That(captured, Is.EqualTo(bodyError));
  }

  [Test]
  public async Task Use_AcquireFails_BodyDoesNotRun()
  {
    var bodyRan = false;
    var acquireError = new RuntimeError.External("acquire", new Exception("acquire failed"));
    var resource = FlowResource.Make<int>(
      acquire: FlowIO.Fail<int>(acquireError),
      release: (_, _) => FlowIO.Pure(FlowUnit.Default)
    );

    var result = await resource.Use(_ => FlowIO.Lift(() => { bodyRan = true; return 1; })).Run();

    Assert.That(bodyRan, Is.False);
    Assert.That(result, Is.InstanceOf<EffResult<int>.Failure>());
    Assert.That(((EffResult<int>.Failure)result).Error, Is.EqualTo(acquireError));
  }

  [Test]
  public async Task Empty_AcquiresAndReleasesWithoutEffect()
  {
    var result = await FlowResource.Empty.Use<int>(_ => FlowIO.Pure(123)).Run();
    Assert.That(result, Is.InstanceOf<EffResult<int>.Success>());
    Assert.That(((EffResult<int>.Success)result).Value, Is.EqualTo(123));
  }

  [Test]
  public async Task Pure_HoldsValue_AndReleasesNoOp()
  {
    var resource = FlowResource.Pure(99);
    var result = await resource.Use(scope => FlowIO.Pure(scope - 1)).Run();
    Assert.That(((EffResult<int>.Success)result).Value, Is.EqualTo(98));
  }

  [Test]
  public async Task IFlowResource_AcquireUntyped_BoxesScope()
  {
    var resource = FlowResource.Pure(42);
    IFlowResource erased = resource;

    var result = await erased.AcquireUntyped().Run();
    Assert.That(((EffResult<object?>.Success)result).Value, Is.EqualTo(42));
  }

  [Test]
  public async Task IFlowResource_ReleaseUntyped_RoundTripsScope()
  {
    var releasedScope = -1;
    var resource = FlowResource.Make(
      acquire: FlowIO.Pure(7),
      release: (scope, _) => FlowIO.Lift(() => { releasedScope = scope; return FlowUnit.Default; })
    );
    IFlowResource erased = resource;

    await erased.ReleaseUntyped(7, bodyError: null).Run();
    Assert.That(releasedScope, Is.EqualTo(7));
  }
}
