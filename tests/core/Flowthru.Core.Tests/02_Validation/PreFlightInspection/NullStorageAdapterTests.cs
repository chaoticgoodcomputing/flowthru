using Flowthru.Core.Data.Storage;
using Flowthru.Core.Steps;
using Flowthru.Tests.Kits.Storage;

namespace Flowthru.Core.Tests.Validation.PreFlightInspection;

/// <summary>
/// Coverage tests for <see cref="NullStorageAdapter{T}"/> via the
/// <see cref="StorageAdapterAssertions"/> harness. Null adapters are trivially valid
/// across all inspections (no data required) and report <c>Exists() = false</c>.
/// </summary>
[TestFixture]
[Category("Validation")]
[Category("PreFlightInspection")]
public class NullStorageAdapterTests
{
  [Test]
  public Task InspectShallow_AlwaysSucceeds() =>
    StorageAdapterAssertions.InspectShallowSucceeds(new NullStorageAdapter<NoData>());

  [Test]
  public Task InspectDeep_AlwaysSucceeds() =>
    StorageAdapterAssertions.InspectDeepSucceeds(new NullStorageAdapter<NoData>());

  [Test]
  public Task InspectTarget_AlwaysSucceeds() =>
    StorageAdapterAssertions.InspectTargetSucceeds(new NullStorageAdapter<NoData>());

  [Test]
  public Task Exists_AlwaysReturnsFalse() =>
    StorageAdapterAssertions.ExistsReturns(new NullStorageAdapter<NoData>(), expected: false);

  [Test]
  public async Task Save_DoesNotThrow()
  {
    var adapter = new NullStorageAdapter<NoData>();
    await adapter.Save(NoData.Value).Run();
    // No assertion needed — Save is a no-op for null adapters; just confirm it runs.
  }

  [Test]
  public void Load_ThrowsNotSupported()
  {
    var adapter = new NullStorageAdapter<NoData>();

    Assert.That(
      async () => await adapter.Load().Run(),
      Throws.TypeOf<NotSupportedException>().With.Message.Contains("Load")
    );
  }
}
