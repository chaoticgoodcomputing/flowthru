using System.Linq.Expressions;

namespace Flowthru.DataFrames;

/// <summary>
/// Non-generic contract for a window specification, used by visitors to translate
/// window definitions without requiring the generic source type parameter.
/// </summary>
public interface IFrameWindowSpec
{
  /// <summary>Partition-by expressions, in the order they were added.</summary>
  IReadOnlyList<LambdaExpression> PartitionByExpressions { get; }

  /// <summary>Order-by expressions, each paired with a descending flag.</summary>
  IReadOnlyList<(LambdaExpression KeySelector, bool Descending)> OrderByExpressions { get; }
}

/// <summary>
/// An immutable, framework-agnostic window specification that describes how rows are
/// partitioned and ordered for windowed computations.
/// </summary>
/// <remarks>
/// <para>
/// <c>FrameWindowSpec&lt;TSource&gt;</c> is a pure data carrier — it holds
/// <see cref="LambdaExpression"/> trees for partition and order keys. No native
/// (Spark, SQL, etc.) objects are created until the provider's expression visitor
/// translates the spec at query compilation time.
/// </para>
/// <para>
/// Build specs with the static <see cref="PartitionBy{TKey}"/> or
/// <see cref="Global"/> entry points and the fluent instance methods.
/// Pass the finished spec as the last argument to each
/// <see cref="WindowContext{TSource}"/> function call inside a
/// <see cref="TypedFrameExtensions.SelectOver{TSource,TResult}"/> projection.
/// </para>
/// </remarks>
/// <typeparam name="TSource">The row schema type the spec applies to.</typeparam>
public sealed class FrameWindowSpec<TSource> : IFrameWindowSpec
{
  private FrameWindowSpec(
    IReadOnlyList<LambdaExpression> partitionBy,
    IReadOnlyList<(LambdaExpression KeySelector, bool Descending)> orderBy
  )
  {
    PartitionByExpressions = partitionBy;
    OrderByExpressions = orderBy;
  }

  /// <summary>
  /// An empty window spanning all rows with no partition or ordering.
  /// Use as the starting point when only ordering is needed:
  /// <c>FrameWindowSpec&lt;T&gt;.Global.OrderBy(x =&gt; x.HireDate)</c>.
  /// </summary>
  public static readonly FrameWindowSpec<TSource> Global = new([], []);

  /// <inheritdoc/>
  public IReadOnlyList<LambdaExpression> PartitionByExpressions { get; }

  /// <inheritdoc/>
  public IReadOnlyList<(LambdaExpression KeySelector, bool Descending)> OrderByExpressions { get; }

  // ──────────────────────────────────────────────
  //  Static entry points
  // ──────────────────────────────────────────────

  /// <summary>
  /// Creates a new spec with a single partition key.
  /// </summary>
  public static FrameWindowSpec<TSource> PartitionBy<TKey>(
    Expression<Func<TSource, TKey>> keySelector
  )
  {
    ArgumentNullException.ThrowIfNull(keySelector);
    return new FrameWindowSpec<TSource>([keySelector], []);
  }

  // ──────────────────────────────────────────────
  //  Fluent instance methods
  // ──────────────────────────────────────────────

  /// <summary>Adds an additional partition key to this spec.</summary>
  public FrameWindowSpec<TSource> ThenPartitionBy<TKey>(
    Expression<Func<TSource, TKey>> keySelector
  )
  {
    ArgumentNullException.ThrowIfNull(keySelector);
    return new FrameWindowSpec<TSource>([..PartitionByExpressions, keySelector], OrderByExpressions);
  }

  /// <summary>Adds an ascending sort key to this spec.</summary>
  public FrameWindowSpec<TSource> OrderBy<TKey>(Expression<Func<TSource, TKey>> keySelector)
  {
    ArgumentNullException.ThrowIfNull(keySelector);
    return new FrameWindowSpec<TSource>(
      PartitionByExpressions,
      [..OrderByExpressions, (keySelector, false)]
    );
  }

  /// <summary>Adds a descending sort key to this spec.</summary>
  public FrameWindowSpec<TSource> OrderByDescending<TKey>(
    Expression<Func<TSource, TKey>> keySelector
  )
  {
    ArgumentNullException.ThrowIfNull(keySelector);
    return new FrameWindowSpec<TSource>(
      PartitionByExpressions,
      [..OrderByExpressions, (keySelector, true)]
    );
  }
}
