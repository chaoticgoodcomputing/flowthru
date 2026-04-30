using System.Text;
using Flowthru.Core.Data;
using Flowthru.Core.Data.Storage;
using Flowthru.Core.Data.Storage.Container;
using Flowthru.Core.Data.Storage.Format;
using Flowthru.Core.Data.Storage.Medium;
using Flowthru.Tests.Kits.Fixtures;
using Flowthru.Tests.Kits.Schemas;
using Flowthru.Tests.Kits.Storage;
using SysIO = System.IO;

namespace Flowthru.Extensions.Csv.Tests.Conformance;

/// <summary>
/// Conformance for the CSV-shaped <see cref="DirectoryStorageAdapter{T}"/> composition that
/// backs <c>ItemFactory.Enumerable.CsvDirectory&lt;TRow&gt;</c>: a directory of CSV files
/// where each file is one independent row collection of the same schema. The kit covers
/// load/save round-trip, missing source, hard-delete on Save, empty round-trip, and
/// non-matching-file isolation.
/// </summary>
[TestFixtureSource(nameof(Fixtures))]
public class CsvDirectoryStorageAdapterConformance
  : DirectoryStorageAdapterConformance<IEnumerable<TraditionalSchema>>
{
  public static IEnumerable<string> Fixtures => new[] { "Flat/Simple/rows.json" };

  private string _rootDir = string.Empty;
  private string _wellFormedDir = string.Empty;

  public CsvDirectoryStorageAdapterConformance(string fixturePath) : base(fixturePath) { }

  [SetUp]
  public void SetUp()
  {
    _rootDir = Path.Combine(
      Path.GetTempPath(),
      $"flowthru-csv-dir-conformance-{Guid.NewGuid():N}"
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
    // Two-file directory so per-file boundary preservation is exercised by the kit's
    // round-trip test, not a single-file degenerate case.
    var midpoint = rows.Count / 2;
    return new Directory<IEnumerable<TraditionalSchema>>(
      new Dictionary<string, IEnumerable<TraditionalSchema>>
      {
        ["alpha.csv"] = rows.Take(midpoint).ToList(),
        ["beta.csv"] = rows.Skip(midpoint).ToList(),
      }
    );
  }

  protected override IStorageAdapter<Directory<IEnumerable<TraditionalSchema>>> CreateWellFormed(
    Directory<IEnumerable<TraditionalSchema>> data
  )
  {
    var adapter = BuildAdapter(_wellFormedDir);
    adapter.Save(data).Run().GetAwaiter().GetResult();
    return adapter;
  }

  protected override IStorageAdapter<Directory<IEnumerable<TraditionalSchema>>> CreateMissingSource() =>
    BuildAdapter(Path.Combine(_rootDir, $"missing-{Guid.NewGuid():N}"));

  protected override string WellFormedDirectoryPath => _wellFormedDir;

  protected override string FileExtension => ".csv";

  protected override IStorageAdapter<Directory<IEnumerable<TraditionalSchema>>> CreateAdapterForWellFormedPath() =>
    BuildAdapter(_wellFormedDir);

  protected override void PlantWellFormedFile(string filePath)
  {
    SysIO.Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
    File.WriteAllText(
      filePath,
      // Header columns mirror TraditionalSchema's serialised CSV form. Content is irrelevant
      // — the test only checks that Save deletes this file before writing the new state.
      "id,name,value\n11111111-1111-1111-1111-111111111111,stale,0\n",
      Encoding.UTF8
    );
  }

  protected override IStorageAdapter<Directory<IEnumerable<TraditionalSchema>>>?
    CreateAdapterMissingExpectedColumn()
  {
    // Phase F negative scenario: seed a directory with a CSV whose header is missing the
    // 'name' column declared by TraditionalSchema. Pre-flight should detect the divergence.
    var dirPath = Path.Combine(_rootDir, $"missing-column-{Guid.NewGuid():N}");
    SysIO.Directory.CreateDirectory(dirPath);
    File.WriteAllText(
      Path.Combine(dirPath, "rows.csv"),
      """
      id,value
      11111111-1111-1111-1111-111111111111,42
      22222222-2222-2222-2222-222222222222,7
      """
    );
    return BuildAdapter(dirPath);
  }

  protected override IEqualityComparer<Directory<IEnumerable<TraditionalSchema>>>? Comparer =>
    new DirectoryEqualityComparer<IEnumerable<TraditionalSchema>>(new SequenceEqualityComparer());

  private static IStorageAdapter<Directory<IEnumerable<TraditionalSchema>>> BuildAdapter(string dir)
  {
    var format = new CsvFormatSerializer<TraditionalSchema>();
    var container = new EnumerableContainerAdapter<TraditionalSchema>();
    return new DirectoryStorageAdapter<IEnumerable<TraditionalSchema>>(
      directoryPath: dir,
      filePattern: "*.csv",
      perFileAdapter: path => new ComposedStorageAdapter<IEnumerable<TraditionalSchema>, TraditionalSchema>(
        new FileStorageMedium(path),
        format,
        container
      )
    );
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
