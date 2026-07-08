using Flowthru.Data.Catalog;
using Flowthru.Data.Storage;
using Flowthru.Data.Storage.Parquet;
using Flowthru.Data.Storage.S3;
using Flowthru.Data.Storage.S3.Local;
using Flowthru.Extensions.DuckDB.Tests.Fixtures;
using Flowthru.Prelude;
using Flowthru.Step.DuckDb;
using Flowthru.Validation.Runtime;
using SysIO = System.IO;

namespace Flowthru.Extensions.DuckDB.Tests;

/// <summary>
/// Pins ADR-0023's note for engine transforms: the <c>s3:read</c>
/// concurrency cap is a <em>medium</em> property, inherited by any step
/// through ordinary item wiring — a DuckDB transform whose endpoint
/// items are S3-backed picks up the cap via
/// <see cref="ConflictKeys.Of"/> with no DuckDB-specific plumbing, the
/// same way an ordinary load step does. Runs fully offline over the
/// file-backed gateway stub; the conflict relation is wire-up data, not
/// I/O.
/// </summary>
[TestFixture]
[Category("DuckDB")]
public class DuckDbS3ConflictKeyTests
{
  private string _root = null!;

  [SetUp]
  public void SetUp()
  {
    _root = SysIO.Path.Combine(
      SysIO.Path.GetTempPath(), $"flowthru-duckdb-s3keys-{Guid.NewGuid():N}");
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
  public void S3BackedEndpoints_InheritTheS3ReadConflictKey_ThroughItemWiring()
  {
    var input = S3Item("s3_events", "in/events.parquet", readCapacity: 2);
    var output = S3Item("s3_sorted", "out/sorted.parquet", readCapacity: 2);

    var step = new DuckDbTransformStep<EventRow>(
      label: "sort_s3",
      sql: "SELECT * FROM s3_events",
      inputs: new[] { DuckDbInputRelation.From(input) },
      output: output,
      engine: new NullEngine()
    );

    var keys = ConflictKeys.Of(step)
      .Select(pair => ConflictKeys.KeyFor(pair.Dep, pair.Op))
      .ToList();

    Assert.Multiple(() =>
    {
      Assert.That(keys, Does.Contain("Read:s3:read"),
        "The S3-backed input must surface the medium's s3:read cap under the Read op.");
      Assert.That(keys, Does.Contain("Write:s3:read"),
        "The S3-backed output surfaces the same dependency under the Write op — a "
        + "distinct key, so bounding reads never serializes writes.");
      Assert.That(
        keys, Does.Contain($"Use:{typeof(IDuckDbEngine).FullName}"),
        "The engine's own service dependency must survive alongside the inherited keys.");
    });
  }

  [Test]
  public void UncappedS3Endpoints_CarryNoReadConflictKey()
  {
    // ADR-0019 posture: the cap is opt-in; an unbounded medium attaches no
    // dependency, so the scheduler's default behaviour is unchanged.
    var input = S3Item("s3_events", "in/events.parquet", readCapacity: int.MaxValue);
    var output = ItemFactory.Enumerable.Parquet<EventRow>(
      "sorted", SysIO.Path.Combine(_root, "sorted.parquet"));

    var step = new DuckDbTransformStep<EventRow>(
      label: "sort_uncapped",
      sql: "SELECT * FROM s3_events",
      inputs: new[] { DuckDbInputRelation.From(input) },
      output: output,
      engine: new NullEngine()
    );

    var keys = ConflictKeys.Of(step)
      .Select(pair => ConflictKeys.KeyFor(pair.Dep, pair.Op))
      .ToList();

    Assert.That(keys, Does.Not.Contain("Read:s3:read"));
  }

  // ── Harness ─────────────────────────────────────────────────────────────

  /// <summary>
  /// An S3-backed Parquet item over the offline file-stub gateway — the
  /// real <see cref="S3StorageMedium"/>, so the conflict-dependency wiring
  /// under test is production code, not a double.
  /// </summary>
  private IItem<IEnumerable<EventRow>> S3Item(string label, string key, int readCapacity) =>
    new Item<IEnumerable<EventRow>>(
      label,
      new ComposedStorageAdapter<IEnumerable<EventRow>, EventRow>(
        new S3StorageMedium(
          new LocalFileS3Gateway(_root), "test-bucket", key, readCapacity),
        new ParquetFormatSerializer<EventRow>(),
        new EnumerableContainerAdapter<EventRow>()
      )
    );

  private sealed class NullEngine : IDuckDbEngine
  {
    public int MaxConcurrency => 1;

    public FlowIO<DuckDbTransformResult> ExecuteTransform(DuckDbTransformRequest request) =>
      FlowIO.Pure(new DuckDbTransformResult(0, Array.Empty<(string, string)>()));
  }
}
