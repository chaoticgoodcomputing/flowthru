using Flowthru.Core.Data;
using Flowthru.Core.Data.Storage;
using Flowthru.Core.Data.Storage.Container;
using Flowthru.Core.Data.Storage.Format;
using Flowthru.Core.Data.Storage.Medium;
using Flowthru.Tests.Kits.Fixtures;
using Flowthru.Tests.Kits.Schemas;
using Flowthru.Tests.Kits.Storage;
using SysIO = System.IO;

namespace Flowthru.Core.Tests.Conformance;

/// <summary>
/// Conformance for the JSON-array-per-file <see cref="DirectoryStorageAdapter{T}"/>
/// composition that backs <c>ItemFactory.Enumerable.JsonDirectory&lt;TRow&gt;</c> — each
/// file is a JSON array of rows of the same schema. Parallel to the CSV/Parquet directory
/// conformance suites; the kit's negative-scenario factory for "schema declares column
/// not in source" maps to a JSON document missing a property.
/// </summary>
[TestFixtureSource(nameof(Fixtures))]
public class JsonDirectoryStorageAdapterConformance
  : DirectoryStorageAdapterConformance<IEnumerable<TraditionalSchema>>
{
  public static IEnumerable<string> Fixtures => new[] { "Flat/Simple/rows.json" };

  private string _rootDir = string.Empty;
  private string _wellFormedDir = string.Empty;

  public JsonDirectoryStorageAdapterConformance(string fixturePath) : base(fixturePath) { }

  [SetUp]
  public void SetUp()
  {
    _rootDir = Path.Combine(
      Path.GetTempPath(),
      $"flowthru-json-dir-conformance-{Guid.NewGuid():N}"
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
        ["alpha.json"] = rows.Take(midpoint).ToList(),
        ["beta.json"] = rows.Skip(midpoint).ToList(),
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

  protected override string FileExtension => ".json";

  protected override IStorageAdapter<Directory<IEnumerable<TraditionalSchema>>> CreateAdapterForWellFormedPath() =>
    BuildAdapter(_wellFormedDir);

  protected override void PlantWellFormedFile(string filePath)
  {
    SysIO.Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
    File.WriteAllText(
      filePath,
      """[ { "Id": "11111111-1111-1111-1111-111111111111", "Name": "stale", "Value": 0 } ]"""
    );
  }

  protected override IEqualityComparer<Directory<IEnumerable<TraditionalSchema>>>? Comparer =>
    new DirectoryEqualityComparer<IEnumerable<TraditionalSchema>>(new SequenceEqualityComparer());

  private static IStorageAdapter<Directory<IEnumerable<TraditionalSchema>>> BuildAdapter(string dir)
  {
    var format = new JsonFormatSerializer<TraditionalSchema>();
    var container = new EnumerableContainerAdapter<TraditionalSchema>();
    return new DirectoryStorageAdapter<IEnumerable<TraditionalSchema>>(
      directoryPath: dir,
      filePattern: "*.json",
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
