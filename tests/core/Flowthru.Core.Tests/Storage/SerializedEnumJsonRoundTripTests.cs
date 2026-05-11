using Flowthru.Data.Schema;
using Flowthru.Data.Schema.Mapping;
using Flowthru.Data.Storage;
using Flowthru.Prelude;
using SysIO = System.IO;

namespace Flowthru.Core.Tests.Storage;

// ──────────────────────────────────────────────────────────────────────────
// Local fixtures — Kit's SerializedEnumTestSchemas are excluded from
// compilation in the FP rewrite, so we inline schema types here. Reuses the
// SeJsonCheckStatus / SeJsonRarity enums declared in
// SerializedEnumJsonConverterTests.cs (same project, same namespace).
// ──────────────────────────────────────────────────────────────────────────

[FlowthruSchema]
public partial record SeJsonRtCheckStatusSchema
{
  [SerializedLabel("id")]
  public required Guid Id { get; init; }

  [SerializedLabel("status")]
  public required SeJsonCheckStatus Status { get; init; }
}

[FlowthruSchema]
public partial record SeJsonRtMultiEnumSchema
{
  [SerializedLabel("id")]
  public required Guid Id { get; init; }

  [SerializedLabel("primary_status")]
  public required SeJsonCheckStatus PrimaryStatus { get; init; }

  [SerializedLabel("secondary_status")]
  public required SeJsonCheckStatus SecondaryStatus { get; init; }

  [SerializedLabel("rarity")]
  public required SeJsonRarity Rarity { get; init; }
}

[FlowthruSchema]
public partial record SeJsonRtOptionalEnumSchema
{
  [SerializedLabel("id")]
  public required Guid Id { get; init; }

  [SerializedLabel("status")]
  public SeJsonCheckStatus? Status { get; init; }
}

/// <summary>
/// End-to-end JSON-on-disk round-trips for <c>[SerializedEnum]</c>
/// schemas via <see cref="SingletonJsonAdapter{T}"/>, plus direct
/// assertions on <see cref="SerializedEnumMappings"/> / planner output.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Port-back status.</strong> On <c>main</c> these tests asserted
/// both symmetric round-trip and wire-format correctness against the
/// <c>SerializedEnumJsonConverter</c> chain. The FP rewrite has the
/// <c>SerializedEnumMappings</c> / <c>EnumBindingInfo</c> infrastructure in
/// place but has not yet ported the JSON converter — so:
/// </para>
/// <list type="bullet">
///   <item>Planner / mappings tests pin the active behaviour and pass today.</item>
///   <item>Symmetric round-trip tests are <c>[Ignore]</c>d: System.Text.Json's
///         default enum handling serializes enums as integers (not the
///         declared <c>[SerializedEnum]</c> strings), so the wire format is
///         wrong even when symmetric round-trip might accidentally succeed.</item>
///   <item>Wire-format tests are <c>[Ignore]</c>d for the same reason.</item>
/// </list>
/// <para>
/// When the converter ports back, removing the <c>[Ignore]</c> attributes
/// should be enough to re-enable the full suite.
/// </para>
/// </remarks>
[TestFixture]
public class SerializedEnumJsonRoundTripTests
{
  private string _tempDir = null!;

  [SetUp]
  public void SetUp()
  {
    _tempDir = SysIO.Path.Combine(
      SysIO.Path.GetTempPath(), $"flowthru-enum-rt-{Guid.NewGuid():N}"
    );
    SysIO.Directory.CreateDirectory(_tempDir);
  }

  [TearDown]
  public void TearDown()
  {
    if (SysIO.Directory.Exists(_tempDir))
    {
      try { SysIO.Directory.Delete(_tempDir, recursive: true); }
      catch { /* best-effort */ }
    }
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Direct EnumBindingInfo assertions via planner output — verify the
  // mapping cache is ready for consumers (CSV, Excel, Parquet, JSON-once-
  // ported) before they need it.
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void EnumBinding_CheckStatusSchema_ProducesExpectedForwardMappings()
  {
    var plan = PropertyMappingPlanner.Build<SeJsonRtCheckStatusSchema>();
    var binding = plan.Bindings.Single(b => b.Property.Name == nameof(SeJsonRtCheckStatusSchema.Status));

    Assert.That(binding.Kind, Is.EqualTo(PropertyKind.Enum));
    Assert.That(binding.Enum, Is.Not.Null);
    Assert.That(binding.Enum!.Forward[SeJsonCheckStatus.Complete], Is.EqualTo("t"));
    Assert.That(binding.Enum.Forward[SeJsonCheckStatus.Incomplete], Is.EqualTo("f"));
  }

  [Test]
  public void EnumBinding_CheckStatusSchema_ProducesExpectedReverseMappings()
  {
    var plan = PropertyMappingPlanner.Build<SeJsonRtCheckStatusSchema>();
    var binding = plan.Bindings.Single(b => b.Property.Name == nameof(SeJsonRtCheckStatusSchema.Status));

    Assert.That(binding.Enum!.Reverse["t"], Is.EqualTo(SeJsonCheckStatus.Complete));
    Assert.That(binding.Enum.Reverse["f"], Is.EqualTo(SeJsonCheckStatus.Incomplete));
  }

  [Test]
  public void EnumBinding_MultiEnumSchema_ResolvesDistinctEnumTypesIndependently()
  {
    var plan = PropertyMappingPlanner.Build<SeJsonRtMultiEnumSchema>();

    var primary = plan.Bindings.Single(b => b.Property.Name == nameof(SeJsonRtMultiEnumSchema.PrimaryStatus));
    var rarity = plan.Bindings.Single(b => b.Property.Name == nameof(SeJsonRtMultiEnumSchema.Rarity));

    Assert.That(primary.Enum!.EnumType, Is.EqualTo(typeof(SeJsonCheckStatus)));
    Assert.That(rarity.Enum!.EnumType, Is.EqualTo(typeof(SeJsonRarity)));

    // Distinct mapping namespaces — no cross-contamination.
    Assert.That(primary.Enum.Reverse.Keys, Is.EquivalentTo(new[] { "t", "f" }));
    Assert.That(
      rarity.Enum.Reverse.Keys,
      Is.EquivalentTo(new[] { "common", "uncommon", "rare", "mythic_rare" })
    );
  }

  [Test]
  public void EnumBinding_OptionalEnumSchema_NullableEnumIsBoundWithIsNullableTrue()
  {
    var plan = PropertyMappingPlanner.Build<SeJsonRtOptionalEnumSchema>();
    var binding = plan.Bindings.Single(b => b.Property.Name == nameof(SeJsonRtOptionalEnumSchema.Status));

    Assert.That(binding.Kind, Is.EqualTo(PropertyKind.Enum));
    Assert.That(binding.IsNullable, Is.True);
    // Underlying enum type is exposed (not Nullable<T>) for downstream consumers.
    Assert.That(binding.Enum!.EnumType, Is.EqualTo(typeof(SeJsonCheckStatus)));
  }

  [Test]
  public void EnumBinding_RaritySchema_SnakeCaseMappingPreserved()
  {
    var plan = PropertyMappingPlanner.Build<SeJsonRtMultiEnumSchema>();
    var rarity = plan.Bindings.Single(b => b.Property.Name == nameof(SeJsonRtMultiEnumSchema.Rarity));

    Assert.That(rarity.Enum!.Forward[SeJsonRarity.MythicRare], Is.EqualTo("mythic_rare"));
    Assert.That(rarity.Enum.Reverse["mythic_rare"], Is.EqualTo(SeJsonRarity.MythicRare));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Symmetric round-trip via SingletonJsonAdapter — saving then loading
  // recovers the same enum values. Re-enable once converter ports back.
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task CheckStatusSchema_RoundTripsThroughJson()
  {
    var path = SysIO.Path.Combine(_tempDir, "check-status.json");
    var adapter = new SingletonJsonAdapter<SeJsonRtCheckStatusSchema>(path);
    var data = new SeJsonRtCheckStatusSchema { Id = Guid.NewGuid(), Status = SeJsonCheckStatus.Complete };

    await adapter.Save(data).Run();
    var loadResult = await adapter.Load().Run();

    Assert.That(loadResult, Is.InstanceOf<EffResult<SeJsonRtCheckStatusSchema>.Success>());
    var loaded = ((EffResult<SeJsonRtCheckStatusSchema>.Success)loadResult).Value;
    Assert.That(loaded, Is.EqualTo(data));
  }

  [Test]
  public async Task MultiEnumSchema_RoundTripsAcrossDistinctEnumTypes()
  {
    var path = SysIO.Path.Combine(_tempDir, "multi-enum.json");
    var adapter = new SingletonJsonAdapter<SeJsonRtMultiEnumSchema>(path);
    var data = new SeJsonRtMultiEnumSchema
    {
      Id = Guid.NewGuid(),
      PrimaryStatus = SeJsonCheckStatus.Complete,
      SecondaryStatus = SeJsonCheckStatus.Incomplete,
      Rarity = SeJsonRarity.MythicRare,
    };

    await adapter.Save(data).Run();
    var loadResult = await adapter.Load().Run();

    Assert.That(loadResult, Is.InstanceOf<EffResult<SeJsonRtMultiEnumSchema>.Success>());
    var loaded = ((EffResult<SeJsonRtMultiEnumSchema>.Success)loadResult).Value;
    Assert.That(loaded, Is.EqualTo(data));
  }

  [Test]
  public async Task OptionalEnumSchema_PresentValue_RoundTrips()
  {
    var path = SysIO.Path.Combine(_tempDir, "optional-enum-present.json");
    var adapter = new SingletonJsonAdapter<SeJsonRtOptionalEnumSchema>(path);
    var data = new SeJsonRtOptionalEnumSchema { Id = Guid.NewGuid(), Status = SeJsonCheckStatus.Complete };

    await adapter.Save(data).Run();
    var loadResult = await adapter.Load().Run();

    var loaded = ((EffResult<SeJsonRtOptionalEnumSchema>.Success)loadResult).Value;
    Assert.That(loaded.Status, Is.EqualTo(SeJsonCheckStatus.Complete));
  }

  [Test]
  public async Task OptionalEnumSchema_NullValue_RoundTrips()
  {
    var path = SysIO.Path.Combine(_tempDir, "optional-enum-null.json");
    var adapter = new SingletonJsonAdapter<SeJsonRtOptionalEnumSchema>(path);
    var data = new SeJsonRtOptionalEnumSchema { Id = Guid.NewGuid(), Status = null };

    await adapter.Save(data).Run();
    var loadResult = await adapter.Load().Run();

    var loaded = ((EffResult<SeJsonRtOptionalEnumSchema>.Success)loadResult).Value;
    Assert.That(loaded.Status, Is.Null);
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Wire-format regression — the on-disk JSON must contain the
  // [SerializedEnum] string, not the C# member name or the integer ordinal.
  // A symmetric round-trip will silently succeed even when this is wrong
  // (write 0, read 0 back), so these assertions are the actual signal.
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task CheckStatusSchema_OnDiskJson_UsesSerializedEnumString()
  {
    var path = SysIO.Path.Combine(_tempDir, "wire-format.json");
    var adapter = new SingletonJsonAdapter<SeJsonRtCheckStatusSchema>(path);
    var data = new SeJsonRtCheckStatusSchema { Id = Guid.NewGuid(), Status = SeJsonCheckStatus.Complete };

    await adapter.Save(data).Run();
    var fileContent = await SysIO.File.ReadAllTextAsync(path);

    Assert.That(
      fileContent,
      Does.Contain("\"status\": \"t\""),
      "Singleton JSON adapter must serialize [SerializedEnum(\"t\")] Complete as \"t\". "
        + "Seeing \"Complete\" or an integer means the SerializedEnum converter is not "
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
    var path = SysIO.Path.Combine(_tempDir, "wire-rarity.json");
    var adapter = new SingletonJsonAdapter<SeJsonRtMultiEnumSchema>(path);
    var data = new SeJsonRtMultiEnumSchema
    {
      Id = Guid.NewGuid(),
      PrimaryStatus = SeJsonCheckStatus.Complete,
      SecondaryStatus = SeJsonCheckStatus.Incomplete,
      Rarity = SeJsonRarity.MythicRare,
    };

    await adapter.Save(data).Run();
    var fileContent = await SysIO.File.ReadAllTextAsync(path);

    Assert.That(
      fileContent,
      Does.Contain("\"rarity\": \"mythic_rare\""),
      "[SerializedEnum(\"mythic_rare\")] MythicRare must serialize as \"mythic_rare\", "
        + "not as the C# member name or the integer ordinal."
    );
  }
}
