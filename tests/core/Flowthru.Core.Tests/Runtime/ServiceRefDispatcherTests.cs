using Flowthru.Prelude;
using Flowthru.Validation.PreFlight;
using Flowthru.Validation.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.Core.Tests.Runtime;

/// <summary>
/// Tests for the <see cref="IServiceRefDispatcher"/> extension-point contract:
/// extension-defined service references (the
/// <see cref="ServiceRef.External"/> variant) reach an implementation via the
/// dispatcher's <see cref="IServiceRefDispatcher.Category"/> matching the
/// wrapped <see cref="IExtensionServiceRef.Category"/>, and inspection
/// returns an aggregating <see cref="Validated{TError, TValue}"/> over
/// <see cref="PreFlightError"/>.
/// </summary>
/// <remarks>
/// <para>
/// Ports the legacy <c>02_Validation/PreFlightInspection/ServiceRefDispatcherTests</c>
/// (gap #2 from the test-coverage gap analysis). The active branch ships the
/// dispatcher contract on Core but has not yet wired the dispatch loop into
/// <see cref="PreFlightPipeline"/>; these tests pin the dispatcher's
/// observable contract — category routing, success/failure shape,
/// exception-to-Validated wrapping, DI-registration plurality — so a future
/// pipeline integration has a regression net for the per-dispatcher behaviour
/// it must preserve.
/// </para>
/// <para>
/// The legacy fixture's <c>CSharpRef-with-PythonDispatcher</c> case (proving
/// the C# inspector path doesn't fall through to the dispatcher) is covered
/// by the sibling <see cref="ServiceInspectionTests"/> in this directory:
/// adding a class-based <see cref="IFlowServiceInspector{TService}"/> runs
/// that probe end-to-end through <see cref="IFlowthruService.RunAsync"/>;
/// pinning a no-fall-through here would require an in-progress integration
/// the dispatcher pipeline doesn't yet have.
/// </para>
/// </remarks>
[TestFixture]
public class ServiceRefDispatcherTests
{
  // ── Test-only extension types ───────────────────────────────────────

  /// <summary>An <see cref="IExtensionServiceRef"/> for a fictional category.</summary>
  private sealed record FakeExtensionServiceRef(string DagId, string DisplayName, string Category)
    : IExtensionServiceRef;

  private enum DispatcherBehavior { Pass, Fail, Throw }

  /// <summary>
  /// Test-only dispatcher: records every <see cref="Inspect"/> call and
  /// returns a configurable outcome. Matches a single category (set in
  /// the constructor); refs of other categories should not reach it.
  /// </summary>
  private sealed class RecordingDispatcher : IServiceRefDispatcher
  {
    private readonly DispatcherBehavior _behavior;
    private readonly string? _failureDetail;

    public RecordingDispatcher(
      string category,
      DispatcherBehavior behavior = DispatcherBehavior.Pass,
      string? failureDetail = null
    )
    {
      Category = category;
      _behavior = behavior;
      _failureDetail = failureDetail;
    }

    public string Category { get; }
    public int InvokeCount { get; private set; }
    public IExtensionServiceRef? LastRef { get; private set; }

    public FlowIO<Validated<PreFlightError, FlowUnit>> Inspect(IExtensionServiceRef serviceRef)
    {
      InvokeCount++;
      LastRef = serviceRef;

      return _behavior switch
      {
        DispatcherBehavior.Pass =>
          FlowIO.Pure(Validated<PreFlightError, FlowUnit>.Pure(FlowUnit.Default)),

        DispatcherBehavior.Fail =>
          FlowIO.Pure(Validated<PreFlightError, FlowUnit>.Fail(
            new PreFlightError.InspectionFailed(
              ItemId: serviceRef.DagId,
              Detail: _failureDetail ?? "simulated dispatcher failure"
            )
          )),

        DispatcherBehavior.Throw =>
          FlowIO.LiftAsync<Validated<PreFlightError, FlowUnit>>(
            _ => throw new InvalidOperationException("simulated dispatcher failure")
          ),

        _ => throw new InvalidOperationException("Unreachable dispatcher behavior."),
      };
    }
  }

  // ── (1) Category routing ────────────────────────────────────────────

  [Test]
  public void Category_MatchesExtensionRefCategory_DispatcherSelected()
  {
    // An extension consumer (the future pre-flight integration site) routes
    // an IExtensionServiceRef to its dispatcher by matching Category. This
    // test pins the routing-by-string contract — without it, a custom
    // dispatcher cannot be found from a heterogeneous registration.
    var ext = new FakeExtensionServiceRef(
      DagId: "ext.python.X.Y", DisplayName: "Y", Category: "python");
    var python = new RecordingDispatcher(category: "python");
    var sql = new RecordingDispatcher(category: "sql");
    var dispatchers = new IServiceRefDispatcher[] { sql, python };

    var matched = dispatchers.FirstOrDefault(d =>
      string.Equals(d.Category, ext.Category, StringComparison.Ordinal));

    Assert.Multiple(() =>
    {
      Assert.That(matched, Is.SameAs(python),
        "Category-based routing should select the python dispatcher.");
      Assert.That(matched!.Category, Is.EqualTo("python"));
    });
  }

  // ── (2) Inspect returns success → Valid ─────────────────────────────

  [Test]
  public async Task Inspect_OnSuccess_ReturnsValid()
  {
    var dispatcher = new RecordingDispatcher(category: "ext");
    var ext = new FakeExtensionServiceRef("ext.svc.X", "X", "ext");

    var io = dispatcher.Inspect(ext);
    var effResult = await io.Run(CancellationToken.None);
    var validated = ((EffResult<Validated<PreFlightError, FlowUnit>>.Success)effResult).Value;

    Assert.Multiple(() =>
    {
      Assert.That(dispatcher.InvokeCount, Is.EqualTo(1));
      Assert.That(dispatcher.LastRef, Is.SameAs(ext));
      Assert.That(validated, Is.InstanceOf<Validated<PreFlightError, FlowUnit>.Valid>(),
        "Pass-through dispatcher should return a Valid result.");
    });
  }

  // ── (3) Inspect returns failure → Invalid with the inner error ──────

  [Test]
  public async Task Inspect_OnFailure_ReturnsInvalidWithInspectionFailedDetail()
  {
    var dispatcher = new RecordingDispatcher(
      category: "ext",
      behavior: DispatcherBehavior.Fail,
      failureDetail: "service unreachable"
    );
    var ext = new FakeExtensionServiceRef("ext.svc.Z", "Z", "ext");

    var effResult = await dispatcher.Inspect(ext).Run(CancellationToken.None);
    var validated = ((EffResult<Validated<PreFlightError, FlowUnit>>.Success)effResult).Value;

    Assert.That(validated, Is.InstanceOf<Validated<PreFlightError, FlowUnit>.Invalid>());
    var invalid = (Validated<PreFlightError, FlowUnit>.Invalid)validated;
    Assert.That(invalid.Errors, Has.Count.EqualTo(1));
    var inspectionFailed = (PreFlightError.InspectionFailed)invalid.Errors[0];
    Assert.Multiple(() =>
    {
      Assert.That(inspectionFailed.ItemId, Is.EqualTo("ext.svc.Z"));
      Assert.That(inspectionFailed.Detail, Is.EqualTo("service unreachable"));
    });
  }

  // ── (4) Dispatcher throws → FlowIO surfaces the throw as a failure ──

  [Test]
  public async Task Inspect_OnThrow_FlowIOFailureSurfacesTheException()
  {
    // A misbehaving dispatcher (a real bug in an extension) must not take
    // down the whole flow — the throw is captured by FlowIO.LiftAsync's
    // boundary translation as a RuntimeError.External, which the
    // integration site can surface as a typed pre-flight failure rather
    // than letting it escape uncaught.
    var dispatcher = new RecordingDispatcher(
      category: "ext", behavior: DispatcherBehavior.Throw);
    var ext = new FakeExtensionServiceRef("ext.svc.Bad", "Bad", "ext");

    var effResult = await dispatcher.Inspect(ext).Run(CancellationToken.None);

    Assert.That(effResult, Is.InstanceOf<EffResult<Validated<PreFlightError, FlowUnit>>.Failure>(),
      "FlowIO.LiftAsync must capture the throw rather than propagate it.");
    var failure = (EffResult<Validated<PreFlightError, FlowUnit>>.Failure)effResult;
    Assert.That(failure.Error, Is.InstanceOf<RuntimeError.External>());
    Assert.That(failure.Error.Message, Does.Contain("simulated dispatcher failure"));
  }

  // ── (5) DI: zero or more dispatchers can be registered ──────────────

  [Test]
  public void DI_NoDispatcherRegistered_GetServicesReturnsEmpty()
  {
    // Mirrors the legacy "no dispatcher → success" contract: when no
    // dispatcher knows how to handle a category, the integration site
    // sees an empty enumeration and the loop simply finds no match. This
    // is the building block for the "non-fatal missing dispatcher"
    // semantic — the loop's job is to decide that empty == warn, not
    // halt.
    using var sp = new ServiceCollection().BuildServiceProvider();

    var dispatchers = sp.GetServices<IServiceRefDispatcher>();

    Assert.That(dispatchers, Is.Empty,
      "With no dispatcher registered, IEnumerable<IServiceRefDispatcher> resolution must be empty.");
  }

  [Test]
  public void DI_TwoDispatchersRegistered_BothResolveViaGetServices()
  {
    // The dispatch surface is plural — Core's integration site resolves
    // IEnumerable<IServiceRefDispatcher> so multiple extensions can
    // coexist (Python + SQL + …). Two distinct registrations must both
    // surface.
    var services = new ServiceCollection();
    var python = new RecordingDispatcher(category: "python");
    var sql = new RecordingDispatcher(category: "sql");
    services.AddSingleton<IServiceRefDispatcher>(python);
    services.AddSingleton<IServiceRefDispatcher>(sql);

    using var sp = services.BuildServiceProvider();
    var dispatchers = sp.GetServices<IServiceRefDispatcher>().ToArray();

    Assert.Multiple(() =>
    {
      Assert.That(dispatchers, Has.Length.EqualTo(2));
      Assert.That(dispatchers.Select(d => d.Category),
        Is.EquivalentTo(new[] { "python", "sql" }),
        "Both dispatchers must surface through IEnumerable resolution.");
    });
  }

  // ── (6) External ServiceRef wraps the extension ref correctly ───────

  [Test]
  public void ServiceRef_External_ExposesExtensionRefProperties()
  {
    // The dispatch loop reaches an IExtensionServiceRef via
    // ServiceRef.External(cause). Pin that DagId/DisplayName flow through
    // the wrapper unchanged — without this, a Category match would still
    // produce the wrong identity at the dispatcher boundary.
    var ext = new FakeExtensionServiceRef("ext.cat.X.Y", "Y", "cat");
    var serviceRef = new ServiceRef.External(ext);

    Assert.Multiple(() =>
    {
      Assert.That(serviceRef.DagId, Is.EqualTo("ext.cat.X.Y"));
      Assert.That(serviceRef.DisplayName, Is.EqualTo("Y"));
      Assert.That(serviceRef.Cause, Is.SameAs(ext),
        "The wrapped IExtensionServiceRef must be reachable for dispatcher routing.");
      Assert.That(serviceRef.Cause.Category, Is.EqualTo("cat"));
    });
  }
}
