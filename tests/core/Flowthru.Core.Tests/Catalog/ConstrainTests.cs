using Flowthru.Data.Catalog;
using Flowthru.Data.Storage;
using Flowthru.Prelude;
using Flowthru.Validation.Runtime;

namespace Flowthru.Core.Tests.Catalog;

/// <summary>
/// Tests the <see cref="CatalogItemExtensions.Constrain{T}"/> surface:
/// trait narrowing, one-way-ratchet enforcement, and typed-error
/// fidelity for blocked operations.
/// </summary>
[TestFixture]
public class ConstrainTests
{
  // ── Narrowing semantics ─────────────────────────────────────────────

  [Test]
  public async Task ConstrainCanWriteToFalse_BlocksSaveWithTypedError()
  {
    var item = ItemFactory.Singleton.Memory<int>("x");
    await item.Save(42).Run();

    var readOnly = item.Constrain(t => t with { CanWrite = false });

    var saveResult = await readOnly.Save(99).Run();
    Assert.That(saveResult, Is.InstanceOf<EffResult<FlowUnit>.Failure>());
    var failure = (EffResult<FlowUnit>.Failure)saveResult;
    Assert.That(failure.Error, Is.InstanceOf<RuntimeError.ConstraintViolated>(),
      "Save against a CanWrite=false constraint should surface as the typed "
      + "RuntimeError.ConstraintViolated variant — not External, not exception.");

    var cv = (RuntimeError.ConstraintViolated)failure.Error;
    Assert.That(cv.ItemLabel, Is.EqualTo("x"));
    Assert.That(cv.Operation, Is.EqualTo("Save"));
    Assert.That(cv.TraitName, Is.EqualTo("CanWrite"));
  }

  [Test]
  public async Task ConstrainCanWriteToFalse_LoadStillSucceeds()
  {
    var item = ItemFactory.Singleton.Memory<int>("x");
    await item.Save(42).Run();

    var readOnly = item.Constrain(t => t with { CanWrite = false });
    var loadResult = await readOnly.Load().Run();

    Assert.That(loadResult, Is.InstanceOf<EffResult<int>.Success>(),
      "Read-only constraint should not impede reads.");
    Assert.That(((EffResult<int>.Success)loadResult).Value, Is.EqualTo(42));
  }

  [Test]
  public async Task ConstrainCanReadToFalse_BlocksLoadWithTypedError()
  {
    var item = ItemFactory.Singleton.Memory<int>("y");
    await item.Save(1).Run();

    var noRead = item.Constrain(t => t with { CanRead = false });
    var loadResult = await noRead.Load().Run();

    Assert.That(loadResult, Is.InstanceOf<EffResult<int>.Failure>());
    var cv = (RuntimeError.ConstraintViolated)((EffResult<int>.Failure)loadResult).Error;
    Assert.That(cv.TraitName, Is.EqualTo("CanRead"));
    Assert.That(cv.Operation, Is.EqualTo("Load"));
  }

  [Test]
  public void ConstrainedItem_Traits_ReflectNarrowing()
  {
    var item = ItemFactory.Singleton.Memory<int>("z");
    var readOnly = item.Constrain(t => t with { CanWrite = false });

    // Reach the constrained adapter via the typed Item<T>.Storage.
    var constrainedItem = (Item<int>)readOnly;
    Assert.That(constrainedItem.Storage.Traits.CanWrite, Is.False);
    Assert.That(constrainedItem.Storage.Traits.CanRead, Is.True,
      "Untouched traits stay at their inner-adapter values.");
  }

  // ── One-way-ratchet enforcement ─────────────────────────────────────

  [Test]
  public void Constrain_AttemptingToWiden_ThrowsAtConstruction()
  {
    // Memory adapter starts with CanStream = false; constraint that
    // tries to flip CanStream true should fail loud at wire-up.
    var item = ItemFactory.Singleton.Memory<int>("widen-attempt");

    Assert.That(
      () => item.Constrain(t => t with { CanStream = true }),
      Throws.ArgumentException.With.Message.Contains("widen trait 'CanStream'"),
      "One-way ratchet: a false → true widening must fail loud at catalog wire-up."
    );
  }

  [Test]
  public void Constrain_NarrowingMultipleTraits_Succeeds()
  {
    // Multiple narrowings in one constraint are fine — the ratchet only
    // forbids widening, not multi-axis tightening.
    var item = ItemFactory.Singleton.Memory<int>("multi");

    Assert.That(
      () => item.Constrain(t => t with { CanWrite = false, IsTransactional = false }),
      Throws.Nothing
    );
  }

  // ── Pure-function semantics ─────────────────────────────────────────

  [Test]
  public async Task Constrain_DoesNotMutateOriginalItem()
  {
    var original = ItemFactory.Singleton.Memory<int>("pure");
    await original.Save(42).Run();

    _ = original.Constrain(t => t with { CanWrite = false });

    // The original item should still accept writes — Constrain returned
    // a new IItem<T> rather than mutating in place.
    var saveResult = await original.Save(99).Run();
    Assert.That(saveResult, Is.InstanceOf<EffResult<FlowUnit>.Success>(),
      "Constrain is a pure function — it must not mutate the source item.");
  }

  // ── Argument validation ─────────────────────────────────────────────

  [Test]
  public void Constrain_NullItem_Throws()
  {
    Assert.That(
      () => CatalogItemExtensions.Constrain<int>(null!, t => t),
      Throws.ArgumentNullException
    );
  }

  [Test]
  public void Constrain_NullNarrow_Throws()
  {
    var item = ItemFactory.Singleton.Memory<int>("null-narrow");
    Assert.That(
      () => item.Constrain(null!),
      Throws.ArgumentNullException
    );
  }
}
