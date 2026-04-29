using Flowthru.Core.Data.Storage;
using Flowthru.Extensions.MLNet.Storage;
using Flowthru.Tests.Kits.Storage;

namespace Flowthru.Extensions.MLNet.Tests.Conformance;

/// <summary>
/// Conformance for <see cref="OnnxModelStorageAdapter"/> — the read-only adapter for ONNX
/// model files (<see cref="IStorageAdapter{T}"/> over <c>byte[]</c>).
/// </summary>
/// <remarks>
/// <para>
/// ONNX models are read-only seed data: <c>Traits.CanWrite = false</c>, <c>Save</c> throws.
/// The kit's round-trip test passes vacuously; trait consistency is what's verified.
/// </para>
/// <para>
/// Fixtures for <c>byte[]</c> adapters can't come from JSON. The subclass synthesizes a
/// non-empty byte array directly and writes it to a <c>.onnx</c>-extensioned temp file —
/// just enough to satisfy <c>InspectShallow</c>'s extension and non-empty checks. The kit's
/// fixture path is decorative for this scenario; we still pass one through the constructor
/// for protocol consistency with parameterized scenarios elsewhere.
/// </para>
/// </remarks>
[TestFixtureSource(nameof(Fixtures))]
public class OnnxModelStorageAdapterConformance : StorageAdapterConformance<byte[]>
{
  public static IEnumerable<string> Fixtures => new[] { "Synthetic/onnx-bytes" };

  private string _tempDir = string.Empty;

  public OnnxModelStorageAdapterConformance(string fixturePath) : base(fixturePath) { }

  [SetUp]
  public void SetUp()
  {
    _tempDir = Path.Combine(
      Path.GetTempPath(),
      $"flowthru-onnx-conformance-{Guid.NewGuid():N}"
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

  /// <summary>Synthetic 32-byte payload — non-empty bytes are all the adapter checks.</summary>
  protected override byte[] LoadFixture(string fixturePath)
  {
    var bytes = new byte[32];
    new Random(42).NextBytes(bytes);
    return bytes;
  }

  protected override IStorageAdapter<byte[]> CreateWellFormed(byte[] data)
  {
    var path = Path.Combine(_tempDir, $"well-formed-{Guid.NewGuid():N}.onnx");
    File.WriteAllBytes(path, data);
    return new OnnxModelStorageAdapter(path);
  }

  protected override IStorageAdapter<byte[]> CreateMissingSource()
  {
    var path = Path.Combine(_tempDir, $"missing-{Guid.NewGuid():N}.onnx");
    return new OnnxModelStorageAdapter(path);
  }

  protected override IEqualityComparer<byte[]>? Comparer => new ByteArrayComparer();

  private sealed class ByteArrayComparer : IEqualityComparer<byte[]>
  {
    public bool Equals(byte[]? x, byte[]? y)
    {
      if (x is null || y is null)
      {
        return ReferenceEquals(x, y);
      }
      return x.AsSpan().SequenceEqual(y);
    }

    public int GetHashCode(byte[] obj) => 0;
  }
}
