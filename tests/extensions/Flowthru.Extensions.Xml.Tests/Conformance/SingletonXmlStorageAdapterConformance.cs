using Flowthru.Core.Data.Storage;
using Flowthru.Extensions.Xml.Tests.Fixtures;
using Flowthru.Tests.Kits.Storage;

namespace Flowthru.Extensions.Xml.Tests.Conformance;

/// <summary>
/// Conformance for <see cref="SingletonXmlStorageAdapter{T}"/> — single-document XML
/// adapter. Pattern parallels <see cref="SingletonJsonStorageAdapter{T}"/> in Core: direct
/// IStorageAdapter implementation that doesn't compose because singleton XML doesn't fit
/// the row-streaming model.
/// </summary>
[TestFixtureSource(nameof(Fixtures))]
public class SingletonXmlStorageAdapterConformance : StorageAdapterConformance<XmlTestItem>
{
  public static IEnumerable<string> Fixtures => new[] { "Synthetic/xml-singleton" };

  private string _tempDir = string.Empty;

  public SingletonXmlStorageAdapterConformance(string fixturePath) : base(fixturePath) { }

  [SetUp]
  public void SetUp()
  {
    _tempDir = Path.Combine(
      Path.GetTempPath(),
      $"flowthru-xml-singleton-conformance-{Guid.NewGuid():N}"
    );
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

  protected override XmlTestItem LoadFixture(string fixturePath) =>
    new XmlTestItem { Name = "alpha", Count = 7 };

  protected override IStorageAdapter<XmlTestItem> CreateWellFormed(XmlTestItem data)
  {
    var path = Path.Combine(_tempDir, $"well-formed-{Guid.NewGuid():N}.xml");
    var adapter = new SingletonXmlStorageAdapter<XmlTestItem>(path);
    adapter.Save(data).Run().GetAwaiter().GetResult();
    return adapter;
  }

  protected override IStorageAdapter<XmlTestItem> CreateMissingSource()
  {
    var path = Path.Combine(_tempDir, $"missing-{Guid.NewGuid():N}.xml");
    return new SingletonXmlStorageAdapter<XmlTestItem>(path);
  }

  protected override IEqualityComparer<XmlTestItem>? Comparer => new TestItemComparer();

  private sealed class TestItemComparer : IEqualityComparer<XmlTestItem>
  {
    public bool Equals(XmlTestItem? x, XmlTestItem? y)
    {
      if (x is null || y is null)
      {
        return ReferenceEquals(x, y);
      }
      return x.Name == y.Name && x.Count == y.Count;
    }

    public int GetHashCode(XmlTestItem obj) => HashCode.Combine(obj.Name, obj.Count);
  }
}
