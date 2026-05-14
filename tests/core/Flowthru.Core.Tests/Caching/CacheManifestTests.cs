using Flowthru.Caching;

namespace Flowthru.Core.Tests.Caching;

/// <summary>
/// Surface tests for the cache manifest record types. The records
/// themselves are simple; the tests pin down invariants the rest of
/// Phase 6 relies on: the empty-manifest default carries the current
/// schema version, schema-mismatch detection works, and the
/// <see cref="NodeFingerprint"/> equality semantics line up with the
/// last-write-wins merge in <see cref="CacheManifestStore"/>.
/// </summary>
[TestFixture]
public class CacheManifestTests
{
  [Test]
  public void Empty_CarriesCurrentSchemaVersion()
  {
    Assert.That(CacheManifest.Empty.SchemaVersion,
      Is.EqualTo(CacheManifestSchema.CurrentVersion));
    Assert.That(CacheManifest.Empty.Entries, Is.Empty);
    Assert.That(CacheManifest.Empty.IsCurrentSchema(), Is.True);
  }

  [Test]
  public void IsCurrentSchema_FalseWhenVersionMismatches()
  {
    var stale = new CacheManifest(
      CacheManifestSchema.CurrentVersion - 1,
      new Dictionary<string, NodeFingerprint>());
    Assert.That(stale.IsCurrentSchema(), Is.False);
  }

  [Test]
  public void NodeFingerprint_RecordsCompareByValue()
  {
    var t = DateTimeOffset.UtcNow;
    var a = new NodeFingerprint("abc", t);
    var b = new NodeFingerprint("abc", t);
    var c = new NodeFingerprint("abc", t.AddSeconds(1));

    Assert.That(b, Is.EqualTo(a),
      "Records with the same Value and RecordedAt should be equal.");
    Assert.That(c, Is.Not.EqualTo(a),
      "Records differing only in RecordedAt must compare unequal — the merge "
      + "path relies on timestamp-based ordering, not value-based collapse.");
  }
}
