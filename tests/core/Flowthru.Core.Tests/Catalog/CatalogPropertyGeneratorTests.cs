using Flowthru.Data.Catalog;
using Flowthru.Data.Schema;
using Flowthru.Prelude;

namespace Flowthru.Core.Tests.Catalog;

/// <summary>
/// Verifies the [JsonItem] catalog API simplification: a partial property
/// declared with <c>[JsonItem("path")]</c> compiles, accesses produce a
/// stable <see cref="IItem{T}"/>, and the wired item performs a Save +
/// Load round-trip end-to-end.
/// </summary>
[TestFixture]
public class CatalogPropertyGeneratorTests
{
  private string _tempDir = null!;
  private string _originalCwd = null!;

  [SetUp]
  public void SetUp()
  {
    _originalCwd = Directory.GetCurrentDirectory();
    _tempDir = Path.Combine(Path.GetTempPath(), $"flowthru-2C-{Guid.NewGuid():N}");
    Directory.CreateDirectory(_tempDir);
    Directory.SetCurrentDirectory(_tempDir);
  }

  [TearDown]
  public void TearDown()
  {
    // Restore the runner's CWD before deleting the temp dir, otherwise the
    // next test's working directory will be a deleted path and OS-level
    // GetCwd will throw.
    Directory.SetCurrentDirectory(_originalCwd);
    if (Directory.Exists(_tempDir))
    {
      try
      {
        Directory.Delete(_tempDir, recursive: true);
      }
      catch
      {
        // Best-effort.
      }
    }
  }

  [Test]
  public void EnumerableJsonItem_CompilesAndReturnsStableInstance()
  {
    var catalog = new TestCatalog();
    var item1 = catalog.Numbers;
    var item2 = catalog.Numbers;

    Assert.That(item1, Is.Not.Null, "Generated property should produce an IItem.");
    Assert.That(ReferenceEquals(item1, item2), Is.True,
      "Repeated property access should return the SAME instance — object identity is load-bearing for the DAG.");
    Assert.That(item1.Label, Is.EqualTo("Numbers"),
      "Label should be inferred from the property name.");
    Assert.That(item1.DataType, Is.EqualTo(typeof(IEnumerable<NumberRow>)),
      "DataType should reflect the property's container type.");
  }

  [Test]
  public async Task EnumerableJsonItem_RoundTrips()
  {
    var catalog = new TestCatalog();
    var item = catalog.Numbers;

    var saveResult = await item.Save(new[]
    {
      new NumberRow { Value = 10 },
      new NumberRow { Value = 20 },
      new NumberRow { Value = 30 },
    }).Run();
    Assert.That(saveResult, Is.InstanceOf<EffResult<FlowUnit>.Success>(),
      "Save through the [JsonItem]-generated property should succeed.");

    var loadResult = await item.Load().Run();
    Assert.That(loadResult, Is.InstanceOf<EffResult<IEnumerable<NumberRow>>.Success>());
    var loaded = ((EffResult<IEnumerable<NumberRow>>.Success)loadResult).Value.ToList();
    Assert.That(loaded, Has.Count.EqualTo(3));
    Assert.That(loaded.Select(r => r.Value), Is.EquivalentTo(new[] { 10, 20, 30 }));
  }

  [Test]
  public async Task SingletonJsonItem_RoundTrips()
  {
    var catalog = new TestCatalog();
    var item = catalog.Config;

    var saveResult = await item.Save(new ConfigRow { Threshold = 0.42 }).Run();
    Assert.That(saveResult, Is.InstanceOf<EffResult<FlowUnit>.Success>());

    var loadResult = await item.Load().Run();
    Assert.That(loadResult, Is.InstanceOf<EffResult<ConfigRow>.Success>());
    var loaded = ((EffResult<ConfigRow>.Success)loadResult).Value;
    Assert.That(loaded.Threshold, Is.EqualTo(0.42));
  }

  [Test]
  public void ManualCreateItem_StillWorks()
  {
    var catalog = new TestCatalog();
    var item1 = catalog.ManuallyCreatedItem;
    var item2 = catalog.ManuallyCreatedItem;

    Assert.That(ReferenceEquals(item1, item2), Is.True,
      "Manual CreateItem fallback should preserve the identity caching.");
    Assert.That(item1.Label, Is.EqualTo("ManuallyCreatedItem"));
  }
}

[FlowthruSchema]
public partial record NumberRow
{
  public required int Value { get; init; }
}

[FlowthruSchema]
public partial record ConfigRow
{
  public required double Threshold { get; init; }
}

public partial class TestCatalog : CatalogAbstract
{
  [JsonItem("numbers.json")]
  public partial IItem<IEnumerable<NumberRow>> Numbers { get; }

  [JsonItem("config.json")]
  public partial IItem<ConfigRow> Config { get; }

  public IItem<IEnumerable<NumberRow>> ManuallyCreatedItem =>
    CreateItem(() => ItemFactory.Enumerable.Json<NumberRow>(
      label: "ManuallyCreatedItem",
      filePath: "manual.json"
    ));
}
