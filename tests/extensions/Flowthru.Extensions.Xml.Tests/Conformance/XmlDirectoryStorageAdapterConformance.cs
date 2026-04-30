using System.Xml.Serialization;
using Flowthru.Core.Data;
using Flowthru.Core.Data.Storage;
using Flowthru.Extensions.Xml.Tests.Fixtures;
using Flowthru.Tests.Kits.Storage;
using SysIO = System.IO;

namespace Flowthru.Extensions.Xml.Tests.Conformance;

/// <summary>
/// Conformance for the XML-shaped <see cref="DirectoryStorageAdapter{T}"/> composition that
/// backs <c>ItemFactory.Enumerable.XmlDocuments&lt;T&gt;</c>: a directory of XML files where
/// each file deserialises to one <typeparamref name="T"/>.
/// </summary>
[TestFixtureSource(nameof(Fixtures))]
public class XmlDirectoryStorageAdapterConformance
  : DirectoryStorageAdapterConformance<XmlTestItem>
{
  public static IEnumerable<string> Fixtures => new[] { "Synthetic/xml-directory" };

  private string _rootDir = string.Empty;
  private string _wellFormedDir = string.Empty;

  public XmlDirectoryStorageAdapterConformance(string fixturePath) : base(fixturePath) { }

  [SetUp]
  public void SetUp()
  {
    _rootDir = Path.Combine(
      Path.GetTempPath(),
      $"flowthru-xml-dir-conformance-{Guid.NewGuid():N}"
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

  protected override Directory<XmlTestItem> LoadFixture(string fixturePath) =>
    new(new Dictionary<string, XmlTestItem>
    {
      ["alpha.xml"] = new XmlTestItem { Name = "alpha", Count = 1 },
      ["beta.xml"] = new XmlTestItem { Name = "beta", Count = 2 },
    });

  protected override IStorageAdapter<Directory<XmlTestItem>> CreateWellFormed(
    Directory<XmlTestItem> data
  )
  {
    var adapter = BuildAdapter(_wellFormedDir);
    adapter.Save(data).Run().GetAwaiter().GetResult();
    return adapter;
  }

  protected override IStorageAdapter<Directory<XmlTestItem>> CreateMissingSource() =>
    BuildAdapter(Path.Combine(_rootDir, $"missing-{Guid.NewGuid():N}"));

  protected override string WellFormedDirectoryPath => _wellFormedDir;

  protected override string FileExtension => ".xml";

  protected override IStorageAdapter<Directory<XmlTestItem>> CreateAdapterForWellFormedPath() =>
    BuildAdapter(_wellFormedDir);

  protected override void PlantWellFormedFile(string filePath)
  {
    SysIO.Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
    var serializer = new XmlSerializer(typeof(XmlTestItem));
    using var writer = new StreamWriter(filePath);
    serializer.Serialize(writer, new XmlTestItem { Name = "stale", Count = 0 });
  }

  protected override IEqualityComparer<Directory<XmlTestItem>>? Comparer =>
    new DirectoryEqualityComparer<XmlTestItem>(new XmlTestItemComparer());

  private static IStorageAdapter<Directory<XmlTestItem>> BuildAdapter(string dir) =>
    new DirectoryStorageAdapter<XmlTestItem>(
      directoryPath: dir,
      filePattern: "*.xml",
      perFileAdapter: path => new SingletonXmlStorageAdapter<XmlTestItem>(path)
    );

  private sealed class XmlTestItemComparer : IEqualityComparer<XmlTestItem>
  {
    public bool Equals(XmlTestItem? x, XmlTestItem? y)
    {
      if (x is null || y is null)
        return ReferenceEquals(x, y);
      return x.Name == y.Name && x.Count == y.Count;
    }

    public int GetHashCode(XmlTestItem obj) => HashCode.Combine(obj.Name, obj.Count);
  }
}
