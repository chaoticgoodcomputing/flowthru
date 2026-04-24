using System.Xml.Serialization;
using Flowthru.Core.Data;
using Flowthru.Core.Data.Storage;
using Flowthru.Extensions.Xml.Tests.Fixtures;

namespace Flowthru.Extensions.Xml.Tests;

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
    Directory.CreateDirectory(_tempDir);
    _serializer = new XmlSerializer(typeof(XmlTestItem));
  }

  [TearDown]
  public void TearDown()
  {
    if (Directory.Exists(_tempDir))
      Directory.Delete(_tempDir, recursive: true);
  }

  // ── Constructor ───────────────────────────────────────────────────────────

  [TestCase("")]
  [TestCase("   ")]
  public void Constructor_NullOrWhitespacePath_ThrowsArgumentException(string path)
  {
    Assert.Throws<ArgumentException>(() => new XmlDirectoryStorageAdapter<XmlTestItem>(path));
  }

  // ── Traits ────────────────────────────────────────────────────────────────

  [Test]
  public void Traits_CanWrite_IsFalse()
  {
    var adapter = new XmlDirectoryStorageAdapter<XmlTestItem>(_tempDir);
    Assert.That(adapter.Traits.CanWrite, Is.False);
  }

  // ── Exists ────────────────────────────────────────────────────────────────

  [Test]
  public async Task Exists_MissingDirectory_ReturnsFalse()
  {
    var adapter = new XmlDirectoryStorageAdapter<XmlTestItem>("/nonexistent/path/xyz_flowthru");
    Assert.That(await adapter.Exists().Run(), Is.False);
  }

  [Test]
  public async Task Exists_EmptyDirectory_ReturnsFalse()
  {
    var adapter = new XmlDirectoryStorageAdapter<XmlTestItem>(_tempDir);
    Assert.That(await adapter.Exists().Run(), Is.False);
  }

  [Test]
  public async Task Exists_DirectoryWithXmlFiles_ReturnsTrue()
  {
    WriteXml("a.xml", new XmlTestItem { Name = "Alpha", Count = 1 });
    var adapter = new XmlDirectoryStorageAdapter<XmlTestItem>(_tempDir);
    Assert.That(await adapter.Exists().Run(), Is.True);
  }

  // ── Load ──────────────────────────────────────────────────────────────────

  [Test]
  public async Task Load_EmptyDirectory_ReturnsEmptySequence()
  {
    var adapter = new XmlDirectoryStorageAdapter<XmlTestItem>(_tempDir);
    var result = (await adapter.Load().Run()).ToList();
    Assert.That(result, Is.Empty);
  }

  [Test]
  public async Task Load_SingleFile_ReturnsDocument()
  {
    WriteXml("item.xml", new XmlTestItem { Name = "Alpha", Count = 42 });
    var adapter = new XmlDirectoryStorageAdapter<XmlTestItem>(_tempDir);

    var result = (await adapter.Load().Run()).ToList();

    Assert.That(result, Has.Count.EqualTo(1));
    Assert.That(result[0].FileName, Is.EqualTo("item.xml"));
    Assert.That(result[0].Document.Name, Is.EqualTo("Alpha"));
    Assert.That(result[0].Document.Count, Is.EqualTo(42));
  }

  [Test]
  public async Task Load_MultipleFiles_ConcatenatesInLexicographicOrder()
  {
    WriteXml("c.xml", new XmlTestItem { Name = "Gamma", Count = 3 });
    WriteXml("a.xml", new XmlTestItem { Name = "Alpha", Count = 1 });
    WriteXml("b.xml", new XmlTestItem { Name = "Beta", Count = 2 });
    var adapter = new XmlDirectoryStorageAdapter<XmlTestItem>(_tempDir);

    var result = (await adapter.Load().Run()).ToList();

    Assert.That(result, Has.Count.EqualTo(3));
    Assert.That(result[0].FileName, Is.EqualTo("a.xml"));
    Assert.That(result[1].FileName, Is.EqualTo("b.xml"));
    Assert.That(result[2].FileName, Is.EqualTo("c.xml"));
  }

  [Test]
  public async Task Load_NonXmlFilesIgnored()
  {
    WriteXml("item.xml", new XmlTestItem { Name = "Alpha", Count = 1 });
    File.WriteAllText(Path.Combine(_tempDir, "readme.txt"), "ignored");
    var adapter = new XmlDirectoryStorageAdapter<XmlTestItem>(_tempDir);

    var result = (await adapter.Load().Run()).ToList();

    Assert.That(result, Has.Count.EqualTo(1));
  }

  // ── Save ──────────────────────────────────────────────────────────────────

  [Test]
  public async Task Save_AlwaysThrowsNotSupportedException()
  {
    var adapter = new XmlDirectoryStorageAdapter<XmlTestItem>(_tempDir);
    await Assert.ThatAsync(
      () => adapter.Save(Enumerable.Empty<XmlDocument<XmlTestItem>>()).Run().AsTask(),
      Throws.TypeOf<NotSupportedException>()
    );
  }

  // ── InspectShallow ────────────────────────────────────────────────────────

  [Test]
  public async Task InspectShallow_MissingDirectory_ReturnsInvalidResult()
  {
    var adapter = new XmlDirectoryStorageAdapter<XmlTestItem>("/nonexistent/path/xyz_flowthru");
    var result = await adapter.InspectShallow(5).Run();
    Assert.That(result.IsValid, Is.False);
  }

  [Test]
  public async Task InspectShallow_EmptyDirectory_ReturnsInvalidResult()
  {
    var adapter = new XmlDirectoryStorageAdapter<XmlTestItem>(_tempDir);
    var result = await adapter.InspectShallow(5).Run();
    Assert.That(result.IsValid, Is.False);
  }

  [Test]
  public async Task InspectShallow_ValidFiles_ReturnsValidResult()
  {
    WriteXml("a.xml", new XmlTestItem { Name = "Alpha", Count = 1 });
    WriteXml("b.xml", new XmlTestItem { Name = "Beta", Count = 2 });
    var adapter = new XmlDirectoryStorageAdapter<XmlTestItem>(_tempDir);
    var result = await adapter.InspectShallow(5).Run();
    Assert.That(result.IsValid, Is.True);
  }

  [Test]
  public async Task InspectShallow_OneInvalidFile_ReturnsInvalidResult()
  {
    WriteXml("a.xml", new XmlTestItem { Name = "Alpha", Count = 1 });
    WriteInvalidXml("b.xml");
    var adapter = new XmlDirectoryStorageAdapter<XmlTestItem>(_tempDir);
    var result = await adapter.InspectShallow(5).Run();
    Assert.That(result.IsValid, Is.False);
  }

  // ── InspectDeep ───────────────────────────────────────────────────────────

  [Test]
  public async Task InspectDeep_ValidFiles_ReturnsValidResult()
  {
    WriteXml("a.xml", new XmlTestItem { Name = "Alpha", Count = 1 });
    WriteXml("b.xml", new XmlTestItem { Name = "Beta", Count = 2 });
    var adapter = new XmlDirectoryStorageAdapter<XmlTestItem>(_tempDir);
    var result = await adapter.InspectDeep().Run();
    Assert.That(result.IsValid, Is.True);
  }

  // ── InspectTarget ─────────────────────────────────────────────────────────

  [Test]
  public async Task InspectTarget_AlwaysReturnsSuccess()
  {
    // XmlDirectoryStorageAdapter is read-only — InspectTarget is a no-op that always succeeds.
    var adapter = new XmlDirectoryStorageAdapter<XmlTestItem>(_tempDir);
    var result = await adapter.InspectTarget().Run();
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
