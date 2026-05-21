using Flowthru.Prelude;
using Flowthru.Validation.Runtime;

namespace Flowthru.Tests.Kits.Prelude;

/// <summary>
/// Laws every <see cref="IResourceBackend{TScope}"/> implementer must
/// satisfy. Subclasses parameterise the backend type via
/// <c>[TestFixture(typeof(TBackend))]</c>; the kit constructs a fresh
/// backend per fixture, runs the
/// <see cref="IResourceBackend{TScope}.RequiredCapabilities"/> gate, and
/// inherits acquire/release + isolation tests for free.
/// </summary>
/// <typeparam name="TBackend">
/// Concrete backend under test. Must be default-constructible — the kit
/// instantiates one per fixture and amortises any expensive shared
/// setup across all tests.
/// </typeparam>
/// <typeparam name="TScope">
/// Scope type the backend produces. Pinned by the subclass (e.g.
/// <c>DbScope</c>, <c>Stream</c>).
/// </typeparam>
[TestFixture]
public abstract class FlowResourceLaws<TBackend, TScope>
  where TBackend : IResourceBackend<TScope>, new()
{
  /// <summary>
  /// The backend under test. Fresh instance per fixture; expensive
  /// setup is amortised across tests via lazy init inside the backend.
  /// </summary>
  protected TBackend Backend { get; private set; } = default!;

  /// <summary>
  /// Capability gate + expensive shared setup. Runs once per fixture
  /// before any test. Order is load-bearing:
  /// <list type="number">
  ///   <item>Construct backend (cheap, configuration-only).</item>
  ///   <item>Run <see cref="IResourceBackend{TScope}.RequiredCapabilities"/>
  ///     via <c>Assume.That</c> — any missing capability yields an
  ///     Inconclusive verdict for the whole fixture, so backends
  ///     whose expensive setup needs the missing dependency
  ///     (Docker, etc.) never attempt it.</item>
  ///   <item>Invoke <see cref="IResourceBackend{TScope}.InitializeAsync"/>
  ///     for any backend-side async init (container start, etc.).</item>
  /// </list>
  /// </summary>
  [OneTimeSetUp]
  public async Task GateAndInitialiseBackend()
  {
    Backend = new TBackend();
    foreach (var capability in Backend.RequiredCapabilities)
    {
      Assume.That(
        capability.IsAvailable(),
        $"[{capability.Name}] {capability.MissingMessage}"
      );
    }
    await Backend.InitializeAsync();
  }

  /// <summary>
  /// Tears down whatever the backend created across the fixture
  /// (containers, temp files, server-side schemas).
  /// </summary>
  [OneTimeTearDown]
  public async Task ReleaseBackendResources()
  {
    if (Backend is not null)
    {
      await Backend.Cleanup();
    }
  }

  // ── Acquire / release laws ────────────────────────────────────────────

  /// <summary>
  /// <c>Use</c> returns a successful result for a well-formed body. The
  /// fundamental "acquire then release" round trip.
  /// </summary>
  [Test]
  public async Task UseProducesSuccessLaw()
  {
    var resource = Backend.CreateResource();

    var result = await resource.Use(scope =>
      FlowIO<TScope>.Pure(scope)
    ).Run();

    Assert.That(result, Is.InstanceOf<EffResult<TScope>.Success>(),
      $"Use over a well-formed body should produce Success. Got: {DescribeResult(result)}");
  }

  /// <summary>
  /// External state exists during the body and is gone after release.
  /// Pins the bracket contract: acquire creates, release drops.
  /// </summary>
  [Test]
  public async Task AcquireCreatesAndReleaseDropsExternalStateLaw()
  {
    var resource = Backend.CreateResource();

    var existsInsideBody = false;
    var result = await resource.Use(scope =>
      FlowIO.LiftAsync(async _ =>
      {
        existsInsideBody = await Backend.ResourceExists();
        return scope;
      }, source: nameof(AcquireCreatesAndReleaseDropsExternalStateLaw))
    ).Run();

    Assert.That(result, Is.InstanceOf<EffResult<TScope>.Success>(),
      $"Body should succeed. Got: {DescribeResult(result)}");
    Assert.That(existsInsideBody, Is.True,
      "External state should exist during the body — acquire must create it.");
    Assert.That(await Backend.ResourceExists(), Is.False,
      "External state should be gone after Use returns — release must drop it.");
  }

  /// <summary>
  /// A failing body propagates the body error and release still runs.
  /// The "release on every exit path" half of the bracket guarantee.
  /// </summary>
  [Test]
  public async Task BodyFailurePropagatesAndStillReleasesLaw()
  {
    var resource = Backend.CreateResource();
    var sentinel = new InvalidOperationException("intentional body failure");

    var result = await resource.Use<TScope>(_ =>
      FlowIO.Fail<TScope>(new RuntimeError.External(
        nameof(BodyFailurePropagatesAndStillReleasesLaw),
        sentinel
      ))
    ).Run();

    Assert.That(result, Is.InstanceOf<EffResult<TScope>.Failure>(),
      "Use should surface the body error.");
    Assert.That(await Backend.ResourceExists(), Is.False,
      "Release must run on failure paths — external state should be gone.");
  }

  // ── Diagnostic helpers ────────────────────────────────────────────────

  private static string DescribeResult(EffResult<TScope> result) => result switch
  {
    EffResult<TScope>.Success s => $"Success({s.Value})",
    EffResult<TScope>.Failure f => $"Failure({f.Error.GetType().Name}: {f.Error})",
    _ => result.ToString() ?? "<null>",
  };

  // ── Re-entrancy / isolation law ───────────────────────────────────────

  /// <summary>
  /// Concurrent <see cref="IResourceBackend{TScope}.CreateResource"/>
  /// calls produce resources whose external state is disjoint, as
  /// reported by
  /// <see cref="IResourceBackend{TScope}.ExternalStateIdentifier"/>.
  /// Catches the most common backend bug: shared mutable state that
  /// works under sequential tests but races under parallel ones, or
  /// collides under any non-trivial test load.
  /// </summary>
  [Test]
  public async Task ConcurrentCreateResourceProducesDisjointStateLaw()
  {
    const int N = 8;

    // Construct N resources concurrently — the path that exercises any
    // mutable state on the backend instance.
    var resources = await Task.WhenAll(
      Enumerable.Range(0, N).Select(_ => Task.Run(() => Backend.CreateResource()))
    );

    // Acquire all of them so each resource's scope is materialised.
    var acquireResults = await Task.WhenAll(
      resources.Select(r => r.Acquire.Run())
    );

    var successes = acquireResults
      .OfType<EffResult<TScope>.Success>()
      .ToArray();

    try
    {
      Assert.That(successes, Has.Length.EqualTo(N),
        "All concurrent acquires should succeed. Failures suggest a non-thread-safe "
          + "backend or a shared external resource that can't be acquired concurrently.\n"
          + "First failure: " + acquireResults
            .Select(DescribeResult)
            .FirstOrDefault(s => s.StartsWith("Failure"), "(no acquire failures detected)"));

      var identifiers = successes.Select(s => Backend.ExternalStateIdentifier(s.Value)).ToArray();
      Assert.That(identifiers.Distinct().Count(), Is.EqualTo(N),
        "Concurrent CreateResource() calls produced overlapping external state — "
          + "ExternalStateIdentifier collided. Backend likely holds shared mutable state; "
          + "make CreateResource() stateless or thread-safe.\n"
          + $"Identifiers seen: [{string.Join(", ", identifiers)}]");
    }
    finally
    {
      // Release everything we acquired, regardless of assertion outcome,
      // so the fixture teardown isn't left with N orphaned resources.
      foreach (var (resource, result) in resources.Zip(acquireResults))
      {
        if (result is EffResult<TScope>.Success success)
        {
          await resource.Release(success.Value, null).Run();
        }
      }
    }
  }
}
