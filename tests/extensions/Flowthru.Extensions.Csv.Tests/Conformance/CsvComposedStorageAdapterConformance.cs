using Flowthru.Core.Data.Storage;
using Flowthru.Core.Data.Storage.Container;
using Flowthru.Core.Data.Storage.Format;
using Flowthru.Core.Data.Storage.Medium;
using Flowthru.Tests.Kits.Fixtures;
using Flowthru.Tests.Kits.Schemas;
using Flowthru.Tests.Kits.Storage;

namespace Flowthru.Extensions.Csv.Tests.Conformance;

/// <summary>
/// Conformance for the composed storage adapter built by
/// <c>CsvItemExtensions.Csv&lt;TRow&gt;</c> — i.e.,
/// <see cref="ComposedStorageAdapter{TContainer, TRow}"/> wrapping a
/// <see cref="FileStorageMedium"/> + <see cref="CsvFormatSerializer{TRow}"/> +
/// <see cref="EnumerableContainerAdapter{TRow}"/>.
/// </summary>
[TestFixtureSource(nameof(Fixtures))]
public class CsvComposedStorageAdapterConformance
  : StorageAdapterConformance<IEnumerable<TraditionalSchema>>
{
  public static IEnumerable<string> Fixtures => new[] { "Flat/Simple/rows.json" };

  private string _tempDir = string.Empty;

  public CsvComposedStorageAdapterConformance(string fixturePath) : base(fixturePath) { }

  [SetUp]
  public void SetUp()
  {
    _tempDir = Path.Combine(
      Path.GetTempPath(),
      $"flowthru-csv-conformance-{Guid.NewGuid():N}"
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

  protected override IEnumerable<TraditionalSchema> LoadFixture(string fixturePath) =>
    FixtureLoader.Load<TraditionalSchema>(fixturePath);

  protected override IStorageAdapter<IEnumerable<TraditionalSchema>> CreateWellFormed(
    IEnumerable<TraditionalSchema> data
  )
  {
    var path = Path.Combine(_tempDir, $"well-formed-{Guid.NewGuid():N}.csv");
    var adapter = BuildAdapter(path);
    adapter.Save(data).Run().GetAwaiter().GetResult();
    return adapter;
  }

  protected override IStorageAdapter<IEnumerable<TraditionalSchema>> CreateMissingSource()
  {
    var path = Path.Combine(_tempDir, $"missing-{Guid.NewGuid():N}.csv");
    return BuildAdapter(path);
  }

  protected override IEqualityComparer<IEnumerable<TraditionalSchema>>? Comparer =>
    new SequenceEqualityComparer();

  private static IStorageAdapter<IEnumerable<TraditionalSchema>> BuildAdapter(string path)
  {
    var medium = new FileStorageMedium(path);
    var format = new CsvFormatSerializer<TraditionalSchema>();
    var container = new EnumerableContainerAdapter<TraditionalSchema>();
    return new ComposedStorageAdapter<IEnumerable<TraditionalSchema>, TraditionalSchema>(
      medium,
      format,
      container
    );
  }

  private sealed class SequenceEqualityComparer : IEqualityComparer<IEnumerable<TraditionalSchema>>
  {
    public bool Equals(IEnumerable<TraditionalSchema>? x, IEnumerable<TraditionalSchema>? y)
    {
      if (x is null || y is null)
      {
        return ReferenceEquals(x, y);
      }
      return x.SequenceEqual(y);
    }

    public int GetHashCode(IEnumerable<TraditionalSchema> obj) => 0;
  }
}
