using Flowthru.Core.Data.Storage;
using Flowthru.Core.Data.Storage.Container;
using Flowthru.Core.Data.Storage.Format;
using Flowthru.Core.Data.Storage.Medium;
using Flowthru.Tests.Kits.Fixtures;
using Flowthru.Tests.Kits.Schemas;
using Flowthru.Tests.Kits.Storage;

namespace Flowthru.Extensions.Parquet.Tests.Conformance;

/// <summary>
/// Conformance for the storage adapter built by <c>ParquetItemExtensions.Parquet&lt;TRow&gt;</c>
/// — i.e., <see cref="ComposedStorageAdapter{TContainer, TRow}"/> wrapping a
/// <see cref="FileStorageMedium"/> + <see cref="ParquetFormatSerializer{TRow}"/> +
/// <see cref="EnumerableContainerAdapter{TRow}"/>.
/// </summary>
[TestFixtureSource(nameof(Fixtures))]
public class ParquetStorageAdapterConformance
  : StorageAdapterConformance<IEnumerable<TraditionalSchema>>
{
  public static IEnumerable<string> Fixtures => new[] { "Flat/Simple/rows.json" };

  private string _tempDir = string.Empty;

  public ParquetStorageAdapterConformance(string fixturePath) : base(fixturePath) { }

  [SetUp]
  public void SetUp()
  {
    _tempDir = Path.Combine(
      Path.GetTempPath(),
      $"flowthru-parquet-conformance-{Guid.NewGuid():N}"
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
    var path = Path.Combine(_tempDir, $"well-formed-{Guid.NewGuid():N}.parquet");
    var adapter = BuildAdapter(path);
    // Pre-seed the destination so Load and InspectShallow / InspectDeep return the data.
    adapter.Save(data).Run().GetAwaiter().GetResult();
    return adapter;
  }

  protected override IStorageAdapter<IEnumerable<TraditionalSchema>> CreateMissingSource()
  {
    var path = Path.Combine(_tempDir, $"missing-{Guid.NewGuid():N}.parquet");
    return BuildAdapter(path);
  }

  protected override IStorageAdapter<IEnumerable<TraditionalSchema>>? CreateAdapterMissingExpectedColumn()
  {
    // Phase F negative scenario: write a Parquet file using a different schema (a row
    // type that omits the 'name' field declared by TraditionalSchema), then point a
    // TraditionalSchema-typed adapter at it. Pre-flight should detect that the on-disk
    // schema diverges from what TraditionalSchema declares and surface SchemaMismatch.
    var path = Path.Combine(_tempDir, $"missing-column-{Guid.NewGuid():N}.parquet");
    var seedAdapter = BuildSchemaMismatchSeedAdapter(path);
    seedAdapter
      .Save(
        new[]
        {
          new SchemaMismatchSeedRow
          {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Value = 42,
          },
          new SchemaMismatchSeedRow
          {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Value = 7,
          },
        }
      )
      .Run()
      .GetAwaiter()
      .GetResult();
    return BuildAdapter(path);
  }

  protected override IEqualityComparer<IEnumerable<TraditionalSchema>>? Comparer =>
    new SequenceEqualityComparer();

  private static IStorageAdapter<IEnumerable<SchemaMismatchSeedRow>> BuildSchemaMismatchSeedAdapter(
    string path
  )
  {
    var medium = new FileStorageMedium(path);
    var format = new ParquetFormatSerializer<SchemaMismatchSeedRow>();
    var container = new EnumerableContainerAdapter<SchemaMismatchSeedRow>();
    return new ComposedStorageAdapter<IEnumerable<SchemaMismatchSeedRow>, SchemaMismatchSeedRow>(
      medium,
      format,
      container
    );
  }

  private static IStorageAdapter<IEnumerable<TraditionalSchema>> BuildAdapter(string path)
  {
    var medium = new FileStorageMedium(path);
    var format = new ParquetFormatSerializer<TraditionalSchema>();
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
