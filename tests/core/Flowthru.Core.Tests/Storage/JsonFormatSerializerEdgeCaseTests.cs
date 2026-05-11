using Flowthru.Data.Catalog;
using Flowthru.Data.Schema;
using Flowthru.Data.Storage;
using Flowthru.Prelude;
using SysIO = System.IO;

namespace Flowthru.Core.Tests.Storage;

[FlowthruSchema]
public partial record JsonEdge_Nullable
{
  public required int Id { get; init; }
  public string? Optional { get; init; }
  public DateTime? OptionalTimestamp { get; init; }
}

[FlowthruSchema]
public partial record JsonEdge_NestedItem
{
  public required int Id { get; init; }
  public required string Title { get; init; }
}

[FlowthruSchema]
public partial record JsonEdge_NestedOuter
{
  public required string Owner { get; init; }
  public required IReadOnlyList<JsonEdge_NestedItem> Items { get; init; }
}

/// <summary>
/// Wider <see cref="JsonFormatSerializer{TRow}"/> conformance —
/// nullable properties, nested schemas, and empty collections.
/// Reactivates a curated subset of the legacy
/// <c>JsonFormatSerializerConformance</c> coverage; the full kit
/// returns when the Tests.Kits Format directory is unblocked.
/// </summary>
[TestFixture]
public class JsonFormatSerializerEdgeCaseTests
{
  private string _root = null!;

  [SetUp]
  public void SetUp()
  {
    _root = SysIO.Path.Combine(SysIO.Path.GetTempPath(), $"flowthru-jfs-{Guid.NewGuid():N}");
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
  public async Task Nullable_Optional_RoundTripsAsNull()
  {
    var path = SysIO.Path.Combine(_root, "nullable.json");
    var item = ItemFactory.Enumerable.Json<JsonEdge_Nullable>("nullable", path);

    var input = new[]
    {
      new JsonEdge_Nullable { Id = 1, Optional = "alpha", OptionalTimestamp = new DateTime(2025, 1, 1) },
      new JsonEdge_Nullable { Id = 2, Optional = null, OptionalTimestamp = null },
    };

    var saveResult = await item.Save(input).Run();
    Assert.That(saveResult, Is.InstanceOf<EffResult<FlowUnit>.Success>());

    var loadResult = await item.Load().Run();
    var loaded = ((EffResult<IEnumerable<JsonEdge_Nullable>>.Success)loadResult).Value.ToList();

    Assert.That(loaded, Has.Count.EqualTo(2));
    Assert.That(loaded[0].Optional, Is.EqualTo("alpha"));
    Assert.That(loaded[1].Optional, Is.Null);
    Assert.That(loaded[1].OptionalTimestamp, Is.Null);
  }

  [Test]
  public async Task Nested_RoundTripsItemsCollection()
  {
    var path = SysIO.Path.Combine(_root, "nested.json");
    var item = ItemFactory.Singleton.Json<JsonEdge_NestedOuter>("nested", path);

    var input = new JsonEdge_NestedOuter
    {
      Owner = "spencer",
      Items = new List<JsonEdge_NestedItem>
      {
        new() { Id = 1, Title = "first" },
        new() { Id = 2, Title = "second" },
      },
    };

    var saveResult = await item.Save(input).Run();
    Assert.That(saveResult, Is.InstanceOf<EffResult<FlowUnit>.Success>());

    var loadResult = await item.Load().Run();
    var loaded = ((EffResult<JsonEdge_NestedOuter>.Success)loadResult).Value;
    Assert.That(loaded.Owner, Is.EqualTo("spencer"));
    Assert.That(loaded.Items.Select(i => i.Title), Is.EquivalentTo(new[] { "first", "second" }));
  }

  [Test]
  public async Task EmptyCollection_RoundTripsAsZeroRows()
  {
    var path = SysIO.Path.Combine(_root, "empty.json");
    var item = ItemFactory.Enumerable.Json<JsonEdge_Nullable>("empty", path);

    await item.Save(Array.Empty<JsonEdge_Nullable>()).Run();
    var loadResult = await item.Load().Run();
    var loaded = ((EffResult<IEnumerable<JsonEdge_Nullable>>.Success)loadResult).Value;

    Assert.That(loaded.Count(), Is.EqualTo(0));
  }

  [Test]
  public async Task Load_OnMissingFile_FailsWithExternalError()
  {
    var path = SysIO.Path.Combine(_root, "does-not-exist.json");
    var item = ItemFactory.Enumerable.Json<JsonEdge_Nullable>("missing", path);

    var loadResult = await item.Load().Run();
    Assert.That(loadResult, Is.InstanceOf<EffResult<IEnumerable<JsonEdge_Nullable>>.Failure>());
  }
}
