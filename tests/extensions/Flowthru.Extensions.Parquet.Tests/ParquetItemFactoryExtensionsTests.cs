using Flowthru.Data.Catalog;
using Flowthru.Data.Storage;
using Flowthru.Data.Storage.Parquet;
using Flowthru.Extensions.Parquet.Tests.Fixtures;
using Flowthru.Prelude;
using Flowthru.Validation.Runtime;
using SysIO = System.IO;

namespace Flowthru.Extensions.Parquet.Tests;

/// <summary>
/// Smart-constructor smoke tests for the Parquet extension methods —
/// <c>ItemFactory.Enumerable.Parquet&lt;T&gt;(...)</c> and
/// <c>ItemFactory.Directory.Parquet&lt;T&gt;(...)</c>. Verifies the
/// composed adapter wires correctly and end-to-end schema-mismatch
/// translation reaches the FlowIO boundary as the typed variant.
/// </summary>
[TestFixture]
[Category("Parquet")]
public class ParquetItemFactoryExtensionsTests
{
  private string _root = null!;

  [SetUp]
  public void SetUp()
  {
    _root = SysIO.Path.Combine(SysIO.Path.GetTempPath(), $"flowthru-parquet-ife-{Guid.NewGuid():N}");
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

  // ── Enumerable.Parquet<T> ────────────────────────────────────────────

  [Test]
  public async Task EnumerableParquet_RoundTripsThroughComposedAdapter()
  {
    var path = SysIO.Path.Combine(_root, "rows.parquet");
    var item = ItemFactory.Enumerable.Parquet<FlatRow>("rows", path);

    var input = new[]
    {
      new FlatRow { Id = 1, Name = "Alice", Value = 1.5 },
      new FlatRow { Id = 2, Name = "Bob",   Value = 2.5 },
    };

    var saveResult = await item.Save(input).Run();
    Assert.That(saveResult, Is.InstanceOf<EffResult<FlowUnit>.Success>());

    var loadResult = await item.Load().Run();
    var loaded = ((EffResult<IEnumerable<FlatRow>>.Success)loadResult).Value.ToList();

    Assert.That(loaded, Has.Count.EqualTo(2));
    Assert.That(loaded[0], Is.EqualTo(input[0]));
    Assert.That(loaded[1], Is.EqualTo(input[1]));
  }

  [Test]
  public void EnumerableParquet_BuildsItemWithExpectedLabel()
  {
    var item = ItemFactory.Enumerable.Parquet<FlatRow>("rows", "ignored.parquet");
    Assert.That(item.Label, Is.EqualTo("rows"));
  }

  [Test]
  public async Task EnumerableParquet_SchemaMismatch_SurfacesTypedRuntimeError()
  {
    var path = SysIO.Path.Combine(_root, "slim.parquet");

    // Write under the slim schema...
    var slim = ItemFactory.Enumerable.Parquet<MismatchSlim>("slim", path);
    var saveResult = await slim.Save(new[]
    {
      new MismatchSlim { Id = 1, Name = "Alice" },
    }).Run();
    Assert.That(saveResult, Is.InstanceOf<EffResult<FlowUnit>.Success>(),
      "Precondition: the slim file must write successfully.");

    // ...then try to load it under a wider schema. The composed adapter
    // should surface this as the typed RuntimeError.SchemaMismatch
    // variant rather than a generic External wrapping the exception.
    var fat = ItemFactory.Enumerable.Parquet<MismatchFat>("fat", path);
    var loadResult = await fat.Load().Run();

    Assert.That(loadResult, Is.InstanceOf<EffResult<IEnumerable<MismatchFat>>.Failure>());
    var failure = (EffResult<IEnumerable<MismatchFat>>.Failure)loadResult;
    Assert.That(failure.Error, Is.InstanceOf<RuntimeError.SchemaMismatch>(),
      "Missing-column detection must round-trip as the typed variant — "
      + "ParquetFormatSerializer throws SchemaMismatchException; "
      + "ComposedStorageAdapter's MapError lifts it past the FlowIO boundary."
    );
  }

  // ── Directory.Parquet<T> ─────────────────────────────────────────────

  [Test]
  public async Task DirectoryParquet_RoundTripsOneFilePerEntry()
  {
    var item = ItemFactory.Directory.Parquet<FlatRow>("dir", _root);

    var input = new Directory<IEnumerable<FlatRow>>(
      new Dictionary<string, IEnumerable<FlatRow>>
      {
        [SysIO.Path.Combine(_root, "a.parquet")] =
          new[] { new FlatRow { Id = 1, Name = "Alice", Value = 1.5 } },
        [SysIO.Path.Combine(_root, "b.parquet")] =
          new[] { new FlatRow { Id = 2, Name = "Bob", Value = 2.5 } },
      }
    );

    await item.Save(input).Run();
    var loadResult = await item.Load().Run();
    var loaded = ((EffResult<Directory<IEnumerable<FlatRow>>>.Success)loadResult).Value;

    Assert.That(loaded.Count, Is.EqualTo(2));
  }

  [Test]
  public async Task DirectoryParquet_HardDeletesExistingMatchingFiles()
  {
    var stale = SysIO.Path.Combine(_root, "stale.parquet");
    // Pre-seed a stale file via the single-file smart constructor.
    var pre = ItemFactory.Enumerable.Parquet<FlatRow>("pre", stale);
    await pre.Save(new[] { new FlatRow { Id = 99, Name = "stale", Value = 9.9 } }).Run();
    Assert.That(SysIO.File.Exists(stale), Is.True);

    var item = ItemFactory.Directory.Parquet<FlatRow>("dir", _root);
    var fresh = new Directory<IEnumerable<FlatRow>>(
      new Dictionary<string, IEnumerable<FlatRow>>
      {
        ["fresh.parquet"] = new[] { new FlatRow { Id = 1, Name = "Fresh", Value = 1.0 } },
      }
    );
    await item.Save(fresh).Run();

    Assert.That(SysIO.File.Exists(stale), Is.False,
      "DirectoryStorageAdapter.Save deletes existing matching files first; "
      + "Parquet inherits this re-run-deterministic behavior.");
    Assert.That(SysIO.File.Exists(SysIO.Path.Combine(_root, "fresh.parquet")), Is.True);
  }

  [Test]
  public void DirectoryParquet_BuildsItemWithExpectedLabel()
  {
    var item = ItemFactory.Directory.Parquet<FlatRow>("shuttles", _root);
    Assert.That(item.Label, Is.EqualTo("shuttles"));
  }
}
