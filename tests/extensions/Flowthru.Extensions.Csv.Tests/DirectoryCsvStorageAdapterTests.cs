using System.Text;
using Flowthru.Core.Data.Storage;
using Flowthru.Extensions.Csv.Tests.Fixtures;

namespace Flowthru.Extensions.Csv.Tests;

[TestFixture]
[Category("Csv")]
public class DirectoryCsvStorageAdapterTests
{
  private string _tempDir = null!;

  [SetUp]
  public void SetUp()
  {
    _tempDir = Path.Combine(Path.GetTempPath(), $"flowthru_csv_{Guid.NewGuid():N}");
    Directory.CreateDirectory(_tempDir);
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
    Assert.Throws<ArgumentException>(() => new DirectoryCsvStorageAdapter<FlatRow>(path));
  }

  // ── Traits ────────────────────────────────────────────────────────────────

  [Test]
  public void Traits_CanWrite_IsFalse_CanStream_IsTrue()
  {
    var adapter = new DirectoryCsvStorageAdapter<FlatRow>(_tempDir);
    Assert.That(adapter.Traits.CanWrite, Is.False);
    Assert.That(adapter.Traits.CanStream, Is.True);
  }

  // ── Exists ────────────────────────────────────────────────────────────────

  [Test]
  public async Task Exists_MissingDirectory_ReturnsFalse()
  {
    var adapter = new DirectoryCsvStorageAdapter<FlatRow>("/nonexistent/path/xyz_flowthru");
    Assert.That(await adapter.Exists().Run(), Is.False);
  }

  [Test]
  public async Task Exists_EmptyDirectory_ReturnsFalse()
  {
    // Directory exists but has no *.csv files.
    var adapter = new DirectoryCsvStorageAdapter<FlatRow>(_tempDir);
    Assert.That(await adapter.Exists().Run(), Is.False);
  }

  [Test]
  public async Task Exists_DirectoryWithCsvFiles_ReturnsTrue()
  {
    WriteCsv("a.csv", "Id,Name,Value\n1,Alice,1.5\n");
    var adapter = new DirectoryCsvStorageAdapter<FlatRow>(_tempDir);
    Assert.That(await adapter.Exists().Run(), Is.True);
  }

  // ── Load ──────────────────────────────────────────────────────────────────

  [Test]
  public async Task Load_EmptyDirectory_ReturnsEmptySequence()
  {
    var adapter = new DirectoryCsvStorageAdapter<FlatRow>(_tempDir);
    var result = (await adapter.Load().Run()).ToList();
    Assert.That(result, Is.Empty);
  }

  [Test]
  public async Task Load_SingleFile_ReturnsAllRows()
  {
    WriteCsv("data.csv", "Id,Name,Value\n1,Alice,1.5\n2,Bob,2.5\n");
    var adapter = new DirectoryCsvStorageAdapter<FlatRow>(_tempDir);

    var result = (await adapter.Load().Run()).ToList();

    Assert.That(result, Has.Count.EqualTo(2));
    Assert.That(result[0].Id, Is.EqualTo(1));
    Assert.That(result[0].Name, Is.EqualTo("Alice"));
    Assert.That(result[1].Id, Is.EqualTo(2));
  }

  [Test]
  public async Task Load_MultipleFiles_ConcatenatesInLexicographicOrder()
  {
    // Files written out of order; rows should appear in filename-sorted order.
    WriteCsv("c_chunk.csv", "Id,Name,Value\n3,Carol,3.5\n");
    WriteCsv("a_chunk.csv", "Id,Name,Value\n1,Alice,1.5\n");
    WriteCsv("b_chunk.csv", "Id,Name,Value\n2,Bob,2.5\n");
    var adapter = new DirectoryCsvStorageAdapter<FlatRow>(_tempDir);

    var result = (await adapter.Load().Run()).ToList();

    Assert.That(result, Has.Count.EqualTo(3));
    Assert.That(result[0].Id, Is.EqualTo(1)); // a_chunk
    Assert.That(result[1].Id, Is.EqualTo(2)); // b_chunk
    Assert.That(result[2].Id, Is.EqualTo(3)); // c_chunk
  }

  [Test]
  public async Task Load_NonCsvFilesIgnored()
  {
    WriteCsv("data.csv", "Id,Name,Value\n1,Alice,1.5\n");
    File.WriteAllText(Path.Combine(_tempDir, "readme.txt"), "ignored");
    var adapter = new DirectoryCsvStorageAdapter<FlatRow>(_tempDir);

    var result = (await adapter.Load().Run()).ToList();

    Assert.That(result, Has.Count.EqualTo(1));
  }

  // ── Save ──────────────────────────────────────────────────────────────────

  [Test]
  public async Task Save_AlwaysThrowsNotSupportedException()
  {
    var adapter = new DirectoryCsvStorageAdapter<FlatRow>(_tempDir);
    await Assert.ThatAsync(
      () => adapter.Save(Enumerable.Empty<FlatRow>()).Run().AsTask(),
      Throws.TypeOf<NotSupportedException>()
    );
  }

  // ── InspectShallow ────────────────────────────────────────────────────────

  [Test]
  public async Task InspectShallow_MissingDirectory_ReturnsInvalidResult()
  {
    var adapter = new DirectoryCsvStorageAdapter<FlatRow>("/nonexistent/path/xyz_flowthru");
    var result = await adapter.InspectShallow(5).Run();
    Assert.That(result.IsValid, Is.False);
  }

  [Test]
  public async Task InspectShallow_EmptyDirectory_ReturnsInvalidResult()
  {
    var adapter = new DirectoryCsvStorageAdapter<FlatRow>(_tempDir);
    var result = await adapter.InspectShallow(5).Run();
    Assert.That(result.IsValid, Is.False);
  }

  [Test]
  public async Task InspectShallow_ValidCsvFile_ReturnsValidResult()
  {
    WriteCsv("data.csv", "Id,Name,Value\n1,Alice,1.5\n");
    var adapter = new DirectoryCsvStorageAdapter<FlatRow>(_tempDir);
    var result = await adapter.InspectShallow(5).Run();
    Assert.That(result.IsValid, Is.True);
  }

  // ── Helpers ───────────────────────────────────────────────────────────────

  private void WriteCsv(string filename, string content) =>
    File.WriteAllText(Path.Combine(_tempDir, filename), content, Encoding.UTF8);
}
