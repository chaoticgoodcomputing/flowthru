using System.Linq.Expressions;
using System.Reflection;

namespace Flowthru.DataFrames;

/// <summary>
/// LINQ-style extension methods for <see cref="TypedFrame{T}"/> that build expression trees.
/// </summary>
/// <remarks>
/// These methods follow the same pattern as <see cref="System.Linq.Queryable"/>: each call
/// captures the lambda as an <see cref="Expression"/> tree node and delegates to
/// <see cref="IQueryProvider.CreateQuery{TElement}"/>. No native operations execute here —
/// translation is deferred to the provider's <see cref="IFrameQueryProvider.Compile"/> method.
/// </remarks>
public static class TypedFrameExtensions
{
  // ──────────────────────────────────────────────
  //  Where — type-preserving filter
  // ──────────────────────────────────────────────

  /// <summary>
  /// Filters rows using a predicate. The schema type is preserved.
  /// </summary>
  public static TypedFrame<TSource> Where<TSource>(
    this TypedFrame<TSource> source,
    Expression<Func<TSource, bool>> predicate
  )
  {
    ArgumentNullException.ThrowIfNull(source);
    ArgumentNullException.ThrowIfNull(predicate);

    return (TypedFrame<TSource>)
      source.Provider.CreateQuery<TSource>(
        Expression.Call(
          null,
          CaptureMethod(Where, source, predicate),
          source.Expression,
          Expression.Quote(predicate)
        )
      );
  }

  // ──────────────────────────────────────────────
  //  Select — type-projecting transformation
  // ──────────────────────────────────────────────

  /// <summary>
  /// Projects each row into a new schema type via a selector expression.
  /// </summary>
  public static TypedFrame<TResult> Select<TSource, TResult>(
    this TypedFrame<TSource> source,
    Expression<Func<TSource, TResult>> selector
  )
  {
    ArgumentNullException.ThrowIfNull(source);
    ArgumentNullException.ThrowIfNull(selector);

    return (TypedFrame<TResult>)
      source.Provider.CreateQuery<TResult>(
        Expression.Call(
          null,
          CaptureMethod(Select, source, selector),
          source.Expression,
          Expression.Quote(selector)
        )
      );
  }

  // ──────────────────────────────────────────────
  //  Join — multi-frame equi-join with projection
  // ──────────────────────────────────────────────

  /// <summary>
  /// Joins two typed frames on matching keys and projects the result into a new schema.
  /// </summary>
  public static TypedFrame<TResult> Join<TOuter, TInner, TKey, TResult>(
    this TypedFrame<TOuter> outer,
    TypedFrame<TInner> inner,
    Expression<Func<TOuter, TKey>> outerKeySelector,
    Expression<Func<TInner, TKey>> innerKeySelector,
    Expression<Func<TOuter, TInner, TResult>> resultSelector
  )
  {
    ArgumentNullException.ThrowIfNull(outer);
    ArgumentNullException.ThrowIfNull(inner);
    ArgumentNullException.ThrowIfNull(outerKeySelector);
    ArgumentNullException.ThrowIfNull(innerKeySelector);
    ArgumentNullException.ThrowIfNull(resultSelector);

    return (TypedFrame<TResult>)
      outer.Provider.CreateQuery<TResult>(
        Expression.Call(
          null,
          CaptureMethod(Join, outer, inner, outerKeySelector, innerKeySelector, resultSelector),
          outer.Expression,
          inner.Expression,
          Expression.Quote(outerKeySelector),
          Expression.Quote(innerKeySelector),
          Expression.Quote(resultSelector)
        )
      );
  }

  // ──────────────────────────────────────────────
  //  OrderBy / OrderByDescending — type-preserving sort
  // ──────────────────────────────────────────────

  /// <summary>
  /// Sorts rows by a key in ascending order. The schema type is preserved.
  /// </summary>
  public static TypedFrame<TSource> OrderBy<TSource, TKey>(
    this TypedFrame<TSource> source,
    Expression<Func<TSource, TKey>> keySelector
  )
  {
    ArgumentNullException.ThrowIfNull(source);
    ArgumentNullException.ThrowIfNull(keySelector);

    return (TypedFrame<TSource>)
      source.Provider.CreateQuery<TSource>(
        Expression.Call(
          null,
          CaptureMethod(OrderBy, source, keySelector),
          source.Expression,
          Expression.Quote(keySelector)
        )
      );
  }

  /// <summary>
  /// Sorts rows by a key in descending order. The schema type is preserved.
  /// </summary>
  public static TypedFrame<TSource> OrderByDescending<TSource, TKey>(
    this TypedFrame<TSource> source,
    Expression<Func<TSource, TKey>> keySelector
  )
  {
    ArgumentNullException.ThrowIfNull(source);
    ArgumentNullException.ThrowIfNull(keySelector);

    return (TypedFrame<TSource>)
      source.Provider.CreateQuery<TSource>(
        Expression.Call(
          null,
          CaptureMethod(OrderByDescending, source, keySelector),
          source.Expression,
          Expression.Quote(keySelector)
        )
      );
  }

  // ──────────────────────────────────────────────
  //  Take — type-preserving row limit
  // ──────────────────────────────────────────────

  /// <summary>
  /// Limits the frame to the first <paramref name="count"/> rows. The schema type is preserved.
  /// </summary>
  public static TypedFrame<TSource> Take<TSource>(this TypedFrame<TSource> source, int count)
  {
    ArgumentNullException.ThrowIfNull(source);

    return (TypedFrame<TSource>)
      source.Provider.CreateQuery<TSource>(
        Expression.Call(
          null,
          CaptureMethod(Take, source, count),
          source.Expression,
          Expression.Constant(count)
        )
      );
  }

  // ──────────────────────────────────────────────
  //  Count — scalar execution
  // ──────────────────────────────────────────────

  /// <summary>
  /// Returns the number of rows in the frame.
  /// </summary>
  /// <remarks>
  /// This triggers compilation and execution via the provider. It is a terminal operation.
  /// </remarks>
  public static long Count<TSource>(this TypedFrame<TSource> source)
  {
    ArgumentNullException.ThrowIfNull(source);

    var expression = Expression.Call(null, CaptureMethod(Count, source), source.Expression);

    return source.Provider.Execute<long>(expression);
  }

  // ──────────────────────────────────────────────
  //  Distinct — deduplicate rows
  // ──────────────────────────────────────────────

  /// <summary>
  /// Returns a frame with duplicate rows removed.
  /// </summary>
  public static TypedFrame<TSource> Distinct<TSource>(this TypedFrame<TSource> source)
  {
    ArgumentNullException.ThrowIfNull(source);

    return (TypedFrame<TSource>)
      source.Provider.CreateQuery<TSource>(
        Expression.Call(null, CaptureMethod(Distinct, source), source.Expression)
      );
  }

  // ──────────────────────────────────────────────
  //  Union — row-wise concatenation
  // ──────────────────────────────────────────────

  /// <summary>
  /// Concatenates two frames of the same schema, preserving all rows (including duplicates).
  /// Equivalent to SQL <c>UNION ALL</c>; use <see cref="Distinct{TSource}"/> after to get
  /// distinct-row semantics.
  /// </summary>
  public static TypedFrame<TSource> Union<TSource>(
    this TypedFrame<TSource> source,
    TypedFrame<TSource> other
  )
  {
    ArgumentNullException.ThrowIfNull(source);
    ArgumentNullException.ThrowIfNull(other);

    return (TypedFrame<TSource>)
      source.Provider.CreateQuery<TSource>(
        Expression.Call(
          null,
          CaptureMethod(Union, source, other),
          source.Expression,
          other.Expression
        )
      );
  }

  // ──────────────────────────────────────────────
  //  GroupBy — intermediate grouped frame
  // ──────────────────────────────────────────────

  /// <summary>
  /// Groups rows by a key selector, producing a <see cref="GroupedFrame{TKey,TSource}"/>
  /// that can be aggregated.
  /// </summary>
  public static GroupedFrame<TKey, TSource> GroupBy<TSource, TKey>(
    this TypedFrame<TSource> source,
    Expression<Func<TSource, TKey>> keySelector
  )
  {
    ArgumentNullException.ThrowIfNull(source);
    ArgumentNullException.ThrowIfNull(keySelector);

    var expression = Expression.Call(
      null,
      CaptureMethod(GroupBy, source, keySelector),
      source.Expression,
      Expression.Quote(keySelector)
    );

    return new GroupedFrame<TKey, TSource>((IFrameQueryProvider)source.Provider, expression);
  }

  // ──────────────────────────────────────────────
  //  SelectOver — windowed projection
  // ──────────────────────────────────────────────

  /// <summary>
  /// Projects each row into a new schema type, with access to windowed aggregate and
  /// ranking functions via the <see cref="WindowContext{TSource}"/> parameter.
  /// </summary>
  /// <remarks>
  /// Each window function call in the selector must pass a
  /// <see cref="FrameWindowSpec{TSource}"/> as its last argument, which defines the
  /// partition and ordering for that specific function. Multiple specs may appear in
  /// the same projection, enabling multi-window queries in a single call.
  /// </remarks>
  public static TypedFrame<TResult> SelectOver<TSource, TResult>(
    this TypedFrame<TSource> source,
    Expression<Func<TSource, WindowContext<TSource>, TResult>> selector
  )
  {
    ArgumentNullException.ThrowIfNull(source);
    ArgumentNullException.ThrowIfNull(selector);

    return (TypedFrame<TResult>)
      source.Provider.CreateQuery<TResult>(
        Expression.Call(
          null,
          CaptureMethod(SelectOver, source, selector),
          source.Expression,
          Expression.Quote(selector)
        )
      );
  }

  // ──────────────────────────────────────────────
  //  MethodInfo capture helpers
  // ──────────────────────────────────────────────
  //  These follow the same pattern as System.Linq.Queryable's GetMethodInfo:
  //  the dummy parameters exist solely for generic type inference so the
  //  compiler resolves the closed generic MethodInfo at the call site.

  private static MethodInfo CaptureMethod<T1, TR>(Func<T1, TR> method, T1 _1) => method.Method;

  private static MethodInfo CaptureMethod<T1, T2, TR>(Func<T1, T2, TR> method, T1 _1, T2 _2) =>
    method.Method;

  private static MethodInfo CaptureMethod<T1, T2, T3, T4, T5, TR>(
    Func<T1, T2, T3, T4, T5, TR> method,
    T1 _1,
    T2 _2,
    T3 _3,
    T4 _4,
    T5 _5
  ) => method.Method;
}
