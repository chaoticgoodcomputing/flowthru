using Flowthru.Extensions.Csv.Tests.Fixtures;
using Flowthru.Data.Catalog;
using Flowthru.Data.Storage;
using Flowthru.Prelude;
using Flowthru.Validation.Runtime;
using SysIO = System.IO;

namespace Flowthru.Extensions.Csv.Tests;

/// <summary>
/// Smart-constructor smoke tests for the
/// <see cref="CsvItemFactoryExtensions"/> extension methods —
/// verifies the user-facing surface
/// <c>ItemFactory.Enumerable.Csv&lt;T&gt;(...)</c> and
/// <c>ItemFactory.Directory.Csv&lt;T&gt;(...)</c> resolves to working
/// <see cref="IItem{T}"/> instances and round-trips through the
/// composed adapter.
/// </summary>
[TestFixture]
[Category("Csv")]
public class CsvItemFactoryExtensionsTests
{
  private string _root = null!;

  [SetUp]
  public void SetUp()
  {
    _root = SysIO.Path.Combine(SysIO.Path.GetTempPath(), $"flowthru-csv-ife-{Guid.NewGuid():N}");
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

  // ── Enumerable.Csv<T> ────────────────────────────────────────────────

  [Test]
  public async Task EnumerableCsv_RoundTripsThroughComposedAdapter()
  {
    var path = SysIO.Path.Combine(_root, "rows.csv");
    var item = ItemFactory.Enumerable.Csv<FlatRow>("rows", path);

    var input = new[]
    {
      new FlatRow { Id = 1, Name = "Alice", Value = 1.5 },
      new FlatRow { Id = 2, Name = "Bob", Value = 2.5 },
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
  public void EnumerableCsv_BuildsItemWithExpectedLabel()
  {
    var item = ItemFactory.Enumerable.Csv<FlatRow>("companies", "ignored.csv");
    Assert.That(item.Label, Is.EqualTo("companies"));
  }

  [Test]
  public async Task EnumerableCsv_CustomNullValues_AppliesOnRead()
  {
    var path = SysIO.Path.Combine(_root, "null-sentinels.csv");
    await SysIO.File.WriteAllTextAsync(
      path,
      "id,nullable_name,non_nullable_name,nullable_value\n" +
        "1,NA,X,7\n" +
        "2,Y,Z,NA\n"
    );

    var item = ItemFactory.Enumerable.Csv<NullableRow>(
      "rows", path, nullValues: new[] { "", "NA" }
    );

    var loadResult = await item.Load().Run();
    var rows = ((EffResult<IEnumerable<NullableRow>>.Success)loadResult).Value.ToList();

    Assert.That(rows[0].NullableName, Is.Null,
      "'NA' under custom null-values should deserialize to null.");
    Assert.That(rows[1].NullableValue, Is.Null,
      "'NA' for nullable int should deserialize to null.");
  }

  // ── Directory.Csv<T> ─────────────────────────────────────────────────

  [Test]
  public async Task DirectoryCsv_RoundTripsOneFilePerEntry()
  {
    var item = ItemFactory.Directory.Csv<FlatRow>("dir", _root);

    var input = new Directory<IEnumerable<FlatRow>>(
      new Dictionary<string, IEnumerable<FlatRow>>
      {
        [SysIO.Path.Combine(_root, "a.csv")] =
          new[] { new FlatRow { Id = 1, Name = "Alice", Value = 1.5 } },
        [SysIO.Path.Combine(_root, "b.csv")] =
          new[] { new FlatRow { Id = 2, Name = "Bob", Value = 2.5 } },
      }
    );

    await item.Save(input).Run();
    var loadResult = await item.Load().Run();
    var loaded = ((EffResult<Directory<IEnumerable<FlatRow>>>.Success)loadResult).Value;

    Assert.That(loaded.Count, Is.EqualTo(2));
  }

  [Test]
  public async Task DirectoryCsv_HardDeletesExistingFilesOnSave()
  {
    var stale = SysIO.Path.Combine(_root, "stale.csv");
    await SysIO.File.WriteAllTextAsync(stale, "Id,Name,Value\n99,stale,9.9\n");

    var item = ItemFactory.Directory.Csv<FlatRow>("dir", _root);
    var fresh = new Directory<IEnumerable<FlatRow>>(
      new Dictionary<string, IEnumerable<FlatRow>>
      {
        ["fresh.csv"] = new[] { new FlatRow { Id = 1, Name = "Fresh", Value = 1.0 } },
      }
    );

    await item.Save(fresh).Run();

    Assert.That(SysIO.File.Exists(stale), Is.False,
      "Save should hard-delete existing matching files before writing.");
    Assert.That(SysIO.File.Exists(SysIO.Path.Combine(_root, "fresh.csv")), Is.True);
  }

  [Test]
  public void DirectoryCsv_BuildsItemWithExpectedLabel()
  {
    var item = ItemFactory.Directory.Csv<FlatRow>("shuttles", _root);
    Assert.That(item.Label, Is.EqualTo("shuttles"));
  }

  // ── Schema-mismatch translation ─────────────────────────────────────

  [Test]
  public async Task EnumerableCsv_HeaderMismatch_SurfacesTypedSchemaMismatch()
  {
    // CsvHelper raises HeaderValidationException when the file's
    // header doesn't match the schema. CsvFormatSerializer translates
    // that to SchemaMismatchException; ComposedStorageAdapter
    // translates that to the typed RuntimeError.SchemaMismatch.
    // Asserting end-to-end that the typed variant reaches the FlowIO
    // boundary — not a generic External wrapping CsvHelper's error.
    var path = SysIO.Path.Combine(_root, "wrong-headers.csv");
    await SysIO.File.WriteAllTextAsync(
      path, "WrongCol,AlsoWrong,VeryWrong\n1,2,3\n"
    );

    var item = ItemFactory.Enumerable.Csv<FlatRow>("rows", path);
    var loadResult = await item.Load().Run();

    Assert.That(loadResult, Is.InstanceOf<EffResult<IEnumerable<FlatRow>>.Failure>(),
      "Loading a CSV with mismatched headers should fail."
    );
    var failure = (EffResult<IEnumerable<FlatRow>>.Failure)loadResult;
    Assert.That(failure.Error, Is.InstanceOf<RuntimeError.SchemaMismatch>(),
      "End-to-end translation: CsvHelper HeaderValidationException → "
      + "SchemaMismatchException → typed RuntimeError.SchemaMismatch."
    );
  }
}
