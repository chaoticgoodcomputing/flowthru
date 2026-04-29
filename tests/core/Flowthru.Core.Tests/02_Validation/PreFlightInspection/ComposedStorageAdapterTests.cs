using Flowthru.Core.Data.Capabilities;
using Flowthru.Core.Data.Storage;
using Flowthru.Core.Data.Validation;
using Flowthru.Core.Effects;
using Flowthru.Tests.Kits.Storage;

namespace Flowthru.Core.Tests.Validation.PreFlightInspection;

/// <summary>
/// Coverage tests for <see cref="ComposedStorageAdapter{TContainer, TRow}"/>'s
/// <c>InspectShallow</c> and <c>InspectDeep</c> via the
/// <see cref="StorageAdapterAssertions"/> harness. Uses stub medium / format / container
/// components so each branch (exists check, deserialize sample, deserialize all) can be
/// exercised independently. Sibling fixture <c>ComposedStorageAdapterTargetInspectionTests</c>
/// covers <c>InspectTarget</c> delegation.
/// </summary>
[TestFixture]
[Category("Validation")]
[Category("PreFlightInspection")]
public class ComposedStorageAdapterTests
{
  // ─────────────────────────────────────────────────────────────────────────
  // InspectShallow
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public Task InspectShallow_MediumExistsAndDeserializes_Succeeds()
  {
    var adapter = MakeAdapter(
      medium: new StubMedium(exists: true),
      format: new StubFormat(rowsToYield: 5)
    );
    return StorageAdapterAssertions.InspectShallowSucceeds(adapter);
  }

  [Test]
  public Task InspectShallow_MediumDoesNotExist_FailsWithNotFound()
  {
    var adapter = MakeAdapter(
      medium: new StubMedium(exists: false),
      format: new StubFormat(rowsToYield: 0)
    );
    return StorageAdapterAssertions.InspectShallowFails(adapter, ValidationErrorType.NotFound);
  }

  [Test]
  public Task InspectShallow_DeserializationThrows_FailsWithDeserializationError()
  {
    var adapter = MakeAdapter(
      medium: new StubMedium(exists: true),
      format: new StubFormat(throwOnDeserialize: true)
    );
    return StorageAdapterAssertions.InspectShallowFails(
      adapter,
      ValidationErrorType.DeserializationError
    );
  }

  // ─────────────────────────────────────────────────────────────────────────
  // InspectDeep
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public Task InspectDeep_MediumExistsAndDeserializesAll_Succeeds()
  {
    var adapter = MakeAdapter(
      medium: new StubMedium(exists: true),
      format: new StubFormat(rowsToYield: 100)
    );
    return StorageAdapterAssertions.InspectDeepSucceeds(adapter);
  }

  [Test]
  public Task InspectDeep_MediumDoesNotExist_FailsWithNotFound()
  {
    var adapter = MakeAdapter(
      medium: new StubMedium(exists: false),
      format: new StubFormat(rowsToYield: 0)
    );
    return StorageAdapterAssertions.InspectDeepFails(adapter, ValidationErrorType.NotFound);
  }

  [Test]
  public Task InspectDeep_DeserializationThrows_FailsWithDeserializationError()
  {
    var adapter = MakeAdapter(
      medium: new StubMedium(exists: true),
      format: new StubFormat(throwOnDeserialize: true)
    );
    return StorageAdapterAssertions.InspectDeepFails(adapter, ValidationErrorType.DeserializationError);
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Helpers
  // ─────────────────────────────────────────────────────────────────────────

  private static ComposedStorageAdapter<IEnumerable<StubRow>, StubRow> MakeAdapter(
    IStorageMedium medium,
    IFormatSerializer<StubRow> format
  ) => new(medium, format, new StubContainer());

  private record StubRow;

  private sealed class StubMedium : IStorageMedium
  {
    private readonly bool _exists;

    public StubMedium(bool exists) => _exists = exists;

    public StorageTraits Traits => new StorageTraits();

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

    public StorageTraits Traits => new StorageTraits();

    public IAsyncEnumerable<StubRow> DeserializeRows(Stream stream)
    {
      if (_throwOnDeserialize)
      {
        throw new InvalidOperationException("Stub deserialization failure");
      }
      return YieldRows(_rowsToYield);
    }

    public Task SerializeRows(Stream stream, IAsyncEnumerable<StubRow> rows) => Task.CompletedTask;

    public PropertyMappingConfiguration GetPropertyMappingConfiguration() =>
      PropertyMappingConfiguration.LibraryControlled();

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
