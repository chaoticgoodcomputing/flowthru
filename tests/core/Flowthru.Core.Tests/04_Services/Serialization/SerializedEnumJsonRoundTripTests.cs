using Flowthru.Core.Data.Storage;
using Flowthru.Core.Serialization;
using Flowthru.Tests.Kits.Storage;
using Flowthru.Tests.Kits.Schemas;

namespace Flowthru.Core.Tests.Services.Serialization;

/// <summary>
/// JSON round-trip tests for <c>[SerializedEnum]</c> infrastructure. Drives the full Core
/// enum chain end-to-end via <see cref="SingletonJsonStorageAdapter{T}"/>:
/// <c>SerializedEnumJsonConverter.Read/Write/.ctor</c>,
/// <c>SerializedEnumJsonConverterFactory.CreateConverter</c>,
/// <c>EnumMetadataCache</c>, <c>EnumMetadataRegistry.Create</c>,
/// <c>EnumSerializationHelper.ParseEnumFromString</c>,
/// and <c>SerializedEnumAttribute..ctor</c>.
/// </summary>
[TestFixture]
[Category("Services")]
[Category("Serialization")]
public class SerializedEnumJsonRoundTripTests
{
  private string _tempDir = null!;

  [SetUp]
  public void SetUp()
  {
    _tempDir = Path.Combine(Path.GetTempPath(), $"flowthru-enum-test-{Guid.NewGuid():N}");
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

  // ─────────────────────────────────────────────────────────────────────────
  // Round-trip via the storage adapter harness
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public Task CheckStatusSchema_RoundTripsThroughJson()
  {
    var path = Path.Combine(_tempDir, "check-status.json");
    var adapter = new SingletonJsonStorageAdapter<CheckStatusSchema>(path);
    var data = new CheckStatusSchema { Id = Guid.NewGuid(), Status = CheckStatus.Complete };

    return StorageAdapterAssertions.SaveAndLoadRoundTrips(adapter, data);
  }

  [Test]
  public Task MultiEnumSchema_RoundTripsAcrossDistinctEnumTypes()
  {
    var path = Path.Combine(_tempDir, "multi-enum.json");
    var adapter = new SingletonJsonStorageAdapter<MultiEnumSchema>(path);
    var data = new MultiEnumSchema
    {
      Id = Guid.NewGuid(),
      PrimaryStatus = CheckStatus.Complete,
      SecondaryStatus = CheckStatus.Incomplete,
      Rarity = Rarity.MythicRare,
    };

    return StorageAdapterAssertions.SaveAndLoadRoundTrips(adapter, data);
  }

  [Test]
  public Task OptionalEnumSchema_PresentValue_RoundTrips()
  {
    var path = Path.Combine(_tempDir, "optional-enum-present.json");
    var adapter = new SingletonJsonStorageAdapter<OptionalEnumSchema>(path);
    var data = new OptionalEnumSchema { Id = Guid.NewGuid(), Status = CheckStatus.Complete };

    return StorageAdapterAssertions.SaveAndLoadRoundTrips(adapter, data);
  }

  [Test]
  public Task OptionalEnumSchema_NullValue_RoundTrips()
  {
    var path = Path.Combine(_tempDir, "optional-enum-null.json");
    var adapter = new SingletonJsonStorageAdapter<OptionalEnumSchema>(path);
    var data = new OptionalEnumSchema { Id = Guid.NewGuid(), Status = null };

    return StorageAdapterAssertions.SaveAndLoadRoundTrips(adapter, data);
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Wire-format regression — the on-disk JSON must contain the [SerializedEnum]
  // string mapping, not the C# member name. Round-trip tests above only verify
  // symmetry (write X, read X back); a symmetric bug — e.g., the singleton
  // adapter dropping the SerializedEnumJsonConverterFactory and writing
  // "Complete"/"Mythic_Rare" instead of "t"/"mythic_rare" — would still
  // round-trip correctly but produce files unreadable by any other tool that
  // expects the declared serialized strings. This was the actual Phase 6 bug:
  // SingletonJsonStorageAdapter registered only SerializedLabelJsonConverterFactory,
  // silently dropping [SerializedEnum] mappings on the wire.
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task CheckStatusSchema_OnDiskJson_UsesSerializedEnumString()
  {
    var path = Path.Combine(_tempDir, "wire-format.json");
    var adapter = new SingletonJsonStorageAdapter<CheckStatusSchema>(path);
    var data = new CheckStatusSchema { Id = Guid.NewGuid(), Status = CheckStatus.Complete };

    await adapter.Save(data).Run();
    var fileContent = await File.ReadAllTextAsync(path);

    Assert.That(
      fileContent,
      Does.Contain("\"status\": \"t\""),
      "Singleton JSON adapter must serialize [SerializedEnum(\"t\")] Complete as \"t\". "
        + "Seeing \"Complete\" instead means SerializedEnumJsonConverterFactory was not "
        + "registered in JsonSerializerOptions.Converters."
    );
    Assert.That(
      fileContent,
      Does.Not.Contain("\"Complete\""),
      "Wire format leaks the C# enum member name; SerializedEnum factory missing or out of order."
    );
  }

  [Test]
  public async Task RaritySnakeCase_OnDiskJson_UsesSerializedEnumString()
  {
    var path = Path.Combine(_tempDir, "wire-rarity.json");
    var adapter = new SingletonJsonStorageAdapter<MultiEnumSchema>(path);
    var data = new MultiEnumSchema
    {
      Id = Guid.NewGuid(),
      PrimaryStatus = CheckStatus.Complete,
      SecondaryStatus = CheckStatus.Incomplete,
      Rarity = Rarity.MythicRare,
    };

    await adapter.Save(data).Run();
    var fileContent = await File.ReadAllTextAsync(path);

    Assert.That(
      fileContent,
      Does.Contain("\"rarity\": \"mythic_rare\""),
      "[SerializedEnum(\"mythic_rare\")] MythicRare must serialize as \"mythic_rare\", "
        + "not as the C# member name."
    );
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Direct EnumMetadataCache assertions — verify the cache produces the
  // expected mappings before they're consumed by the JSON converter.
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void EnumMetadataCache_CheckStatus_ProducesExpectedMappings()
  {
    var cache = new EnumMetadataCache<CheckStatus>();

    // Forward: enum → string
    Assert.That(cache.ToString(CheckStatus.Complete), Is.EqualTo("t"));
    Assert.That(cache.ToString(CheckStatus.Incomplete), Is.EqualTo("f"));

    // Reverse: string → enum
    Assert.That(cache.Parse("t"), Is.EqualTo(CheckStatus.Complete));
    Assert.That(cache.Parse("f"), Is.EqualTo(CheckStatus.Incomplete));

    // GetValues / GetSerializedValues enumerate the full set
    Assert.That(cache.GetValues(), Is.EquivalentTo(new[] { CheckStatus.Complete, CheckStatus.Incomplete }));
    Assert.That(cache.GetSerializedValues(), Is.EquivalentTo(new[] { "t", "f" }));
  }

  [Test]
  public void EnumMetadataCache_TryParse_UnknownValue_ReturnsFalse()
  {
    var cache = new EnumMetadataCache<CheckStatus>();

    Assert.That(cache.TryParse("not-a-real-value", out _), Is.False);
    Assert.That(cache.TryParse("t", out var parsed), Is.True);
    Assert.That(parsed, Is.EqualTo(CheckStatus.Complete));
  }

  [Test]
  public void EnumMetadataCache_TryToString_DefinedValue_ReturnsTrue()
  {
    var cache = new EnumMetadataCache<CheckStatus>();

    Assert.That(cache.TryToString(CheckStatus.Complete, out var serialized), Is.True);
    Assert.That(serialized, Is.EqualTo("t"));
  }

  [Test]
  public void EnumMetadataCache_Rarity_HandlesSnakeCaseMapping()
  {
    var cache = new EnumMetadataCache<Rarity>();

    Assert.That(cache.ToString(Rarity.MythicRare), Is.EqualTo("mythic_rare"));
    Assert.That(cache.Parse("mythic_rare"), Is.EqualTo(Rarity.MythicRare));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // EnumMetadataRegistry — verifies the registry creates and caches
  // metadata correctly.
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void EnumMetadataRegistry_Create_ProducesUsableMetadata()
  {
    var metadata = EnumMetadataRegistry.Create<CheckStatus>();

    Assert.That(metadata, Is.Not.Null);
    // Round-trip through the runtime metadata path
    var serialized = metadata.ToString(CheckStatus.Complete);
    Assert.That(serialized, Is.EqualTo("t"));
  }
}
