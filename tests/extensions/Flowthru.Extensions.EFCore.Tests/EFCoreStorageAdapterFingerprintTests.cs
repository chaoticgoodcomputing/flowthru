using Flowthru.Data.Catalog;
using Flowthru.Data.Storage;
using Flowthru.Data.Storage.EFCore;
using Flowthru.Extensions.EFCore.Tests.Fixtures;
using Flowthru.Prelude;
using Microsoft.EntityFrameworkCore;

namespace Flowthru.Extensions.EFCore.Tests;

/// <summary>
/// Verifies the <see cref="EFCoreStorageAdapter{T}.WithFingerprintColumn"/>
/// opt-in. Adapters without a fingerprint column remain
/// uncacheable; adapters with one expose
/// <see cref="ISupportsFingerprint"/> and produce a stable digest
/// derived from <c>SELECT COUNT(*), MAX(&lt;column&gt;) FROM &lt;table&gt;</c>.
/// </summary>
[TestFixture]
[Category("EFCore")]
public class EFCoreStorageAdapterFingerprintTests
{
  private IDbContextFactory<TestDbContext> _factory = null!;
  private string _dbPath = null!;

  [SetUp]
  public void SetUp()
  {
    (_factory, _dbPath) = TestDbContextFactoryBuilder.Build();
  }

  [TearDown]
  public void TearDown()
  {
    if (File.Exists(_dbPath))
    {
      try { File.Delete(_dbPath); } catch { /* best effort */ }
    }
  }

  private EFCoreStorageAdapter<TimestampedEntity> BareAdapter() =>
    new(() => _factory.CreateDbContext());

  // ── Opt-in shape ──────────────────────────────────────────────────────

  [Test]
  public void BareAdapter_DoesNotImplement_ISupportsFingerprint()
  {
    var adapter = BareAdapter();
    Assert.That(adapter, Is.Not.InstanceOf<ISupportsFingerprint>(),
      "EF Core adapters without WithFingerprintColumn(...) remain uncacheable — "
      + "the cache plan must not silently observe a fingerprint on tables the "
      + "catalog author didn't opt in.");
  }

  [Test]
  public void WithFingerprintColumn_ReturnsFingerprintingAdapter()
  {
    var fp = BareAdapter().WithFingerprintColumn(e => e.UpdatedAt);
    Assert.That(fp, Is.InstanceOf<ISupportsFingerprint>(),
      "The configurator returns an adapter that opts into ISupportsFingerprint.");
    Assert.That(fp.FingerprintColumnName, Is.EqualTo(nameof(TimestampedEntity.UpdatedAt)));
  }

  [Test]
  public void WithFingerprintColumn_RejectsNonPropertyExpression()
  {
    Assert.That(
      () => BareAdapter().WithFingerprintColumn(e => DateTime.UtcNow),
      Throws.InstanceOf<ArgumentException>(),
      "Computed/non-property selectors fail fast — the framework reflects the column name."
    );
  }

  // ── Fingerprint behaviour ─────────────────────────────────────────────

  [Test]
  public async Task Fingerprint_EmptyTable_ReturnsStableValue()
  {
    var fp = BareAdapter().WithFingerprintColumn(e => e.UpdatedAt);
    var first = await fp.Fingerprint().Run();
    var second = await fp.Fingerprint().Run();
    Assert.That(first, Is.InstanceOf<EffResult<string>.Success>());
    Assert.That(
      ((EffResult<string>.Success)second).Value,
      Is.EqualTo(((EffResult<string>.Success)first).Value),
      "Empty table fingerprint must be stable across calls."
    );
  }

  [Test]
  public async Task Fingerprint_ChangesWhenRowAdded()
  {
    var adapter = BareAdapter();
    var fp = adapter.WithFingerprintColumn(e => e.UpdatedAt);
    var before = ((EffResult<string>.Success)await fp.Fingerprint().Run()).Value;

    await adapter
      .Save(new[]
      {
        new TimestampedEntity { Id = 1, Name = "a", UpdatedAt = DateTime.UtcNow },
      })
      .Run();
    var after = ((EffResult<string>.Success)await fp.Fingerprint().Run()).Value;

    Assert.That(after, Is.Not.EqualTo(before),
      "Adding a row must change the fingerprint (COUNT increases).");
  }

  [Test]
  public async Task Fingerprint_ChangesWhenMaxUpdatedAtAdvances()
  {
    var adapter = BareAdapter();
    var fp = adapter.WithFingerprintColumn(e => e.UpdatedAt);

    var t0 = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    await adapter
      .Save(new[]
      {
        new TimestampedEntity { Id = 1, Name = "a", UpdatedAt = t0 },
      })
      .Run();
    var before = ((EffResult<string>.Success)await fp.Fingerprint().Run()).Value;

    // Replace the row with a newer UpdatedAt — count stays at 1, but MAX advances.
    await adapter
      .Save(new[]
      {
        new TimestampedEntity { Id = 1, Name = "a", UpdatedAt = t0.AddDays(1) },
      })
      .Run();
    var after = ((EffResult<string>.Success)await fp.Fingerprint().Run()).Value;

    Assert.That(after, Is.Not.EqualTo(before),
      "Bumping MAX(UpdatedAt) must change the fingerprint even when COUNT is constant.");
  }

  [Test]
  public async Task Fingerprint_StableAcrossRepeatCalls_NoMutationBetween()
  {
    var adapter = BareAdapter();
    var fp = adapter.WithFingerprintColumn(e => e.UpdatedAt);
    await adapter
      .Save(new[]
      {
        new TimestampedEntity { Id = 1, Name = "x", UpdatedAt = DateTime.UtcNow },
        new TimestampedEntity { Id = 2, Name = "y", UpdatedAt = DateTime.UtcNow },
      })
      .Run();

    var first = ((EffResult<string>.Success)await fp.Fingerprint().Run()).Value;
    var second = ((EffResult<string>.Success)await fp.Fingerprint().Run()).Value;
    var third = ((EffResult<string>.Success)await fp.Fingerprint().Run()).Value;

    Assert.That(second, Is.EqualTo(first));
    Assert.That(third, Is.EqualTo(first));
  }

  // ── Item-level plumbing ──────────────────────────────────────────────

  [Test]
  public void Item_OverFingerprintingAdapter_TryGetFingerprint_NonNull()
  {
    var item = new Item<IEnumerable<TimestampedEntity>>(
      "ts",
      BareAdapter().WithFingerprintColumn(e => e.UpdatedAt)
    );
    Assert.That(item.TryGetFingerprint(), Is.Not.Null);
  }

  [Test]
  public void Item_OverBareEFCoreAdapter_TryGetFingerprint_ReturnsNull()
  {
    var item = new Item<IEnumerable<TimestampedEntity>>("ts", BareAdapter());
    Assert.That(item.TryGetFingerprint(), Is.Null,
      "Without WithFingerprintColumn(...), the EF Core item is uncacheable; "
      + "TryGetFingerprint must return null so the cache plan downgrades dependents.");
  }
}
