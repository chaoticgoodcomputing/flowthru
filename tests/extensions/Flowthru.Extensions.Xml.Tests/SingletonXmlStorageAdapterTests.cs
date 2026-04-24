using System.Xml.Serialization;
using Flowthru.Core.Data.Storage;
using Flowthru.Extensions.Xml.Tests.Fixtures;

namespace Flowthru.Extensions.Xml.Tests;

[TestFixture]
[Category("Xml")]
public class SingletonXmlStorageAdapterTests
{
  private string _tempDir = null!;
  private string _filePath = null!;
  private XmlSerializer _serializer = null!;

  [SetUp]
  public void SetUp()
  {
    _tempDir = Path.Combine(Path.GetTempPath(), $"flowthru_xml_singleton_{Guid.NewGuid():N}");
    Directory.CreateDirectory(_tempDir);
    _filePath = Path.Combine(_tempDir, "item.xml");
    _serializer = new XmlSerializer(typeof(XmlTestItem));
  }

  [TearDown]
  public void TearDown()
  {
    if (Directory.Exists(_tempDir))
      Directory.Delete(_tempDir, recursive: true);
  }

  // ── Save + Load ───────────────────────────────────────────────────────────

  [Test]
  public async Task SaveAndLoad_RoundTrip_PreservesData()
  {
    var adapter = new SingletonXmlStorageAdapter<XmlTestItem>(_filePath);
    var original = new XmlTestItem { Name = "RoundTrip", Count = 99 };

    await adapter.Save(original).Run();
    var result = await adapter.Load().Run();

    Assert.That(result.Name, Is.EqualTo("RoundTrip"));
    Assert.That(result.Count, Is.EqualTo(99));
  }

  // ── InspectShallow ────────────────────────────────────────────────────────

  [Test]
  public async Task InspectShallow_FileExists_ReturnsValidResult()
  {
    WriteXml(new XmlTestItem { Name = "Alpha", Count = 1 });
    var adapter = new SingletonXmlStorageAdapter<XmlTestItem>(_filePath);
    var result = await adapter.InspectShallow(1).Run();
    Assert.That(result.IsValid, Is.True);
  }

  [Test]
  public async Task InspectShallow_MissingFile_ReturnsInvalidResult()
  {
    var adapter = new SingletonXmlStorageAdapter<XmlTestItem>(
      Path.Combine(_tempDir, "missing.xml")
    );
    var result = await adapter.InspectShallow(1).Run();
    Assert.That(result.IsValid, Is.False);
  }

  // ── InspectTarget ─────────────────────────────────────────────────────────

  [Test]
  public async Task InspectTarget_WritableDirectory_ReturnsSuccess()
  {
    var adapter = new SingletonXmlStorageAdapter<XmlTestItem>(_filePath);
    var result = await adapter.InspectTarget().Run();
    Assert.That(result.IsValid, Is.True);
  }

  [Test]
  public async Task InspectTarget_MissingButCreatableDirectory_ReturnsSuccess()
  {
    // Parent dir doesn't exist yet, but its grandparent (_tempDir) does.
    // Save() would create it; InspectTarget should not pre-emptively fail.
    var deepPath = Path.Combine(_tempDir, "sub", "nested", "item.xml");
    var adapter = new SingletonXmlStorageAdapter<XmlTestItem>(deepPath);
    var result = await adapter.InspectTarget().Run();
    Assert.That(result.IsValid, Is.True);
  }

  // ── Helpers ───────────────────────────────────────────────────────────────

  private void WriteXml(XmlTestItem item)
  {
    using var writer = new StreamWriter(_filePath);
    _serializer.Serialize(writer, item);
  }
}
