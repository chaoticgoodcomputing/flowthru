using Flowthru.Core.Data.Capabilities;
using Flowthru.Core.Data.Storage;
using Flowthru.Core.Data.Validation;
using Flowthru.Core.Effects;

namespace Flowthru.Core.Tests.Validation.TargetInspection;

/// <summary>
/// Tests verifying that <see cref="ComposedStorageAdapter{TContainer,TRow}.InspectTarget()"/>
/// delegates to the underlying <see cref="IStorageMedium"/>.
/// </summary>
[TestFixture]
[Category("Validation")]
[Category("TargetInspection")]
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

    Assert.That(result.IsValid, Is.True);
  }

  [Test]
  public async Task InspectTarget_MediumReturnsFailure_PropagatesFailure()
  {
    var failure = new ValidationResult(
      new[]
      {
        new ValidationError(
          "row",
          ValidationErrorType.WriteAccessDenied,
          "Medium says no write",
          null
        ),
      }
    );
    var adapter = MakeAdapter(new StubMedium(failure));

    var result = await adapter.InspectTarget().Run();

    Assert.That(result.IsValid, Is.False);
    Assert.That(result.Errors[0].ErrorType, Is.EqualTo(ValidationErrorType.WriteAccessDenied));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Helpers
  // ─────────────────────────────────────────────────────────────────────────

  private static ComposedStorageAdapter<IEnumerable<StubRow>, StubRow> MakeAdapter(
    IStorageMedium medium
  ) =>
    new ComposedStorageAdapter<IEnumerable<StubRow>, StubRow>(
      medium,
      new StubFormat(),
      new StubContainer()
    );

  private record StubRow;

  /// <summary>
  /// Medium stub that returns a fixed <c>InspectTarget()</c> result.
  /// </summary>
  private sealed class StubMedium : IStorageMedium
  {
    private readonly ValidationResult _targetResult;

    public StubMedium(ValidationResult targetResult) => _targetResult = targetResult;

    public StorageTraits Traits => new StorageTraits();

    public FlowIO<Stream> ReadStream() => FlowIO.Lift<Stream>(() => new MemoryStream());

    public FlowIO<FlowUnit> WriteStream(Stream stream) => FlowIO.Pure(FlowUnit.Default);

    public FlowIO<bool> Exists() => FlowIO.Pure(false);

    public FlowIO<ValidationResult> InspectTarget() => FlowIO.Pure(_targetResult);
  }

  private sealed class StubFormat : IFormatSerializer<StubRow>
  {
    public StorageTraits Traits => new StorageTraits();

    public IAsyncEnumerable<StubRow> DeserializeRows(Stream stream) =>
      AsyncEnumerable.Empty<StubRow>();

    public Task SerializeRows(Stream stream, IAsyncEnumerable<StubRow> rows) => Task.CompletedTask;

    public PropertyMappingConfiguration GetPropertyMappingConfiguration() =>
      PropertyMappingConfiguration.LibraryControlled();
  }

  private sealed class StubContainer : IContainerAdapter<IEnumerable<StubRow>, StubRow>
  {
    public Task<IEnumerable<StubRow>> FromRows(IAsyncEnumerable<StubRow> rows) =>
      Task.FromResult(Enumerable.Empty<StubRow>());

    public IAsyncEnumerable<StubRow> ToRows(IEnumerable<StubRow> container) =>
      AsyncEnumerable.Empty<StubRow>();
  }
}
