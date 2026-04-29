using Flowthru.Core.Data.Storage;
using Flowthru.Core.Data.Validation;
using Flowthru.Tests.Kits.Storage;
using Flowthru.Tests.Kits.Schemas;

namespace Flowthru.Core.Tests.Validation.PreFlightInspection;

/// <summary>
/// Coverage tests for <see cref="MemoryStorageAdapter{T}"/> via the
/// <see cref="StorageAdapterAssertions"/> harness. Memory adapters succeed on inspection
/// only after data has been saved at least once.
/// </summary>
[TestFixture]
[Category("Validation")]
[Category("PreFlightInspection")]
public class MemoryStorageAdapterTests
{
  [Test]
  public Task InspectShallow_AfterSave_Succeeds()
  {
    var adapter = new MemoryStorageAdapter<RequiredMembersSchema>(SeedRow());
    return StorageAdapterAssertions.InspectShallowSucceeds(adapter);
  }

  [Test]
  public Task InspectShallow_EmptyAdapter_FailsWithNotFound()
  {
    var adapter = new MemoryStorageAdapter<RequiredMembersSchema>();
    return StorageAdapterAssertions.InspectShallowFails(adapter, ValidationErrorType.NotFound);
  }

  [Test]
  public Task InspectDeep_AfterSave_Succeeds()
  {
    var adapter = new MemoryStorageAdapter<RequiredMembersSchema>(SeedRow());
    return StorageAdapterAssertions.InspectDeepSucceeds(adapter);
  }

  [Test]
  public Task InspectTarget_AlwaysSucceeds()
  {
    var adapter = new MemoryStorageAdapter<RequiredMembersSchema>();
    return StorageAdapterAssertions.InspectTargetSucceeds(adapter);
  }

  [Test]
  public Task Exists_AfterSave_ReturnsTrue()
  {
    var adapter = new MemoryStorageAdapter<RequiredMembersSchema>(SeedRow());
    return StorageAdapterAssertions.ExistsReturns(adapter, expected: true);
  }

  [Test]
  public Task Exists_EmptyAdapter_ReturnsFalse()
  {
    var adapter = new MemoryStorageAdapter<RequiredMembersSchema>();
    return StorageAdapterAssertions.ExistsReturns(adapter, expected: false);
  }

  [Test]
  public Task SaveAndLoad_RoundTripsContent()
  {
    var adapter = new MemoryStorageAdapter<RequiredMembersSchema>();
    return StorageAdapterAssertions.SaveAndLoadRoundTrips(adapter, SeedRow());
  }

  private static RequiredMembersSchema SeedRow() =>
    new()
    {
      Id = Guid.NewGuid(),
      Name = "memory-seed",
      Value = 7,
      Timestamp = null,
      Description = null,
    };
}
