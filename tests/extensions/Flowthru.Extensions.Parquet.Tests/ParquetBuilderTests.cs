using Flowthru.Data.Catalog;
using Flowthru.Data.Storage;
using Flowthru.Data.Storage.Parquet;
using Flowthru.Extensions.Parquet.Tests.Fixtures;

namespace Flowthru.Extensions.Parquet.Tests;

/// <summary>
/// Pins the <see cref="ParquetBuilder{TRow}"/> validation surface and the
/// <c>IFileItemBuilder</c> contract. Like CSV but with a different default
/// pattern and an optional <c>WithOptions</c> setter (compression / row-
/// group size / dictionary encoding).
/// </summary>
[TestFixture]
[Category("Parquet")]
public class ParquetBuilderTests
{
  private static ParquetBuilder<FlatRow> NewBuilder() =>
    Item.Of<IEnumerable<FlatRow>>("rows").Parquet();

  // ── IFileItemBuilder properties ─────────────────────────────────────

  [Test]
  public void Label_ReturnsAnchorLabel()
  {
    var builder = Item.Of<IEnumerable<FlatRow>>("Rows").Parquet();
    Assert.That(builder.Label, Is.EqualTo("Rows"));
  }

  [Test]
  public void DefaultFilePattern_IsParquetGlob()
  {
    Assert.That(NewBuilder().DefaultFilePattern, Is.EqualTo("*.parquet"));
  }

  // ── AtPath argument validation ──────────────────────────────────────

  [TestCase(null)]
  [TestCase("")]
  [TestCase("   ")]
  public void AtPath_NullOrWhitespace_Throws(string? path)
  {
    Assert.That(
      () => NewBuilder().AtPath(path!),
      Throws.TypeOf<ArgumentException>().With.Message.Contain("Path")
    );
  }

  // ── WithOptions guards ──────────────────────────────────────────────

  [Test]
  public void WithOptions_Null_ThrowsArgumentNull()
  {
    Assert.That(
      () => NewBuilder().WithOptions(null!),
      Throws.TypeOf<ArgumentNullException>()
    );
  }

  [Test]
  public void WithOptions_ReturnsBuilderForChaining()
  {
    var builder = NewBuilder();
    Assert.That(builder.WithOptions(new ParquetItemOptions<FlatRow>()), Is.SameAs(builder));
  }

  // ── Build() guards ──────────────────────────────────────────────────

  [Test]
  public void Build_WithoutAtPath_Throws_AndNamesTheItem()
  {
    Assert.That(
      () => Item.Of<IEnumerable<FlatRow>>("UnpathedRows").Parquet().Build(),
      Throws.TypeOf<InvalidOperationException>()
        .With.Message.Contain("'UnpathedRows'")
        .And.Message.Contain("AtPath(...)")
    );
  }

  [Test]
  public void Build_WithAtPath_ReturnsItemWithMatchingLabel()
  {
    var item = Item
      .Of<IEnumerable<FlatRow>>("Rows")
      .Parquet()
      .AtPath("/tmp/rows.parquet")
      .Build();
    Assert.That(item.Label, Is.EqualTo("Rows"));
  }

  // ── IFileItemBuilder.CreateAdapterForFile ───────────────────────────

  [Test]
  public void CreateAdapterForFile_DoesNotRequireAtPath()
  {
    var adapter = NewBuilder().CreateAdapterForFile("/tmp/per-file.parquet");
    Assert.That(adapter, Is.Not.Null);
  }

  [Test]
  public void CreateAdapterForFile_WithCustomResolver_DispatchesThroughIt()
  {
    var resolver = new RecordingResolver();
    NewBuilder()
      .WithResolver(resolver)
      .CreateAdapterForFile("/tmp/recorded.parquet");
    Assert.That(resolver.LastResolved, Is.EqualTo("/tmp/recorded.parquet"));
  }

  private sealed class RecordingResolver : IStorageMediumResolver
  {
    public string? LastResolved { get; private set; }

    public IStorageMedium Resolve(string pathOrUri)
    {
      LastResolved = pathOrUri;
      return new FileStorageMedium(pathOrUri);
    }
  }
}
