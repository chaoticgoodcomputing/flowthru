using Flowthru.Data.Catalog;
using Flowthru.Extensions.GQL.Tests.Fixtures;
using Flowthru.Flow;
using Flowthru.Hosting;
using Flowthru.Prelude;
using Flowthru.Step;
using Flowthru.Validation.Runtime;
using Flowthru.Validation.Runtime.Gql;
using Microsoft.Extensions.DependencyInjection;
using StrawberryShake;

namespace Flowthru.Extensions.GQL.Tests;

/// <summary>
/// #104 acceptance: a GQL item opted into a concurrency cap via
/// <c>WithGqlConcurrency</c> declares its endpoint as a conflict resource,
/// and <see cref="GqlEndpointProfileContributor"/> resolves it to the
/// declared cap. Throttled calls to one endpoint serialize; without the
/// opt-in (the default) they parallelize. (ADR-0019.)
/// </summary>
[TestFixture]
public sealed class GqlConflictGatingTests
{
  private const string Endpoint = "https://api.example.test/graphql";

  private static readonly IServiceProfileProvider Gated =
    new CompositeServiceProfileProvider(new IServiceProfileContributor[]
    {
      new GqlEndpointProfileContributor(),
    });

  private static readonly IServiceProfileProvider Ungated =
    new CompositeServiceProfileProvider(Array.Empty<IServiceProfileContributor>());

  // ── Acceptance: opt-in cap serializes; default parallelizes ──────────────

  [Test]
  public async Task ThrottledCallsToOneEndpoint_Serialize()
  {
    var maxConcurrent = await RunTwoQueriesAsync(Gated, throttleTo: 1);
    Assert.That(maxConcurrent, Is.EqualTo(1),
      "Two queries throttled to capacity 1 on one endpoint must serialize at Parallelism=4."
    );
  }

  [Test]
  public async Task UnthrottledCalls_RunConcurrently()
  {
    // Default GQL behaviour: no opt-in cap declared, so the endpoint is
    // unbounded and the two queries co-run. Confirms the cap is opt-in and
    // the harness observes overlap.
    var maxConcurrent = await RunTwoQueriesAsync(Gated, throttleTo: null);
    Assert.That(maxConcurrent, Is.EqualTo(2),
      "Without WithGqlConcurrency the endpoint is unbounded — the two queries parallelize."
    );
  }

  [Test]
  public async Task ThrottledButContributorMissing_RunConcurrently()
  {
    // Opt-in declared but UseGql() not called: the dependency resolves to
    // unbounded, so the cap is a no-op (default behaviour preserved).
    var maxConcurrent = await RunTwoQueriesAsync(Ungated, throttleTo: 1);
    Assert.That(maxConcurrent, Is.EqualTo(2),
      "Without the contributor (UseGql) the throttle declaration doesn't gate — confirms the "
      + "contributor enforces it."
    );
  }

  // ── Combinator / contributor declarations ────────────────────────────────

  [Test]
  public void WithGqlConcurrency_DeclaresEndpointDependency()
  {
    var item = MakeQueryItem(_ => Task.FromResult(Ok())).WithGqlConcurrency(Endpoint, 4);

    var dep = item.ServiceDependencies
      .OfType<ServiceDependency.External>()
      .Select(e => e.Cause)
      .OfType<GqlEndpointDependency>()
      .SingleOrDefault();

    Assert.That(dep, Is.Not.Null, "WithGqlConcurrency must declare the endpoint as a conflict resource.");
    Assert.That(dep!.Endpoint, Is.EqualTo(Endpoint));
    Assert.That(dep.MaxConcurrency, Is.EqualTo(4));
    Assert.That(dep.Category, Is.EqualTo("gql"));
  }

  [Test]
  public void GqlItem_WithoutThrottle_DeclaresNoDependencies()
  {
    var item = MakeQueryItem(_ => Task.FromResult(Ok()));
    Assert.That(item.ServiceDependencies, Is.Empty,
      "GQL items are unbounded by default — no conflict declaration unless throttled.");
  }

  [Test]
  public void WithGqlConcurrency_RejectsInvalidCap()
  {
    var item = MakeQueryItem(_ => Task.FromResult(Ok()));
    Assert.That(() => item.WithGqlConcurrency(Endpoint, 0),
      Throws.TypeOf<ArgumentOutOfRangeException>());
    Assert.That(() => item.WithGqlConcurrency("", 4),
      Throws.TypeOf<ArgumentException>());
  }

  [Test]
  public void Contributor_MapsEndpointDependency_AndStaysSilentOtherwise()
  {
    var dep = new ServiceDependency.External(new GqlEndpointDependency(Endpoint, 3));
    var profile = new GqlEndpointProfileContributor().Contribute(dep);

    Assert.That(profile, Is.Not.Null);
    Assert.That(profile!.Capacity, Is.EqualTo(3), "Cap applies to writes (mutations).");
    Assert.That(profile.ReadCapacity, Is.EqualTo(3), "Cap applies to reads (queries) too — a rate limit caps all calls.");

    Assert.That(new GqlEndpointProfileContributor().Contribute(ServiceDependency.Of<IDisposable>()), Is.Null);
  }

  [Test]
  public void UseGql_RegistersProfileContributor()
  {
    var services = new ServiceCollection();
    new FlowthruServiceBuilder(services).UseGql();

    Assert.That(
      services.Any(d => d.ServiceType == typeof(IServiceProfileContributor)
        && d.ImplementationType == typeof(GqlEndpointProfileContributor)),
      Is.True,
      "UseGql() must register the endpoint profile contributor."
    );
  }

  // ── Harness ──────────────────────────────────────────────────────────────

  private static IOperationResult<TestSingleResult> Ok() =>
    StubOperationResult<TestSingleResult>.Success(new TestSingleResult { User = new TestUser { Id = 1, Name = "x" } });

  private static IItem<TestUser> MakeQueryItem(
    Func<CancellationToken, Task<IOperationResult<TestSingleResult>>> queryFunc) =>
    ItemFactory.Singleton.GqlQuery<TestSingleResult, TestUser>("q", queryFunc, r => r.User!);

  private static async Task<int> RunTwoQueriesAsync(IServiceProfileProvider provider, int? throttleTo)
  {
    var (recordEntry, recordExit, max) = MakeConcurrencyMeter();
    Func<CancellationToken, Task<IOperationResult<TestSingleResult>>> recordingQuery = async ct =>
    {
      recordEntry();
      await Task.Delay(60, ct).ConfigureAwait(false);
      recordExit();
      return Ok();
    };

    IItem<TestUser> Throttle(IItem<TestUser> item) =>
      throttleTo is int cap ? item.WithGqlConcurrency(Endpoint, cap) : item;

    var inA = Throttle(MakeQueryItem(recordingQuery));
    var inB = Throttle(MakeQueryItem(recordingQuery));
    var outA = ItemFactory.Singleton.Memory<int>($"gql-out-a-{Guid.NewGuid():N}");
    var outB = ItemFactory.Singleton.Memory<int>($"gql-out-b-{Guid.NewGuid():N}");

    Func<TestUser, FlowIO<int>> transform = _ => FlowIO.Pure(0);

    IStepNode Step(string label, IItem<TestUser> input, IItem<int> output) =>
      new Step<TestUser, int>(
        label, transform, new IItem[] { input }, new IItem[] { output },
        loadInputs: () => input.Load(), saveOutputs: v => output.Save(v));

    var flow = FlowBuilder.CreateFlow("gql-conflict", b =>
    {
      b.Add(Step("gql-step-a", inA, outA));
      b.Add(Step("gql-step-b", inB, outB));
    });

    var result = await new ParallelFlowScheduler(profiles: provider)
      .ExecuteAsync(flow, new ExecutionOptions { Parallelism = 4 });

    Assert.That(result.IsSuccess, Is.True, "both query steps should succeed");
    return max();
  }

  private static (Action Entry, Action Exit, Func<int> Max) MakeConcurrencyMeter()
  {
    var running = 0;
    var max = 0;
    var gate = new object();
    void Entry()
    {
      var now = Interlocked.Increment(ref running);
      lock (gate) max = Math.Max(max, now);
    }
    void Exit() => Interlocked.Decrement(ref running);
    int Max() { lock (gate) return max; }
    return (Entry, Exit, Max);
  }

  private sealed class EmptyCatalog : CatalogAbstract { }
}
