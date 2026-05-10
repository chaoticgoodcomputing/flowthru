using Flowthru.Data.Catalog;
using Flowthru.Extensions.Xml.Tests.Fixtures;

namespace Flowthru.Extensions.Xml.Tests;

/// <summary>
/// Pins the <see cref="XmlBuilder{T}"/> validation surface and the
/// <c>IFileItemBuilder</c> contract. XML is document-mode (one document
/// per file); the builder has no per-format options beyond the path.
/// </summary>
[TestFixture]
[Category("Xml")]
public class XmlBuilderTests
{
  private static XmlBuilder<XmlTestItem> NewBuilder() =>
    Item.Of<XmlTestItem>("doc").Xml();

  // ── IFileItemBuilder properties ─────────────────────────────────────

  [Test]
  public void Label_ReturnsAnchorLabel()
  {
    var builder = Item.Of<XmlTestItem>("Manifest").Xml();
    Assert.That(builder.Label, Is.EqualTo("Manifest"));
  }

  [Test]
  public void DefaultFilePattern_IsXmlGlob()
  {
    Assert.That(NewBuilder().DefaultFilePattern, Is.EqualTo("*.xml"));
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
    Assert.That(builder.AtPath("/tmp/x.xml"), Is.SameAs(builder));
  }

  // ── Build() guards ──────────────────────────────────────────────────

  [Test]
  public void Build_WithoutAtPath_Throws_AndNamesTheItem()
  {
    Assert.That(
      () => Item.Of<XmlTestItem>("UnpathedDoc").Xml().Build(),
      Throws.TypeOf<InvalidOperationException>()
        .With.Message.Contain("'UnpathedDoc'")
        .And.Message.Contain("AtPath(...)")
    );
  }

  [Test]
  public void Build_WithAtPath_ReturnsItemWithMatchingLabel()
  {
    var item = Item.Of<XmlTestItem>("Doc").Xml().AtPath("/tmp/d.xml").Build();
    Assert.That(item.Label, Is.EqualTo("Doc"));
  }

  // ── IFileItemBuilder.CreateAdapterForFile ───────────────────────────

  [Test]
  public void CreateAdapterForFile_DoesNotRequireAtPath()
  {
    // Used by the directory lift to construct per-entry adapters.
    var adapter = NewBuilder().CreateAdapterForFile("/tmp/per-file.xml");
    Assert.That(adapter, Is.Not.Null);
  }
}
