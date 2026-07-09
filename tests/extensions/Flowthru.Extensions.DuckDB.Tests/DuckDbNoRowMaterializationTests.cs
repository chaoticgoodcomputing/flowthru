using Flowthru.Data.Catalog;
using Flowthru.Extensions.DuckDB.Tests.Fixtures;
using Flowthru.Flow;
using Flowthru.Prelude;
using Flowthru.Step.DuckDb.Internal;
using SysIO = System.IO;

namespace Flowthru.Extensions.DuckDB.Tests;

/// <summary>
/// Pins the transform's central guarantee: <em>rows never enter the
/// CLR</em>. The assertion is structural, not wall-clock —
/// <see cref="InstrumentedRow"/> counts every CLR materialization of
/// itself (its <c>Id</c> init-accessor fires on object initializers and
/// on the Parquet adapter's reflection-driven <c>Load()</c> alike), so
/// a transform wired between <see cref="InstrumentedRow"/> items that
/// finishes with the counter at zero provably never took the
/// load-rows/save-rows path. Seeding and verification go through the
/// uninstrumented <see cref="PlainRow"/> twin over the same files, so
/// only the transform itself could have bumped the counter.
/// </summary>
[TestFixture]
[Category("DuckDB")]
public class DuckDbNoRowMaterializationTests
{
  private string _root = null!;

  [SetUp]
  public void SetUp()
  {
    _root = SysIO.Path.Combine(
      SysIO.Path.GetTempPath(), $"flowthru-duckdb-norows-{Guid.NewGuid():N}");
    SysIO.Directory.CreateDirectory(_root);
  }

  [TearDown]
  public void TearDown()
  {
    if (SysIO.Directory.Exists(_root))
    {
      try { SysIO.Directory.Delete(_root, recursive: true); }
      catch { /* best effort */ }
    }
  }

  [Test]
  public async Task Transform_MaterializesZeroRowsInTheClr()
  {
    var inputPath = SysIO.Path.Combine(_root, "input.parquet");
    var outputPath = SysIO.Path.Combine(_root, "output.parquet");

    // Seed through the uninstrumented twin so the counter stays clean.
    var seed = ItemFactory.Enumerable.Parquet<PlainRow>("seed", inputPath);
    var seedRows = Enumerable.Range(1, 5_000)
      .Select(i => new PlainRow { Id = i, Country = i % 2 == 0 ? "AU" : "NZ", Value = i * 0.5 })
      .ToList();
    var seeded = await seed.Save(seedRows).Run();
    Assert.That(seeded, Is.InstanceOf<EffResult<FlowUnit>.Success>());

    // The transform's endpoints are typed with the INSTRUMENTED schema.
    var input = ItemFactory.Enumerable.Parquet<InstrumentedRow>("rows", inputPath);
    var output = ItemFactory.Enumerable.Parquet<InstrumentedRow>("sorted_rows", outputPath);

    RowMaterializationCounter.Reset();

    var flow = FlowBuilder.CreateFlow("duckdb-norows", f => f.AddDuckDbTransform(
      label: "sort_rows",
      input: input,
      output: output,
      sql: "SELECT * FROM rows ORDER BY Country, Id",
      engine: new InProcessDuckDbEngine()
    ));
    var result = await flow.RunAsync();

    Assert.That(result.IsSuccess, Is.True,
      string.Join("; ", result.StepResults.Select(r => r.ToString())));
    Assert.That(RowMaterializationCounter.Count, Is.Zero,
      "The engine-delegated transform must not materialize a single row in the CLR — "
      + "a non-zero count means some path loaded or constructed InstrumentedRow values.");

    // Verify correctness through the uninstrumented twin: the counter
    // assertion above must not be satisfied by an empty or wrong output.
    var verify = ItemFactory.Enumerable.Parquet<PlainRow>("verify", outputPath);
    var loaded = await verify.Load().Run();
    Assert.That(loaded, Is.InstanceOf<EffResult<IEnumerable<PlainRow>>.Success>());
    var rows = ((EffResult<IEnumerable<PlainRow>>.Success)loaded).Value.ToList();

    Assert.That(rows, Has.Count.EqualTo(seedRows.Count));
    Assert.That(
      rows.Select(r => (r.Country, r.Id)),
      Is.EqualTo(seedRows.Select(r => (r.Country, r.Id))
        .OrderBy(t => t.Country, StringComparer.Ordinal).ThenBy(t => t.Id)),
      "The transform must have produced the composite-key sort."
    );
    Assert.That(RowMaterializationCounter.Count, Is.Zero,
      "Verification used the PlainRow twin — the instrumented counter must still be zero.");
  }

  [Test]
  public async Task ControlExperiment_TheRowPathDoesBumpTheCounter()
  {
    // Sanity-check the instrument itself: loading the same file through
    // the INSTRUMENTED item must count one materialization per row. If
    // this control fails, the zero assertion above proves nothing.
    var path = SysIO.Path.Combine(_root, "control.parquet");
    var seed = ItemFactory.Enumerable.Parquet<PlainRow>("seed_ctl", path);
    await seed.Save(new[]
    {
      new PlainRow { Id = 1, Country = "AU", Value = 1.0 },
      new PlainRow { Id = 2, Country = "NZ", Value = 2.0 },
    }).Run();

    RowMaterializationCounter.Reset();
    var instrumented = ItemFactory.Enumerable.Parquet<InstrumentedRow>("rows_ctl", path);
    var loaded = await instrumented.Load().Run();

    Assert.That(loaded, Is.InstanceOf<EffResult<IEnumerable<InstrumentedRow>>.Success>());
    _ = ((EffResult<IEnumerable<InstrumentedRow>>.Success)loaded).Value.ToList();
    Assert.That(RowMaterializationCounter.Count, Is.EqualTo(2),
      "The eager row path must bump the counter once per row — proving the "
      + "instrument observes exactly what the transform is asserted not to do.");
  }
}
