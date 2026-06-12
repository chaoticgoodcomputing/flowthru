using Flowthru.Data.Catalog;
using Flowthru.Data.Schema;
using Flowthru.Data.Storage.Sheets;
using Flowthru.Data.Storage.Sheets.InMemory;
using Flowthru.Flow;
using Flowthru.Hosting;
using Flowthru.Prelude;
using Flowthru.Step;
using Flowthru.Validation.Runtime;
using Flowthru.Validation.Runtime.Sheets;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.Extensions.Google.Sheets.Tests;

/// <summary>
/// #103 acceptance: a Sheets catalog item declares its spreadsheet as a
/// conflict resource, and <see cref="SheetsSpreadsheetProfileContributor"/>
/// resolves it to write capacity 1 / read capacity ∞. Concurrent writes
/// to one spreadsheet serialize (no race, no quota spike); concurrent
/// reads parallelize. (ADR-0019.)
/// </summary>
[TestFixture]
public sealed class SheetsConflictGatingTests
{
  private const string SpreadsheetId = "ss-conflict";

  public sealed class Row : IFlatSchema
  {
    public int Id { get; set; }
  }

  private static readonly IServiceProfileProvider Gated =
    new CompositeServiceProfileProvider(new IServiceProfileContributor[]
    {
      new SheetsSpreadsheetProfileContributor(),
    });

  private static readonly IServiceProfileProvider Ungated =
    new CompositeServiceProfileProvider(Array.Empty<IServiceProfileContributor>());

  // ── Acceptance: write serialization ────────────────────────────────────

  [Test]
  public async Task ConcurrentWritesToOneSpreadsheet_Serialize()
  {
    var maxConcurrent = await RunTwoWritesAsync(Gated);
    Assert.That(maxConcurrent, Is.EqualTo(1),
      "Two steps writing to one spreadsheet must serialize at Parallelism=4 — its write capacity is 1, "
      + "so concurrent batchUpdates can't race or double the per-user quota draw."
    );
  }

  [Test]
  public async Task ConcurrentWritesToOneSpreadsheet_WithoutContributor_RunConcurrently()
  {
    var maxConcurrent = await RunTwoWritesAsync(Ungated);
    Assert.That(maxConcurrent, Is.EqualTo(2),
      "Without the Sheets contributor the spreadsheet resolves to unbounded capacity, so the two "
      + "writes co-run — confirming gating, not DAG precedence, is what serializes them."
    );
  }

  // ── Contributor / item declarations ──────────────────────────────────────

  [Test]
  public void Contributor_MapsSpreadsheetDependency_ToDeclaredCapacities()
  {
    var dep = new ServiceDependency.External(
      new SheetsSpreadsheetDependency("ss-x", WriteCapacity: 1, ReadCapacity: int.MaxValue));

    var profile = new SheetsSpreadsheetProfileContributor().Contribute(dep);

    Assert.That(profile, Is.Not.Null);
    Assert.That(profile!.Capacity, Is.EqualTo(1));
    Assert.That(profile.ReadCapacity, Is.EqualTo(int.MaxValue));
  }

  [Test]
  public void Contributor_StaysSilent_OnUnrelatedDependency()
  {
    Assert.That(
      new SheetsSpreadsheetProfileContributor().Contribute(ServiceDependency.Of<IDisposable>()),
      Is.Null
    );
  }

  [Test]
  public void SheetsItem_DeclaresSpreadsheetDependency_WithSingleWriterCapacity()
  {
    var item = ItemFactory.Enumerable.GoogleSheets<Row>(
      "rows", SpreadsheetId, "Sheet1", new InMemorySheetsGateway());

    var dep = item.ServiceDependencies
      .OfType<ServiceDependency.External>()
      .Select(e => e.Cause)
      .OfType<SheetsSpreadsheetDependency>()
      .SingleOrDefault();

    Assert.That(dep, Is.Not.Null, "A Sheets item must declare its spreadsheet as a conflict resource.");
    Assert.That(dep!.SpreadsheetId, Is.EqualTo(SpreadsheetId));
    Assert.That(dep.WriteCapacity, Is.EqualTo(1), "A spreadsheet serializes concurrent writers.");
    Assert.That(dep.ReadCapacity, Is.EqualTo(int.MaxValue), "Reads parallelize.");
    Assert.That(dep.Category, Is.EqualTo("sheets"));
  }

  [Test]
  public void AddGoogleSheets_RegistersProfileContributor()
  {
    var services = new ServiceCollection();
    services.AddFlowthru(b =>
    {
      b.RegisterCatalog(_ => new EmptyCatalog());
      b.RegisterFlow("noop", () => FlowBuilder.CreateFlow("noop", _ => { }));
      b.AddGoogleSheets(new InMemorySheetsGateway());
    });
    using var sp = services.BuildServiceProvider();

    Assert.That(
      sp.GetServices<IServiceProfileContributor>().Any(c => c is SheetsSpreadsheetProfileContributor),
      Is.True,
      "AddGoogleSheets() must register the Sheets profile contributor so the scheduler can gate spreadsheet writes."
    );
  }

  // ── Harness ──────────────────────────────────────────────────────────────

  private static async Task<int> RunTwoWritesAsync(IServiceProfileProvider provider)
  {
    var gateway = new InMemorySheetsGateway();
    var root = ItemFactory.Singleton.Memory<int>($"sheets-root-{Guid.NewGuid():N}");
    await root.Save(0).Run();

    var (recordEntry, recordExit, max) = MakeConcurrencyMeter();
    // Recording save: measures overlap, performs no real write — isolates
    // scheduler gating from the gateway's own behaviour. Both tables live
    // in the SAME spreadsheet, so they share one write conflict key.
    Func<ISheetsGateway, string, string, IReadOnlyList<Row>, CancellationToken, Task> recordingSave =
      async (_, _, _, _, ct) =>
      {
        recordEntry();
        await Task.Delay(60, ct).ConfigureAwait(false);
        recordExit();
      };

    var outA = ItemFactory.Enumerable.GoogleSheets<Row>("sheet-a", SpreadsheetId, "TableA", gateway, recordingSave);
    var outB = ItemFactory.Enumerable.GoogleSheets<Row>("sheet-b", SpreadsheetId, "TableB", gateway, recordingSave);

    Func<int, FlowIO<IEnumerable<Row>>> transform = x =>
      FlowIO.Pure<IEnumerable<Row>>(new[] { new Row { Id = x } });

    IStepNode Step(string label, IItem<IEnumerable<Row>> output) =>
      new Step<int, IEnumerable<Row>>(
        label, transform, new IItem[] { root }, new IItem[] { output },
        loadInputs: () => root.Load(), saveOutputs: v => output.Save(v));

    var flow = FlowBuilder.CreateFlow("sheets-writes", b =>
    {
      b.Add(Step("sheet-step-a", outA));
      b.Add(Step("sheet-step-b", outB));
    });

    var result = await new ParallelFlowScheduler(profiles: provider)
      .ExecuteAsync(flow, new ExecutionOptions { Parallelism = 4 });

    Assert.That(result.IsSuccess, Is.True, "both write steps should succeed");
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
