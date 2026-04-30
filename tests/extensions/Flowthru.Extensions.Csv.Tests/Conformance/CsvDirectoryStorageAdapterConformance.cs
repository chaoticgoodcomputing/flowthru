using Flowthru.Core.Data.Storage;
using Flowthru.Core.Data.Storage.Format;
using Flowthru.Tests.Kits.Fixtures;
using Flowthru.Tests.Kits.Schemas;
using Flowthru.Tests.Kits.Storage;

namespace Flowthru.Extensions.Csv.Tests.Conformance;

/// <summary>
/// Conformance for <see cref="DirectoryCsvStorageAdapter{TRow}"/> — the read-only adapter that
/// concatenates every <c>*.csv</c> file in a directory.
/// </summary>
/// <remarks>
/// The adapter reports <c>Traits.CanWrite = false</c>, so the round-trip test takes the
/// kit's read-only skip path. The well-formed scenario seeds the directory by writing the
/// fixture rows to a single CSV file via <see cref="CsvFormatSerializer{TRow}"/>; the missing
/// scenario points at a directory that doesn't exist.
/// </remarks>
[TestFixtureSource(nameof(Fixtures))]
public class CsvDirectoryStorageAdapterConformance
  : StorageAdapterConformance<IEnumerable<TraditionalSchema>>
{
  public static IEnumerable<string> Fixtures => new[] { "Flat/Simple/rows.json" };

  private string _rootDir = string.Empty;

  public CsvDirectoryStorageAdapterConformance(string fixturePath) : base(fixturePath) { }

  [SetUp]
  public void SetUp()
  {
    _rootDir = Path.Combine(
      Path.GetTempPath(),
      $"flowthru-csv-dir-conformance-{Guid.NewGuid():N}"
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

  protected override IEnumerable<TraditionalSchema> LoadFixture(string fixturePath) =>
    FixtureLoader.Load<TraditionalSchema>(fixturePath);

  protected override IStorageAdapter<IEnumerable<TraditionalSchema>> CreateWellFormed(
    IEnumerable<TraditionalSchema> data
  )
  {
    var dirPath = Path.Combine(_rootDir, $"well-formed-{Guid.NewGuid():N}");
    Directory.CreateDirectory(dirPath);

    // Seed the directory by writing the fixture rows to a single CSV file via the format
    // serializer. The directory adapter will discover it via *.csv glob.
    var csvPath = Path.Combine(dirPath, "rows.csv");
    using (var stream = File.Create(csvPath))
    {
      var serializer = new CsvFormatSerializer<TraditionalSchema>();
      serializer.SerializeRows(stream, ToAsync(data)).GetAwaiter().GetResult();
    }

    return new DirectoryCsvStorageAdapter<TraditionalSchema>(dirPath);
  }

  protected override IStorageAdapter<IEnumerable<TraditionalSchema>> CreateMissingSource()
  {
    var dirPath = Path.Combine(_rootDir, $"missing-{Guid.NewGuid():N}");
    return new DirectoryCsvStorageAdapter<TraditionalSchema>(dirPath);
  }

  protected override IStorageAdapter<IEnumerable<TraditionalSchema>>? CreateAdapterMissingExpectedColumn()
  {
    // Phase F negative scenario: seed the directory with a CSV whose header row is
    // missing the 'name' column declared by TraditionalSchema.
    var dirPath = Path.Combine(_rootDir, $"missing-column-{Guid.NewGuid():N}");
    Directory.CreateDirectory(dirPath);
    File.WriteAllText(
      Path.Combine(dirPath, "rows.csv"),
      """
      id,value
      11111111-1111-1111-1111-111111111111,42
      22222222-2222-2222-2222-222222222222,7
      """
    );
    return new DirectoryCsvStorageAdapter<TraditionalSchema>(dirPath);
  }

  protected override IEqualityComparer<IEnumerable<TraditionalSchema>>? Comparer =>
    new SequenceEqualityComparer();

  private static async IAsyncEnumerable<TraditionalSchema> ToAsync(
    IEnumerable<TraditionalSchema> source
  )
  {
    foreach (var row in source)
    {
      yield return row;
      await Task.Yield();
    }
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
