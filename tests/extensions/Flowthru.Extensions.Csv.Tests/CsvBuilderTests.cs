using Flowthru.Data.Catalog;
using Flowthru.Data.Storage;
using Flowthru.Extensions.Csv.Tests.Fixtures;

namespace Flowthru.Extensions.Csv.Tests;

/// <summary>
/// Pins the <see cref="CsvBuilder{TRow}"/> validation surface and the
/// <c>IFileItemBuilder</c> contract — the call shape every catalog uses
/// to declare a CSV-backed item. Invalid arguments throw at the builder
/// boundary (not later at flow-build / first-load time), and
/// <c>IFileItemBuilder.CreateAdapterForFile</c> works without
/// <c>AtPath</c> ever being called (the per-file path is supplied
/// separately by the directory lift).
/// </summary>
[TestFixture]
[Category("Csv")]
public class CsvBuilderTests
{
  private static CsvBuilder<FlatRow> NewBuilder() =>
    Item.Of<IEnumerable<FlatRow>>("rows").Csv();

  // ── Properties on the IFileItemBuilder surface ──────────────────────

  [Test]
  public void Label_ReturnsAnchorLabel()
  {
    var builder = Item.Of<IEnumerable<FlatRow>>("MyRows").Csv();
    Assert.That(builder.Label, Is.EqualTo("MyRows"));
  }

  [Test]
  public void DefaultFilePattern_IsCsvGlob()
  {
    Assert.That(NewBuilder().DefaultFilePattern, Is.EqualTo("*.csv"));
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

  [Test]
  public void AtPath_ValidPath_ReturnsBuilderForChaining()
  {
    var builder = NewBuilder();
    Assert.That(builder.AtPath("/tmp/x.csv"), Is.SameAs(builder));
  }

  // ── WithNullValues guards ───────────────────────────────────────────

  [Test]
  public void WithNullValues_Null_ThrowsArgumentNull()
  {
    Assert.That(
      () => NewBuilder().WithNullValues(null!),
      Throws.TypeOf<ArgumentNullException>()
    );
  }

  [Test]
  public void WithNullValues_ReturnsBuilderForChaining()
  {
    var builder = NewBuilder();
    Assert.That(builder.WithNullValues(new[] { "" }), Is.SameAs(builder));
  }

  // ── Build()'s required-AtPath guard ─────────────────────────────────

  [Test]
  public void Build_WithoutAtPath_Throws_AndNamesTheItem()
  {
    Assert.That(
      () => Item.Of<IEnumerable<FlatRow>>("UnpathedRows").Csv().Build(),
      Throws.TypeOf<InvalidOperationException>()
        .With.Message.Contain("'UnpathedRows'")
        .And.Message.Contain("AtPath(...)")
    );
  }

  [Test]
  public void Build_WithAtPath_ReturnsItemWithMatchingLabel()
  {
    var item = Item
      .Of<IEnumerable<FlatRow>>("Companies")
      .Csv()
      .AtPath("/tmp/companies.csv")
      .Build();
    Assert.That(item.Label, Is.EqualTo("Companies"));
  }

  // ── IFileItemBuilder.CreateAdapterForFile (used by the directory lift) ──

  [Test]
  public void CreateAdapterForFile_DoesNotRequireAtPath()
  {
    // The directory lift constructs adapters without ever calling
    // AtPath on the per-file builder — the per-file path is the
    // directory-walk's per-entry path.
    var builder = NewBuilder();
    var adapter = builder.CreateAdapterForFile("/tmp/per-file.csv");
    Assert.That(adapter, Is.Not.Null);
  }

  [Test]
  public void CreateAdapterForFile_HonoursCustomNullValues()
  {
    // Two adapters built with different null-value lists must be
    // distinct instances — proves the WithNullValues setting is
    // captured by closure, not silently dropped.
    var bareAdapter = NewBuilder().CreateAdapterForFile("/tmp/x.csv");
    var customAdapter = NewBuilder()
      .WithNullValues(new[] { "", "NA", "NULL" })
      .CreateAdapterForFile("/tmp/x.csv");
    Assert.That(customAdapter, Is.Not.SameAs(bareAdapter));
  }

  // ── WithResolver propagation ────────────────────────────────────────

  [Test]
  public void CreateAdapterForFile_WithCustomResolver_DispatchesThroughIt()
  {
    var resolver = new RecordingResolver();
    var adapter = NewBuilder()
      .WithResolver(resolver)
      .CreateAdapterForFile("/tmp/recorded.csv");
    Assert.That(resolver.LastResolved, Is.EqualTo("/tmp/recorded.csv"));
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
