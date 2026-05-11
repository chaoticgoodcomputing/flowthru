using Flowthru.Data.Catalog;
using Flowthru.Data.Storage;
using Flowthru.Flow;
using Flowthru.Prelude;
using Flowthru.Validation.PreFlight;
using static Flowthru.Core.Tests.Storage.StorageAdapterTestHelpers;

namespace Flowthru.Core.Tests.Storage;

/// <summary>
/// Per-adapter coverage for <see cref="TextFileStorageAdapter"/>:
/// <c>InspectShallow</c> / <c>InspectDeep</c> / <c>InspectTarget</c> /
/// <c>Exists</c> / <c>Save</c>+<c>Load</c> in isolation, plus the
/// adapter-NotFound -> pre-flight <see cref="PreFlightError.MissingInput"/>
/// path the <c>SrcInventory</c> FT4004-wrapping bug ran through.
/// </summary>
[TestFixture]
public class TextFileStorageAdapterTests
{
  private string _tempDir = null!;

  [SetUp]
  public void SetUp()
  {
    _tempDir = Path.Combine(Path.GetTempPath(), $"flowthru-txt-{Guid.NewGuid():N}");
    Directory.CreateDirectory(_tempDir);
  }

  [TearDown]
  public void TearDown()
  {
    if (Directory.Exists(_tempDir))
    {
      try { Directory.Delete(_tempDir, recursive: true); }
      catch { /* best effort */ }
    }
  }

  // ── InspectShallow ────────────────────────────────────────────────────

  [Test]
  public async Task InspectShallow_FileExists_Succeeds()
  {
    var path = await WriteSeed();

    var inspection = await UnwrapSuccess(new TextFileStorageAdapter(path).InspectShallow(sampleSize: 10));

    Assert.That(inspection.IsValid, Is.True,
      $"Expected InspectShallow to succeed on a written file. First error: {FormatFirstError(inspection)}");
  }

  [Test]
  public async Task InspectShallow_FileMissing_FailsWithNotFound()
  {
    var path = Path.Combine(_tempDir, "missing.txt");

    var inspection = await UnwrapSuccess(new TextFileStorageAdapter(path).InspectShallow(sampleSize: 10));

    Assert.That(inspection.IsValid, Is.False, "InspectShallow on a missing file must fail.");
    Assert.That(inspection.Errors,
      Has.Some.Matches<ValidationError>(e => e.ErrorType == ValidationErrorType.NotFound),
      "Adapter must classify missing-file as NotFound — pre-flight maps "
        + "ValidationErrorType.NotFound to PreFlightError.MissingInput.");
  }

  // ── InspectDeep ───────────────────────────────────────────────────────

  [Test]
  public async Task InspectDeep_FileExists_Succeeds()
  {
    var path = await WriteSeed();

    var inspection = await UnwrapSuccess(new TextFileStorageAdapter(path).InspectDeep());

    Assert.That(inspection.IsValid, Is.True,
      $"Expected InspectDeep to succeed on a written file. First error: {FormatFirstError(inspection)}");
  }

  // ── InspectTarget ─────────────────────────────────────────────────────

  [Test]
  public async Task InspectTarget_WritableDirectory_Succeeds()
  {
    var path = Path.Combine(_tempDir, "writable.txt");

    var inspection = await UnwrapSuccess(new TextFileStorageAdapter(path).InspectTarget());

    Assert.That(inspection.IsValid, Is.True,
      $"Expected InspectTarget to succeed for a writable temp directory. "
        + $"First error: {FormatFirstError(inspection)}");
  }

  // ── Exists ────────────────────────────────────────────────────────────

  [Test]
  public async Task Exists_FilePresent_ReturnsTrue()
  {
    var path = await WriteSeed();

    var exists = await UnwrapSuccess(new TextFileStorageAdapter(path).Exists());

    Assert.That(exists, Is.True);
  }

  [Test]
  public async Task Exists_FileMissing_ReturnsFalse()
  {
    var path = Path.Combine(_tempDir, "missing.txt");

    var exists = await UnwrapSuccess(new TextFileStorageAdapter(path).Exists());

    Assert.That(exists, Is.False);
  }

  // ── Save / Load round-trip ────────────────────────────────────────────

  [Test]
  public async Task SaveAndLoad_RoundTripsText()
  {
    var path = Path.Combine(_tempDir, "roundtrip.txt");
    var adapter = new TextFileStorageAdapter(path);
    var data = "line one\nline two with embedded newline\nfinal line\n";

    await UnwrapSuccess(adapter.Save(data));
    var loaded = await UnwrapSuccess(adapter.Load());

    Assert.That(loaded, Is.EqualTo(data),
      "Round-trip should preserve the exact text content (including embedded newlines).");
  }

  // ── Pre-flight integration: adapter NotFound -> MissingInput ─────────

  /// <summary>
  /// End-to-end pinning of the path the <c>SrcInventory</c> FT4004-wrapping
  /// bug surfaced: a <see cref="TextFileStorageAdapter"/> reporting
  /// <see cref="ValidationErrorType.NotFound"/> must surface through
  /// <see cref="PreFlightPipeline.Run"/> as a
  /// <see cref="PreFlightError.MissingInput"/> — not as
  /// <c>InspectionFailed</c> and not as an unhandled exception.
  /// </summary>
  [Test]
  public async Task PreFlight_AdapterReportsNotFound_EmitsMissingInput()
  {
    var missingPath = Path.Combine(_tempDir, "absent.txt");
    var input = new Item<string>("text-input", new TextFileStorageAdapter(missingPath));
    var output = ItemFactory.Singleton.Memory<string>("text-output");

    var flow = FlowBuilder.CreateFlow("text-missing", b =>
      b.AddStep<string, string>("identity", x => x, input, output)
    );

    var validated = await UnwrapSuccess(PreFlightPipeline.Run(flow));

    Assert.That(validated, Is.InstanceOf<Validated<PreFlightError, FlowUnit>.Invalid>(),
      "Pre-flight must surface an Invalid when the bound adapter reports NotFound.");
    var errors = ((Validated<PreFlightError, FlowUnit>.Invalid)validated).Errors;
    Assert.That(errors, Has.Some.InstanceOf<PreFlightError.MissingInput>(),
      "Adapter NotFound must translate to PreFlightError.MissingInput (not InspectionFailed).");
    Assert.That(errors.OfType<PreFlightError.MissingInput>().Any(e => e.ItemId == "text-input"),
      Is.True, "MissingInput must name the failing item label.");
  }

  private async Task<string> WriteSeed()
  {
    var path = Path.Combine(_tempDir, "seed.txt");
    var adapter = new TextFileStorageAdapter(path);
    await UnwrapSuccess(adapter.Save("seed content"));
    return path;
  }
}
