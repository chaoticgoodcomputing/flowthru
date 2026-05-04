using Flowthru.Core.Effects;

namespace Flowthru.Core.Tests.Execution;

/// <summary>
/// Tests for the <see cref="FlowResource{TScope}"/> bracket pattern. Verifies
/// that release runs on every exit path (success, exception), that release
/// receives the body's primary exception when one occurred, and that the
/// type-erased <see cref="IFlowResource"/> view round-trips scope values
/// correctly.
/// </summary>
[TestFixture]
[Category("Execution")]
public class FlowResourceTests
{
  [Test]
  public async Task Use_AcquireBodyRelease_RunsInOrder()
  {
    var trace = new List<string>();

    var resource = FlowResource.Make<string>(
      acquire: FlowIO.Lift(() =>
      {
        trace.Add("acquire");
        return "scope-value";
      }),
      release: (scope, _) =>
        FlowIO.Lift(() =>
        {
          trace.Add($"release({scope})");
          return FlowUnit.Default;
        })
    );

    var result = await resource
      .Use(scope =>
        FlowIO.Lift(() =>
        {
          trace.Add($"body({scope})");
          return scope.Length;
        })
      )
      .Run();

    Assert.That(result, Is.EqualTo("scope-value".Length));
    Assert.That(trace, Is.EqualTo(new[] { "acquire", "body(scope-value)", "release(scope-value)" }));
  }

  [Test]
  public void Use_BodyThrows_ReleaseStillRuns()
  {
    var trace = new List<string>();
    var resource = FlowResource.Make<int>(
      acquire: FlowIO.Lift(() =>
      {
        trace.Add("acquire");
        return 42;
      }),
      release: (scope, ex) =>
        FlowIO.Lift(() =>
        {
          trace.Add($"release(scope={scope}, ex={ex?.Message ?? "null"})");
          return FlowUnit.Default;
        })
    );

    var thrown = Assert.ThrowsAsync<InvalidOperationException>(async () =>
      await resource
        .Use<int>(_ => FlowIO.Fail<int>(new InvalidOperationException("body-failed")))
        .Run()
    );

    Assert.That(thrown!.Message, Is.EqualTo("body-failed"));
    Assert.That(trace, Is.EqualTo(new[] { "acquire", "release(scope=42, ex=body-failed)" }));
  }

  [Test]
  public async Task Use_BodySucceeds_ReleaseSeesNullException()
  {
    Exception? capturedException = null;

    var resource = FlowResource.Make<FlowUnit>(
      acquire: FlowIO.Pure(FlowUnit.Default),
      release: (_, ex) =>
      {
        capturedException = ex;
        return FlowIO.Pure(FlowUnit.Default);
      }
    );

    await resource.Use(_ => FlowIO.Pure(123)).Run();

    Assert.That(capturedException, Is.Null);
  }

  [Test]
  public void Use_ReleaseError_DoesNotMaskBodyError()
  {
    // When body throws AND release also throws, the body error wins —
    // release errors are suppressed by the Use overload (the framework's
    // resource loop captures release errors separately into TeardownErrors).
    var resource = FlowResource.Make<int>(
      acquire: FlowIO.Pure(1),
      release: (_, _) => FlowIO.Fail<FlowUnit>(new Exception("release-failed"))
    );

    var thrown = Assert.ThrowsAsync<InvalidOperationException>(async () =>
      await resource
        .Use<int>(_ => FlowIO.Fail<int>(new InvalidOperationException("body-failed")))
        .Run()
    );

    Assert.That(thrown!.Message, Is.EqualTo("body-failed"));
  }

  [Test]
  public async Task Empty_AcquiresAndReleasesWithoutEffect()
  {
    // The default catalog Resource override returns null in the framework,
    // but FlowResource.Empty exists for users who want a no-op explicit
    // resource (e.g., as a placeholder).
    var ranBody = false;
    var result = await FlowResource
      .Empty.Use(_ =>
        FlowIO.Lift(() =>
        {
          ranBody = true;
          return "ok";
        })
      )
      .Run();

    Assert.That(ranBody, Is.True);
    Assert.That(result, Is.EqualTo("ok"));
  }

  [Test]
  public async Task IFlowResource_AcquireUntyped_BoxesScope()
  {
    var resource = FlowResource.Make<int>(
      acquire: FlowIO.Pure(42),
      release: (_, _) => FlowIO.Pure(FlowUnit.Default)
    );

    var boxed = await ((IFlowResource)resource).AcquireUntyped().Run();

    Assert.That(boxed, Is.EqualTo(42));
  }

  [Test]
  public async Task IFlowResource_ReleaseUntyped_RoundTripsScope()
  {
    int? releasedScope = null;
    var resource = FlowResource.Make<int>(
      acquire: FlowIO.Pure(99),
      release: (scope, _) =>
      {
        releasedScope = scope;
        return FlowIO.Pure(FlowUnit.Default);
      }
    );

    var iface = (IFlowResource)resource;
    var boxed = await iface.AcquireUntyped().Run();
    await iface.ReleaseUntyped(boxed, bodyException: null).Run();

    Assert.That(releasedScope, Is.EqualTo(99));
  }

  [Test]
  public async Task Pure_HoldsValue_AndReleasesNoOp()
  {
    var resource = FlowResource.Pure("hello");

    var captured = await resource.Use(s => FlowIO.Pure(s.ToUpperInvariant())).Run();

    Assert.That(captured, Is.EqualTo("HELLO"));
  }
}
