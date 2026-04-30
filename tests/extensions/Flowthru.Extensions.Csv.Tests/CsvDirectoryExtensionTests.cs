using Flowthru.Core.Data;
using Flowthru.Tests.Kits.Schemas;

namespace Flowthru.Extensions.Csv.Tests;

/// <summary>
/// Tests for the <see cref="CsvItemExtensions.CsvDirectory{TRow}"/> factory extension —
/// the residual overload that wasn't reached by the full-file CSV conformance kit (which
/// uses <c>CsvFormatSerializer</c> directly rather than the directory-spread adapter).
/// </summary>
[TestFixture]
public class CsvDirectoryExtensionTests
{
  private string _tempDir = string.Empty;

  [SetUp]
  public void SetUp()
  {
    _tempDir = Path.Combine(Path.GetTempPath(), $"flowthru-csv-directory-{Guid.NewGuid():N}");
    Directory.CreateDirectory(_tempDir);
  }

  [TearDown]
  public void TearDown()
  {
    if (Directory.Exists(_tempDir))
    {
      Directory.Delete(_tempDir, recursive: true);
    }
  }

  [Test]
  public void CsvDirectory_BuildsEntryWithExpectedLabel()
  {
    var entry = ItemFactory.Enumerable.CsvDirectory<TraditionalSchema>(
      label: "shuttles",
      directoryPath: _tempDir
    );

    Assert.That(entry, Is.Not.Null);
    Assert.That(entry.Label, Is.EqualTo("shuttles"));
  }

  [Test]
  public void CsvDirectory_CustomNullValues_BuildsWithoutThrowing()
  {
    var entry = ItemFactory.Enumerable.CsvDirectory<TraditionalSchema>(
      label: "shuttles",
      directoryPath: _tempDir,
      nullValues: new[] { "", "NA", "N/A", "NULL" }
    );

    Assert.That(entry, Is.Not.Null);
  }
}
