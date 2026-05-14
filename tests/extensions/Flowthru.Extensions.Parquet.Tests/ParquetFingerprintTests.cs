using Flowthru.Data.Catalog;
using Flowthru.Data.Storage;
using Flowthru.Extensions.Parquet.Tests.Fixtures;
using Flowthru.Prelude;

namespace Flowthru.Extensions.Parquet.Tests;

/// <summary>
/// File-backed parity for Parquet items: the file medium underlying
/// a <c>ComposedStorageAdapter</c> opts into
/// <see cref="ISupportsFingerprint"/>, so Parquet inputs participate
/// in the cache plan automatically (mtime+size for v1). Phase 3's
/// scope deliberately defers footer-hash fingerprinting.
/// </summary>
[TestFixture]
[Category("Parquet")]
public class ParquetFingerprintTests
{
  private string _tempDir = null!;

  [SetUp]
  public void SetUp()
  {
    _tempDir = Path.Combine(Path.GetTempPath(), $"flowthru-fp-parquet-{Guid.NewGuid():N}");
    Directory.CreateDirectory(_tempDir);
  }

  [TearDown]
  public void TearDown()
  {
    if (Directory.Exists(_tempDir))
    {
      try { Directory.Delete(_tempDir, recursive: true); } catch { /* best effort */ }
    }
  }

  [Test]
  public async Task ParquetItem_TryGetFingerprint_ReturnsValueAfterSave()
  {
    var path = Path.Combine(_tempDir, "rows.parquet");
    var item = Item.Of<IEnumerable<FlatRow>>("rows").Parquet().AtPath(path).Build();

    await item.Save(new[]
    {
      new FlatRow { Id = 1, Name = "alpha", Value = 1.0 },
    }).Run();

    var io = item.TryGetFingerprint();
    Assert.That(io, Is.Not.Null,
      "Parquet over filesystem composes a fingerprintable medium — TryGetFingerprint must be non-null.");
    var result = await io!.Run();
    Assert.That(result, Is.InstanceOf<EffResult<string>.Success>());
  }

  [Test]
  public async Task ParquetItem_Fingerprint_ChangesWhenFileRewritten()
  {
    var path = Path.Combine(_tempDir, "rows.parquet");
    var item = Item.Of<IEnumerable<FlatRow>>("rows").Parquet().AtPath(path).Build();

    await item.Save(new[]
    {
      new FlatRow { Id = 1, Name = "alpha", Value = 1.0 },
    }).Run();
    var before = ((EffResult<string>.Success)await item.TryGetFingerprint()!.Run()).Value;

    // Re-write with strictly more rows to ensure size differs.
    await item.Save(new[]
    {
      new FlatRow { Id = 1, Name = "alpha", Value = 1.0 },
      new FlatRow { Id = 2, Name = "beta",  Value = 2.0 },
      new FlatRow { Id = 3, Name = "gamma", Value = 3.0 },
    }).Run();
    var after = ((EffResult<string>.Success)await item.TryGetFingerprint()!.Run()).Value;

    Assert.That(after, Is.Not.EqualTo(before),
      "Re-writing the parquet file with different content must produce a new fingerprint.");
  }
}
