using System.Text.Json;
using Flowthru.Tests.Helpers.Schemas;

namespace Flowthru.Core.Tests.Services.Serialization;

/// <summary>
/// Direct unit tests for the <c>SerializedEnumJsonConverter&lt;TEnum&gt;</c> chain. The
/// converter is internal but consumed by <c>JsonFormatSerializer&lt;TRow&gt;</c>'s default
/// options, where it auto-registers via the converter factory. To exercise the converter
/// directly here, we register a <c>JsonFormatSerializer&lt;DummySchema&gt;</c> just to obtain
/// its options (which include the <c>SerializedEnumJsonConverterFactory</c>), then use those
/// options for plain JsonSerializer.Serialize/Deserialize calls.
/// </summary>
/// <remarks>
/// Note: <c>SingletonJsonStorageAdapter</c> does NOT register the
/// <c>SerializedEnumJsonConverterFactory</c> in its default options — only the
/// <c>JsonFormatSerializer</c> path does. <c>[SerializedEnum]</c> mappings via the singleton
/// adapter therefore fall through to System.Text.Json's default enum handling. Worth fixing
/// in a future refactor; tracked here for reference.
/// </remarks>
[TestFixture]
[Category("Services")]
[Category("Serialization")]
public class SerializedEnumJsonConverterTests
{
  private static JsonSerializerOptions OptionsWithEnumConverter()
  {
    // Reuse JsonFormatSerializer's option-building so the converter chain is registered
    // exactly as it would be in production.
    var serializer =
      new Flowthru.Core.Data.Storage.Format.JsonFormatSerializer<EnumOnlySchema>();
    return serializer.Options;
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Read / Write round-trip
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void Write_SerializesEnumToConfiguredString()
  {
    var options = OptionsWithEnumConverter();

    var json = JsonSerializer.Serialize(CheckStatus.Complete, options);

    Assert.That(json, Is.EqualTo("\"t\""));
  }

  [Test]
  public void Read_DeserializesConfiguredStringToEnum()
  {
    var options = OptionsWithEnumConverter();

    var value = JsonSerializer.Deserialize<CheckStatus>("\"f\"", options);

    Assert.That(value, Is.EqualTo(CheckStatus.Incomplete));
  }

  [Test]
  public void RoundTrip_PreservesAllEnumMembers()
  {
    var options = OptionsWithEnumConverter();

    foreach (var original in new[] { CheckStatus.Complete, CheckStatus.Incomplete })
    {
      var json = JsonSerializer.Serialize(original, options);
      var roundTripped = JsonSerializer.Deserialize<CheckStatus>(json, options);

      Assert.That(roundTripped, Is.EqualTo(original), $"Round-trip failed for {original}.");
    }
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Read error paths
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void Read_NonStringToken_ThrowsJsonException()
  {
    var options = OptionsWithEnumConverter();

    Assert.That(
      () => JsonSerializer.Deserialize<CheckStatus>("42", options),
      Throws.TypeOf<JsonException>().With.Message.Contains("Expected string value")
    );
  }

  [Test]
  public void Read_UnknownStringValue_ThrowsJsonException()
  {
    var options = OptionsWithEnumConverter();

    Assert.That(
      () => JsonSerializer.Deserialize<CheckStatus>("\"unknown\"", options),
      Throws.TypeOf<JsonException>()
    );
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Write error paths
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void Write_UndefinedEnumValue_ThrowsJsonException()
  {
    var options = OptionsWithEnumConverter();
    var bogus = (CheckStatus)999;

    Assert.That(
      () => JsonSerializer.Serialize(bogus, options),
      Throws.TypeOf<JsonException>()
    );
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Factory
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void Factory_AcceptsAnyEnumType()
  {
    var options = OptionsWithEnumConverter();

    // Both CheckStatus and Rarity are independently-defined enums in the helpers.
    // Both should round-trip through the factory-resolved converter.
    var csJson = JsonSerializer.Serialize(CheckStatus.Complete, options);
    var rarityJson = JsonSerializer.Serialize(Rarity.MythicRare, options);

    Assert.That(csJson, Is.EqualTo("\"t\""));
    Assert.That(rarityJson, Is.EqualTo("\"mythic_rare\""));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Schema-context round-trip — exercises the full chain in a realistic shape
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void NestedInRecord_RoundTripsEnumPropertyCorrectly()
  {
    var options = OptionsWithEnumConverter();
    var data = new EnumOnlySchema { Status = CheckStatus.Complete };

    var json = JsonSerializer.Serialize(data, options);
    var loaded = JsonSerializer.Deserialize<EnumOnlySchema>(json, options);

    Assert.That(loaded?.Status, Is.EqualTo(CheckStatus.Complete));
    Assert.That(json, Does.Contain("\"t\""));
  }

  // Minimal local fixture to satisfy JsonFormatSerializer<TRow>'s IStructuredSerializable
  // constraint without dragging in the full helper schemas (which already exercise
  // SerializedLabel + SerializedEnum together).
  public sealed record EnumOnlySchema : Flowthru.Core.Abstractions.IStructuredSerializable
  {
    public CheckStatus Status { get; init; }
  }
}
