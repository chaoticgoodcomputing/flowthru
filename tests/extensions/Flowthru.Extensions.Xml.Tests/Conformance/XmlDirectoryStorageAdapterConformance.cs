using System.Xml.Serialization;
using Flowthru.Core.Data;
using Flowthru.Core.Data.Storage;
using Flowthru.Extensions.Xml.Tests.Fixtures;
using Flowthru.Tests.Kits.Storage;

namespace Flowthru.Extensions.Xml.Tests.Conformance;

/// <summary>
/// Conformance for <see cref="XmlDirectoryStorageAdapter{T}"/> — read-only directory
/// adapter that yields one <see cref="XmlDocument{T}"/> per <c>*.xml</c> file. Each
/// wrapper carries the file's name plus the deserialized payload.
/// </summary>
[TestFixtureSource(nameof(Fixtures))]
public class XmlDirectoryStorageAdapterConformance
  : StorageAdapterConformance<IEnumerable<XmlDocument<XmlTestItem>>>
{
  public static IEnumerable<string> Fixtures => new[] { "Synthetic/xml-directory" };

  private string _rootDir = string.Empty;

  public XmlDirectoryStorageAdapterConformance(string fixturePath) : base(fixturePath) { }

  [SetUp]
  public void SetUp()
  {
    _rootDir = Path.Combine(
      Path.GetTempPath(),
      $"flowthru-xml-dir-conformance-{Guid.NewGuid():N}"
    );
    Directory.CreateDirectory(_rootDir);
  }

  [TearDown]
  public void TearDown()
  {
    if (Directory.Exists(_rootDir))
    {
      Directory.Delete(_rootDir, recursive: true);
    }
  }

  protected override IEnumerable<XmlDocument<XmlTestItem>> LoadFixture(string fixturePath) =>
    new[]
    {
      new XmlDocument<XmlTestItem>("alpha.xml", new XmlTestItem { Name = "alpha", Count = 1 }),
      new XmlDocument<XmlTestItem>("beta.xml", new XmlTestItem { Name = "beta", Count = 2 }),
    };

  protected override IStorageAdapter<IEnumerable<XmlDocument<XmlTestItem>>> CreateWellFormed(
    IEnumerable<XmlDocument<XmlTestItem>> data
  )
  {
    var dirPath = Path.Combine(_rootDir, $"well-formed-{Guid.NewGuid():N}");
    Directory.CreateDirectory(dirPath);

    // Seed the directory by writing each document's payload as a separate XML file.
    var serializer = new XmlSerializer(typeof(XmlTestItem));
    foreach (var doc in data)
    {
      var path = Path.Combine(dirPath, doc.FileName);
      using var stream = File.Create(path);
      serializer.Serialize(stream, doc.Document);
    }

    return new XmlDirectoryStorageAdapter<XmlTestItem>(dirPath);
  }

  protected override IStorageAdapter<IEnumerable<XmlDocument<XmlTestItem>>> CreateMissingSource()
  {
    var dirPath = Path.Combine(_rootDir, $"missing-{Guid.NewGuid():N}");
    return new XmlDirectoryStorageAdapter<XmlTestItem>(dirPath);
  }

  protected override IEqualityComparer<IEnumerable<XmlDocument<XmlTestItem>>>? Comparer =>
    new XmlDocumentSequenceComparer();

  private sealed class XmlDocumentSequenceComparer
    : IEqualityComparer<IEnumerable<XmlDocument<XmlTestItem>>>
  {
    public bool Equals(
      IEnumerable<XmlDocument<XmlTestItem>>? x,
      IEnumerable<XmlDocument<XmlTestItem>>? y
    )
    {
      if (x is null || y is null)
      {
        return ReferenceEquals(x, y);
      }
      var xList = x.OrderBy(d => d.FileName).ToList();
      var yList = y.OrderBy(d => d.FileName).ToList();
      if (xList.Count != yList.Count)
      {
        return false;
      }
      for (var i = 0; i < xList.Count; i++)
      {
        if (
          xList[i].FileName != yList[i].FileName
          || xList[i].Document.Name != yList[i].Document.Name
          || xList[i].Document.Count != yList[i].Document.Count
        )
        {
          return false;
        }
      }
      return true;
    }

    public int GetHashCode(IEnumerable<XmlDocument<XmlTestItem>> obj) => 0;
  }
}
