using System.Collections.Generic;
using System.Linq;
using Flowthru.Prelude;
using Flowthru.Step;

namespace Flowthru.Data.Catalog;

/// <summary>
/// Container/row introspection for catalog items. Views that decompose
/// <c>IItem&lt;TContainer&lt;TRow&gt;&gt;</c> into its
/// <see cref="StepContainerKind"/> tag and underlying row type.
/// </summary>
/// <remarks>
/// <para>
/// These helpers exist primarily for the framework (analyzers, source
/// generators, pre-flight diagnostics) to reason about a catalog
/// item's shape without naming its element type. End users typically
/// don't need them — type inference handles their day-to-day wiring.
/// They're <c>internal</c> for now; if extension authors outside the
/// repo ask for the surface we can promote without API churn.
/// </para>
/// <para>
/// Resolution order: <see cref="StepContainerKind.Source"/> →
/// <see cref="StepContainerKind.Queryable"/> →
/// <see cref="StepContainerKind.Enumerable"/> →
/// <see cref="StepContainerKind.Singleton"/>, with two special cases
/// for primitive-shaped types that incidentally implement
/// <c>IEnumerable&lt;T&gt;</c>: <see cref="string"/> resolves to
/// Singleton (a string is a value, not a sequence of characters in
/// catalog terms), and arrays resolve to Singleton (binary blobs and
/// fixed-shape payloads use array <c>T</c>; explicit collections use
/// <c>IEnumerable&lt;T&gt;</c> or <c>List&lt;T&gt;</c>).
/// </para>
/// </remarks>
public static partial class Item
{
  /// <summary>
  /// View function: the <see cref="StepContainerKind"/> implied by
  /// <paramref name="type"/>. Strictly type-level — no runtime data
  /// is consulted.
  /// </summary>
  internal static StepContainerKind ContainerKindOf(Type type)
  {
    if (type is null) throw new ArgumentNullException(nameof(type));

    // Singletons-by-special-case: types that implement
    // IEnumerable<T> by accident of being a primitive shape rather
    // than by catalog intent.
    if (type == typeof(string)) return StepContainerKind.Singleton;
    if (type.IsArray) return StepContainerKind.Singleton;

    // FlowSource<T> is the streaming catalog payload (the .AsStream()
    // view). It is a sealed class implementing none of the sequence
    // interfaces, so without this structural case it falls through to
    // Singleton with row type FlowSource<T> — the exact
    // misclassification ADR-0023 corrects.
    if (ImplementsOpenGeneric(type, typeof(FlowSource<>)))
      return StepContainerKind.Source;

    // Walk in specificity order — IQueryable<T> implements
    // IEnumerable<T>, so we test it first to pick the more-specific
    // tag.
    if (ImplementsOpenGeneric(type, typeof(System.Linq.IQueryable<>)))
      return StepContainerKind.Queryable;

    if (ImplementsOpenGeneric(type, typeof(IEnumerable<>)))
      return StepContainerKind.Enumerable;

    return StepContainerKind.Singleton;
  }

  /// <summary>
  /// View function: the <see cref="StepContainerKind"/> implied by
  /// <typeparamref name="T"/>. Generic-T form of
  /// <see cref="ContainerKindOf(Type)"/>.
  /// </summary>
  internal static StepContainerKind ContainerKindOf<T>() =>
    ContainerKindOf(typeof(T));

  /// <summary>
  /// View function: the <see cref="StepContainerKind"/> for an
  /// <see cref="IItem{T}"/>. The argument is only a hint for type
  /// inference — the result is determined purely by
  /// <typeparamref name="T"/>.
  /// </summary>
  internal static StepContainerKind ContainerKindOf<T>(IItem<T> item) =>
    ContainerKindOf(typeof(T));

  /// <summary>
  /// View function: the underlying row type of <paramref name="type"/>.
  /// For containers (<c>Flowthru.Prelude.FlowSource&lt;TRow&gt;</c>,
  /// <c>IEnumerable&lt;TRow&gt;</c>, <c>IQueryable&lt;TRow&gt;</c>)
  /// returns the element type; for singletons returns
  /// <paramref name="type"/> itself.
  /// </summary>
  internal static Type RowTypeOf(Type type)
  {
    if (type is null) throw new ArgumentNullException(nameof(type));

    // Strings and arrays are Singletons; their row type is themselves.
    if (type == typeof(string)) return type;
    if (type.IsArray) return type;

    var source = FindClosedGeneric(type, typeof(FlowSource<>));
    if (source is not null) return source.GetGenericArguments()[0];

    var queryable = FindClosedGeneric(type, typeof(System.Linq.IQueryable<>));
    if (queryable is not null) return queryable.GetGenericArguments()[0];

    var enumerable = FindClosedGeneric(type, typeof(IEnumerable<>));
    if (enumerable is not null) return enumerable.GetGenericArguments()[0];

    return type;
  }

  /// <summary>
  /// View function: the underlying row type of
  /// <typeparamref name="T"/>.
  /// </summary>
  internal static Type RowTypeOf<T>() => RowTypeOf(typeof(T));

  /// <summary>
  /// View function: the underlying row type of an
  /// <see cref="IItem{T}"/>.
  /// </summary>
  internal static Type RowTypeOf<T>(IItem<T> item) => RowTypeOf(typeof(T));

  /// <summary>
  /// Derived predicate: does the item's container resolve to
  /// <paramref name="kind"/>? Sugar for
  /// <c>ContainerKindOf(item) == kind</c>.
  /// </summary>
  internal static bool IsKind<T>(IItem<T> item, StepContainerKind kind) =>
    ContainerKindOf(typeof(T)) == kind;

  // ── Helpers ────────────────────────────────────────────────────────

  private static bool ImplementsOpenGeneric(Type type, Type openGeneric) =>
    FindClosedGeneric(type, openGeneric) is not null;

  private static Type? FindClosedGeneric(Type type, Type openGeneric)
  {
    if (type.IsGenericType && type.GetGenericTypeDefinition() == openGeneric)
    {
      return type;
    }

    return type
      .GetInterfaces()
      .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == openGeneric);
  }
}
