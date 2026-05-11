using Flowthru.Data.Catalog;
using Flowthru.Data.Storage;
using Flowthru.Flow;
using Flowthru.Prelude;
using Flowthru.Validation.PreFlight;

namespace Flowthru.Core.Tests.Storage;

/// <summary>
/// Granular failure-mode coverage for <see cref="ComposedStorageAdapter{TContainer, TRow}"/>
/// using stubbed medium / format / container so each branch (exists check,
/// deserialize sample, deserialize all) can be exercised independently.
/// Complements <see cref="ComposedStorageAdapterRoundTripTests"/> (happy-path
/// + NotFound on InspectShallow + read-only) by pinning the deep-inspection
/// branch and the deserialization-failure code paths.
/// </summary>
/// <remarks>
/// <para>
/// Ported from the old <c>02_Validation/PreFlightInspection/ComposedStorageAdapterTests</c>.
/// The previous fixture pinned every branch through stub components; the
/// active round-trip fixture only pins the file-backed-JSON composition. This
/// file restores the stubbed-branch coverage and adds the pre-flight
/// translation pin called out in the gap analysis: adapter returns NotFound
/// → pre-flight emits <see cref="PreFlightError.MissingInput"/>.
/// </para>
/// </remarks>
[TestFixture]
public class ComposedStorageAdapterFailureTests
{
  // ─────────────────────────────────────────────────────────────────────────
  // InspectShallow
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task InspectShallow_MediumExistsAndDeserializes_Succeeds()
  {
    var adapter = MakeAdapter(
      medium: new StubMedium(exists: true),
      format: new StubFormat(rowsToYield: 5)
    );

    var validation = await RunInspect(adapter.InspectShallow(sampleSize: 3));
    Assert.That(validation.IsValid, Is.True,
      "InspectShallow should succeed when the medium exists and deserialization yields rows.");
  }

  [Test]
  public async Task InspectShallow_MediumDoesNotExist_FailsWithNotFound()
  {
    var adapter = MakeAdapter(
      medium: new StubMedium(exists: false),
      format: new StubFormat(rowsToYield: 0)
    );

    var validation = await RunInspect(adapter.InspectShallow(sampleSize: 10));
    Assert.That(validation.IsValid, Is.False);
    Assert.That(
      validation.Errors,
      Has.Some.Matches<ValidationError>(e => e.ErrorType == ValidationErrorType.NotFound)
    );
  }

  [Test]
  public async Task InspectShallow_DeserializationThrows_FailsWithDeserializationError()
  {
    var adapter = MakeAdapter(
      medium: new StubMedium(exists: true),
      format: new StubFormat(throwOnDeserialize: true)
    );

    var validation = await RunInspect(adapter.InspectShallow(sampleSize: 10));
    Assert.That(validation.IsValid, Is.False,
      "Deserialization failure must surface as an InspectShallow failure, not throw.");
    Assert.That(
      validation.Errors,
      Has.Some.Matches<ValidationError>(e => e.ErrorType == ValidationErrorType.DeserializationError)
    );
  }

  // ─────────────────────────────────────────────────────────────────────────
  // InspectDeep
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task InspectDeep_MediumExistsAndDeserializesAll_Succeeds()
  {
    // Deep inspection iterates every row — verify it doesn't short-circuit
    // at the sample threshold and that no failure is reported when every
    // row deserializes successfully.
    var adapter = MakeAdapter(
      medium: new StubMedium(exists: true),
      format: new StubFormat(rowsToYield: 100)
    );

    var validation = await RunInspect(adapter.InspectDeep());
    Assert.That(validation.IsValid, Is.True,
      "InspectDeep should succeed when every row deserializes without error.");
  }

  [Test]
  public async Task InspectDeep_MediumDoesNotExist_FailsWithNotFound()
  {
    var adapter = MakeAdapter(
      medium: new StubMedium(exists: false),
      format: new StubFormat(rowsToYield: 0)
    );

    var validation = await RunInspect(adapter.InspectDeep());
    Assert.That(validation.IsValid, Is.False);
    Assert.That(
      validation.Errors,
      Has.Some.Matches<ValidationError>(e => e.ErrorType == ValidationErrorType.NotFound)
    );
  }

  [Test]
  public async Task InspectDeep_DeserializationThrows_FailsWithDeserializationError()
  {
    // The full-iteration path must catch and report deserialization
    // failures the same way the sampled path does.
    var adapter = MakeAdapter(
      medium: new StubMedium(exists: true),
      format: new StubFormat(throwOnDeserialize: true)
    );

    var validation = await RunInspect(adapter.InspectDeep());
    Assert.That(validation.IsValid, Is.False,
      "InspectDeep must surface a thrown deserialization error as a validation failure.");
    Assert.That(
      validation.Errors,
      Has.Some.Matches<ValidationError>(e => e.ErrorType == ValidationErrorType.DeserializationError)
    );
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Pre-flight translation
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task PreFlight_AdapterReturnsNotFound_EmitsMissingInputError()
  {
    // The gap-analysis "Impact" note for this gap: when an adapter returns
    // NotFound, pre-flight translates that into PreFlightError.MissingInput
    // for the caller. Pin that translation here so future changes to either
    // ValidationErrorType or PreFlightError shape can't drift unnoticed.
    var input = ItemFactory.Singleton.Memory<int>("missing-input");
    var output = ItemFactory.Singleton.Memory<int>("output");

    var flow = FlowBuilder.CreateFlow("missing-pin", b =>
      b.AddStep<int, int>("noop", x => x, input, output)
    );

    var result = await PreFlightPipeline.Run(flow).Run();
    var validated = ((EffResult<Validated<PreFlightError, FlowUnit>>.Success)result).Value;
    var invalid = validated as Validated<PreFlightError, FlowUnit>.Invalid;
    Assert.That(invalid, Is.Not.Null,
      "Memory adapter without Save should fail pre-flight with an Invalid result.");
    Assert.That(
      invalid!.Errors,
      Has.Some.Matches<PreFlightError>(e => e is PreFlightError.MissingInput),
      "NotFound from the adapter must map to PreFlightError.MissingInput."
    );
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Helpers
  // ─────────────────────────────────────────────────────────────────────────

  private static ComposedStorageAdapter<IEnumerable<StubRow>, StubRow> MakeAdapter(
    IStorageMedium medium,
    IFormatSerializer<StubRow> format
  ) => new(medium, format, new StubContainer());

  private static async Task<ValidationResult> RunInspect(FlowIO<ValidationResult> inspect)
  {
    var result = await inspect.Run();
    return ((EffResult<ValidationResult>.Success)result).Value;
  }

  // ── Stub components ────────────────────────────────────────────────────

  private sealed record StubRow;

  private sealed class StubMedium : IStorageMedium
  {
    private readonly bool _exists;

    public StubMedium(bool exists) => _exists = exists;

    public StorageTraits Traits => new();

    public FlowIO<Stream> ReadStream() => FlowIO.Lift<Stream>(() => new MemoryStream());

    public FlowIO<FlowUnit> WriteStream(Stream stream) => FlowIO.Pure(FlowUnit.Default);

    public FlowIO<bool> Exists() => FlowIO.Pure(_exists);

    public FlowIO<ValidationResult> InspectTarget() => FlowIO.Pure(ValidationResult.Success());
  }

  private sealed class StubFormat : IFormatSerializer<StubRow>
  {
    private readonly int _rowsToYield;
    private readonly bool _throwOnDeserialize;

    public StubFormat(int rowsToYield = 0, bool throwOnDeserialize = false)
    {
      _rowsToYield = rowsToYield;
      _throwOnDeserialize = throwOnDeserialize;
    }

    public StorageTraits Traits => new();

    public IAsyncEnumerable<StubRow> DeserializeRows(Stream stream)
    {
      if (_throwOnDeserialize)
      {
        throw new InvalidOperationException("Stub deserialization failure");
      }
      return YieldRows(_rowsToYield);
    }

    public Task SerializeRows(Stream stream, IAsyncEnumerable<StubRow> rows) =>
      Task.CompletedTask;

    private static async IAsyncEnumerable<StubRow> YieldRows(int count)
    {
      for (int i = 0; i < count; i++)
      {
        yield return new StubRow();
        await Task.Yield();
      }
    }
  }

  private sealed class StubContainer : IContainerAdapter<IEnumerable<StubRow>, StubRow>
  {
    public Task<IEnumerable<StubRow>> FromRows(IAsyncEnumerable<StubRow> rows) =>
      Task.FromResult(Enumerable.Empty<StubRow>());

    public IAsyncEnumerable<StubRow> ToRows(IEnumerable<StubRow> container) =>
      AsyncEnumerable.Empty<StubRow>();
  }
}
