using ClosedXML.Excel;
using Flowthru.Core.Data;
using Flowthru.Core.Data.Storage;
using Flowthru.Core.Data.Storage.Container;
using Flowthru.Core.Data.Storage.Format;
using Flowthru.Core.Data.Storage.Medium;
using Flowthru.Tests.Kits.Fixtures;
using Flowthru.Tests.Kits.Schemas;
using Flowthru.Tests.Kits.Storage;
using SysIO = System.IO;

namespace Flowthru.Extensions.Excel.Tests.Conformance;

/// <summary>
/// Conformance for the Excel-shaped <see cref="DirectoryStorageAdapter{T}"/> composition
/// that backs <c>ItemFactory.Enumerable.ExcelDirectory&lt;TRow&gt;</c>: a directory of
/// <c>.xlsx</c> files where each file's designated sheet deserialises to one row collection
/// of the same schema. Excel is read-only, so the kit's write-side scenarios skip via the
/// <see cref="StorageTraits.CanWrite"/> gate; the read-side scenarios verify load,
/// missing-source, and non-matching-file isolation.
/// </summary>
[TestFixtureSource(nameof(Fixtures))]
public class ExcelDirectoryStorageAdapterConformance
  : DirectoryStorageAdapterConformance<IEnumerable<TraditionalSchema>>
{
  public static IEnumerable<string> Fixtures => new[] { "Flat/Simple/rows.json" };

  private const string SheetName = "Sheet1";

  private string _rootDir = string.Empty;
  private string _wellFormedDir = string.Empty;

  public ExcelDirectoryStorageAdapterConformance(string fixturePath) : base(fixturePath) { }

  [SetUp]
  public void SetUp()
  {
    _rootDir = Path.Combine(
      Path.GetTempPath(),
      $"flowthru-excel-dir-conformance-{Guid.NewGuid():N}"
    );
    SysIO.Directory.CreateDirectory(_rootDir);
    _wellFormedDir = Path.Combine(_rootDir, "well-formed");
  }

  [TearDown]
  public void TearDown()
  {
    if (SysIO.Directory.Exists(_rootDir))
      SysIO.Directory.Delete(_rootDir, recursive: true);
  }

  protected override Directory<IEnumerable<TraditionalSchema>> LoadFixture(string fixturePath)
  {
    var rows = FixtureLoader.Load<TraditionalSchema>(fixturePath).ToList();
    var midpoint = rows.Count / 2;
    return new Directory<IEnumerable<TraditionalSchema>>(
      new Dictionary<string, IEnumerable<TraditionalSchema>>
      {
        ["alpha.xlsx"] = rows.Take(midpoint).ToList(),
        ["beta.xlsx"] = rows.Skip(midpoint).ToList(),
      }
    );
  }

  protected override IStorageAdapter<Directory<IEnumerable<TraditionalSchema>>> CreateWellFormed(
    Directory<IEnumerable<TraditionalSchema>> data
  )
  {
    SysIO.Directory.CreateDirectory(_wellFormedDir);
    foreach (var (key, rows) in data)
    {
      var fileName = Path.GetFileName(key);
      WriteXlsx(Path.Combine(_wellFormedDir, fileName), rows);
    }
    return BuildAdapter(_wellFormedDir);
  }

  protected override IStorageAdapter<Directory<IEnumerable<TraditionalSchema>>> CreateMissingSource() =>
    BuildAdapter(Path.Combine(_rootDir, $"missing-{Guid.NewGuid():N}"));

  protected override string WellFormedDirectoryPath => _wellFormedDir;

  protected override string FileExtension => ".xlsx";

  protected override IStorageAdapter<Directory<IEnumerable<TraditionalSchema>>> CreateAdapterForWellFormedPath() =>
    BuildAdapter(_wellFormedDir);

  protected override void PlantWellFormedFile(string filePath)
  {
    SysIO.Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
    WriteXlsx(filePath, new[] { new TraditionalSchema { Id = Guid.NewGuid(), Name = "stale", Value = 0 } });
  }

  protected override IEqualityComparer<Directory<IEnumerable<TraditionalSchema>>>? Comparer =>
    new DirectoryEqualityComparer<IEnumerable<TraditionalSchema>>(new SequenceEqualityComparer());

  private static IStorageAdapter<Directory<IEnumerable<TraditionalSchema>>> BuildAdapter(string dir)
  {
    var format = new ExcelFormatSerializer<TraditionalSchema>(SheetName);
    var container = new EnumerableContainerAdapter<TraditionalSchema>();
    return new DirectoryStorageAdapter<IEnumerable<TraditionalSchema>>(
      directoryPath: dir,
      filePattern: "*.xlsx",
      perFileAdapter: path => new ComposedStorageAdapter<IEnumerable<TraditionalSchema>, TraditionalSchema>(
        new FileStorageMedium(path),
        format,
        container
      )
    );
  }

  private static void WriteXlsx(string filePath, IEnumerable<TraditionalSchema> rows)
  {
    using var workbook = new XLWorkbook();
    var ws = workbook.Worksheets.Add(SheetName);

    ws.Cell(1, 1).Value = "id";
    ws.Cell(1, 2).Value = "name";
    ws.Cell(1, 3).Value = "value";

    var r = 2;
    foreach (var row in rows)
    {
      ws.Cell(r, 1).Value = row.Id.ToString();
      ws.Cell(r, 2).Value = row.Name;
      ws.Cell(r, 3).Value = row.Value;
      r++;
    }

    workbook.SaveAs(filePath);
  }

  private sealed class SequenceEqualityComparer : IEqualityComparer<IEnumerable<TraditionalSchema>>
  {
    public bool Equals(IEnumerable<TraditionalSchema>? x, IEnumerable<TraditionalSchema>? y)
    {
      if (x is null || y is null)
        return ReferenceEquals(x, y);
      return x.SequenceEqual(y);
    }

    public int GetHashCode(IEnumerable<TraditionalSchema> obj) => 0;
  }
}
