using Flowthru.Core.Data;
using Flowthru.Core.Data.Storage;
using Flowthru.Tests.Kits.Schemas;
using Flowthru.Tests.Kits.Storage;
using SysIO = System.IO;

namespace Flowthru.Core.Tests.Conformance;

/// <summary>
/// Conformance for the singleton-JSON-document-per-file
/// <see cref="DirectoryStorageAdapter{T}"/> composition that backs
/// <c>ItemFactory.Enumerable.JsonDocuments&lt;T&gt;</c> — each file is one JSON object
/// deserialised to <typeparamref name="T"/>. Mirrors the XML directory shape but with
/// JSON's <see cref="SingletonJsonStorageAdapter{T}"/> per file.
/// </summary>
[TestFixtureSource(nameof(Fixtures))]
public class JsonDocumentsStorageAdapterConformance
  : DirectoryStorageAdapterConformance<TraditionalSchema>
{
  // Fixture path is unused — we synthesize documents directly. The kit base requires it
  // for [TestFixtureSource] machinery.
  public static IEnumerable<string> Fixtures => new[] { "Synthetic/json-documents" };

  private string _rootDir = string.Empty;
  private string _wellFormedDir = string.Empty;

  public JsonDocumentsStorageAdapterConformance(string fixturePath) : base(fixturePath) { }

  [SetUp]
  public void SetUp()
  {
    _rootDir = Path.Combine(
      Path.GetTempPath(),
      $"flowthru-json-docs-conformance-{Guid.NewGuid():N}"
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

  protected override Directory<TraditionalSchema> LoadFixture(string fixturePath) =>
    new(new Dictionary<string, TraditionalSchema>
    {
      ["alpha.json"] = new TraditionalSchema
      {
        Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        Name = "alpha",
        Value = 1,
      },
      ["beta.json"] = new TraditionalSchema
      {
        Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
        Name = "beta",
        Value = 2,
      },
    });

  protected override IStorageAdapter<Directory<TraditionalSchema>> CreateWellFormed(
    Directory<TraditionalSchema> data
  )
  {
    var adapter = BuildAdapter(_wellFormedDir);
    adapter.Save(data).Run().GetAwaiter().GetResult();
    return adapter;
  }

  protected override IStorageAdapter<Directory<TraditionalSchema>> CreateMissingSource() =>
    BuildAdapter(Path.Combine(_rootDir, $"missing-{Guid.NewGuid():N}"));

  protected override string WellFormedDirectoryPath => _wellFormedDir;

  protected override string FileExtension => ".json";

  protected override IStorageAdapter<Directory<TraditionalSchema>> CreateAdapterForWellFormedPath() =>
    BuildAdapter(_wellFormedDir);

  protected override void PlantWellFormedFile(string filePath)
  {
    SysIO.Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
    File.WriteAllText(
      filePath,
      """{ "Id": "33333333-3333-3333-3333-333333333333", "Name": "stale", "Value": 0 }"""
    );
  }

  protected override IEqualityComparer<Directory<TraditionalSchema>>? Comparer =>
    new DirectoryEqualityComparer<TraditionalSchema>(new SchemaComparer());

  private static IStorageAdapter<Directory<TraditionalSchema>> BuildAdapter(string dir) =>
    new DirectoryStorageAdapter<TraditionalSchema>(
      directoryPath: dir,
      filePattern: "*.json",
      perFileAdapter: path => new SingletonJsonStorageAdapter<TraditionalSchema>(path)
    );

  private sealed class SchemaComparer : IEqualityComparer<TraditionalSchema>
  {
    public bool Equals(TraditionalSchema? x, TraditionalSchema? y)
    {
      if (x is null || y is null)
        return ReferenceEquals(x, y);
      return x.Id == y.Id && x.Name == y.Name && x.Value == y.Value;
    }

    public int GetHashCode(TraditionalSchema obj) => HashCode.Combine(obj.Id, obj.Name, obj.Value);
  }
}
