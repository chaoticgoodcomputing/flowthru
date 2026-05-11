using Flowthru.Data.Storage;
using Flowthru.Prelude;

namespace Flowthru.Core.Tests.Storage;

/// <summary>
/// Pins that <see cref="ComposedStorageAdapter{TContainer,TRow}.InspectTarget"/>
/// delegates to the underlying <see cref="IStorageMedium.InspectTarget"/>.
/// </summary>
/// <remarks>
/// The composed adapter doesn't probe write access itself — that concern
/// belongs to the medium. These tests prove the composed adapter
/// propagates the medium's <see cref="ValidationResult"/> (both success
/// and failure) through unchanged, so consumers can rely on a single
/// well-defined <c>InspectTarget()</c> contract regardless of which
/// segment authored the result.
/// </remarks>
[TestFixture]
public class ComposedStorageAdapterTargetInspectionTests
{
  // ─────────────────────────────────────────────────────────────────────────
  // Delegation
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task InspectTarget_MediumReturnsSuccess_PropagatesSuccess()
  {
    var adapter = MakeAdapter(new StubMedium(ValidationResult.Success()));

    var result = await adapter.InspectTarget().Run();

    Assert.That(result, Is.InstanceOf<EffResult<ValidationResult>.Success>());
    var validationResult = ((EffResult<ValidationResult>.Success)result).Value;
    Assert.That(validationResult.IsValid, Is.True);
  }

  [Test]
  public async Task InspectTarget_MediumReturnsFailure_PropagatesFailure()
  {
    var failure = ValidationResult.Failure(
      catalogKey: "row",
      errorType: ValidationErrorType.WriteAccessDenied,
      message: "Medium says no write"
    );
    var adapter = MakeAdapter(new StubMedium(failure));

    var result = await adapter.InspectTarget().Run();

    Assert.That(result, Is.InstanceOf<EffResult<ValidationResult>.Success>());
    var validationResult = ((EffResult<ValidationResult>.Success)result).Value;
    Assert.That(validationResult.IsValid, Is.False);
    Assert.That(validationResult.Errors[0].ErrorType,
      Is.EqualTo(ValidationErrorType.WriteAccessDenied));
    Assert.That(validationResult.Errors[0].Message, Is.EqualTo("Medium says no write"));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Helpers
  // ─────────────────────────────────────────────────────────────────────────

  private static ComposedStorageAdapter<IEnumerable<StubRow>, StubRow> MakeAdapter(
    IStorageMedium medium
  ) =>
    new(medium, new StubFormat(), new StubContainer());

  private sealed record StubRow;

  /// <summary>
  /// Medium stub that returns a fixed <see cref="InspectTarget"/> result.
  /// </summary>
  private sealed class StubMedium : IStorageMedium
  {
    private readonly ValidationResult _targetResult;

    public StubMedium(ValidationResult targetResult) => _targetResult = targetResult;

    public StorageTraits Traits => new();

    public FlowIO<Stream> ReadStream() => FlowIO.Lift<Stream>(() => new MemoryStream());

    public FlowIO<FlowUnit> WriteStream(Stream stream) => FlowIO.Pure(FlowUnit.Default);

    public FlowIO<bool> Exists() => FlowIO.Pure(false);

    public FlowIO<ValidationResult> InspectTarget() => FlowIO.Pure(_targetResult);
  }

  private sealed class StubFormat : IFormatSerializer<StubRow>
  {
    public StorageTraits Traits => new();

    public IAsyncEnumerable<StubRow> DeserializeRows(Stream stream) =>
      AsyncEnumerable.Empty<StubRow>();

    public Task SerializeRows(Stream stream, IAsyncEnumerable<StubRow> rows) =>
      Task.CompletedTask;
  }

  private sealed class StubContainer : IContainerAdapter<IEnumerable<StubRow>, StubRow>
  {
    public Task<IEnumerable<StubRow>> FromRows(IAsyncEnumerable<StubRow> rows) =>
      Task.FromResult(Enumerable.Empty<StubRow>());

    public IAsyncEnumerable<StubRow> ToRows(IEnumerable<StubRow> container) =>
      AsyncEnumerable.Empty<StubRow>();
  }
}
