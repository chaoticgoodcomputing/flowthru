using Flowthru.Data.Schema;
using Flowthru.Data.Storage;
using Flowthru.Prelude;
using Flowthru.Validation.Runtime;
using SysIO = System.IO;

namespace Flowthru.Core.Tests.Storage;

[FlowthruSchema]
public partial record SmTestRow
{
  public required int Id { get; init; }
  public required string Name { get; init; }
}

/// <summary>
/// Asserts that <see cref="ComposedStorageAdapter{TContainer, TRow}"/>
/// translates <see cref="SchemaMismatchException"/> thrown from inside
/// the row stream into a typed <see cref="RuntimeError.SchemaMismatch"/>
/// at the FlowIO boundary — preserving typed-error fidelity through
/// the iterator-throws-exception bridge that's structurally required
/// by <c>IAsyncEnumerable.MoveNextAsync</c>.
/// </summary>
[TestFixture]
public class SchemaMismatchTranslationTests
{
  /// <summary>
  /// Reader that throws <see cref="SchemaMismatchException"/> from
  /// inside the row stream — emulates a real format extension's
  /// header-mismatch translation (CsvHelper, Parquet, etc.).
  /// </summary>
  private sealed class ThrowingReader : IFormatRowReader<SmTestRow>
  {
    public StorageTraits Traits => new();

    public async IAsyncEnumerable<SmTestRow> DeserializeRows(SysIO.Stream stream)
    {
      await Task.Yield();
      throw new SchemaMismatchException(
        "Expected column 'foo' was not found",
        new InvalidOperationException("upstream provider details")
      );
#pragma warning disable CS0162 // Unreachable
      yield break;
#pragma warning restore CS0162
    }
  }

  /// <summary>
  /// Writer/reader pair: the reader throws SchemaMismatch on every
  /// call. We don't exercise the writer in this test.
  /// </summary>
  private sealed class ThrowingFormat : IFormatSerializer<SmTestRow>
  {
    private readonly ThrowingReader _reader = new();
    public StorageTraits Traits => _reader.Traits;
    public IAsyncEnumerable<SmTestRow> DeserializeRows(SysIO.Stream stream) =>
      _reader.DeserializeRows(stream);
    public Task SerializeRows(SysIO.Stream stream, IAsyncEnumerable<SmTestRow> rows) =>
      Task.CompletedTask;
  }

  [Test]
  public async Task Load_SchemaMismatchThrownFromReader_SurfacesTypedRuntimeError()
  {
    var path = SysIO.Path.Combine(
      SysIO.Path.GetTempPath(), $"flowthru-sm-{Guid.NewGuid():N}.bin"
    );
    try
    {
      // Reader doesn't actually parse, so any non-empty file works.
      await SysIO.File.WriteAllBytesAsync(path, new byte[] { 0x00 });

      var adapter = new ComposedStorageAdapter<IEnumerable<SmTestRow>, SmTestRow>(
        new FileStorageMedium(path),
        new ThrowingFormat(),
        new EnumerableContainerAdapter<SmTestRow>()
      );

      var loadResult = await adapter.Load().Run();

      Assert.That(loadResult, Is.InstanceOf<EffResult<IEnumerable<SmTestRow>>.Failure>());
      var failure = (EffResult<IEnumerable<SmTestRow>>.Failure)loadResult;

      Assert.That(failure.Error, Is.InstanceOf<RuntimeError.SchemaMismatch>(),
        "ComposedStorageAdapter must translate SchemaMismatchException into the typed "
        + "RuntimeError.SchemaMismatch variant — not leave it wrapped as External."
      );
      var sm = (RuntimeError.SchemaMismatch)failure.Error;
      Assert.That(sm.Detail, Does.Contain("Expected column 'foo'"),
        "Detail should preserve the message verbatim from the provider exception."
      );
      Assert.That(sm.InnerExceptionInfo, Does.Contain("upstream provider details"),
        "InnerExceptionInfo should carry the inner-exception's diagnostic surface."
      );
    }
    finally
    {
      if (SysIO.File.Exists(path)) SysIO.File.Delete(path);
    }
  }

  [Test]
  public async Task Load_NonSchemaMismatchException_RemainsExternal()
  {
    // Sanity check: only SchemaMismatchException promotes to typed
    // SchemaMismatch. Other exceptions still surface as External so
    // we don't accidentally over-classify.
    var missingPath = SysIO.Path.Combine(
      SysIO.Path.GetTempPath(), $"flowthru-sm-missing-{Guid.NewGuid():N}.bin"
    );

    var adapter = new ComposedStorageAdapter<IEnumerable<SmTestRow>, SmTestRow>(
      new FileStorageMedium(missingPath),
      new ThrowingFormat(),
      new EnumerableContainerAdapter<SmTestRow>()
    );

    var loadResult = await adapter.Load().Run();
    Assert.That(loadResult, Is.InstanceOf<EffResult<IEnumerable<SmTestRow>>.Failure>());
    var failure = (EffResult<IEnumerable<SmTestRow>>.Failure)loadResult;

    Assert.That(failure.Error, Is.InstanceOf<RuntimeError.External>(),
      "Non-schema-mismatch failures (here: file-not-found from the medium) "
      + "must surface as External, not get up-classified to SchemaMismatch."
    );
  }
}
