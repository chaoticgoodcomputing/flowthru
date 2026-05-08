using System.Text;
using Flowthru.Extensions.Csv.Tests.Fixtures;
using Flowthru.Data.Storage;
using Flowthru.Data.Storage.Csv;
using Flowthru.Prelude;
using SysIO = System.IO;

namespace Flowthru.Extensions.Csv.Tests;

/// <summary>
/// End-to-end tests for the
/// <see cref="DirectoryStorageAdapter{T}"/> + per-file CSV adapter
/// composition that backs <c>ItemFactory.Directory.Csv&lt;TRow&gt;</c>.
/// Covers Load / Save / Exists / InspectShallow contracts under the
/// directory-spread shape.
/// </summary>
[TestFixture]
[Category("Csv")]
public class DirectoryCsvStorageAdapterTests
{
  private string _tempDir = null!;

  [SetUp]
  public void SetUp()
  {
    _tempDir = SysIO.Path.Combine(SysIO.Path.GetTempPath(), $"flowthru-dir-csv-{Guid.NewGuid():N}");
    SysIO.Directory.CreateDirectory(_tempDir);
  }

  [TearDown]
  public void TearDown()
  {
    if (SysIO.Directory.Exists(_tempDir))
    {
      try { SysIO.Directory.Delete(_tempDir, recursive: true); }
      catch { /* best effort */ }
    }
  }

  private static DirectoryStorageAdapter<IEnumerable<FlatRow>> Adapter(string dir)
  {
    var format = new CsvFormatSerializer<FlatRow>();
    var container = new EnumerableContainerAdapter<FlatRow>();
    return new DirectoryStorageAdapter<IEnumerable<FlatRow>>(
      directoryPath: dir,
      filePattern: "*.csv",
      perFileAdapter: path => new ComposedStorageAdapter<IEnumerable<FlatRow>, FlatRow>(
        new FileStorageMedium(path),
        format,
        container
      )
    );
  }

  // ── Constructor ──────────────────────────────────────────────────────

  [TestCase("")]
  [TestCase("   ")]
  public void Constructor_NullOrWhitespacePath_ThrowsArgumentException(string path)
  {
    Assert.Throws<ArgumentException>(() => Adapter(path));
  }

  // ── Exists ───────────────────────────────────────────────────────────

  [Test]
  public async Task Exists_MissingDirectory_ReturnsFalse()
  {
    var result = await Adapter("/nonexistent/path/xyz_flowthru").Exists().Run();
    Assert.That(((EffResult<bool>.Success)result).Value, Is.False);
  }

  [Test]
  public async Task Exists_DirectoryExistsEmpty_ReturnsTrue()
  {
    var result = await Adapter(_tempDir).Exists().Run();
    Assert.That(((EffResult<bool>.Success)result).Value, Is.True);
  }

  // ── Load ─────────────────────────────────────────────────────────────

  [Test]
  public async Task Load_EmptyDirectory_ReturnsEmptyDirectory()
  {
    var result = await Adapter(_tempDir).Load().Run();
    var loaded = ((EffResult<Directory<IEnumerable<FlatRow>>>.Success)result).Value;
    Assert.That(loaded.Count, Is.EqualTo(0));
  }

  [Test]
  public async Task Load_SingleFile_OneEntryWithAllRows()
  {
    WriteCsv("data.csv", "Id,Name,Value\n1,Alice,1.5\n2,Bob,2.5\n");

    var result = await Adapter(_tempDir).Load().Run();
    var loaded = ((EffResult<Directory<IEnumerable<FlatRow>>>.Success)result).Value;

    Assert.That(loaded.Count, Is.EqualTo(1));
    var rows = loaded.Values.Single().ToList();
    Assert.That(rows, Has.Count.EqualTo(2));
    Assert.That(rows[0].Id, Is.EqualTo(1));
    Assert.That(rows[1].Id, Is.EqualTo(2));
  }

  [Test]
  public async Task Load_MultipleFiles_PreservesPerFileBoundaries()
  {
    WriteCsv("c_chunk.csv", "Id,Name,Value\n3,Carol,3.5\n");
    WriteCsv("a_chunk.csv", "Id,Name,Value\n1,Alice,1.5\n");
    WriteCsv("b_chunk.csv", "Id,Name,Value\n2,Bob,2.5\n");

    var result = await Adapter(_tempDir).Load().Run();
    var loaded = ((EffResult<Directory<IEnumerable<FlatRow>>>.Success)result).Value;

    Assert.That(loaded.Count, Is.EqualTo(3));
    var byBaseName = loaded.ToDictionary(
      kvp => SysIO.Path.GetFileName(kvp.Key),
      kvp => kvp.Value.Single().Id
    );
    Assert.That(byBaseName["a_chunk.csv"], Is.EqualTo(1));
    Assert.That(byBaseName["b_chunk.csv"], Is.EqualTo(2));
    Assert.That(byBaseName["c_chunk.csv"], Is.EqualTo(3));
  }

  [Test]
  public async Task Load_NonCsvFilesIgnored()
  {
    WriteCsv("data.csv", "Id,Name,Value\n1,Alice,1.5\n");
    SysIO.File.WriteAllText(SysIO.Path.Combine(_tempDir, "readme.txt"), "ignored");

    var result = await Adapter(_tempDir).Load().Run();
    var loaded = ((EffResult<Directory<IEnumerable<FlatRow>>>.Success)result).Value;

    Assert.That(loaded.Count, Is.EqualTo(1));
  }

  // ── Save ─────────────────────────────────────────────────────────────

  [Test]
  public async Task Save_WritesOneFilePerEntry()
  {
    var dir = new Directory<IEnumerable<FlatRow>>(
      new Dictionary<string, IEnumerable<FlatRow>>
      {
        ["a.csv"] = new[] { new FlatRow { Id = 1, Name = "Alice", Value = 1.5 } },
        ["b.csv"] = new[] { new FlatRow { Id = 2, Name = "Bob", Value = 2.5 } },
      }
    );

    await Adapter(_tempDir).Save(dir).Run();

    var files = SysIO.Directory
      .EnumerateFiles(_tempDir, "*.csv")
      .Select(SysIO.Path.GetFileName)
      .OrderBy(n => n)
      .ToList();
    Assert.That(files, Is.EqualTo(new[] { "a.csv", "b.csv" }));
  }

  [Test]
  public async Task Save_DeletesExistingMatchingFiles_ForDeterministicReruns()
  {
    WriteCsv("stale.csv", "Id,Name,Value\n99,Stale,9.9\n");

    var dir = new Directory<IEnumerable<FlatRow>>(
      new Dictionary<string, IEnumerable<FlatRow>>
      {
        ["fresh.csv"] = new[] { new FlatRow { Id = 1, Name = "Fresh", Value = 1.0 } },
      }
    );

    await Adapter(_tempDir).Save(dir).Run();

    var files = SysIO.Directory
      .EnumerateFiles(_tempDir, "*.csv")
      .Select(SysIO.Path.GetFileName)
      .ToList();
    Assert.That(files, Is.EqualTo(new[] { "fresh.csv" }));
  }

  [Test]
  public async Task SaveLoad_RoundTrips()
  {
    var input = new Directory<IEnumerable<FlatRow>>(
      new Dictionary<string, IEnumerable<FlatRow>>
      {
        ["one.csv"] = new[] { new FlatRow { Id = 1, Name = "X", Value = 1.0 } },
        ["two.csv"] = new[]
        {
          new FlatRow { Id = 2, Name = "Y", Value = 2.0 },
          new FlatRow { Id = 3, Name = "Z", Value = 3.0 },
        },
      }
    );

    var adapter = Adapter(_tempDir);
    await adapter.Save(input).Run();
    var loadResult = await adapter.Load().Run();
    var loaded = ((EffResult<Directory<IEnumerable<FlatRow>>>.Success)loadResult).Value;

    Assert.That(loaded.Count, Is.EqualTo(2));
    var byBase = loaded.ToDictionary(
      kvp => SysIO.Path.GetFileName(kvp.Key),
      kvp => kvp.Value.ToList()
    );
    Assert.That(byBase["one.csv"], Has.Count.EqualTo(1));
    Assert.That(byBase["two.csv"], Has.Count.EqualTo(2));
  }

  // ── InspectShallow ───────────────────────────────────────────────────

  [Test]
  public async Task InspectShallow_MissingDirectory_ReturnsInvalidResult()
  {
    var result = await Adapter("/nonexistent/path/xyz_flowthru").InspectShallow(5).Run();
    var validation = ((EffResult<ValidationResult>.Success)result).Value;
    Assert.That(validation.IsValid, Is.False);
  }

  [Test]
  public async Task InspectShallow_ValidCsvFile_ReturnsValidResult()
  {
    WriteCsv("data.csv", "Id,Name,Value\n1,Alice,1.5\n");
    var result = await Adapter(_tempDir).InspectShallow(5).Run();
    var validation = ((EffResult<ValidationResult>.Success)result).Value;
    Assert.That(validation.IsValid, Is.True);
  }

  [Test]
  public async Task InspectShallow_SecondFileInvalid_ReturnsInvalidResult()
  {
    WriteCsv("a.csv", "Id,Name,Value\n1,Alice,1.5\n");
    WriteCsv("b.csv", "Id,Name,Value\nnot_a_number,Bob,2.5\n");
    var result = await Adapter(_tempDir).InspectShallow(5).Run();
    var validation = ((EffResult<ValidationResult>.Success)result).Value;
    Assert.That(validation.IsValid, Is.False);
  }

  // ── Helpers ──────────────────────────────────────────────────────────

  private void WriteCsv(string filename, string content) =>
    SysIO.File.WriteAllText(SysIO.Path.Combine(_tempDir, filename), content, Encoding.UTF8);
}
