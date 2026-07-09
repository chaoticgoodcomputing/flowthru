using Flowthru.Data.Catalog;
using Flowthru.Data.Storage;
using Flowthru.Extensions.DuckDB.Tests.Fixtures;
using Flowthru.Flow;
using Flowthru.Hosting;
using Flowthru.Prelude;
using Flowthru.Step.DuckDb;
using Flowthru.Step.DuckDb.Internal;
using Flowthru.Validation.PreFlight;
using Flowthru.Validation.PreFlight.DuckDb;
using Flowthru.Validation.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SysIO = System.IO;

namespace Flowthru.Extensions.DuckDB.Tests;

/// <summary>
/// The hermetic SQL schema check (#136): pre-flight builds empty
/// in-memory tables from the <em>declared</em> input record schemas,
/// DESCRIBEs the transform SQL against them, and verifies the result
/// against the declared output schema — with zero I/O against real
/// data. Every test here points its Parquet items at paths that never
/// exist; the checks must still produce full-fidelity diagnostics
/// naming the step, the relation binding, and the offending column(s).
/// </summary>
[TestFixture]
[Category("DuckDB")]
public class DuckDbSqlPreFlightTests
{
  private string _root = null!;
  private IDuckDbEngine _engine = null!;

  [SetUp]
  public void SetUp()
  {
    // Never created on disk — the hermetic check must not need it.
    _root = SysIO.Path.Combine(
      SysIO.Path.GetTempPath(), $"flowthru-duckdb-preflight-{Guid.NewGuid():N}");
    _engine = new InProcessDuckDbEngine();
  }

  [TearDown]
  public void TearDown()
  {
    Assert.That(SysIO.Directory.Exists(_root), Is.False,
      "The hermetic check must not create or touch any file — no path under the "
      + "test root should ever materialise.");
  }

  // ── Hook classification ─────────────────────────────────────────────────

  [Test]
  public void Hook_SelfClassifiesHermetic_WithStableId()
  {
    var hook = new DuckDbTransformValidationHook();
    Assert.Multiple(() =>
    {
      Assert.That(hook.MinimumDepth, Is.EqualTo(ValidationDepth.Hermetic),
        "The check reaches nothing outside the process, so it must survive an "
        + "offline smoke test (DryRun.On + ValidationDepth.Hermetic).");
      Assert.That(hook.HookId, Is.EqualTo("duckdb.sql-schema"));
    });
  }

  // ── Happy path (no real data anywhere) ──────────────────────────────────

  [Test]
  public async Task Hook_ValidJoinSql_Passes_WithoutAnyInputFileExisting()
  {
    var flow = FlowBuilder.CreateFlow("ok", f => f.AddDuckDbTransform(
      label: "enrich_events",
      inputs: new[]
      {
        DuckDbInputRelation.From(Events("events"), "ev"),
        DuckDbInputRelation.From(Regions("country_regions"), "region_lookup"),
      },
      output: Enriched("enriched_events"),
      sql: """
        SELECT ev.Id, ev.Country, region_lookup.Region
        FROM ev JOIN region_lookup USING (Country)
        """,
      engine: _engine
    ));

    var outcome = await RunHook(flow);
    Assert.That(outcome.IsValid, Is.True,
      $"Declared schemas satisfy the SQL — expected a pass, got: {Render(outcome)}");
  }

  // ── Output-side mismatches (FTDDB3002) ──────────────────────────────────

  [Test]
  public async Task Hook_MissingAndExtraColumns_NamesStepOutputAndColumns()
  {
    var flow = FlowBuilder.CreateFlow("mismatch", f => f.AddDuckDbTransform(
      label: "bad_projection",
      input: Events("events"),
      output: Sorted("sorted_events"),
      // Drops OccurredAt/Value and invents Extra.
      sql: "SELECT Id, Country, 1 AS Extra FROM events",
      engine: _engine
    ));

    var errors = await RunHookExpectingErrors(flow);
    var mismatch = (DuckDbPreFlightError.ResultSchemaMismatch)Single(errors);
    Assert.Multiple(() =>
    {
      Assert.That(mismatch.DiagnosticCode, Is.EqualTo("FTDDB3002"));
      Assert.That(mismatch.StepLabel, Is.EqualTo("bad_projection"));
      Assert.That(mismatch.OutputItemLabel, Is.EqualTo("sorted_events"));
      Assert.That(mismatch.Message, Does.Contain("bad_projection"), "names the step");
      Assert.That(mismatch.Message, Does.Contain("sorted_events"), "names the output binding");
      Assert.That(mismatch.Message, Does.Contain("OccurredAt"), "names the missing column");
      Assert.That(mismatch.Message, Does.Contain("Extra"), "names the extra column");
    });
  }

  [Test]
  public async Task Hook_IncompatibleAggregateType_PointsAtCast()
  {
    var flow = FlowBuilder.CreateFlow("badtype", f => f.AddDuckDbTransform(
      label: "totals_by_country",
      input: Events("events"),
      output: Totals("country_totals"),
      // SUM(BIGINT) is HUGEINT — incompatible with the declared double
      // without an explicit CAST.
      sql: """
        SELECT Country, COUNT(*) AS EventCount, SUM(Id) AS TotalValue
        FROM events GROUP BY Country
        """,
      engine: _engine
    ));

    var errors = await RunHookExpectingErrors(flow);
    var mismatch = (DuckDbPreFlightError.ResultSchemaMismatch)Single(errors);
    Assert.Multiple(() =>
    {
      Assert.That(mismatch.Message, Does.Contain("TotalValue"), "names the offending column");
      Assert.That(mismatch.Message, Does.Contain("HUGEINT"), "names the actual engine type");
      Assert.That(mismatch.Message, Does.Contain("CAST"), "points at the fix");
    });
  }

  // ── Input-side disagreements (FTDDB3001) ────────────────────────────────

  [Test]
  public async Task Hook_UnknownColumn_ReportsPreparationFailure_WithRelationBindings()
  {
    var flow = FlowBuilder.CreateFlow("badcol", f => f.AddDuckDbTransform(
      label: "sort_events",
      inputs: new[] { DuckDbInputRelation.From(Events("events"), "ev") },
      output: Sorted("sorted_events"),
      // 'Contry' exists on no declared input schema.
      sql: "SELECT Id, Contry AS Country, OccurredAt, Value FROM ev",
      engine: _engine
    ));

    var errors = await RunHookExpectingErrors(flow);
    var failure = (DuckDbPreFlightError.SqlPreparationFailed)Single(errors);
    Assert.Multiple(() =>
    {
      Assert.That(failure.DiagnosticCode, Is.EqualTo("FTDDB3001"));
      Assert.That(failure.StepLabel, Is.EqualTo("sort_events"));
      Assert.That(failure.Message, Does.Contain("sort_events"), "names the step");
      Assert.That(failure.Message, Does.Contain("'ev'"), "names the relation binding");
      Assert.That(failure.Message, Does.Contain("'events'"), "names the bound item");
      Assert.That(failure.Message, Does.Contain("EventRow"), "names the declared schema");
      Assert.That(failure.Message, Does.Contain("Contry"),
        "carries DuckDB's binder detail naming the offending column");
    });
  }

  [Test]
  public async Task Hook_UnboundRelation_ReportsPreparationFailure()
  {
    var flow = FlowBuilder.CreateFlow("badrel", f => f.AddDuckDbTransform(
      label: "sort_events",
      input: Events("events"),
      output: Sorted("sorted_events"),
      // The input binds as relation "events"; "evnts" is bound nowhere.
      sql: "SELECT * FROM evnts",
      engine: _engine
    ));

    var errors = await RunHookExpectingErrors(flow);
    var failure = (DuckDbPreFlightError.SqlPreparationFailed)Single(errors);
    Assert.Multiple(() =>
    {
      Assert.That(failure.Message, Does.Contain("evnts"), "names the missing relation");
      Assert.That(failure.Message, Does.Contain("relation 'events'"),
        "lists what was actually bound, so the typo is diagnosable");
    });
  }

  // ── Applicative aggregation ─────────────────────────────────────────────

  [Test]
  public async Task Hook_TwoBrokenTransforms_ReportsBothAtOnce()
  {
    var flow = FlowBuilder.CreateFlow("both", f => f
      .AddDuckDbTransform(
        label: "broken_projection",
        input: Events("events"),
        output: Sorted("sorted_events"),
        sql: "SELECT Id FROM events",
        engine: _engine)
      .AddDuckDbTransform(
        label: "broken_binder",
        input: Sorted("sorted_events"),
        output: Enriched("enriched_events"),
        sql: "SELECT Nope FROM sorted_events",
        engine: _engine));

    var errors = await RunHookExpectingErrors(flow);
    Assert.Multiple(() =>
    {
      Assert.That(errors, Has.Count.EqualTo(2),
        "Both transforms' findings must aggregate — every problem at once, not first-error.");
      Assert.That(errors.OfType<DuckDbPreFlightError.ResultSchemaMismatch>()
          .Single().StepLabel, Is.EqualTo("broken_projection"));
      Assert.That(errors.OfType<DuckDbPreFlightError.SqlPreparationFailed>()
          .Single().StepLabel, Is.EqualTo("broken_binder"));
    });
  }

  // ── Design-time surfaces (FUnit affordance) ─────────────────────────────

  [Test]
  public async Task StepValidate_BrokenSql_FailsWithTheSameDiagnostics()
  {
    var step = new DuckDbTransformStep<EventRow>(
      label: "bad_projection",
      sql: "SELECT Id, Country, 1 AS Extra FROM events",
      inputs: new[] { DuckDbInputRelation.From(Events("events")) },
      output: Sorted("sorted_events"),
      engine: _engine
    );

    // The same surface FUnitContext.Validate(step) unwinds.
    var outcome = await step.Validate().Run();
    var result = ((EffResult<ValidationResult>.Success)outcome).Value;

    Assert.That(result.IsValid, Is.False,
      "A schema-breaking SQL edit must fail the unit test.");
    var error = result.Errors.Single();
    Assert.Multiple(() =>
    {
      Assert.That(error.Details, Is.EqualTo("FTDDB3002"), "carries the diagnostic code");
      Assert.That(error.Message, Does.Contain("bad_projection"));
      Assert.That(error.Message, Does.Contain("OccurredAt"));
      Assert.That(error.Message, Does.Contain("Extra"));
    });
  }

  [Test]
  public async Task StepValidate_HealthySql_Passes()
  {
    var step = new DuckDbTransformStep<EventRow>(
      label: "sort_events",
      sql: "SELECT * FROM events ORDER BY Country, Id",
      inputs: new[] { DuckDbInputRelation.From(Events("events")) },
      output: Sorted("sorted_events"),
      engine: _engine
    );

    var outcome = await step.Validate().Run();
    var result = ((EffResult<ValidationResult>.Success)outcome).Value;
    Assert.That(result.IsValid, Is.True,
      string.Join("; ", result.Errors.Select(e => e.Message)));
  }

  [Test]
  public async Task ValidateDuckDbTransforms_FlowSurface_AggregatesOnlyBrokenSteps()
  {
    var flow = FlowBuilder.CreateFlow("mixed", f => f
      .AddDuckDbTransform(
        label: "healthy_sort",
        input: Events("events"),
        output: Sorted("sorted_events"),
        sql: "SELECT * FROM events",
        engine: _engine)
      .AddDuckDbTransform(
        label: "broken_projection",
        input: Sorted("sorted_events"),
        output: Enriched("enriched_events"),
        sql: "SELECT Id, Country FROM sorted_events",
        engine: _engine));

    var result = await flow.ValidateDuckDbTransforms();

    Assert.That(result.IsValid, Is.False);
    var error = result.Errors.Single();
    Assert.Multiple(() =>
    {
      Assert.That(error.CatalogKey, Is.EqualTo("broken_projection"),
        "only the broken transform reports");
      Assert.That(error.Message, Does.Contain("Region"), "names the missing column");
      Assert.That(error.Details, Is.EqualTo("FTDDB3002"));
    });
  }

  [Test]
  public async Task ValidateDuckDbTransforms_FlowWithoutDuckDbSteps_IsVacuouslyValid()
  {
    var input = ItemFactory.Singleton.Memory<int>("input");
    var output = ItemFactory.Singleton.Memory<int>("output");
    var flow = FlowBuilder.CreateFlow("plain", b =>
      b.AddStep<int, int>("noop", x => x, input, output));

    var result = await flow.ValidateDuckDbTransforms();
    Assert.That(result.IsValid, Is.True);
  }

  // ── Hosted pre-flight (composes with the validation-depth model) ────────

  [Test]
  public async Task HostedRun_OfflineSmokeTest_FailsPreFlightWithTypedDuckDbError()
  {
    var service = HostService(sql: "SELECT Id, Country, 1 AS Extra FROM events");

    // The offline smoke test pairing from ADR-0021: nothing executes,
    // nothing external is reached — and the broken SQL still fails.
    var result = await service.RunAsync(options: new ExecutionOptions
    {
      DryRun = DryRunOption.On,
      ValidationDepth = ValidationDepth.Hermetic,
    });

    Assert.That(result.IsSuccess, Is.False,
      "A schema-breaking SQL edit must fail even an offline (Hermetic) smoke test.");
    var failed = result.StepResults.OfType<StepResult.Failed>().Single();
    Assert.Multiple(() =>
    {
      Assert.That(failed.StepLabel, Is.EqualTo("preflight:external:duckdb"));
      Assert.That(failed.Error, Is.InstanceOf<RuntimeError.PreFlightFailed>());
      var cause = ((RuntimeError.PreFlightFailed)failed.Error).Cause;
      Assert.That(cause, Is.InstanceOf<PreFlightError.External>());
      Assert.That(((PreFlightError.External)cause).Cause,
        Is.InstanceOf<DuckDbPreFlightError.ResultSchemaMismatch>());
      Assert.That(cause.Message, Does.Contain("bad_projection"));
      Assert.That(cause.Message, Does.Contain("Extra"));
    });
  }

  [Test]
  public async Task HostedRun_HealthySql_PassesTheOfflineSmokeTest()
  {
    var service = HostService(sql: "SELECT * FROM events");

    var result = await service.RunAsync(options: new ExecutionOptions
    {
      DryRun = DryRunOption.On,
      ValidationDepth = ValidationDepth.Hermetic,
    });

    Assert.That(result.IsSuccess, Is.True, Describe(result));
  }

  [Test]
  public async Task HostedRun_DepthNone_SkipsTheCheck()
  {
    // None means "no introspection above plan-build" — the SQL check is
    // introspection, so it must not run. (The broken SQL then fails at
    // runtime instead; DryRun keeps this test from executing anything.)
    var service = HostService(sql: "SELECT Id, Country, 1 AS Extra FROM events");

    var result = await service.RunAsync(options: new ExecutionOptions
    {
      DryRun = DryRunOption.On,
      ValidationDepth = ValidationDepth.None,
    });

    Assert.That(
      result.StepResults.OfType<StepResult.Failed>()
        .Any(f => f.StepLabel.StartsWith("preflight:external:duckdb")),
      Is.False,
      "ValidationDepth.None runs no pre-flight introspection at all."
    );
  }

  // ── Harness ─────────────────────────────────────────────────────────────

  private string Path(string fileName) => SysIO.Path.Combine(_root, fileName);

  private IItem<IEnumerable<EventRow>> Events(string label) =>
    ItemFactory.Enumerable.Parquet<EventRow>(label, Path($"{label}.parquet"));

  private IItem<IEnumerable<EventRow>> Sorted(string label) =>
    ItemFactory.Enumerable.Parquet<EventRow>(label, Path($"{label}.parquet"));

  private IItem<IEnumerable<CountryRegionRow>> Regions(string label) =>
    ItemFactory.Enumerable.Parquet<CountryRegionRow>(label, Path($"{label}.parquet"));

  private IItem<IEnumerable<EnrichedEventRow>> Enriched(string label) =>
    ItemFactory.Enumerable.Parquet<EnrichedEventRow>(label, Path($"{label}.parquet"));

  private IItem<IEnumerable<CountryTotalRow>> Totals(string label) =>
    ItemFactory.Enumerable.Parquet<CountryTotalRow>(label, Path($"{label}.parquet"));

  private static async Task<Validated<PreFlightError, FlowUnit>> RunHook(BuiltFlow flow)
  {
    var outcome = await new DuckDbTransformValidationHook().Validate(flow).Run();
    Assert.That(outcome, Is.InstanceOf<EffResult<Validated<PreFlightError, FlowUnit>>.Success>(),
      $"The hook itself must not fail: {outcome}");
    return ((EffResult<Validated<PreFlightError, FlowUnit>>.Success)outcome).Value;
  }

  private static async Task<IReadOnlyList<DuckDbPreFlightError>> RunHookExpectingErrors(
    BuiltFlow flow
  )
  {
    var outcome = await RunHook(flow);
    Assert.That(outcome, Is.InstanceOf<Validated<PreFlightError, FlowUnit>.Invalid>(),
      "Expected pre-flight findings, got a pass.");
    var errors = ((Validated<PreFlightError, FlowUnit>.Invalid)outcome).Errors;
    Assert.That(errors, Is.All.InstanceOf<PreFlightError.External>(),
      "DuckDB findings surface through the External extension point.");
    return errors
      .Cast<PreFlightError.External>()
      .Select(e => (DuckDbPreFlightError)e.Cause)
      .ToList();
  }

  private static DuckDbPreFlightError Single(IReadOnlyList<DuckDbPreFlightError> errors)
  {
    Assert.That(errors, Has.Count.EqualTo(1),
      $"Expected exactly one finding, got: {string.Join(" | ", errors.Select(e => e.Message))}");
    return errors[0];
  }

  private IFlowthruService HostService(string sql)
  {
    var services = new ServiceCollection();
    services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
    services.AddFlowthru(b =>
    {
      b.UseDuckDb();
      b.RegisterFlow<IDuckDbEngine>("analytics", engine =>
        FlowBuilder.CreateFlow("analytics", f => f.AddDuckDbTransform(
          label: "bad_projection",
          input: Events("events"),
          output: Sorted("sorted_events"),
          sql: sql,
          engine: engine
        )));
    });
    return services.BuildServiceProvider().GetRequiredService<IFlowthruService>();
  }

  private static string Render(Validated<PreFlightError, FlowUnit> outcome) =>
    outcome is Validated<PreFlightError, FlowUnit>.Invalid invalid
      ? string.Join(" | ", invalid.Errors.Select(e => e.Message))
      : "(valid)";

  private static string Describe(FlowResult result) =>
    string.Join("; ", result.StepResults.Select(r => r switch
    {
      StepResult.Failed f => $"{f.StepLabel}: FAILED {f.Error.Message}",
      StepResult.Skipped s => $"{s.StepLabel}: skipped ({s.Reason})",
      _ => $"{r.StepLabel}: ok",
    }));
}
