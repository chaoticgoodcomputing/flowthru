using Flowthru.Data.Catalog;
using Flowthru.Prelude;
using Flowthru.Step;

namespace Flowthru.Core.Tests.Catalog;

/// <summary>
/// Tests for the Phase 9 catalog-item container/row introspection
/// helpers on the static <see cref="Item"/> class. These helpers
/// underpin per-extension source generators (overload emission keyed
/// on the catalog item's container kind) and pre-flight diagnostics
/// (FT1302 dispatch).
/// </summary>
[TestFixture]
public class ItemIntrospectionTests
{
  private sealed record Row(int Id, string Name);

  // ── ContainerKindOf ──────────────────────────────────────────────────

  [Test]
  public void ContainerKindOf_BareValueType_IsSingleton() =>
    Assert.That(Item.ContainerKindOf<int>(), Is.EqualTo(StepContainerKind.Singleton));

  [Test]
  public void ContainerKindOf_BarePoco_IsSingleton() =>
    Assert.That(Item.ContainerKindOf<Row>(), Is.EqualTo(StepContainerKind.Singleton));

  [Test]
  public void ContainerKindOf_String_IsSingleton()
  {
    // string implements IEnumerable<char> but is semantically a value —
    // never appears in catalog as a sequence of characters.
    Assert.That(Item.ContainerKindOf<string>(), Is.EqualTo(StepContainerKind.Singleton));
  }

  [Test]
  public void ContainerKindOf_ByteArray_IsSingleton()
  {
    // Arrays incidentally implement IEnumerable<T> but in catalog terms
    // are binary blobs / fixed-shape payloads — explicit collections
    // use IEnumerable<T> or List<T>.
    Assert.That(Item.ContainerKindOf<byte[]>(), Is.EqualTo(StepContainerKind.Singleton));
  }

  [Test]
  public void ContainerKindOf_IEnumerable_IsEnumerable() =>
    Assert.That(Item.ContainerKindOf<IEnumerable<Row>>(), Is.EqualTo(StepContainerKind.Enumerable));

  [Test]
  public void ContainerKindOf_List_IsEnumerable() =>
    Assert.That(Item.ContainerKindOf<List<Row>>(), Is.EqualTo(StepContainerKind.Enumerable));

  [Test]
  public void ContainerKindOf_IQueryable_IsQueryable()
  {
    // IQueryable<T> is also IEnumerable<T>-assignable — the resolver
    // must walk in specificity order to pick Queryable.
    Assert.That(Item.ContainerKindOf<IQueryable<Row>>(), Is.EqualTo(StepContainerKind.Queryable));
  }

  [Test]
  public void ContainerKindOf_FlowSource_IsSource() =>
    // FlowSource<T> is the streaming catalog payload (.AsStream()). It
    // implements none of the sequence interfaces, so introspection must
    // recognise it structurally — otherwise it falls through to Singleton
    // with row type FlowSource<T>, the bug ADR-0023 corrects.
    Assert.That(
      Item.ContainerKindOf<FlowSource<Row>>(),
      Is.EqualTo(StepContainerKind.Source)
    );

  [Test]
  public void ContainerKindOf_IAsyncEnumerable_IsSingleton() =>
    // The bare-IAsyncEnumerable AsyncStream kind was removed (ADR-0023);
    // a raw IAsyncEnumerable is no longer a recognised container kind and
    // now resolves to Singleton. FlowSource is the sole streaming kind.
    Assert.That(
      Item.ContainerKindOf<IAsyncEnumerable<Row>>(),
      Is.EqualTo(StepContainerKind.Singleton)
    );

  // ── RowTypeOf ────────────────────────────────────────────────────────

  [Test]
  public void RowTypeOf_Singleton_ReturnsSelf() =>
    Assert.That(Item.RowTypeOf<Row>(), Is.EqualTo(typeof(Row)));

  [Test]
  public void RowTypeOf_String_ReturnsString() =>
    Assert.That(Item.RowTypeOf<string>(), Is.EqualTo(typeof(string)));

  [Test]
  public void RowTypeOf_Enumerable_UnwrapsToRow() =>
    Assert.That(Item.RowTypeOf<IEnumerable<Row>>(), Is.EqualTo(typeof(Row)));

  [Test]
  public void RowTypeOf_List_UnwrapsToRow() =>
    Assert.That(Item.RowTypeOf<List<Row>>(), Is.EqualTo(typeof(Row)));

  [Test]
  public void RowTypeOf_Queryable_UnwrapsToRow() =>
    Assert.That(Item.RowTypeOf<IQueryable<Row>>(), Is.EqualTo(typeof(Row)));

  [Test]
  public void RowTypeOf_FlowSource_UnwrapsToRow() =>
    Assert.That(Item.RowTypeOf<FlowSource<Row>>(), Is.EqualTo(typeof(Row)));

  [Test]
  public void RowTypeOf_IAsyncEnumerable_ReturnsSelf() =>
    // No longer a recognised container kind — treated as a Singleton, so
    // its row type is the IAsyncEnumerable<Row> itself.
    Assert.That(
      Item.RowTypeOf<IAsyncEnumerable<Row>>(),
      Is.EqualTo(typeof(IAsyncEnumerable<Row>))
    );

  [Test]
  public void RowTypeOf_Array_ReturnsArrayType()
  {
    // Arrays are Singletons in our scheme; row type is the array itself
    // (the framework treats byte[] as a single blob).
    Assert.That(Item.RowTypeOf<byte[]>(), Is.EqualTo(typeof(byte[])));
  }

  // ── Type-level overloads ─────────────────────────────────────────────

  [Test]
  public void ContainerKindOf_TypeOverload_MatchesGenericOverload()
  {
    Assert.That(Item.ContainerKindOf(typeof(IEnumerable<Row>)), Is.EqualTo(StepContainerKind.Enumerable));
    Assert.That(Item.ContainerKindOf(typeof(string)), Is.EqualTo(StepContainerKind.Singleton));
  }

  [Test]
  public void RowTypeOf_TypeOverload_MatchesGenericOverload() =>
    Assert.That(Item.RowTypeOf(typeof(IEnumerable<Row>)), Is.EqualTo(typeof(Row)));

  [Test]
  public void ContainerKindOf_NullType_Throws() =>
    Assert.Throws<ArgumentNullException>(() => Item.ContainerKindOf(null!));

  [Test]
  public void RowTypeOf_NullType_Throws() =>
    Assert.Throws<ArgumentNullException>(() => Item.RowTypeOf(null!));
}
