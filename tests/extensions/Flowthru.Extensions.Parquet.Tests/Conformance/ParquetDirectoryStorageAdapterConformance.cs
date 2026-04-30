using Flowthru.Core.Data;
using Flowthru.Core.Data.Storage;
using Flowthru.Core.Data.Storage.Container;
using Flowthru.Core.Data.Storage.Format;
using Flowthru.Core.Data.Storage.Medium;
using Flowthru.Tests.Kits.Fixtures;
using Flowthru.Tests.Kits.Schemas;
using Flowthru.Tests.Kits.Storage;
using SysIO = System.IO;

namespace Flowthru.Extensions.Parquet.Tests.Conformance;

/// <summary>
/// Conformance for the Parquet-shaped <see cref="DirectoryStorageAdapter{T}"/> composition
/// that backs <c>ItemFactory.Enumerable.ParquetDirectory&lt;TRow&gt;</c>: a directory of
/// Parquet files where each file is one independent row collection of the same schema.
/// </summary>
[TestFixtureSource(nameof(Fixtures))]
public class ParquetDirectoryStorageAdapterConformance
  : DirectoryStorageAdapterConformance<IEnumerable<TraditionalSchema>>
{
  public static IEnumerable<string> Fixtures => new[] { "Flat/Simple/rows.json" };

  private string _rootDir = string.Empty;
  private string _wellFormedDir = string.Empty;

  public ParquetDirectoryStorageAdapterConformance(string fixturePath) : base(fixturePath) { }

  [SetUp]
  public void SetUp()
  {
    _rootDir = Path.Combine(
      Path.GetTempPath(),
      $"flowthru-parquet-dir-conformance-{Guid.NewGuid():N}"
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
        ["alpha.parquet"] = rows.Take(midpoint).ToList(),
        ["beta.parquet"] = rows.Skip(midpoint).ToList(),
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

  protected override string FileExtension => ".parquet";

  protected override IStorageAdapter<Directory<IEnumerable<TraditionalSchema>>> CreateAdapterForWellFormedPath() =>
    BuildAdapter(_wellFormedDir);

  protected override void PlantWellFormedFile(string filePath)
  {
    SysIO.Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
    var medium = new FileStorageMedium(filePath);
    var format = new ParquetFormatSerializer<TraditionalSchema>();
    var container = new EnumerableContainerAdapter<TraditionalSchema>();
    var seed = new ComposedStorageAdapter<IEnumerable<TraditionalSchema>, TraditionalSchema>(
      medium,
      format,
      container
    );
    seed.Save(new[] { new TraditionalSchema { Id = Guid.NewGuid(), Name = "stale", Value = 0 } })
      .Run()
      .GetAwaiter()
      .GetResult();
  }

  protected override IStorageAdapter<Directory<IEnumerable<TraditionalSchema>>>?
    CreateAdapterMissingExpectedColumn()
  {
    // Phase F negative scenario: write a Parquet file using a schema that omits the
    // 'name' field declared by TraditionalSchema. Pre-flight should detect divergence.
    var dirPath = Path.Combine(_rootDir, $"missing-column-{Guid.NewGuid():N}");
    SysIO.Directory.CreateDirectory(dirPath);
    var seedPath = Path.Combine(dirPath, "rows.parquet");
    var medium = new FileStorageMedium(seedPath);
    var format = new ParquetFormatSerializer<SchemaMismatchSeedRow>();
    var container = new EnumerableContainerAdapter<SchemaMismatchSeedRow>();
    var seed = new ComposedStorageAdapter<IEnumerable<SchemaMismatchSeedRow>, SchemaMismatchSeedRow>(
      medium,
      format,
      container
    );
    seed.Save(new[]
    {
      new SchemaMismatchSeedRow { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Value = 42 },
      new SchemaMismatchSeedRow { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Value = 7 },
    }).Run().GetAwaiter().GetResult();

    return BuildAdapter(dirPath);
  }

  protected override IEqualityComparer<Directory<IEnumerable<TraditionalSchema>>>? Comparer =>
    new DirectoryEqualityComparer<IEnumerable<TraditionalSchema>>(new SequenceEqualityComparer());

  private static IStorageAdapter<Directory<IEnumerable<TraditionalSchema>>> BuildAdapter(string dir)
  {
    var format = new ParquetFormatSerializer<TraditionalSchema>();
    var container = new EnumerableContainerAdapter<TraditionalSchema>();
    return new DirectoryStorageAdapter<IEnumerable<TraditionalSchema>>(
      directoryPath: dir,
      filePattern: "*.parquet",
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
