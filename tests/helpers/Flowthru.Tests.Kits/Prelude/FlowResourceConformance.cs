using Flowthru.Core.Effects;

namespace Flowthru.Tests.Kits.Prelude;

/// <summary>
/// Conformance suite for any <see cref="FlowResource{TScope}"/> publisher.
/// Verifies the bracket contract: <c>Acquire → body → Release</c> in order,
/// release fires on every exit path, body errors propagate, and the
/// type-erased <see cref="IFlowResource"/> view round-trips scope values.
/// </summary>
/// <remarks>
/// <para>
/// Subclass with a backend type and NUnit's matrix construction:
/// </para>
/// <code>
/// [TestFixture(typeof(SqliteEphemeralDatabaseBackend))]
/// [TestFixture(typeof(PostgresEphemeralSchemaBackend))]
/// public class MyResourceConformance&lt;TBackend&gt;
///     : FlowResourceConformance&lt;TBackend, DbScope&gt;
///     where TBackend : IResourceBackend&lt;DbScope&gt;, new() { }
/// </code>
/// </remarks>
public abstract class FlowResourceConformance<TBackend, TScope>
  where TBackend : IResourceBackend<TScope>, new()
{
  protected TBackend Backend { get; private set; } = default!;

  [SetUp]
  public void SetUpBackend()
  {
    Backend = new TBackend();
  }

  [TearDown]
  public async Task CleanupBackend()
  {
    await Backend.Cleanup();
  }

  // ── Acquire ────────────────────────────────────────────────────────────

  [Test]
  public async Task Acquire_ProducesScope()
  {
    var resource = Backend.CreateResource();

    var scope = await resource.Acquire.Run();

    Assert.That(scope, Is.Not.Null, "Acquire should produce a non-null scope.");
  }

  // ── Use: body lifecycle ────────────────────────────────────────────────

  [Test]
  public async Task Use_BodyRuns_ResultIsReturned()
  {
    var resource = Backend.CreateResource();

    var result = await resource.Use(_ => FlowIO.Pure(42)).Run();

    Assert.That(result, Is.EqualTo(42));
  }

  [Test]
  public async Task Use_BodySucceeds_ReleaseSeesNullException()
  {
    Exception? capturedRelease = null;
    var probe = WrapResourceWithReleaseProbe(
      Backend.CreateResource(),
      ex => capturedRelease = ex
    );

    await probe.Use(_ => FlowIO.Pure(FlowUnit.Default)).Run();

    Assert.That(
      capturedRelease,
      Is.Null,
      "Release should observe a null body exception when the body succeeds."
    );
  }

  [Test]
  public void Use_BodyThrows_ReleaseSeesException_AndExceptionPropagates()
  {
    Exception? capturedRelease = null;
    var probe = WrapResourceWithReleaseProbe(
      Backend.CreateResource(),
      ex => capturedRelease = ex
    );
    var bodyEx = new InvalidOperationException("body-failed");

    var thrown = Assert.ThrowsAsync<InvalidOperationException>(async () =>
      await probe.Use<FlowUnit>(_ => FlowIO.Fail<FlowUnit>(bodyEx)).Run()
    );

    Assert.That(thrown, Is.SameAs(bodyEx), "Body exception should propagate unchanged.");
    Assert.That(
      capturedRelease,
      Is.SameAs(bodyEx),
      "Release should observe the body's exception."
    );
  }

  [Test]
  public void Use_BodyThrows_AndReleaseThrows_BodyExceptionWins()
  {
    var probe = WrapResourceWithReleaseFailure(
      Backend.CreateResource(),
      new Exception("release-failed")
    );

    var thrown = Assert.ThrowsAsync<InvalidOperationException>(async () =>
      await probe
        .Use<FlowUnit>(_ => FlowIO.Fail<FlowUnit>(new InvalidOperationException("body-failed")))
        .Run()
    );

    Assert.That(
      thrown!.Message,
      Is.EqualTo("body-failed"),
      "Single-resource Use suppresses release errors; the body exception wins."
    );
  }

  // ── Type-erased view ───────────────────────────────────────────────────

  [Test]
  public async Task IFlowResource_AcquireReleaseRoundTripsScope()
  {
    var resource = Backend.CreateResource();
    var iface = (IFlowResource)resource;

    var boxed = await iface.AcquireUntyped().Run();
    Assert.That(boxed, Is.Not.Null);

    // Release with the boxed scope should not throw.
    await iface.ReleaseUntyped(boxed, bodyException: null).Run();
  }

  // ── Helpers ────────────────────────────────────────────────────────────

  /// <summary>
  /// Wraps a resource so the release callback is observable. The wrapped
  /// resource's acquire and release effects still hit the real backend; the
  /// probe runs alongside.
  /// </summary>
  private static FlowResource<TScope> WrapResourceWithReleaseProbe(
    FlowResource<TScope> inner,
    Action<Exception?> probe
  )
  {
    return FlowResource.Make<TScope>(
      acquire: inner.Acquire,
      release: (scope, ex) =>
      {
        probe(ex);
        return inner.Release(scope, ex);
      }
    );
  }

  /// <summary>
  /// Wraps a resource so its release effect throws after the inner release
  /// completes. Used to verify the body-error-wins semantic of
  /// <see cref="FlowResource{TScope}.Use{TResult}"/>.
  /// </summary>
  private static FlowResource<TScope> WrapResourceWithReleaseFailure(
    FlowResource<TScope> inner,
    Exception releaseError
  )
  {
    return FlowResource.Make<TScope>(
      acquire: inner.Acquire,
      release: (scope, ex) =>
        FlowIO.LiftAsync<FlowUnit>(async ct =>
        {
          // Run the inner release first (best-effort cleanup), then throw
          // to exercise the release-error path.
          try
          {
            await inner.Release(scope, ex).Run(ct);
          }
          catch
          {
            // Swallow inner-release errors; the test cares about the
            // injected release failure.
          }
          throw releaseError;
        })
    );
  }
}
