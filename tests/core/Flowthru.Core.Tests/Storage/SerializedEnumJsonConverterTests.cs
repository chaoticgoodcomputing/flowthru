using System.Text.Json;
using Flowthru.Data.Schema;
using Flowthru.Data.Schema.Mapping;
using Flowthru.Data.Storage;

namespace Flowthru.Core.Tests.Storage;

// ──────────────────────────────────────────────────────────────────────────
// Local fixtures — the broader Flowthru.Tests.Kits.Schemas fixtures are
// currently excluded from compilation in the FP rewrite (see
// tests/helpers/Flowthru.Tests.Kits/Flowthru.Tests.Kits.csproj), so we
// inline the minimum surface needed to exercise [SerializedEnum] here.
// ──────────────────────────────────────────────────────────────────────────

/// <summary>Two-member enum with abbreviated serialized strings.</summary>
public enum SeJsonCheckStatus
{
  [SerializedEnum("t")] Complete,
  [SerializedEnum("f")] Incomplete,
}

/// <summary>Four-member enum exercising snake_case serialization.</summary>
public enum SeJsonRarity
{
  [SerializedEnum("common")] Common,
  [SerializedEnum("uncommon")] Uncommon,
  [SerializedEnum("rare")] Rare,
  [SerializedEnum("mythic_rare")] MythicRare,
}

[FlowthruSchema]
public partial record SeJsonEnumOnlySchema
{
  public required SeJsonCheckStatus Status { get; init; }
}

/// <summary>
/// Pins the active <c>[SerializedEnum]</c> infrastructure in the FP-rewrite
/// surface — <see cref="SerializedEnumMappings.Build"/> and the planner's
/// <see cref="PropertyKind.Enum"/> classification with a populated
/// <see cref="EnumBindingInfo"/>.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Scope.</strong> On <c>main</c> these behaviours were tested through a
/// dedicated <c>SerializedEnumJsonConverter</c> / <c>SerializedEnumJsonConverterFactory</c>
/// chain wired into <c>JsonFormatSerializer</c>'s default options. In the FP
/// rewrite that converter has not yet been ported back — the JSON path falls
/// through to <c>System.Text.Json</c>'s default enum handling. Tests that
/// directly exercise the converter chain are present below but marked
/// <c>[Ignore]</c> until the converter lands.
/// </para>
/// <para>
/// <strong>What we pin today.</strong> The planner-level mappings (forward /
/// reverse / member enumeration) and the attribute validation rules. These
/// are the building blocks every format extension (CSV, Excel, JSON when the
/// converter ports back, Parquet) reads off the binding rather than
/// re-reflecting over the enum themselves.
/// </para>
/// </remarks>
[TestFixture]
public class SerializedEnumJsonConverterTests
{
  // ─────────────────────────────────────────────────────────────────────────
  // SerializedEnumMappings.Build — bidirectional mapping construction.
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void Build_ProducesForwardMappingsForEveryMember()
  {
    var (forward, _) = SerializedEnumMappings.Build(typeof(SeJsonCheckStatus));

    Assert.That(forward[SeJsonCheckStatus.Complete], Is.EqualTo("t"));
    Assert.That(forward[SeJsonCheckStatus.Incomplete], Is.EqualTo("f"));
  }

  [Test]
  public void Build_ProducesReverseMappingsForEveryMember()
  {
    var (_, reverse) = SerializedEnumMappings.Build(typeof(SeJsonCheckStatus));

    Assert.That(reverse["t"], Is.EqualTo(SeJsonCheckStatus.Complete));
    Assert.That(reverse["f"], Is.EqualTo(SeJsonCheckStatus.Incomplete));
  }

  [Test]
  public void Build_RoundTripsEveryMemberThroughForwardThenReverse()
  {
    var (forward, reverse) = SerializedEnumMappings.Build(typeof(SeJsonRarity));

    foreach (var value in Enum.GetValues<SeJsonRarity>())
    {
      var serialized = forward[value];
      var deserialized = reverse[serialized];
      Assert.That(deserialized, Is.EqualTo(value), $"Round-trip failed for {value}.");
    }
  }

  [Test]
  public void Build_SnakeCaseMapping_IsPreservedVerbatim()
  {
    var (forward, reverse) = SerializedEnumMappings.Build(typeof(SeJsonRarity));

    Assert.That(forward[SeJsonRarity.MythicRare], Is.EqualTo("mythic_rare"));
    Assert.That(reverse["mythic_rare"], Is.EqualTo(SeJsonRarity.MythicRare));
  }

  [Test]
  public void Build_ReverseLookup_IsOrdinalCaseSensitive()
  {
    var (_, reverse) = SerializedEnumMappings.Build(typeof(SeJsonRarity));

    Assert.That(reverse.TryGetValue("Common", out _), Is.False,
      "Reverse lookup must be ordinal/case-sensitive — 'Common' != 'common'."
    );
    Assert.That(reverse.TryGetValue("common", out var v), Is.True);
    Assert.That(v, Is.EqualTo(SeJsonRarity.Common));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Build error paths.
  // ─────────────────────────────────────────────────────────────────────────

  public enum SeJsonMissingAttribute
  {
    [SerializedEnum("a")] Alpha,
    Bravo, // intentionally missing [SerializedEnum]
  }

  [Test]
  public void Build_MissingAttribute_ThrowsInvalidOperationException()
  {
    Assert.That(
      () => SerializedEnumMappings.Build(typeof(SeJsonMissingAttribute)),
      Throws.TypeOf<InvalidOperationException>()
        .With.Message.Contain("[SerializedEnum]")
    );
  }

  public enum SeJsonDuplicateValue
  {
    [SerializedEnum("dup")] One,
    [SerializedEnum("dup")] Two,
  }

  [Test]
  public void Build_DuplicateSerializedValue_ThrowsInvalidOperationException()
  {
    Assert.That(
      () => SerializedEnumMappings.Build(typeof(SeJsonDuplicateValue)),
      Throws.TypeOf<InvalidOperationException>()
        .With.Message.Contain("Duplicate serialized value")
    );
  }

  [Test]
  public void Build_NonEnumType_ThrowsArgumentException()
  {
    Assert.That(
      () => SerializedEnumMappings.Build(typeof(int)),
      Throws.TypeOf<ArgumentException>()
    );
  }

  [Test]
  public void Build_NullType_ThrowsArgumentNullException()
  {
    Assert.That(
      () => SerializedEnumMappings.Build(null!),
      Throws.TypeOf<ArgumentNullException>()
    );
  }

  // ─────────────────────────────────────────────────────────────────────────
  // PropertyMappingPlanner — verifies enums get the Enum kind + EnumBindingInfo.
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void Planner_EnumProperty_IsClassifiedAsEnumKind()
  {
    var plan = PropertyMappingPlanner.Build<SeJsonEnumOnlySchema>();
    Assert.That(plan.Bindings, Has.Count.EqualTo(1));

    var binding = plan.Bindings[0];
    Assert.That(binding.Kind, Is.EqualTo(PropertyKind.Enum));
    Assert.That(binding.Enum, Is.Not.Null);
    Assert.That(binding.Enum!.EnumType, Is.EqualTo(typeof(SeJsonCheckStatus)));
  }

  [Test]
  public void Planner_EnumProperty_CarriesFullForwardAndReverseMappings()
  {
    var plan = PropertyMappingPlanner.Build<SeJsonEnumOnlySchema>();
    var info = plan.Bindings[0].Enum!;

    Assert.That(info.Forward[SeJsonCheckStatus.Complete], Is.EqualTo("t"));
    Assert.That(info.Forward[SeJsonCheckStatus.Incomplete], Is.EqualTo("f"));
    Assert.That(info.Reverse["t"], Is.EqualTo(SeJsonCheckStatus.Complete));
    Assert.That(info.Reverse["f"], Is.EqualTo(SeJsonCheckStatus.Incomplete));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // JSON round-trip via JsonFormatSerializer's options — exercised through
  // plain JsonSerializer to pin the converter's per-enum behaviour. The
  // factory + per-type converter live at Data/Storage/SerializedEnumJsonConverter.cs;
  // JsonFormatSerializer.ctor registers the factory next to the SerializedLabel
  // factory, so any [SerializedEnum]-annotated enum surfaces here.
  // ─────────────────────────────────────────────────────────────────────────

  private static JsonSerializerOptions OptionsWithEnumConverter()
  {
    var serializer = new JsonFormatSerializer<SeJsonEnumOnlySchema>();
    return serializer.Options;
  }

  [Test]
  public void Write_SerializesEnumToConfiguredString()
  {
    var options = OptionsWithEnumConverter();
    var json = JsonSerializer.Serialize(SeJsonCheckStatus.Complete, options);
    Assert.That(json, Is.EqualTo("\"t\""));
  }

  [Test]
  public void Read_DeserializesConfiguredStringToEnum()
  {
    var options = OptionsWithEnumConverter();
    var value = JsonSerializer.Deserialize<SeJsonCheckStatus>("\"f\"", options);
    Assert.That(value, Is.EqualTo(SeJsonCheckStatus.Incomplete));
  }

  [Test]
  public void Read_NonStringToken_ThrowsJsonException()
  {
    var options = OptionsWithEnumConverter();
    Assert.That(
      () => JsonSerializer.Deserialize<SeJsonCheckStatus>("42", options),
      Throws.TypeOf<JsonException>()
    );
  }

  [Test]
  public void Read_UnknownStringValue_ThrowsJsonException()
  {
    var options = OptionsWithEnumConverter();
    Assert.That(
      () => JsonSerializer.Deserialize<SeJsonCheckStatus>("\"unknown\"", options),
      Throws.TypeOf<JsonException>()
    );
  }

  [Test]
  public void Write_UndefinedEnumValue_ThrowsJsonException()
  {
    var options = OptionsWithEnumConverter();
    var bogus = (SeJsonCheckStatus)999;
    Assert.That(
      () => JsonSerializer.Serialize(bogus, options),
      Throws.TypeOf<JsonException>()
    );
  }

  [Test]
  public void Factory_AcceptsAnyEnumType()
  {
    var options = OptionsWithEnumConverter();
    Assert.That(JsonSerializer.Serialize(SeJsonCheckStatus.Complete, options), Is.EqualTo("\"t\""));
    Assert.That(JsonSerializer.Serialize(SeJsonRarity.MythicRare, options), Is.EqualTo("\"mythic_rare\""));
  }
}
