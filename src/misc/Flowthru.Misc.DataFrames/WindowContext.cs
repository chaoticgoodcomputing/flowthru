using System.Linq.Expressions;

namespace Flowthru.DataFrames;

/// <summary>
/// A throw-only marker type whose methods are intercepted as expression tree nodes
/// inside a <see cref="TypedFrameExtensions.SelectOver{TSource,TResult}"/> projection.
/// </summary>
/// <remarks>
/// <para>
/// Instances of this type are never constructed at runtime. The provider's expression
/// visitor recognises method calls on the <c>win</c> parameter and translates them to
/// the corresponding native window functions (e.g., Spark's
/// <c>Functions.Rank().Over(windowSpec)</c>).
/// </para>
/// <para>
/// Each method accepts a <see cref="FrameWindowSpec{TSource}"/> as its last argument.
/// This makes multi-window projections natural — different columns can reference different
/// specs in the same <c>SelectOver</c> call.
/// </para>
/// </remarks>
/// <typeparam name="TSource">The row schema type of the source frame.</typeparam>
public sealed class WindowContext<TSource>
{
  private WindowContext() { }

  private const string Error =
    "WindowContext methods are expression tree placeholders and must not be invoked directly. "
      + "They are translated to native window functions by the provider's expression visitor.";

  // ──────────────────────────────────────────────
  //  Ranking functions (no column selector)
  // ──────────────────────────────────────────────

  /// <summary>Sequential row number within the partition, starting at 1.</summary>
  public long RowNumber(FrameWindowSpec<TSource> spec) => throw new InvalidOperationException(Error);

  /// <summary>Rank with gaps (ties share a rank; the next rank reflects the gap).</summary>
  public long Rank(FrameWindowSpec<TSource> spec) => throw new InvalidOperationException(Error);

  /// <summary>Rank without gaps (ties share a rank; next rank is always rank + 1).</summary>
  public long DenseRank(FrameWindowSpec<TSource> spec) => throw new InvalidOperationException(Error);

  /// <summary>Fraction of rows within the partition that are less than or equal to the current row.</summary>
  public double CumeDist(FrameWindowSpec<TSource> spec) => throw new InvalidOperationException(Error);

  /// <summary>Relative rank of the current row: (rank - 1) / (partition size - 1).</summary>
  public double PercentRank(FrameWindowSpec<TSource> spec) => throw new InvalidOperationException(Error);

  /// <summary>Count of rows seen so far within the window frame.</summary>
  public long Count(FrameWindowSpec<TSource> spec) => throw new InvalidOperationException(Error);

  // ──────────────────────────────────────────────
  //  Offset functions (column selector + offset)
  // ──────────────────────────────────────────────

  /// <summary>
  /// Value of <paramref name="selector"/> from the row <paramref name="offset"/> rows before
  /// the current row, or <c>null</c> if no such row exists.
  /// </summary>
  public TValue? Lag<TValue>(
    Expression<Func<TSource, TValue>> selector,
    int offset,
    FrameWindowSpec<TSource> spec
  ) => throw new InvalidOperationException(Error);

  /// <summary>
  /// Value of <paramref name="selector"/> from the row <paramref name="offset"/> rows after
  /// the current row, or <c>null</c> if no such row exists.
  /// </summary>
  public TValue? Lead<TValue>(
    Expression<Func<TSource, TValue>> selector,
    int offset,
    FrameWindowSpec<TSource> spec
  ) => throw new InvalidOperationException(Error);

  // ──────────────────────────────────────────────
  //  Aggregate window functions (column selector)
  // ──────────────────────────────────────────────

  /// <summary>Running sum of <paramref name="selector"/> over the window frame.</summary>
  public double Sum(
    Expression<Func<TSource, double>> selector,
    FrameWindowSpec<TSource> spec
  ) => throw new InvalidOperationException(Error);

  /// <inheritdoc cref="Sum(Expression{Func{TSource, double}}, FrameWindowSpec{TSource})"/>
  public decimal Sum(
    Expression<Func<TSource, decimal>> selector,
    FrameWindowSpec<TSource> spec
  ) => throw new InvalidOperationException(Error);

  /// <inheritdoc cref="Sum(Expression{Func{TSource, double}}, FrameWindowSpec{TSource})"/>
  public long Sum(
    Expression<Func<TSource, int>> selector,
    FrameWindowSpec<TSource> spec
  ) => throw new InvalidOperationException(Error);

  /// <summary>Running average of <paramref name="selector"/> over the window frame.</summary>
  public double Avg(
    Expression<Func<TSource, double>> selector,
    FrameWindowSpec<TSource> spec
  ) => throw new InvalidOperationException(Error);

  /// <inheritdoc cref="Avg(Expression{Func{TSource, double}}, FrameWindowSpec{TSource})"/>
  public double Avg(
    Expression<Func<TSource, int>> selector,
    FrameWindowSpec<TSource> spec
  ) => throw new InvalidOperationException(Error);

  /// <summary>Running maximum of <paramref name="selector"/> over the window frame.</summary>
  public TValue Max<TValue>(
    Expression<Func<TSource, TValue>> selector,
    FrameWindowSpec<TSource> spec
  ) => throw new InvalidOperationException(Error);

  /// <summary>Running minimum of <paramref name="selector"/> over the window frame.</summary>
  public TValue Min<TValue>(
    Expression<Func<TSource, TValue>> selector,
    FrameWindowSpec<TSource> spec
  ) => throw new InvalidOperationException(Error);
}
