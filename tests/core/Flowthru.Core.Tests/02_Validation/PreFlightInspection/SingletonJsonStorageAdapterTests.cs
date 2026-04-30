using Flowthru.Core.Data.Storage;
using Flowthru.Core.Data.Validation;
using Flowthru.Tests.Kits.Storage;
using Flowthru.Tests.Kits.Schemas;

namespace Flowthru.Core.Tests.Validation.PreFlightInspection;

/// <summary>
/// Coverage tests for <see cref="SingletonJsonStorageAdapter{T}"/> via the
/// <see cref="StorageAdapterAssertions"/> harness from <c>Flowthru.Tests.Helpers</c>.
/// </summary>
/// <remarks>
/// First consumer of the harness — proves the pattern that other adapter tests
/// (Binary, Text, Composed, Configuration, Memory, Null) will follow. Uses
/// <see cref="RequiredMembersSchema"/> as the row type so <c>SchemaActivator</c>'s
/// slow path is exercised transitively.
/// </remarks>
[TestFixture]
[Category("Validation")]
[Category("PreFlightInspection")]
public class SingletonJsonStorageAdapterTests
{
  private string _tempDir = null!;

  [SetUp]
  public void SetUp()
  {
    _tempDir = Path.Combine(Path.GetTempPath(), $"flowthru-test-{Guid.NewGuid():N}");
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
  // InspectShallow
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task InspectShallow_FileExistsAndDeserializes_Succeeds()
  {
    var path = await WriteSeed();
    var adapter = new SingletonJsonStorageAdapter<RequiredMembersSchema>(path);

    await StorageAdapterAssertions.InspectShallowSucceeds(adapter);
  }

  [Test]
  public async Task InspectShallow_FileMissing_FailsWithNotFound()
  {
    var path = Path.Combine(_tempDir, "missing.json");
    var adapter = new SingletonJsonStorageAdapter<RequiredMembersSchema>(path);

    await StorageAdapterAssertions.InspectShallowFails(
      adapter,
      ValidationErrorType.NotFound
    );
  }

  [Test]
  public async Task InspectShallow_InvalidJson_FailsWithDeserializationError()
  {
    var path = Path.Combine(_tempDir, "corrupt.json");
    await File.WriteAllTextAsync(path, "{ not valid json");
    var adapter = new SingletonJsonStorageAdapter<RequiredMembersSchema>(path);

    await StorageAdapterAssertions.InspectShallowFails(
      adapter,
      ValidationErrorType.DeserializationError
    );
  }

  [Test]
  public async Task InspectShallow_FileMissingRequiredSchemaProperty_FailsWithSchemaMismatch()
  {
    // Phase F negative scenario: file is valid JSON but is missing a property the
    // schema declares as `required`. Pre-flight should catch this structural
    // mismatch and surface SchemaMismatch — same category EFCore, CSV, and Parquet
    // use post-Phase-F.
    //
    // JSON-specific limitation (Phase F deviation): only *required* properties are
    // detectable here. JSON's serialization with WhenWritingNull legitimately omits
    // null-valued keys, so "key absent" and "key present with null value" produce
    // byte-identical files for nullable properties. The kit can't ask JSON whether
    // a missing optional key is structural or null-valued; that distinction simply
    // doesn't exist in the JSON wire format. CSV and Parquet have a separate header
    // / schema metadata layer that does carry the distinction, which is why those
    // adapters can also catch missing-optional cases.
    var path = Path.Combine(_tempDir, "missing-required-property.json");
    await File.WriteAllTextAsync(
      path,
      """
      {
        "id": "11111111-1111-1111-1111-111111111111",
        "value": 42
      }
      """
    );
    var adapter = new SingletonJsonStorageAdapter<RequiredMembersSchema>(path);

    await StorageAdapterAssertions.InspectShallowFails(
      adapter,
      ValidationErrorType.SchemaMismatch
    );
  }

  // ─────────────────────────────────────────────────────────────────────────
  // InspectDeep
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task InspectDeep_FileExistsAndDeserializes_Succeeds()
  {
    var path = await WriteSeed();
    var adapter = new SingletonJsonStorageAdapter<RequiredMembersSchema>(path);

    await StorageAdapterAssertions.InspectDeepSucceeds(adapter);
  }

  [Test]
  public async Task InspectDeep_FileMissing_FailsWithNotFound()
  {
    var path = Path.Combine(_tempDir, "missing.json");
    var adapter = new SingletonJsonStorageAdapter<RequiredMembersSchema>(path);

    await StorageAdapterAssertions.InspectDeepFails(adapter, ValidationErrorType.NotFound);
  }

  // ─────────────────────────────────────────────────────────────────────────
  // InspectTarget
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task InspectTarget_WritableDirectory_Succeeds()
  {
    var path = Path.Combine(_tempDir, "writable.json");
    var adapter = new SingletonJsonStorageAdapter<RequiredMembersSchema>(path);

    await StorageAdapterAssertions.InspectTargetSucceeds(adapter);
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Exists
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task Exists_FilePresent_ReturnsTrue()
  {
    var path = await WriteSeed();
    var adapter = new SingletonJsonStorageAdapter<RequiredMembersSchema>(path);

    await StorageAdapterAssertions.ExistsReturns(adapter, expected: true);
  }

  [Test]
  public async Task Exists_FileMissing_ReturnsFalse()
  {
    var path = Path.Combine(_tempDir, "missing.json");
    var adapter = new SingletonJsonStorageAdapter<RequiredMembersSchema>(path);

    await StorageAdapterAssertions.ExistsReturns(adapter, expected: false);
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Save / Load round-trip — exercises SchemaActivator slow path through
  // RequiredMembersSchema's deserialization.
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task SaveAndLoad_RoundTripsRequiredMembersSchema()
  {
    var path = Path.Combine(_tempDir, "roundtrip.json");
    var adapter = new SingletonJsonStorageAdapter<RequiredMembersSchema>(path);
    var data = new RequiredMembersSchema
    {
      Id = Guid.NewGuid(),
      Name = "round-trip",
      Value = 42,
      Timestamp = new DateTime(2026, 1, 15, 12, 30, 0, DateTimeKind.Utc),
      Description = "Phase 6 task 3 fixture",
    };

    await StorageAdapterAssertions.SaveAndLoadRoundTrips(adapter, data);
  }

  [Test]
  public void FilePath_ReturnsConfiguredPath()
  {
    var path = Path.Combine(_tempDir, "configured.json");
    var adapter = new SingletonJsonStorageAdapter<RequiredMembersSchema>(path);

    Assert.That(adapter.FilePath, Is.EqualTo(path));
  }

  [Test]
  public void Options_ReturnsConfiguredJsonOptions()
  {
    var path = Path.Combine(_tempDir, "configured.json");
    var adapter = new SingletonJsonStorageAdapter<RequiredMembersSchema>(path);

    Assert.That(adapter.Options, Is.Not.Null);
    Assert.That(adapter.Options.WriteIndented, Is.True);
  }

  [Test]
  public async Task SaveAndLoad_RoundTripsPositionalRecordSchema()
  {
    // Positional records have no parameterless constructor, so deserialization MUST
    // route through SchemaActivator's slow path (FormatterServices.GetUninitializedObject).
    // This test exercises that path transitively.
    var path = Path.Combine(_tempDir, "positional.json");
    var adapter = new SingletonJsonStorageAdapter<PositionalRecordSchema>(path);
    var data = new PositionalRecordSchema(
      EntityId: Guid.NewGuid(),
      EntityName: "positional",
      Score: 3.14,
      CreatedAt: new DateTime(2026, 4, 28, 9, 0, 0, DateTimeKind.Utc)
    );

    await StorageAdapterAssertions.SaveAndLoadRoundTrips(adapter, data);
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Helpers
  // ─────────────────────────────────────────────────────────────────────────

  private async Task<string> WriteSeed()
  {
    var path = Path.Combine(_tempDir, "seed.json");
    var seed = new RequiredMembersSchema
    {
      Id = Guid.NewGuid(),
      Name = "seed",
      Value = 1,
      Timestamp = null,
      Description = null,
    };
    var adapter = new SingletonJsonStorageAdapter<RequiredMembersSchema>(path);
    await adapter.Save(seed).Run();
    return path;
  }
}
