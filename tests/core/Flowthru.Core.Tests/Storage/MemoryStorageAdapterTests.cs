using Flowthru.Data.Storage;
using Flowthru.Prelude;

namespace Flowthru.Core.Tests.Storage;

/// <summary>
/// Per-adapter unit coverage for <see cref="MemoryStorageAdapter{T}"/>. The
/// shared <see cref="MemoryStorageAdapterLaws"/> fixture covers the
/// algebra-laws surface; this fixture pins behaviours specific to the
/// in-memory adapter's "no data until Save" semantics — inspections fail
/// fast with <see cref="ValidationErrorType.NotFound"/>, and pre-flight
/// translates that finding into a <c>MissingInput</c> error.
/// </summary>
/// <remarks>
/// Ported from <c>02_Validation/PreFlightInspection/MemoryStorageAdapterTests</c>.
/// The kit's <c>StorageAdapterAssertions</c> harness is not compiled yet on
/// this branch, so each test runs the relevant <see cref="FlowIO{A}"/>
/// directly.
/// </remarks>
[TestFixture]
public class MemoryStorageAdapterTests
{
  // ─────────────────────────────────────────────────────────────────────────
  // InspectShallow
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task InspectShallow_AfterSave_Succeeds()
  {
    var adapter = new MemoryStorageAdapter<MemoryRow>();
    await adapter.Save(SeedRow()).Run();

    var validation = await RunInspect(adapter.InspectShallow(sampleSize: 10));
    Assert.That(validation.IsValid, Is.True,
      "InspectShallow on a memory adapter that has been saved should succeed.");
  }

  [Test]
  public async Task InspectShallow_EmptyAdapter_FailsWithNotFound()
  {
    var adapter = new MemoryStorageAdapter<MemoryRow>();

    var validation = await RunInspect(adapter.InspectShallow(sampleSize: 10));
    Assert.That(validation.IsValid, Is.False,
      "A fresh memory adapter has no data — InspectShallow should report missing.");
    Assert.That(
      validation.Errors,
      Has.Some.Matches<ValidationError>(e => e.ErrorType == ValidationErrorType.NotFound),
      "Memory adapters surface absent data as NotFound."
    );
  }

  // ─────────────────────────────────────────────────────────────────────────
  // InspectDeep
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task InspectDeep_AfterSave_Succeeds()
  {
    var adapter = new MemoryStorageAdapter<MemoryRow>();
    await adapter.Save(SeedRow()).Run();

    var validation = await RunInspect(adapter.InspectDeep());
    Assert.That(validation.IsValid, Is.True,
      "Memory adapter has no on-disk representation — InspectDeep mirrors InspectShallow.");
  }

  [Test]
  public async Task InspectDeep_EmptyAdapter_FailsWithNotFound()
  {
    var adapter = new MemoryStorageAdapter<MemoryRow>();

    var validation = await RunInspect(adapter.InspectDeep());
    Assert.That(validation.IsValid, Is.False,
      "InspectDeep on an empty memory adapter should report NotFound, matching InspectShallow.");
    Assert.That(
      validation.Errors,
      Has.Some.Matches<ValidationError>(e => e.ErrorType == ValidationErrorType.NotFound)
    );
  }

  // ─────────────────────────────────────────────────────────────────────────
  // InspectTarget
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task InspectTarget_AlwaysSucceeds()
  {
    // Memory adapters are non-persistent and have no destination to probe —
    // InspectTarget is a trivial success regardless of save state.
    var empty = new MemoryStorageAdapter<MemoryRow>();
    var saved = new MemoryStorageAdapter<MemoryRow>();
    await saved.Save(SeedRow()).Run();

    var emptyResult = await RunInspect(empty.InspectTarget());
    var savedResult = await RunInspect(saved.InspectTarget());

    Assert.That(emptyResult.IsValid, Is.True,
      "InspectTarget on an empty memory adapter is trivially valid.");
    Assert.That(savedResult.IsValid, Is.True,
      "InspectTarget on a saved memory adapter is trivially valid.");
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Exists
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task Exists_AfterSave_ReturnsTrue()
  {
    var adapter = new MemoryStorageAdapter<MemoryRow>();
    await adapter.Save(SeedRow()).Run();

    var exists = await RunBool(adapter.Exists());
    Assert.That(exists, Is.True);
  }

  [Test]
  public async Task Exists_EmptyAdapter_ReturnsFalse()
  {
    var adapter = new MemoryStorageAdapter<MemoryRow>();

    var exists = await RunBool(adapter.Exists());
    Assert.That(exists, Is.False);
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Traits
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void Traits_ReportIsPersistentFalse()
  {
    // Pins the trait that drives pre-flight's per-item treatment: in-memory
    // data is ephemeral and doesn't survive across pipeline runs.
    var adapter = new MemoryStorageAdapter<MemoryRow>();
    Assert.That(adapter.Traits.IsPersistent, Is.False,
      "MemoryStorageAdapter is non-persistent by design.");
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Helpers
  // ─────────────────────────────────────────────────────────────────────────

  private static async Task<ValidationResult> RunInspect(FlowIO<ValidationResult> inspect)
  {
    var result = await inspect.Run();
    return ((EffResult<ValidationResult>.Success)result).Value;
  }

  private static async Task<bool> RunBool(FlowIO<bool> io)
  {
    var result = await io.Run();
    return ((EffResult<bool>.Success)result).Value;
  }

  private static MemoryRow SeedRow() =>
    new() { Id = 7, Name = "memory-seed" };

  /// <summary>Plain row type — no <c>[FlowthruSchema]</c> required because
  /// memory adapter stores values directly without serialization.</summary>
  public sealed record MemoryRow
  {
    public required int Id { get; init; }
    public required string Name { get; init; }
  }
}
