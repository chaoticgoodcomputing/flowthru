using Flowthru.Data.Catalog;
using Flowthru.Data.Storage;
using Flowthru.Flow;
using Flowthru.Prelude;
using Flowthru.Validation.PreFlight;
using static Flowthru.Core.Tests.Storage.StorageAdapterTestHelpers;

namespace Flowthru.Core.Tests.Storage;

/// <summary>
/// Per-adapter coverage for <see cref="BinaryFileStorageAdapter"/>:
/// <c>InspectShallow</c> / <c>InspectDeep</c> / <c>InspectTarget</c> /
/// <c>Exists</c> / <c>Save</c>+<c>Load</c> in isolation, plus the
/// adapter-NotFound -> pre-flight <see cref="PreFlightError.MissingInput"/>
/// path the <c>SrcInventory</c> FT4004-wrapping bug ran through.
/// </summary>
[TestFixture]
public class BinaryFileStorageAdapterTests
{
  private string _tempDir = null!;

  [SetUp]
  public void SetUp()
  {
    _tempDir = Path.Combine(Path.GetTempPath(), $"flowthru-bin-{Guid.NewGuid():N}");
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

    var inspection = await UnwrapSuccess(new BinaryFileStorageAdapter(path).InspectShallow(sampleSize: 10));

    Assert.That(inspection.IsValid, Is.True,
      $"Expected InspectShallow to succeed on a written file. First error: {FormatFirstError(inspection)}");
  }

  [Test]
  public async Task InspectShallow_FileMissing_FailsWithNotFound()
  {
    var path = Path.Combine(_tempDir, "missing.bin");

    var inspection = await UnwrapSuccess(new BinaryFileStorageAdapter(path).InspectShallow(sampleSize: 10));

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

    var inspection = await UnwrapSuccess(new BinaryFileStorageAdapter(path).InspectDeep());

    Assert.That(inspection.IsValid, Is.True,
      $"Expected InspectDeep to succeed on a written file. First error: {FormatFirstError(inspection)}");
  }

  // ── InspectTarget ─────────────────────────────────────────────────────

  [Test]
  public async Task InspectTarget_WritableDirectory_Succeeds()
  {
    var path = Path.Combine(_tempDir, "writable.bin");

    var inspection = await UnwrapSuccess(new BinaryFileStorageAdapter(path).InspectTarget());

    Assert.That(inspection.IsValid, Is.True,
      $"Expected InspectTarget to succeed for a writable temp directory. "
        + $"First error: {FormatFirstError(inspection)}");
  }

  // ── Exists ────────────────────────────────────────────────────────────

  [Test]
  public async Task Exists_FilePresent_ReturnsTrue()
  {
    var path = await WriteSeed();

    var exists = await UnwrapSuccess(new BinaryFileStorageAdapter(path).Exists());

    Assert.That(exists, Is.True);
  }

  [Test]
  public async Task Exists_FileMissing_ReturnsFalse()
  {
    var path = Path.Combine(_tempDir, "missing.bin");

    var exists = await UnwrapSuccess(new BinaryFileStorageAdapter(path).Exists());

    Assert.That(exists, Is.False);
  }

  // ── Save / Load round-trip ────────────────────────────────────────────

  [Test]
  public async Task SaveAndLoad_RoundTripsBytes()
  {
    var path = Path.Combine(_tempDir, "roundtrip.bin");
    var adapter = new BinaryFileStorageAdapter(path);
    var data = new byte[] { 1, 2, 3, 4, 5, 250, 251, 252, 253, 254, 255 };

    await UnwrapSuccess(adapter.Save(data));
    var loaded = await UnwrapSuccess(adapter.Load());

    Assert.That(loaded, Is.EqualTo(data).AsCollection,
      "Round-trip should preserve byte-for-byte content.");
  }

  // ── Pre-flight integration: adapter NotFound -> MissingInput ─────────

  /// <summary>
  /// End-to-end pinning of the path the <c>SrcInventory</c> FT4004-wrapping
  /// bug surfaced: a <see cref="BinaryFileStorageAdapter"/> reporting
  /// <see cref="ValidationErrorType.NotFound"/> must surface through
  /// <see cref="PreFlightPipeline.Run"/> as a
  /// <see cref="PreFlightError.MissingInput"/> — not as
  /// <c>InspectionFailed</c> and not as an unhandled exception.
  /// </summary>
  [Test]
  public async Task PreFlight_AdapterReportsNotFound_EmitsMissingInput()
  {
    var missingPath = Path.Combine(_tempDir, "absent.bin");
    var input = new Item<byte[]>("binary-input", new BinaryFileStorageAdapter(missingPath));
    var output = ItemFactory.Singleton.Memory<byte[]>("binary-output");

    var flow = FlowBuilder.CreateFlow("binary-missing", b =>
      b.AddStep<byte[], byte[]>("identity", x => x, input, output)
    );

    var validated = await UnwrapSuccess(PreFlightPipeline.Run(flow));

    Assert.That(validated, Is.InstanceOf<Validated<PreFlightError, FlowUnit>.Invalid>(),
      "Pre-flight must surface an Invalid when the bound adapter reports NotFound.");
    var errors = ((Validated<PreFlightError, FlowUnit>.Invalid)validated).Errors;
    Assert.That(errors, Has.Some.InstanceOf<PreFlightError.MissingInput>(),
      "Adapter NotFound must translate to PreFlightError.MissingInput (not InspectionFailed).");
    Assert.That(errors.OfType<PreFlightError.MissingInput>().Any(e => e.ItemId == "binary-input"),
      Is.True, "MissingInput must name the failing item label.");
  }

  private async Task<string> WriteSeed()
  {
    var path = Path.Combine(_tempDir, "seed.bin");
    var adapter = new BinaryFileStorageAdapter(path);
    await UnwrapSuccess(adapter.Save(new byte[] { 0xCA, 0xFE, 0xBA, 0xBE }));
    return path;
  }
}
