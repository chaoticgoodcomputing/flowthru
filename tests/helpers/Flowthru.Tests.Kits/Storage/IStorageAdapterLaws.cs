using Flowthru.Data.Storage;
using Flowthru.Prelude;

namespace Flowthru.Tests.Kits.Storage;

/// <summary>
/// Laws every <see cref="IStorageAdapter{T}"/> implementer must satisfy.
/// Subclasses bind a concrete container type, build well-formed and
/// missing-source variants, and inherit tests covering round-trip,
/// inspection, and read-only-fail-fast semantics.
/// </summary>
/// <typeparam name="T">The container type the adapter loads/saves.</typeparam>
/// <remarks>
/// Replaces the prior <c>StorageAdapterConformance&lt;T&gt;</c> kit from
/// §1.7. Behaves identically; renamed per §2.11 to align with the
/// algebra-laws framing.
/// </remarks>
public abstract class IStorageAdapterLaws<T>
{
  /// <summary>Build an adapter that, when read, returns equivalent to <see cref="SampleData"/>.</summary>
  protected abstract IStorageAdapter<T> CreateWellFormed();

  /// <summary>Build an adapter pointing at a nonexistent / inaccessible source.</summary>
  protected abstract IStorageAdapter<T> CreateMissingSource();

  /// <summary>Sample data the round-trip law uses.</summary>
  protected abstract T SampleData { get; }

  /// <summary>Optional comparer for round-trip equivalence.</summary>
  protected virtual IEqualityComparer<T>? Comparer => null;

  /// <summary>
  /// The <see cref="ValidationErrorType"/> a missing source should report.
  /// Filesystem and HTTP adapters return <see cref="ValidationErrorType.NotFound"/>;
  /// EFCore adapters return <see cref="ValidationErrorType.EmptyDataset"/>
  /// because their "missing" semantics are an empty table rather than a
  /// missing source.
  /// </summary>
  protected virtual ValidationErrorType MissingSourceErrorType => ValidationErrorType.NotFound;

  // ── Round-trip law ─────────────────────────────────────────────────────

  /// <summary>
  /// Save then Load returns the saved value. The fundamental adapter
  /// invariant. Skipped for read-only adapters
  /// (<see cref="StorageTraits.CanWrite"/> = <c>false</c>).
  /// </summary>
  [Test]
  public async Task RoundTripLaw()
  {
    var adapter = CreateWellFormed();
    if (!adapter.Traits.CanWrite)
    {
      Assert.Pass(
        "Adapter is read-only (Traits.CanWrite=false); round-trip law is not applicable. "
          + "Read-side behavior is exercised by InspectShallow_OnWellFormed_Succeeds."
      );
    }

    var saveResult = await adapter.Save(SampleData).Run();
    Assert.That(saveResult, Is.InstanceOf<EffResult<FlowUnit>.Success>(),
      "Save should succeed against the well-formed adapter.");

    var loadResult = await adapter.Load().Run();
    Assert.That(loadResult, Is.InstanceOf<EffResult<T>.Success>(),
      "Load should succeed after a successful Save.");

    var loaded = ((EffResult<T>.Success)loadResult).Value;
    if (Comparer is not null)
    {
      Assert.That(Comparer.Equals(loaded, SampleData), Is.True,
        "Loaded data should equal saved data per the supplied comparer.");
    }
  }

  // ── Inspect-shallow laws ───────────────────────────────────────────────

  /// <summary>InspectShallow on a well-formed source returns <c>IsValid = true</c>.</summary>
  [Test]
  public async Task InspectShallowOnWellFormedLaw()
  {
    var adapter = CreateWellFormed();

    if (adapter.Traits.CanWrite)
    {
      // Most adapters need data present before an inspect read can validate.
      await adapter.Save(SampleData).Run();
    }

    var result = await adapter.InspectShallow(sampleSize: 10).Run();
    Assert.That(result, Is.InstanceOf<EffResult<ValidationResult>.Success>());
    var validation = ((EffResult<ValidationResult>.Success)result).Value;
    Assert.That(validation.IsValid, Is.True,
      $"InspectShallow on a well-formed source should succeed. "
        + $"Errors: {string.Join("; ", validation.Errors.Select(e => e.Message))}");
  }

  /// <summary>InspectShallow on a missing source surfaces the expected error type.</summary>
  [Test]
  public async Task InspectShallowOnMissingSourceLaw()
  {
    var adapter = CreateMissingSource();

    var result = await adapter.InspectShallow(sampleSize: 10).Run();
    Assert.That(result, Is.InstanceOf<EffResult<ValidationResult>.Success>());
    var validation = ((EffResult<ValidationResult>.Success)result).Value;
    Assert.That(validation.IsValid, Is.False,
      "InspectShallow on a missing source should produce an Invalid result.");
    Assert.That(validation.Errors, Has.Some.Matches<ValidationError>(e => e.ErrorType == MissingSourceErrorType),
      $"Expected error type {MissingSourceErrorType}. Got: "
        + $"{string.Join(", ", validation.Errors.Select(e => $"[{e.ErrorType}] {e.Message}"))}");
  }

  // ── Existence laws ─────────────────────────────────────────────────────

  /// <summary>Exists on a missing source returns <c>false</c>.</summary>
  [Test]
  public async Task ExistsOnMissingSourceLaw()
  {
    var adapter = CreateMissingSource();
    var result = await adapter.Exists().Run();
    Assert.That(result, Is.InstanceOf<EffResult<bool>.Success>());
    Assert.That(((EffResult<bool>.Success)result).Value, Is.False);
  }

  // ── Inspect-target law ─────────────────────────────────────────────────

  /// <summary>InspectTarget on a writable, well-formed adapter succeeds.</summary>
  [Test]
  public async Task InspectTargetOnWritableLaw()
  {
    var adapter = CreateWellFormed();
    var result = await adapter.InspectTarget().Run();
    Assert.That(result, Is.InstanceOf<EffResult<ValidationResult>.Success>());
    var validation = ((EffResult<ValidationResult>.Success)result).Value;
    Assert.That(validation.IsValid, Is.True,
      $"InspectTarget should succeed for a writable adapter. "
        + $"Errors: {string.Join("; ", validation.Errors.Select(e => e.Message))}");
  }
}
