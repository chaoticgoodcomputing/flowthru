using Flowthru.Data.Catalog;
using Flowthru.Data.Storage;
using Flowthru.Extensions.Excel.Tests.Fixtures;

namespace Flowthru.Extensions.Excel.Tests;

/// <summary>
/// Pins the <see cref="ExcelBuilder{TRow}"/> validation surface and its
/// <c>IFileItemBuilder</c> contract. Excel adds two requirements over the
/// other formats: a worksheet name (<c>WithSheet</c>) is mandatory before
/// adapter construction, and the adapter is read-only (writer = null).
/// </summary>
[TestFixture]
[Category("Excel")]
public class ExcelBuilderTests
{
  private static ExcelBuilder<ProductRow> NewBuilder() =>
    Item.Of<IEnumerable<ProductRow>>("rows").Excel();

  // ── IFileItemBuilder properties ─────────────────────────────────────

  [Test]
  public void Label_ReturnsAnchorLabel()
  {
    var builder = Item.Of<IEnumerable<ProductRow>>("Products").Excel();
    Assert.That(builder.Label, Is.EqualTo("Products"));
  }

  [Test]
  public void DefaultFilePattern_IsXlsxGlob()
  {
    Assert.That(NewBuilder().DefaultFilePattern, Is.EqualTo("*.xlsx"));
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

  // ── WithSheet argument validation ───────────────────────────────────

  [TestCase(null)]
  [TestCase("")]
  [TestCase("   ")]
  public void WithSheet_NullOrWhitespace_Throws(string? sheet)
  {
    Assert.That(
      () => NewBuilder().WithSheet(sheet!),
      Throws.TypeOf<ArgumentException>().With.Message.Contain("Sheet")
    );
  }

  [Test]
  public void WithSheet_ReturnsBuilderForChaining()
  {
    var builder = NewBuilder();
    Assert.That(builder.WithSheet("Sheet1"), Is.SameAs(builder));
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

  // ── Build() guards ──────────────────────────────────────────────────

  [Test]
  public void Build_WithoutAtPath_Throws_AndNamesTheItem()
  {
    Assert.That(
      () => Item.Of<IEnumerable<ProductRow>>("UnpathedProducts").Excel().Build(),
      Throws.TypeOf<InvalidOperationException>()
        .With.Message.Contain("'UnpathedProducts'")
        .And.Message.Contain("AtPath(...)")
    );
  }

  [Test]
  public void Build_WithoutWithSheet_Throws_AndNamesTheItem()
  {
    // AtPath is set but WithSheet is missing — Build() should still fail
    // because CreateAdapterForFile demands a sheet name.
    Assert.That(
      () => Item.Of<IEnumerable<ProductRow>>("MissingSheet")
        .Excel()
        .AtPath("/tmp/p.xlsx")
        .Build(),
      Throws.TypeOf<InvalidOperationException>()
        .With.Message.Contain("'MissingSheet'")
        .And.Message.Contain("WithSheet(...)")
    );
  }

  [Test]
  public void Build_WithAllRequiredCalls_ReturnsItemWithMatchingLabel()
  {
    var item = Item
      .Of<IEnumerable<ProductRow>>("Products")
      .Excel()
      .WithSheet("Sheet1")
      .AtPath("/tmp/products.xlsx")
      .Build();
    Assert.That(item.Label, Is.EqualTo("Products"));
  }

  // ── IFileItemBuilder.CreateAdapterForFile (used by the directory lift) ──

  [Test]
  public void CreateAdapterForFile_DoesNotRequireAtPath_ButDoesRequireWithSheet()
  {
    // The directory lift supplies the per-file path; sheet name is still
    // required since it's per-format configuration.
    var builder = NewBuilder();
    Assert.That(
      () => builder.CreateAdapterForFile("/tmp/per-file.xlsx"),
      Throws.TypeOf<InvalidOperationException>().With.Message.Contain("WithSheet(...)")
    );
  }

  [Test]
  public void CreateAdapterForFile_WithSheet_BuildsWithoutAtPath()
  {
    var adapter = NewBuilder()
      .WithSheet("Sheet1")
      .CreateAdapterForFile("/tmp/per-file.xlsx");
    Assert.That(adapter, Is.Not.Null);
  }

  [Test]
  public void CreateAdapterForFile_HonoursCustomNullValues()
  {
    var bareAdapter = NewBuilder().WithSheet("Sheet1").CreateAdapterForFile("/tmp/x.xlsx");
    var customAdapter = NewBuilder()
      .WithSheet("Sheet1")
      .WithNullValues(new[] { "", "NA" })
      .CreateAdapterForFile("/tmp/x.xlsx");
    Assert.That(customAdapter, Is.Not.SameAs(bareAdapter));
  }

  // ── WithResolver propagation ────────────────────────────────────────

  [Test]
  public void CreateAdapterForFile_WithCustomResolver_DispatchesThroughIt()
  {
    var resolver = new RecordingResolver();
    NewBuilder()
      .WithSheet("Sheet1")
      .WithResolver(resolver)
      .CreateAdapterForFile("/tmp/recorded.xlsx");
    Assert.That(resolver.LastResolved, Is.EqualTo("/tmp/recorded.xlsx"));
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
