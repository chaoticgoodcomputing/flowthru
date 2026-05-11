using Flowthru.Data.Schema;
using Flowthru.Data.Storage;
using Flowthru.Prelude;

namespace Flowthru.Core.Tests.Storage;

/// <summary>
/// Per-adapter unit inspection for <see cref="SingletonJsonAdapter{T}"/>.
/// Drills into <c>InspectShallow</c> / <c>InspectDeep</c> / <c>InspectTarget</c>
/// / <c>Exists</c> / <c>Save</c> / <c>Load</c> for the singleton JSON adapter.
/// </summary>
/// <remarks>
/// <strong>Pre-flight contract pinned:</strong> adapter returns
/// <see cref="ValidationErrorType.NotFound"/> on a missing source → pre-flight
/// emits <c>MissingInput</c>. This is the per-adapter half of the
/// <c>SrcInventory</c> FT4004-wrapping bug regression net (see gap #1 in
/// <c>docs/scratch/test-coverage-gap-analysis.md</c>).
/// </remarks>
[TestFixture]
public class SingletonJsonStorageAdapterTests
{
  private string _tempDir = null!;

  [SetUp]
  public void SetUp()
  {
    _tempDir = Path.Combine(Path.GetTempPath(), $"flowthru-singleton-{Guid.NewGuid():N}");
    Directory.CreateDirectory(_tempDir);
  }

  [TearDown]
  public void TearDown()
  {
    if (Directory.Exists(_tempDir))
    {
      try { Directory.Delete(_tempDir, recursive: true); }
      catch { /* best-effort */ }
    }
  }

  // ─────────────────────────────────────────────────────────────────────────
  // InspectShallow
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task InspectShallow_FileExistsAndDeserializes_Succeeds()
  {
    var path = await WriteSeed();
    var adapter = new SingletonJsonAdapter<SingletonRow>(path);

    var result = await UnwrapInspectShallow(adapter);
    Assert.That(
      result.IsValid,
      Is.True,
      $"Expected InspectShallow to succeed; got: {FormatFirstError(result)}"
    );
  }

  [Test]
  public async Task InspectShallow_FileMissing_FailsWithNotFound()
  {
    var path = Path.Combine(_tempDir, "missing.json");
    var adapter = new SingletonJsonAdapter<SingletonRow>(path);

    var result = await UnwrapInspectShallow(adapter);
    Assert.That(result.IsValid, Is.False);
    Assert.That(
      result.Errors,
      Has.Some.Matches<ValidationError>(e => e.ErrorType == ValidationErrorType.NotFound),
      "Adapter must surface NotFound on missing source — pre-flight maps this to MissingInput."
    );
  }

  [Test]
  public async Task InspectShallow_InvalidJson_FailsWithDeserializationError()
  {
    var path = Path.Combine(_tempDir, "corrupt.json");
    await File.WriteAllTextAsync(path, "{ not valid json");
    var adapter = new SingletonJsonAdapter<SingletonRow>(path);

    var result = await UnwrapInspectShallow(adapter);
    Assert.That(result.IsValid, Is.False);
    Assert.That(
      result.Errors,
      Has.Some.Matches<ValidationError>(e => e.ErrorType == ValidationErrorType.DeserializationError),
      "Corrupt JSON should surface DeserializationError, not NotFound."
    );
  }

  [Test]
  public async Task InspectShallow_EmptyFile_FailsWithDeserializationError()
  {
    var path = Path.Combine(_tempDir, "empty.json");
    await File.WriteAllTextAsync(path, string.Empty);
    var adapter = new SingletonJsonAdapter<SingletonRow>(path);

    var result = await UnwrapInspectShallow(adapter);
    Assert.That(result.IsValid, Is.False);
    Assert.That(
      result.Errors,
      Has.Some.Matches<ValidationError>(e => e.ErrorType == ValidationErrorType.DeserializationError),
      "Empty file is not the same as a missing file — must surface DeserializationError."
    );
  }

  [Test]
  public async Task InspectShallow_ErrorCarriesSchemaCatalogKey()
  {
    var path = Path.Combine(_tempDir, "missing.json");
    var adapter = new SingletonJsonAdapter<SingletonRow>(path);

    var result = await UnwrapInspectShallow(adapter);
    Assert.That(result.IsValid, Is.False);
    Assert.That(
      result.Errors[0].CatalogKey,
      Is.EqualTo(nameof(SingletonRow)),
      "Singleton adapter uses typeof(T).Name as the catalog key for its findings."
    );
  }

  // ─────────────────────────────────────────────────────────────────────────
  // InspectShallow — partial-match (data ⊇ schema) contract
  //
  // Required fields per SingletonRow: "id", "name", "value".
  // Optional fields: "timestamp", "description".
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task InspectShallow_DataIsSupersetOfSchema_Succeeds()
  {
    // Data has all 3 required fields + 2 unknown extras. The unknowns are
    // tolerated; presence of every required field is the only thing
    // InspectShallow checks at this depth.
    var path = Path.Combine(_tempDir, "superset.json");
    await File.WriteAllTextAsync(path,
      """
      {
        "id": "11111111-2222-3333-4444-555555555555",
        "name": "test",
        "value": 42,
        "extra_field_one": "ignored on load",
        "extra_field_two": 9001
      }
      """
    );
    var adapter = new SingletonJsonAdapter<SingletonRow>(path);

    var result = await UnwrapInspectShallow(adapter);
    Assert.That(result.IsValid, Is.True,
      $"Data ⊇ schema must succeed (extras are tolerated). Got: {FormatFirstError(result)}");
  }

  [Test]
  public async Task InspectShallow_DataIsExactMatchOfRequiredFields_Succeeds()
  {
    // Data has exactly the 3 required fields (no optionals, no extras).
    var path = Path.Combine(_tempDir, "exact.json");
    await File.WriteAllTextAsync(path,
      """
      {
        "id": "11111111-2222-3333-4444-555555555555",
        "name": "test",
        "value": 42
      }
      """
    );
    var adapter = new SingletonJsonAdapter<SingletonRow>(path);

    var result = await UnwrapInspectShallow(adapter);
    Assert.That(result.IsValid, Is.True,
      $"Exact required-field match must succeed. Got: {FormatFirstError(result)}");
  }

  [Test]
  public async Task InspectShallow_OptionalFieldsAbsent_Succeeds()
  {
    // Same as exact-match: optional fields ("timestamp", "description")
    // may be absent. The required-field set is what matters.
    var path = Path.Combine(_tempDir, "no-optionals.json");
    await File.WriteAllTextAsync(path,
      """
      {
        "id": "11111111-2222-3333-4444-555555555555",
        "name": "test",
        "value": 42
      }
      """
    );
    var adapter = new SingletonJsonAdapter<SingletonRow>(path);

    var result = await UnwrapInspectShallow(adapter);
    Assert.That(result.IsValid, Is.True,
      $"Absent optional fields must not block InspectShallow. Got: {FormatFirstError(result)}");
  }

  [Test]
  public async Task InspectShallow_MissingRequiredField_FailsWithSchemaMismatch()
  {
    // "value" is missing from the data — schema requires it.
    var path = Path.Combine(_tempDir, "missing-required.json");
    await File.WriteAllTextAsync(path,
      """
      {
        "id": "11111111-2222-3333-4444-555555555555",
        "name": "test"
      }
      """
    );
    var adapter = new SingletonJsonAdapter<SingletonRow>(path);

    var result = await UnwrapInspectShallow(adapter);
    Assert.That(result.IsValid, Is.False,
      "Missing required field must surface as SchemaMismatch, not silently default.");
    Assert.That(result.Errors.Single().ErrorType, Is.EqualTo(ValidationErrorType.SchemaMismatch));
    Assert.That(result.Errors.Single().Message, Does.Contain("'value'"),
      "Error message must name the missing field so the user can locate it.");
  }

  [Test]
  public async Task InspectShallow_MultipleMissingRequiredFields_ListsAllInDiff()
  {
    // Two required fields missing — error must name BOTH.
    var path = Path.Combine(_tempDir, "missing-multiple.json");
    await File.WriteAllTextAsync(path,
      """
      {
        "id": "11111111-2222-3333-4444-555555555555"
      }
      """
    );
    var adapter = new SingletonJsonAdapter<SingletonRow>(path);

    var result = await UnwrapInspectShallow(adapter);
    Assert.That(result.IsValid, Is.False);
    Assert.That(result.Errors.Single().ErrorType, Is.EqualTo(ValidationErrorType.SchemaMismatch));
    var message = result.Errors.Single().Message;
    Assert.That(message, Does.Contain("'name'"),
      "Multi-missing diff must name every missing required field.");
    Assert.That(message, Does.Contain("'value'"));
  }

  [Test]
  public async Task InspectShallow_MissingFieldErrorDetailsListPresentAndRequiredSets()
  {
    // The Details field should make the diff self-explanatory — which
    // fields the schema requires vs. which the data provided. Helps the
    // user diagnose upstream schema drift (e.g., a column rename).
    var path = Path.Combine(_tempDir, "diff-details.json");
    await File.WriteAllTextAsync(path,
      """
      {
        "id": "11111111-2222-3333-4444-555555555555",
        "cust_id": 7
      }
      """
    );
    var adapter = new SingletonJsonAdapter<SingletonRow>(path);

    var result = await UnwrapInspectShallow(adapter);
    Assert.That(result.IsValid, Is.False);
    var details = result.Errors.Single().Details!;
    Assert.That(details, Does.Contain("'cust_id'"),
      "Details should report what fields the data actually provided.");
    Assert.That(details, Does.Contain("'id'"));
    Assert.That(details, Does.Contain("'name'"));
    Assert.That(details, Does.Contain("'value'"));
  }

  [Test]
  public async Task InspectShallow_TopLevelArray_FailsWithSchemaMismatch()
  {
    // Singleton JSON expects a top-level object. An array (or any
    // non-object shape) is a SchemaMismatch.
    var path = Path.Combine(_tempDir, "array.json");
    await File.WriteAllTextAsync(path, "[{\"id\":\"x\"}]");
    var adapter = new SingletonJsonAdapter<SingletonRow>(path);

    var result = await UnwrapInspectShallow(adapter);
    Assert.That(result.IsValid, Is.False);
    Assert.That(result.Errors.Single().ErrorType, Is.EqualTo(ValidationErrorType.SchemaMismatch));
    Assert.That(result.Errors.Single().Message, Does.Contain("object"));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // InspectDeep — for singletons, equivalent to InspectShallow (must
  // round-trip the entire document either way).
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task InspectDeep_FileExistsAndDeserializes_Succeeds()
  {
    var path = await WriteSeed();
    var adapter = new SingletonJsonAdapter<SingletonRow>(path);

    var result = await UnwrapInspectDeep(adapter);
    Assert.That(
      result.IsValid,
      Is.True,
      $"Expected InspectDeep to succeed; got: {FormatFirstError(result)}"
    );
  }

  [Test]
  public async Task InspectDeep_FileMissing_FailsWithNotFound()
  {
    var path = Path.Combine(_tempDir, "missing.json");
    var adapter = new SingletonJsonAdapter<SingletonRow>(path);

    var result = await UnwrapInspectDeep(adapter);
    Assert.That(result.IsValid, Is.False);
    Assert.That(
      result.Errors,
      Has.Some.Matches<ValidationError>(e => e.ErrorType == ValidationErrorType.NotFound)
    );
  }

  // ─────────────────────────────────────────────────────────────────────────
  // InspectTarget
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task InspectTarget_WritableDirectory_Succeeds()
  {
    var path = Path.Combine(_tempDir, "writable.json");
    var adapter = new SingletonJsonAdapter<SingletonRow>(path);

    var result = await UnwrapInspectTarget(adapter);
    Assert.That(
      result.IsValid,
      Is.True,
      $"Expected InspectTarget to succeed; got: {FormatFirstError(result)}"
    );
  }

  [Test]
  public async Task InspectTarget_DoesNotLeaveProbeFile()
  {
    var path = Path.Combine(_tempDir, "writable.json");
    var adapter = new SingletonJsonAdapter<SingletonRow>(path);

    await UnwrapInspectTarget(adapter);

    var leftovers = Directory.EnumerateFiles(_tempDir, ".flowthru-probe-*").ToList();
    Assert.That(
      leftovers,
      Is.Empty,
      "InspectTarget must clean up its probe file even on success."
    );
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Exists
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task Exists_FilePresent_ReturnsTrue()
  {
    var path = await WriteSeed();
    var adapter = new SingletonJsonAdapter<SingletonRow>(path);

    var result = await adapter.Exists().Run();
    Assert.That(result, Is.InstanceOf<EffResult<bool>.Success>());
    Assert.That(((EffResult<bool>.Success)result).Value, Is.True);
  }

  [Test]
  public async Task Exists_FileMissing_ReturnsFalse()
  {
    var path = Path.Combine(_tempDir, "missing.json");
    var adapter = new SingletonJsonAdapter<SingletonRow>(path);

    var result = await adapter.Exists().Run();
    Assert.That(result, Is.InstanceOf<EffResult<bool>.Success>());
    Assert.That(((EffResult<bool>.Success)result).Value, Is.False);
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Save / Load round-trip
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task SaveAndLoad_RoundTripsScalarFields()
  {
    var path = Path.Combine(_tempDir, "roundtrip.json");
    var adapter = new SingletonJsonAdapter<SingletonRow>(path);
    var data = new SingletonRow
    {
      Id = Guid.NewGuid(),
      Name = "round-trip",
      Value = 42,
      Timestamp = new DateTime(2026, 1, 15, 12, 30, 0, DateTimeKind.Utc),
      Description = "ported gap-1c fixture",
    };

    var saveResult = await adapter.Save(data).Run();
    Assert.That(saveResult, Is.InstanceOf<EffResult<FlowUnit>.Success>());

    var loadResult = await adapter.Load().Run();
    Assert.That(loadResult, Is.InstanceOf<EffResult<SingletonRow>.Success>());
    var loaded = ((EffResult<SingletonRow>.Success)loadResult).Value;

    Assert.That(loaded.Id, Is.EqualTo(data.Id));
    Assert.That(loaded.Name, Is.EqualTo(data.Name));
    Assert.That(loaded.Value, Is.EqualTo(data.Value));
    Assert.That(loaded.Timestamp, Is.EqualTo(data.Timestamp));
    Assert.That(loaded.Description, Is.EqualTo(data.Description));
  }

  [Test]
  public async Task SaveAndLoad_NullableFieldsRoundTripAsNull()
  {
    var path = Path.Combine(_tempDir, "nullable.json");
    var adapter = new SingletonJsonAdapter<SingletonRow>(path);
    var data = new SingletonRow
    {
      Id = Guid.NewGuid(),
      Name = "nullable-fields",
      Value = 0,
      Timestamp = null,
      Description = null,
    };

    await adapter.Save(data).Run();
    var loadResult = await adapter.Load().Run();
    Assert.That(loadResult, Is.InstanceOf<EffResult<SingletonRow>.Success>());
    var loaded = ((EffResult<SingletonRow>.Success)loadResult).Value;

    Assert.That(loaded.Timestamp, Is.Null);
    Assert.That(loaded.Description, Is.Null);
  }

  [Test]
  public async Task Save_WritesJsonObjectNotArray()
  {
    // SingletonJsonAdapter exists specifically because not every catalog item
    // is a sequence; pin that the wire format is a single object, never an
    // array wrapper. A regression here would break every consumer (models,
    // metrics, config documents).
    var path = Path.Combine(_tempDir, "shape.json");
    var adapter = new SingletonJsonAdapter<SingletonRow>(path);

    await adapter.Save(new SingletonRow
    {
      Id = Guid.NewGuid(),
      Name = "shape-check",
      Value = 1,
    }).Run();

    var contents = (await File.ReadAllTextAsync(path)).TrimStart();
    Assert.That(contents.StartsWith("{"), Is.True, "Singleton wire format must begin with '{', not '['.");
  }

  [Test]
  public async Task Save_HonorsSerializedLabel()
  {
    // The Singleton adapter routes through JsonFormatSerializer's converter
    // factories; verify [SerializedLabel("...")] reaches the wire.
    var path = Path.Combine(_tempDir, "label.json");
    var adapter = new SingletonJsonAdapter<SingletonRow>(path);

    await adapter.Save(new SingletonRow
    {
      Id = Guid.NewGuid(),
      Name = "labelled",
      Value = 7,
    }).Run();

    var json = await File.ReadAllTextAsync(path);
    Assert.That(json, Does.Contain("\"id\""));
    Assert.That(json, Does.Contain("\"name\""));
    Assert.That(json, Does.Contain("\"value\""));
  }

  [Test]
  public async Task Save_CreatesMissingIntermediateDirectory()
  {
    var nested = Path.Combine(_tempDir, "nested", "deep", "path.json");
    var adapter = new SingletonJsonAdapter<SingletonRow>(nested);

    var result = await adapter.Save(new SingletonRow
    {
      Id = Guid.NewGuid(),
      Name = "deep",
      Value = 1,
    }).Run();

    Assert.That(result, Is.InstanceOf<EffResult<FlowUnit>.Success>());
    Assert.That(File.Exists(nested), Is.True);
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Constructor guards
  // ─────────────────────────────────────────────────────────────────────────

  [TestCase(null)]
  [TestCase("")]
  [TestCase("   ")]
  public void Constructor_RejectsMissingFilePath(string? filePath)
  {
    Assert.That(
      () => new SingletonJsonAdapter<SingletonRow>(filePath!),
      Throws.InstanceOf<ArgumentException>()
    );
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Helpers
  // ─────────────────────────────────────────────────────────────────────────

  private async Task<string> WriteSeed()
  {
    var path = Path.Combine(_tempDir, "seed.json");
    var adapter = new SingletonJsonAdapter<SingletonRow>(path);
    await adapter.Save(new SingletonRow
    {
      Id = Guid.NewGuid(),
      Name = "seed",
      Value = 1,
    }).Run();
    return path;
  }

  private static async Task<ValidationResult> UnwrapInspectShallow<T>(IStorageAdapter<T> adapter)
  {
    var result = await adapter.InspectShallow(sampleSize: 10).Run();
    return ((EffResult<ValidationResult>.Success)result).Value;
  }

  private static async Task<ValidationResult> UnwrapInspectDeep<T>(IStorageAdapter<T> adapter)
  {
    var result = await adapter.InspectDeep().Run();
    return ((EffResult<ValidationResult>.Success)result).Value;
  }

  private static async Task<ValidationResult> UnwrapInspectTarget<T>(IStorageAdapter<T> adapter)
  {
    var result = await adapter.InspectTarget().Run();
    return ((EffResult<ValidationResult>.Success)result).Value;
  }

  private static string FormatFirstError(ValidationResult result)
  {
    var first = result.Errors.FirstOrDefault();
    return first is null
      ? "<no errors>"
      : $"[{first.ErrorType}] {first.CatalogKey}: {first.Message}";
  }
}

/// <summary>
/// Local fixture for the singleton-JSON adapter tests. Inline because the
/// kits' <c>Schemas/</c> directory is excluded from compilation during the
/// FP rewrite. Carries required + optional fields and <c>[SerializedLabel]</c>
/// on every property so the converter wiring is exercised transitively.
/// </summary>
[FlowthruSchema]
public partial record SingletonRow
{
  [SerializedLabel("id")]
  public required Guid Id { get; init; }

  [SerializedLabel("name")]
  public required string Name { get; init; }

  [SerializedLabel("value")]
  public required int Value { get; init; }

  [SerializedLabel("timestamp")]
  public DateTime? Timestamp { get; init; }

  [SerializedLabel("description")]
  public string? Description { get; init; }
}
