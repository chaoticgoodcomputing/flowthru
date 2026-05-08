using Flowthru.Data.Catalog;
using Flowthru.Data.Storage;
using Flowthru.Extensions.Xml.Tests.Fixtures;
using Flowthru.Prelude;
using SysIO = System.IO;

namespace Flowthru.Extensions.Xml.Tests;

/// <summary>
/// Smart-constructor smoke tests for the
/// <see cref="XmlItemFactoryExtensions"/> extension methods —
/// verifies the user-facing surface
/// <c>ItemFactory.Singleton.Xml&lt;T&gt;(...)</c> and
/// <c>ItemFactory.Directory.Xml&lt;T&gt;(...)</c> resolves to working
/// <see cref="IItem{T}"/> instances and round-trips through the
/// adapter.
/// </summary>
[TestFixture]
[Category("Xml")]
public class XmlItemFactoryExtensionsTests
{
  private string _root = null!;

  [SetUp]
  public void SetUp()
  {
    _root = SysIO.Path.Combine(SysIO.Path.GetTempPath(), $"flowthru-xml-ife-{Guid.NewGuid():N}");
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

  // ── Singleton.Xml<T> ────────────────────────────────────────────────

  [Test]
  public async Task SingletonXml_RoundTripsThroughAdapter()
  {
    var path = SysIO.Path.Combine(_root, "doc.xml");
    var item = ItemFactory.Singleton.Xml<XmlTestItem>("doc", path);

    var input = new XmlTestItem { Name = "Hello", Count = 7 };
    await item.Save(input).Run();
    var result = await item.Load().Run();

    var loaded = ((EffResult<XmlTestItem>.Success)result).Value;
    Assert.That(loaded.Name, Is.EqualTo("Hello"));
    Assert.That(loaded.Count, Is.EqualTo(7));
  }

  [Test]
  public void SingletonXml_AssignsLabel()
  {
    var item = ItemFactory.Singleton.Xml<XmlTestItem>(
      "configured-label",
      SysIO.Path.Combine(_root, "x.xml")
    );
    Assert.That(item.Label, Is.EqualTo("configured-label"));
  }

  // ── Directory.Xml<T> ────────────────────────────────────────────────

  [Test]
  public async Task DirectoryXml_RoundTripsThroughAdapter()
  {
    var item = ItemFactory.Directory.Xml<XmlTestItem>("docs", _root);

    var input = new Directory<XmlTestItem>(new Dictionary<string, XmlTestItem>
    {
      ["a.xml"] = new XmlTestItem { Name = "Alpha", Count = 1 },
      ["b.xml"] = new XmlTestItem { Name = "Beta", Count = 2 },
    });

    await item.Save(input).Run();
    var loadResult = await item.Load().Run();
    var loaded = ((EffResult<Directory<XmlTestItem>>.Success)loadResult).Value;

    Assert.That(loaded.Count, Is.EqualTo(2));
    var byBase = loaded.ToDictionary(
      kvp => SysIO.Path.GetFileName(kvp.Key),
      kvp => kvp.Value.Name
    );
    Assert.That(byBase["a.xml"], Is.EqualTo("Alpha"));
    Assert.That(byBase["b.xml"], Is.EqualTo("Beta"));
  }

  [Test]
  public async Task DirectoryXml_RespectsCustomFilePattern()
  {
    var item = ItemFactory.Directory.Xml<XmlTestItem>(
      "docs",
      _root,
      filePattern: "*.config.xml"
    );

    var input = new Directory<XmlTestItem>(new Dictionary<string, XmlTestItem>
    {
      ["alpha.config.xml"] = new XmlTestItem { Name = "Alpha", Count = 1 },
    });
    await item.Save(input).Run();

    // A non-matching pre-existing file should NOT be deleted.
    SysIO.File.WriteAllText(SysIO.Path.Combine(_root, "other.xml"), "kept");

    await item.Save(input).Run();

    Assert.That(SysIO.File.Exists(SysIO.Path.Combine(_root, "other.xml")), Is.True,
      "Non-matching files should survive the Save's hard-delete pass.");
  }
}
