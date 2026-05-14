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
    var t1 = new DateTimeOffset(2026, 5, 14, 12, 0, 0, TimeSpan.Zero);
    var t2 = new DateTimeOffset(2026, 5, 14, 12, 1, 0, TimeSpan.Zero);
    var original = new CacheManifest(
      CacheManifestSchema.CurrentVersion,
      new Dictionary<string, NodeFingerprint>(StringComparer.Ordinal)
      {
        ["step-alpha"] = new NodeFingerprint("hash-step-alpha", t1),
      },
      new Dictionary<string, NodeFingerprint>(StringComparer.Ordinal)
      {
        ["item-beta"] = new NodeFingerprint("hash-item-beta", t2),
      });

    await item.Save(original).Run();

    var loaded = await CacheManifestStore.LoadAsync(item);

    Assert.That(loaded.Steps, Has.Count.EqualTo(1));
    Assert.That(loaded.Steps["step-alpha"].Value, Is.EqualTo("hash-step-alpha"));
    Assert.That(loaded.Items, Has.Count.EqualTo(1));
    Assert.That(loaded.Items["item-beta"].Value, Is.EqualTo("hash-item-beta"));
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
      },
      new Dictionary<string, NodeFingerprint>(StringComparer.Ordinal));
    await item.Save(stale).Run();

    var loaded = await CacheManifestStore.LoadAsync(item);

    Assert.That(loaded, Is.SameAs(CacheManifest.Empty),
      "A loaded manifest carrying the wrong schema version is silently absorbed — "
      + "callers see Empty, the next successful run re-records every entry.");
  }

  [Test]
  public async Task UpsertEntriesAsync_AddsNewStepAndItemEntries()
  {
    var item = MakeManifestItem("upsert-new.json");
    var now = DateTimeOffset.UtcNow;
    var newStepEntries = new Dictionary<string, string>(StringComparer.Ordinal)
    {
      ["step-alpha"] = "hash-step-alpha",
    };
    var newItemEntries = new Dictionary<string, string>(StringComparer.Ordinal)
    {
      ["item-beta"] = "hash-item-beta",
    };

    await CacheManifestStore.UpsertEntriesAsync(item, newStepEntries, newItemEntries, now);

    var loaded = await CacheManifestStore.LoadAsync(item);
    Assert.That(loaded.Steps["step-alpha"].Value, Is.EqualTo("hash-step-alpha"));
    Assert.That(loaded.Steps["step-alpha"].RecordedAt, Is.EqualTo(now));
    Assert.That(loaded.Items["item-beta"].Value, Is.EqualTo("hash-item-beta"));
    Assert.That(loaded.Items["item-beta"].RecordedAt, Is.EqualTo(now));
  }

  [Test]
  public async Task UpsertEntriesAsync_LaterTimestampWins()
  {
    var item = MakeManifestItem("upsert-lww.json");
    var early = new DateTimeOffset(2026, 5, 14, 12, 0, 0, TimeSpan.Zero);
    var late = new DateTimeOffset(2026, 5, 14, 12, 1, 0, TimeSpan.Zero);
    var empty = new Dictionary<string, string>(StringComparer.Ordinal);

    // Step 1 — write the initial value at the late timestamp.
    await CacheManifestStore.UpsertEntriesAsync(
      item,
      new Dictionary<string, string>(StringComparer.Ordinal) { ["alpha"] = "later-value" },
      empty,
      late);

    // Step 2 — try to write an earlier-timestamped update. LWW should reject it.
    await CacheManifestStore.UpsertEntriesAsync(
      item,
      new Dictionary<string, string>(StringComparer.Ordinal) { ["alpha"] = "earlier-value" },
      empty,
      early);

    var loaded = await CacheManifestStore.LoadAsync(item);
    Assert.That(loaded.Steps["alpha"].Value, Is.EqualTo("later-value"),
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
    var empty = new Dictionary<string, string>(StringComparer.Ordinal);

    await CacheManifestStore.UpsertEntriesAsync(
      item,
      new Dictionary<string, string>(StringComparer.Ordinal)
      {
        ["x"] = "x-from-A",
        ["y"] = "y-from-A",
      },
      empty,
      tEarly);

    await CacheManifestStore.UpsertEntriesAsync(
      item,
      new Dictionary<string, string>(StringComparer.Ordinal)
      {
        ["y"] = "y-from-B",
        ["z"] = "z-from-B",
      },
      empty,
      tLate);

    var loaded = await CacheManifestStore.LoadAsync(item);
    Assert.That(loaded.Steps["x"].Value, Is.EqualTo("x-from-A"),
      "Process B's save must preserve A's disjoint entry.");
    Assert.That(loaded.Steps["y"].Value, Is.EqualTo("y-from-B"),
      "Process B's later write to the same key wins.");
    Assert.That(loaded.Steps["z"].Value, Is.EqualTo("z-from-B"),
      "Process B's new entry must land.");
  }

  [Test]
  public async Task UpsertEntriesAsync_EmptyInputIsNoOp()
  {
    var item = MakeManifestItem("upsert-empty.json");
    var empty = new Dictionary<string, string>(StringComparer.Ordinal);
    await CacheManifestStore.UpsertEntriesAsync(
      item,
      empty,
      empty,
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
