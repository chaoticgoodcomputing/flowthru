using Flowthru.Data.Storage;
using Flowthru.Data.Storage.Xml;
using Flowthru.Extensions.Xml.Tests.Fixtures;
using Flowthru.Tests.Kits.Storage;

namespace Flowthru.Extensions.Xml.Tests.Laws;

/// <summary>
/// <see cref="IStorageAdapterLaws{T}"/> binding for
/// <see cref="SingletonXmlAdapter{T}"/> over the simple
/// <see cref="XmlTestItem"/> fixture. Inherits round-trip,
/// inspect-shallow on well-formed + missing source, exists, and
/// inspect-target laws from the kit.
/// </summary>
[TestFixture]
[Category("Xml")]
[Category("Laws")]
public class SingletonXmlAdapterLaws : IStorageAdapterLaws<XmlTestItem>
{
  private string _tempDir = null!;

  [SetUp]
  public void SetUp()
  {
    _tempDir = Path.Combine(
      Path.GetTempPath(),
      $"flowthru-xml-singleton-laws-{Guid.NewGuid():N}"
    );
    Directory.CreateDirectory(_tempDir);
  }

  [TearDown]
  public void TearDown()
  {
    if (Directory.Exists(_tempDir))
    {
      try { Directory.Delete(_tempDir, recursive: true); }
      catch { /* best effort */ }
    }
  }

  protected override XmlTestItem SampleData =>
    new XmlTestItem { Name = "alpha", Count = 7 };

  protected override IStorageAdapter<XmlTestItem> CreateWellFormed() =>
    new SingletonXmlAdapter<XmlTestItem>(
      Path.Combine(_tempDir, $"well-formed-{Guid.NewGuid():N}.xml")
    );

  protected override IStorageAdapter<XmlTestItem> CreateMissingSource() =>
    new SingletonXmlAdapter<XmlTestItem>(
      Path.Combine(_tempDir, $"missing-{Guid.NewGuid():N}.xml")
    );

  protected override IEqualityComparer<XmlTestItem>? Comparer => new TestItemComparer();

  private sealed class TestItemComparer : IEqualityComparer<XmlTestItem>
  {
    public bool Equals(XmlTestItem? x, XmlTestItem? y)
    {
      if (x is null || y is null) return ReferenceEquals(x, y);
      return x.Name == y.Name && x.Count == y.Count;
    }
    public int GetHashCode(XmlTestItem obj) => HashCode.Combine(obj.Name, obj.Count);
  }
}
