using System.Text;
using Flowthru.Data.Schema;
using Flowthru.Data.Storage;
using Flowthru.Prelude;

namespace Flowthru.Tests.Kits.Storage;

/// <summary>
/// Laws every schema-aware <see cref="IStorageAdapter{T}"/> implementer
/// must satisfy to honor the partial-match contract codified on
/// <c>IStorageAdapter.InspectShallow</c>: <em>data ⊇ schema</em>.
/// </summary>
/// <remarks>
/// <para>
/// Replaces the prior <c>InspectShallowConformance&lt;TContainer&gt;</c>
/// kit. Behaves identically; renamed per §2.11 to align with the
/// algebra-laws framing.
/// </para>
/// <para>
/// <strong>Two-lap design.</strong> JSON is the canonical seed format
/// because it expresses field presence/absence unambiguously. The kit
/// defines a small fixture set of JSON payloads — valid, with-extras,
/// missing-required, multiple-missing, optional-absent — and asks the
/// implementer to project each payload into the format under test
/// (lap 1). The kit then constructs the adapter against the projected
/// file and runs <c>InspectShallow</c> (lap 2). The implementer is
/// responsible for the projection only; every test case + assertion
/// lives in the kit.
/// </para>
/// <para>
/// <strong>Why JSON.</strong> A JSON object's property-name set is the
/// most faithful representation of "what fields the data source
/// exposes". The implementer's projection preserves that set in the
/// format's native shape (CSV header row, Parquet column schema, XML
/// elements, etc.). For JSON adapters the projection is identity; for
/// other text formats it's a parse-and-rewrite; for binary formats it
/// requires more work and may be deferred via
/// <see cref="EnforcesPartialMatchOnInspectShallow"/>.
/// </para>
/// <para>
/// <strong>Schema-less adapters.</strong> Adapters that store raw bytes
/// or unstructured text (no field-level schema) don't apply — the
/// contract only covers adapters that can introspect field names.
/// </para>
/// </remarks>
[TestFixture]
public abstract class IInspectShallowLaws<TContainer>
{
  // ── Canonical JSON fixtures (shared across every format) ───────────────

  // Fixtures use CLR-cased property names (Id, Name, Value, …) to match
  // both case-insensitive adapters (JSON SingletonAdapter) and case-
  // sensitive ones (CSV via CsvHelper). Adapters that map to different
  // external casings via [SerializedLabel] can override Project to
  // remap; the kit's canonical fixtures stay portable.

  /// <summary>All three required fields + two optionals + one extra. Tests data⊇schema with optionals.</summary>
  protected const string JsonAllPresent =
    """
    {
      "Id": "00000000-0000-0000-0000-000000000001",
      "Name": "alpha",
      "Value": 1,
      "Comment": "fully populated",
      "Tag": "kit",
      "ExtraExtensionField": "tolerated"
    }
    """;

  /// <summary>Exactly the three required fields. Tests minimum-conformant data.</summary>
  protected const string JsonExactRequired =
    """
    {
      "Id": "00000000-0000-0000-0000-000000000002",
      "Name": "beta",
      "Value": 2
    }
    """;

  /// <summary>All required fields + extras only. Tests that extras don't break inspection.</summary>
  protected const string JsonExtraFields =
    """
    {
      "Id": "00000000-0000-0000-0000-000000000003",
      "Name": "gamma",
      "Value": 3,
      "FutureFieldA": 99,
      "FutureFieldB": "irrelevant"
    }
    """;

  /// <summary>One required field absent ("Value"). Tests SchemaMismatch with single-field diff.</summary>
  protected const string JsonMissingOneRequired =
    """
    {
      "Id": "00000000-0000-0000-0000-000000000004",
      "Name": "delta"
    }
    """;

  /// <summary>Two required fields absent ("Name", "Value"). Tests multi-field diff.</summary>
  protected const string JsonMissingMultipleRequired =
    """
    {
      "Id": "00000000-0000-0000-0000-000000000005"
    }
    """;

  // ── Implementer hooks ──────────────────────────────────────────────────

  /// <summary>
  /// Translate a canonical JSON object payload into the format's native
  /// byte representation. JSON adapters return the input bytes verbatim;
  /// CSV adapters parse the JSON and write a header + values row;
  /// Parquet adapters parse and write columnar pages; etc. The
  /// implementer's responsibility is to preserve the JSON's field-name
  /// set in the format's native shape — including absences.
  /// </summary>
  protected abstract Task<byte[]> ProjectJsonPayloadAsync(string jsonPayload);

  /// <summary>
  /// Construct a fresh adapter instance pointing at the given file path
  /// (where the kit has just written the projected bytes from
  /// <see cref="ProjectJsonPayloadAsync"/>).
  /// </summary>
  protected abstract IStorageAdapter<TContainer> CreateAdapter(string filePath);

  /// <summary>
  /// File extension the adapter expects (with leading dot — e.g.
  /// <c>".json"</c>, <c>".csv"</c>, <c>".parquet"</c>). Used so each
  /// test writes to a uniquely-named file the adapter recognises.
  /// </summary>
  protected abstract string FileExtension { get; }

  /// <summary>
  /// True when the adapter enforces partial-match at the
  /// <see cref="IStorageAdapter{T}.InspectShallow"/> boundary today.
  /// Adapters that haven't yet been brought into conformance set this
  /// to <c>false</c> with a documented reason — the gap is then visible
  /// as skipped tests in run summaries rather than hidden via a
  /// passing-but-non-conformant adapter. The flag should be removed
  /// (and the adapter brought into conformance) before a v1 release.
  /// </summary>
  protected virtual bool EnforcesPartialMatchOnInspectShallow => true;

  // ── Fixture lifecycle ──────────────────────────────────────────────────

  private string _tempDir = null!;

  [SetUp]
  public void SetUp()
  {
    if (!EnforcesPartialMatchOnInspectShallow)
    {
      Assert.Ignore(
        "Adapter does not yet honor the IStorageAdapter.InspectShallow "
          + "partial-match contract (data ⊇ schema-required-fields, "
          + "extras tolerated, missing required → SchemaMismatch with diff). "
          + "See EnforcesPartialMatchOnInspectShallow = false on the implementing fixture."
      );
    }
    _tempDir = Path.Combine(Path.GetTempPath(), $"flowthru-inspect-shallow-{Guid.NewGuid():N}");
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

  // ── Tests — data ⊇ schema (passing cases) ─────────────────────────────

  [Test]
  public async Task InspectShallow_DataIsSupersetOfSchema_Succeeds()
  {
    var result = await SeedAndInspect(JsonAllPresent, "all-present");
    Assert.That(result.IsValid, Is.True,
      $"Data ⊇ schema with optionals + extras must succeed. Got: {FormatErrors(result)}");
  }

  [Test]
  public async Task InspectShallow_ExactRequiredMatch_Succeeds()
  {
    var result = await SeedAndInspect(JsonExactRequired, "exact-required");
    Assert.That(result.IsValid, Is.True,
      $"Exact required-field match (no optionals, no extras) must succeed. Got: {FormatErrors(result)}");
  }

  [Test]
  public async Task InspectShallow_DataHasExtraFields_Succeeds()
  {
    var result = await SeedAndInspect(JsonExtraFields, "extras-only");
    Assert.That(result.IsValid, Is.True,
      $"Extra fields beyond the schema must be tolerated, not rejected. Got: {FormatErrors(result)}");
  }

  // ── Tests — data ⊉ schema (SchemaMismatch with field-diff) ────────────

  [Test]
  public async Task InspectShallow_MissingOneRequiredField_FailsWithSchemaMismatch()
  {
    var result = await SeedAndInspect(JsonMissingOneRequired, "missing-one");
    Assert.That(result.IsValid, Is.False,
      "Missing required field must surface as SchemaMismatch, not silently default.");
    Assert.That(result.Errors.Single().ErrorType, Is.EqualTo(ValidationErrorType.SchemaMismatch));
    Assert.That(result.Errors.Single().Message, Does.Contain("value").IgnoreCase,
      "Error message must name the missing field so the user can locate it. "
        + "Casing is implementer-defined (CLR vs. external label).");
  }

  [Test]
  public async Task InspectShallow_MissingMultipleRequiredFields_DiffListsAll()
  {
    var result = await SeedAndInspect(JsonMissingMultipleRequired, "missing-multi");
    Assert.That(result.IsValid, Is.False);
    Assert.That(result.Errors.Single().ErrorType, Is.EqualTo(ValidationErrorType.SchemaMismatch));
    var message = result.Errors.Single().Message;
    Assert.That(message, Does.Contain("name").IgnoreCase,
      "Multi-missing diff must name every missing required field.");
    Assert.That(message, Does.Contain("value").IgnoreCase);
  }

  // ── Helpers ────────────────────────────────────────────────────────────

  /// <summary>
  /// Two-lap orchestration: project the canonical JSON into the format,
  /// write to a uniquely-named temp file, construct the adapter, run
  /// <c>InspectShallow</c>, return the result.
  /// </summary>
  private async Task<ValidationResult> SeedAndInspect(string jsonPayload, string caseName)
  {
    var path = Path.Combine(_tempDir, $"{caseName}{FileExtension}");
    var bytes = await ProjectJsonPayloadAsync(jsonPayload);
    await File.WriteAllBytesAsync(path, bytes);

    var adapter = CreateAdapter(path);
    var effResult = await adapter.InspectShallow(sampleSize: 0).Run();
    // FT5002 fires on the kit-infrastructure throw below — the analyzer
    // correctly enforces fail-as-value, but here we're at the kit's
    // assertion boundary: a FlowIO-level failure from an InspectShallow
    // adapter is a kit-contract violation (adapters must surface
    // findings as ValidationResult values, not FlowIO failures), so we
    // throw to fail the conformance test. Suppression with rationale.
#pragma warning disable FT5002
    return effResult switch
    {
      EffResult<ValidationResult>.Success ok => ok.Value,
      EffResult<ValidationResult>.Failure fail => throw new InvalidOperationException(
        $"InspectShallow lifted to a FlowIO failure (expected fail-as-value): {fail.Error.Message}"
      ),
      _ => throw new InvalidOperationException("Unreachable: EffResult is a closed sum"),
    };
#pragma warning restore FT5002
  }

  private static string FormatErrors(ValidationResult result) =>
    string.Join(", ", result.Errors.Select(e => $"{e.ErrorType}: {e.Message}"));
}

// ── Laws fixture (shared across every adapter) ─────────────────────────

/// <summary>
/// Row schema used by <see cref="IInspectShallowLaws{TContainer}"/>. Three
/// required fields, two optional. Field names align with the canonical
/// JSON fixtures' property names verbatim — no <c>[SerializedLabel]</c>
/// remapping, so the field-presence check works directly against the
/// data's natural shape.
/// </summary>
[FlowthruSchema]
public partial record InspectShallowKitRow
{
  /// <summary>Required — identifier.</summary>
  public required Guid Id { get; init; }
  /// <summary>Required — display name.</summary>
  public required string Name { get; init; }
  /// <summary>Required — integer value.</summary>
  public required int Value { get; init; }
  /// <summary>Optional — free-form comment.</summary>
  public string? Comment { get; init; }
  /// <summary>Optional — tag identifier.</summary>
  public string? Tag { get; init; }
}
