using System.Xml.Serialization;
using Flowthru.Core.Data;
using Flowthru.Core.Data.Storage;
using Flowthru.Extensions.Xml.Tests.Fixtures;

namespace Flowthru.Extensions.Xml.Tests;

/// <summary>
/// Tests the <see cref="DirectoryStorageAdapter{T}"/> + <see cref="SingletonXmlStorageAdapter{T}"/>
/// composition that backs <c>ItemFactory.Enumerable.XmlDocuments&lt;T&gt;</c>. The previous
/// read-only <c>XmlDirectoryStorageAdapter</c> was retired in favour of the format-agnostic
/// directory adapter; tests here exercise the full load + save symmetry over the same
/// per-file factory the facade builds.
/// </summary>
[TestFixture]
[Category("Xml")]
public class XmlDirectoryStorageAdapterTests
{
  private string _tempDir = null!;
  private XmlSerializer _serializer = null!;

  [SetUp]
  public void SetUp()
  {
    _tempDir = Path.Combine(Path.GetTempPath(), $"flowthru_xml_{Guid.NewGuid():N}");
    System.IO.Directory.CreateDirectory(_tempDir);
    _serializer = new XmlSerializer(typeof(XmlTestItem));
  }

  [TearDown]
  public void TearDown()
  {
    if (System.IO.Directory.Exists(_tempDir))
      System.IO.Directory.Delete(_tempDir, recursive: true);
  }

  private static DirectoryStorageAdapter<XmlTestItem> Adapter(string dir) =>
    new(
      directoryPath: dir,
      filePattern: "*.xml",
      perFileAdapter: path => new SingletonXmlStorageAdapter<XmlTestItem>(path)
    );

  // ── Constructor ───────────────────────────────────────────────────────────

  [TestCase("")]
  [TestCase("   ")]
  public void Constructor_NullOrWhitespacePath_ThrowsArgumentException(string path)
  {
    Assert.Throws<ArgumentException>(() => Adapter(path));
  }

  // ── Exists ────────────────────────────────────────────────────────────────

  [Test]
  public async Task Exists_MissingDirectory_ReturnsFalse()
  {
    Assert.That(await Adapter("/nonexistent/path/xyz_flowthru").Exists().Run(), Is.False);
  }

  [Test]
  public async Task Exists_DirectoryExistsEmpty_ReturnsTrue()
  {
    Assert.That(await Adapter(_tempDir).Exists().Run(), Is.True);
  }

  // ── Load ──────────────────────────────────────────────────────────────────

  [Test]
  public async Task Load_EmptyDirectory_ReturnsEmptyDirectory()
  {
    var result = await Adapter(_tempDir).Load().Run();
    Assert.That(result.Count, Is.EqualTo(0));
  }

  [Test]
  public async Task Load_SingleFile_ReturnsOneEntry()
  {
    WriteXml("item.xml", new XmlTestItem { Name = "Alpha", Count = 42 });

    var result = await Adapter(_tempDir).Load().Run();

    Assert.That(result.Count, Is.EqualTo(1));
    var (path, item) = result.Single();
    Assert.That(Path.GetFileName(path), Is.EqualTo("item.xml"));
    Assert.That(item.Name, Is.EqualTo("Alpha"));
    Assert.That(item.Count, Is.EqualTo(42));
  }

  [Test]
  public async Task Load_MultipleFiles_PreservesPerFileBoundaries()
  {
    WriteXml("c.xml", new XmlTestItem { Name = "Gamma", Count = 3 });
    WriteXml("a.xml", new XmlTestItem { Name = "Alpha", Count = 1 });
    WriteXml("b.xml", new XmlTestItem { Name = "Beta", Count = 2 });

    var result = await Adapter(_tempDir).Load().Run();

    Assert.That(result.Count, Is.EqualTo(3));
    var byBase = result.ToDictionary(kvp => Path.GetFileName(kvp.Key), kvp => kvp.Value.Name);
    Assert.That(byBase["a.xml"], Is.EqualTo("Alpha"));
    Assert.That(byBase["b.xml"], Is.EqualTo("Beta"));
    Assert.That(byBase["c.xml"], Is.EqualTo("Gamma"));
  }

  [Test]
  public async Task Load_NonXmlFilesIgnored()
  {
    WriteXml("item.xml", new XmlTestItem { Name = "Alpha", Count = 1 });
    File.WriteAllText(Path.Combine(_tempDir, "readme.txt"), "ignored");

    var result = await Adapter(_tempDir).Load().Run();

    Assert.That(result.Count, Is.EqualTo(1));
  }

  // ── Save ──────────────────────────────────────────────────────────────────

  [Test]
  public async Task Save_WritesOneFilePerEntry()
  {
    var dir = new Directory<XmlTestItem>(new Dictionary<string, XmlTestItem>
    {
      ["alpha.xml"] = new XmlTestItem { Name = "Alpha", Count = 1 },
      ["beta.xml"] = new XmlTestItem { Name = "Beta", Count = 2 },
    });

    await Adapter(_tempDir).Save(dir).Run();

    var files = System.IO.Directory.EnumerateFiles(_tempDir, "*.xml").Select(Path.GetFileName).OrderBy(n => n).ToList();
    Assert.That(files, Is.EqualTo(new[] { "alpha.xml", "beta.xml" }));
  }

  [Test]
  public async Task Save_DeletesExistingMatchingFiles_ForDeterministicReruns()
  {
    WriteXml("stale.xml", new XmlTestItem { Name = "Stale", Count = 99 });

    var dir = new Directory<XmlTestItem>(new Dictionary<string, XmlTestItem>
    {
      ["fresh.xml"] = new XmlTestItem { Name = "Fresh", Count = 1 },
    });

    await Adapter(_tempDir).Save(dir).Run();

    var files = System.IO.Directory.EnumerateFiles(_tempDir, "*.xml").Select(Path.GetFileName).ToList();
    Assert.That(files, Is.EqualTo(new[] { "fresh.xml" }));
  }

  [Test]
  public async Task SaveLoad_RoundTrips()
  {
    var input = new Directory<XmlTestItem>(new Dictionary<string, XmlTestItem>
    {
      ["a.xml"] = new XmlTestItem { Name = "Alpha", Count = 1 },
      ["b.xml"] = new XmlTestItem { Name = "Beta", Count = 2 },
    });

    var adapter = Adapter(_tempDir);
    await adapter.Save(input).Run();
    var loaded = await adapter.Load().Run();

    Assert.That(loaded.Count, Is.EqualTo(2));
    var byBase = loaded.ToDictionary(kvp => Path.GetFileName(kvp.Key), kvp => kvp.Value.Name);
    Assert.That(byBase["a.xml"], Is.EqualTo("Alpha"));
    Assert.That(byBase["b.xml"], Is.EqualTo("Beta"));
  }

  // ── InspectShallow ────────────────────────────────────────────────────────

  [Test]
  public async Task InspectShallow_MissingDirectory_ReturnsInvalidResult()
  {
    var result = await Adapter("/nonexistent/path/xyz_flowthru").InspectShallow(5).Run();
    Assert.That(result.IsValid, Is.False);
  }

  [Test]
  public async Task InspectShallow_ValidFiles_ReturnsValidResult()
  {
    WriteXml("a.xml", new XmlTestItem { Name = "Alpha", Count = 1 });
    WriteXml("b.xml", new XmlTestItem { Name = "Beta", Count = 2 });
    var result = await Adapter(_tempDir).InspectShallow(5).Run();
    Assert.That(result.IsValid, Is.True);
  }

  [Test]
  public async Task InspectShallow_OneInvalidFile_ReturnsInvalidResult()
  {
    WriteXml("a.xml", new XmlTestItem { Name = "Alpha", Count = 1 });
    WriteInvalidXml("b.xml");
    var result = await Adapter(_tempDir).InspectShallow(5).Run();
    Assert.That(result.IsValid, Is.False);
  }

  // ── InspectDeep ───────────────────────────────────────────────────────────

  [Test]
  public async Task InspectDeep_ValidFiles_ReturnsValidResult()
  {
    WriteXml("a.xml", new XmlTestItem { Name = "Alpha", Count = 1 });
    WriteXml("b.xml", new XmlTestItem { Name = "Beta", Count = 2 });
    var result = await Adapter(_tempDir).InspectDeep().Run();
    Assert.That(result.IsValid, Is.True);
  }

  // ── Helpers ───────────────────────────────────────────────────────────────

  private void WriteXml(string filename, XmlTestItem item)
  {
    using var writer = new StreamWriter(Path.Combine(_tempDir, filename));
    _serializer.Serialize(writer, item);
  }

  private void WriteInvalidXml(string filename) =>
    File.WriteAllText(Path.Combine(_tempDir, filename), "<<<not valid xml>>>");
}
