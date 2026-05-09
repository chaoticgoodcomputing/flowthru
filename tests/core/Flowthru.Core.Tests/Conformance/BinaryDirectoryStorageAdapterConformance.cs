using Flowthru.Core.Data;
using Flowthru.Core.Data.Storage;
using Flowthru.Tests.Kits.Storage;
using SysIO = System.IO;

namespace Flowthru.Core.Tests.Conformance;

/// <summary>
/// Conformance for <see cref="DirectoryStorageAdapter{T}"/> wrapping
/// <see cref="BinaryFileStorageAdapter"/> — the shape returned by
/// <see cref="ItemFactory.Enumerable.BinaryDirectory"/>. Verifies the directory
/// contract end-to-end for raw binary blobs: round-trip, hard-delete, empty round-trip,
/// non-matching-file isolation.
/// </summary>
[TestFixtureSource(nameof(Fixtures))]
public class BinaryDirectoryStorageAdapterConformance : DirectoryStorageAdapterConformance<byte[]>
{
  // The fixture path is unused for binary — we synthesize bytes directly. Kept for the
  // [TestFixtureSource] machinery that the kit base requires.
  public static IEnumerable<string> Fixtures => new[] { "Synthetic/binary-directory" };

  private string _rootDir = string.Empty;
  private string _wellFormedDir = string.Empty;

  public BinaryDirectoryStorageAdapterConformance(string fixturePath) : base(fixturePath) { }

  [SetUp]
  public void SetUp()
  {
    _rootDir = Path.Combine(
      Path.GetTempPath(),
      $"flowthru-binary-dir-conformance-{Guid.NewGuid():N}"
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

  protected override DirectoryOf<byte[]> LoadFixture(string fixturePath) =>
    new(new Dictionary<string, byte[]>
    {
      ["alpha.bin"] = new byte[] { 0x01, 0x02, 0x03 },
      ["beta.bin"] = new byte[] { 0x10, 0x20, 0x30, 0x40 },
    });

  protected override IStorageAdapter<DirectoryOf<byte[]>> CreateWellFormed(DirectoryOf<byte[]> data)
  {
    var adapter = BuildAdapter(_wellFormedDir);
    adapter.Save(data).Run().GetAwaiter().GetResult();
    return adapter;
  }

  protected override IStorageAdapter<DirectoryOf<byte[]>> CreateMissingSource() =>
    BuildAdapter(Path.Combine(_rootDir, $"missing-{Guid.NewGuid():N}"));

  protected override string WellFormedDirectoryPath => _wellFormedDir;

  protected override string FileExtension => ".bin";

  protected override IStorageAdapter<DirectoryOf<byte[]>> CreateAdapterForWellFormedPath() =>
    BuildAdapter(_wellFormedDir);

  protected override void PlantWellFormedFile(string filePath)
  {
    SysIO.Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
    File.WriteAllBytes(filePath, new byte[] { 0xFF, 0xEE, 0xDD });
  }

  protected override IEqualityComparer<DirectoryOf<byte[]>>? Comparer =>
    new DirectoryEqualityComparer<byte[]>(new ByteArrayComparer());

  private static IStorageAdapter<DirectoryOf<byte[]>> BuildAdapter(string dir) =>
    new DirectoryStorageAdapter<byte[]>(
      directoryPath: dir,
      filePattern: "*.bin",
      perFileAdapter: path => new BinaryFileStorageAdapter(path)
    );

  private sealed class ByteArrayComparer : IEqualityComparer<byte[]>
  {
    public bool Equals(byte[]? x, byte[]? y)
    {
      if (x is null || y is null)
        return ReferenceEquals(x, y);
      return x.SequenceEqual(y);
    }

    public int GetHashCode(byte[] obj) => obj.Length;
  }
}
