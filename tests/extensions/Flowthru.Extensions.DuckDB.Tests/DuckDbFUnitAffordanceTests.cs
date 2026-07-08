using Flowthru.Data.Catalog;
using Flowthru.Extensions.DuckDB.Tests.Fixtures;
using Flowthru.Step.DuckDb;
using Flowthru.Step.DuckDb.Internal;
using Flowthru.Step.Testing;
using SysIO = System.IO;

namespace Flowthru.Extensions.DuckDB.Tests;

/// <summary>
/// The FUnit affordance for #136, exercised through the real FUnit
/// surface: an <see cref="FUnitContext"/>-derived fixture calls the
/// standard <c>Validate(step)</c> sugar and a schema-breaking SQL edit
/// fails the test with the same diagnostic quality pre-flight delivers —
/// design-time by the glossary's definition. No input file exists; the
/// check is hermetic.
/// </summary>
[TestFixture]
[Category("DuckDB")]
public class DuckDbFUnitAffordanceTests : FUnitContext
{
  private static readonly string Root = SysIO.Path.Combine(
    SysIO.Path.GetTempPath(), $"flowthru-duckdb-funit-{Guid.NewGuid():N}");

  [Test]
  public async Task Validate_BrokenTransform_FailsWithFullDiagnostics()
  {
    var result = await Validate(MakeStep(
      "SELECT Id, Country, 1 AS Extra FROM events"));

    Assert.That(result.IsValid, Is.False,
      "A schema-breaking SQL edit must fail the unit test.");
    var error = result.Errors.Single();
    Assert.Multiple(() =>
    {
      Assert.That(error.Message, Does.Contain("bad_projection"), "names the step");
      Assert.That(error.Message, Does.Contain("sorted_events"), "names the output binding");
      Assert.That(error.Message, Does.Contain("OccurredAt"), "names the missing column");
      Assert.That(error.Message, Does.Contain("Extra"), "names the extra column");
      Assert.That(error.Details, Is.EqualTo("FTDDB3002"), "carries the diagnostic code");
    });
  }

  [Test]
  public async Task Validate_HealthyTransform_Passes()
  {
    var result = await Validate(MakeStep("SELECT * FROM events"));
    Assert.That(result.IsValid, Is.True,
      string.Join("; ", result.Errors.Select(e => e.Message)));
  }

  private static DuckDbTransformStep<EventRow> MakeStep(string sql) =>
    new(
      label: "bad_projection",
      sql: sql,
      inputs: new[]
      {
        DuckDbInputRelation.From(ItemFactory.Enumerable.Parquet<EventRow>(
          "events", SysIO.Path.Combine(Root, "events.parquet"))),
      },
      output: ItemFactory.Enumerable.Parquet<EventRow>(
        "sorted_events", SysIO.Path.Combine(Root, "sorted.parquet")),
      engine: new InProcessDuckDbEngine()
    );
}
