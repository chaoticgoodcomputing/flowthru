using System.Xml.Serialization;
using Flowthru.Data.Storage;
using Flowthru.Data.Storage.Xml;
using Flowthru.Extensions.Xml.Tests.Fixtures;
using Flowthru.Prelude;

namespace Flowthru.Extensions.Xml.Tests;

/// <summary>
/// Direct exercises of <see cref="SingletonXmlAdapter{T}"/> over a
/// temporary directory. Validates round-trip, atomic-write semantics,
/// and the inspect-side reporting of missing files / parse errors.
/// </summary>
[TestFixture]
[Category("Xml")]
public class SingletonXmlAdapterTests
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
    var adapter = new SingletonXmlAdapter<XmlTestItem>(_filePath);
    var original = new XmlTestItem { Name = "RoundTrip", Count = 99 };

    await adapter.Save(original).Run();
    var result = await adapter.Load().Run();

    var loaded = ((EffResult<XmlTestItem>.Success)result).Value;
    Assert.That(loaded.Name, Is.EqualTo("RoundTrip"));
    Assert.That(loaded.Count, Is.EqualTo(99));
  }

  [Test]
  public async Task Save_OverwritesExistingFile()
  {
    var adapter = new SingletonXmlAdapter<XmlTestItem>(_filePath);
    await adapter.Save(new XmlTestItem { Name = "First", Count = 1 }).Run();
    await adapter.Save(new XmlTestItem { Name = "Second", Count = 2 }).Run();

    var result = await adapter.Load().Run();
    var loaded = ((EffResult<XmlTestItem>.Success)result).Value;
    Assert.That(loaded.Name, Is.EqualTo("Second"));
    Assert.That(loaded.Count, Is.EqualTo(2));
  }

  [Test]
  public async Task Save_CreatesParentDirectoryIfMissing()
  {
    var nestedPath = Path.Combine(_tempDir, "nested", "deeper", "item.xml");
    Assert.That(Directory.Exists(Path.GetDirectoryName(nestedPath)!), Is.False,
      "Precondition: parent directory must not exist yet.");

    var adapter = new SingletonXmlAdapter<XmlTestItem>(nestedPath);
    var result = await adapter.Save(new XmlTestItem { Name = "Deep", Count = 5 }).Run();

    Assert.That(result, Is.InstanceOf<EffResult<FlowUnit>.Success>());
    Assert.That(File.Exists(nestedPath), Is.True);
  }

  // ── InspectShallow ────────────────────────────────────────────────────────

  [Test]
  public async Task InspectShallow_FileExists_ReturnsValidResult()
  {
    WriteXml(new XmlTestItem { Name = "Alpha", Count = 1 });
    var adapter = new SingletonXmlAdapter<XmlTestItem>(_filePath);
    var result = await adapter.InspectShallow(1).Run();
    var validation = ((EffResult<ValidationResult>.Success)result).Value;
    Assert.That(validation.IsValid, Is.True);
  }

  [Test]
  public async Task InspectShallow_MissingFile_ReturnsInvalidResult()
  {
    var adapter = new SingletonXmlAdapter<XmlTestItem>(
      Path.Combine(_tempDir, "missing.xml")
    );
    var result = await adapter.InspectShallow(1).Run();
    var validation = ((EffResult<ValidationResult>.Success)result).Value;
    Assert.That(validation.IsValid, Is.False);
    Assert.That(validation.Errors, Has.Some.Matches<ValidationError>(
      e => e.ErrorType == ValidationErrorType.NotFound));
  }

  [Test]
  public async Task InspectShallow_MalformedFile_ReturnsDeserializationError()
  {
    File.WriteAllText(_filePath, "<<<not valid xml>>>");
    var adapter = new SingletonXmlAdapter<XmlTestItem>(_filePath);

    var result = await adapter.InspectShallow(1).Run();
    var validation = ((EffResult<ValidationResult>.Success)result).Value;
    Assert.That(validation.IsValid, Is.False);
    Assert.That(validation.Errors, Has.Some.Matches<ValidationError>(
      e => e.ErrorType == ValidationErrorType.DeserializationError));
  }

  // ── InspectTarget ─────────────────────────────────────────────────────────

  [Test]
  public async Task InspectTarget_WritableDirectory_ReturnsSuccess()
  {
    var adapter = new SingletonXmlAdapter<XmlTestItem>(_filePath);
    var result = await adapter.InspectTarget().Run();
    var validation = ((EffResult<ValidationResult>.Success)result).Value;
    Assert.That(validation.IsValid, Is.True);
  }

  // ── Helpers ───────────────────────────────────────────────────────────────

  private void WriteXml(XmlTestItem item)
  {
    using var writer = new StreamWriter(_filePath);
    _serializer.Serialize(writer, item);
  }
}
