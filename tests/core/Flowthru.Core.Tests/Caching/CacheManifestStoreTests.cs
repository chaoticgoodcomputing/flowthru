using Flowthru.Caching;
using Flowthru.Data.Catalog;

namespace Flowthru.Core.Tests.Caching;

/// <summary>
/// Load, upsert, and last-write-wins-merge behaviour of
/// <see cref="CacheManifestStore"/>. Tests use a real file-backed
/// JSON item to exercise the round-trip path the default
/// <c>UseCacheStorage</c> uses.
/// </summary>
[TestFixture]
public class CacheManifestStoreTests
{
  private string _tempDir = null!;

  [SetUp]
  public void SetUp()
  {
    _tempDir = Path.Combine(Path.GetTempPath(), $"flowthru-manifest-{Guid.NewGuid():N}");
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
  public async Task LoadAsync_FileMissing_ReturnsEmpty()
  {
    var item = MakeManifestItem("missing.json");
    var loaded = await CacheManifestStore.LoadAsync(item);

    Assert.That(loaded, Is.SameAs(CacheManifest.Empty),
      "Absent file collapses to the canonical empty manifest — no I/O failure surfaces.");
  }

  [Test]
  public async Task LoadAsync_RoundTripsManifest()
  {
    var item = MakeManifestItem("round-trip.json");
    var original = new CacheManifest(
      CacheManifestSchema.CurrentVersion,
      new Dictionary<string, NodeFingerprint>(StringComparer.Ordinal)
      {
        ["alpha"] = new NodeFingerprint("hash-alpha", new DateTimeOffset(2026, 5, 14, 12, 0, 0, TimeSpan.Zero)),
        ["beta"] = new NodeFingerprint("hash-beta", new DateTimeOffset(2026, 5, 14, 12, 1, 0, TimeSpan.Zero)),
      });

    await item.Save(original).Run();

    var loaded = await CacheManifestStore.LoadAsync(item);

    Assert.That(loaded.Entries, Has.Count.EqualTo(2));
    Assert.That(loaded.Entries["alpha"].Value, Is.EqualTo("hash-alpha"));
    Assert.That(loaded.Entries["beta"].Value, Is.EqualTo("hash-beta"));
  }

  [Test]
  public async Task LoadAsync_SchemaMismatchCollapsesToEmpty()
  {
    var item = MakeManifestItem("stale-schema.json");
    var stale = new CacheManifest(
      CacheManifestSchema.CurrentVersion - 1,
      new Dictionary<string, NodeFingerprint>(StringComparer.Ordinal)
      {
        ["alpha"] = new NodeFingerprint("hash-alpha", DateTimeOffset.UtcNow),
      });
    await item.Save(stale).Run();

    var loaded = await CacheManifestStore.LoadAsync(item);

    Assert.That(loaded, Is.SameAs(CacheManifest.Empty),
      "A loaded manifest carrying the wrong schema version is silently absorbed — "
      + "callers see Empty, the next successful run re-records every entry.");
  }

  [Test]
  public async Task UpsertEntriesAsync_AddsNewEntries()
  {
    var item = MakeManifestItem("upsert-new.json");
    var now = DateTimeOffset.UtcNow;
    var newEntries = new Dictionary<string, string>(StringComparer.Ordinal)
    {
      ["alpha"] = "hash-alpha",
      ["beta"] = "hash-beta",
    };

    await CacheManifestStore.UpsertEntriesAsync(item, newEntries, now);

    var loaded = await CacheManifestStore.LoadAsync(item);
    Assert.That(loaded.Entries["alpha"].Value, Is.EqualTo("hash-alpha"));
    Assert.That(loaded.Entries["alpha"].RecordedAt, Is.EqualTo(now));
    Assert.That(loaded.Entries["beta"].Value, Is.EqualTo("hash-beta"));
  }

  [Test]
  public async Task UpsertEntriesAsync_LaterTimestampWins()
  {
    var item = MakeManifestItem("upsert-lww.json");
    var early = new DateTimeOffset(2026, 5, 14, 12, 0, 0, TimeSpan.Zero);
    var late = new DateTimeOffset(2026, 5, 14, 12, 1, 0, TimeSpan.Zero);

    // Step 1 — write the initial value at the late timestamp.
    await CacheManifestStore.UpsertEntriesAsync(
      item,
      new Dictionary<string, string>(StringComparer.Ordinal) { ["alpha"] = "later-value" },
      late);

    // Step 2 — try to write an earlier-timestamped update. LWW should reject it.
    await CacheManifestStore.UpsertEntriesAsync(
      item,
      new Dictionary<string, string>(StringComparer.Ordinal) { ["alpha"] = "earlier-value" },
      early);

    var loaded = await CacheManifestStore.LoadAsync(item);
    Assert.That(loaded.Entries["alpha"].Value, Is.EqualTo("later-value"),
      "An earlier-timestamped upsert must not overwrite a later-timestamped entry — "
      + "this is how concurrent runs avoid losing each other's writes.");
  }

  [Test]
  public async Task UpsertEntriesAsync_PreservesDisjointEntriesFromConcurrentWrite()
  {
    // Simulate two concurrent processes: A writes entries {x, y}; B
    // writes entries {y, z}. The merge path should preserve x AND z,
    // and resolve y by greater timestamp.
    var item = MakeManifestItem("upsert-concurrent.json");
    var tEarly = new DateTimeOffset(2026, 5, 14, 12, 0, 0, TimeSpan.Zero);
    var tLate = new DateTimeOffset(2026, 5, 14, 12, 5, 0, TimeSpan.Zero);

    await CacheManifestStore.UpsertEntriesAsync(
      item,
      new Dictionary<string, string>(StringComparer.Ordinal)
      {
        ["x"] = "x-from-A",
        ["y"] = "y-from-A",
      },
      tEarly);

    await CacheManifestStore.UpsertEntriesAsync(
      item,
      new Dictionary<string, string>(StringComparer.Ordinal)
      {
        ["y"] = "y-from-B",
        ["z"] = "z-from-B",
      },
      tLate);

    var loaded = await CacheManifestStore.LoadAsync(item);
    Assert.That(loaded.Entries["x"].Value, Is.EqualTo("x-from-A"),
      "Process B's save must preserve A's disjoint entry.");
    Assert.That(loaded.Entries["y"].Value, Is.EqualTo("y-from-B"),
      "Process B's later write to the same key wins.");
    Assert.That(loaded.Entries["z"].Value, Is.EqualTo("z-from-B"),
      "Process B's new entry must land.");
  }

  [Test]
  public async Task UpsertEntriesAsync_EmptyInputIsNoOp()
  {
    var item = MakeManifestItem("upsert-empty.json");
    await CacheManifestStore.UpsertEntriesAsync(
      item,
      new Dictionary<string, string>(StringComparer.Ordinal),
      DateTimeOffset.UtcNow);

    // No file should have been created since there was nothing to write.
    Assert.That(File.Exists(Path.Combine(_tempDir, "upsert-empty.json")), Is.False);
  }

  private IItem<CacheManifest> MakeManifestItem(string filename) =>
    Item.Of<CacheManifest>($"cache-{filename}")
      .Json()
      .AtPath(Path.Combine(_tempDir, filename))
      .Build();
}
