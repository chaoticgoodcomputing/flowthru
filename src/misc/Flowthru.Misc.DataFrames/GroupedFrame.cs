using System.Linq.Expressions;
using System.Reflection;

namespace Flowthru.Misc.DataFrames;

/// <summary>
/// An intermediate representation of a grouped <see cref="TypedFrame{T}"/>, produced by
/// <see cref="TypedFrameExtensions.GroupBy{TSource,TKey}"/>.
/// </summary>
/// <remarks>
/// This type exists solely as a typed anchor for the subsequent <see cref="GroupedFrameExtensions.Aggregate{TKey,TSource,TResult}"/>
/// call. It carries the accumulated group expression and prevents accidental misuse
/// of a grouped frame as a regular frame.
/// </remarks>
/// <typeparam name="TKey">The type of the grouping key.</typeparam>
/// <typeparam name="TSource">The row schema type before grouping.</typeparam>
public sealed class GroupedFrame<TKey, TSource>
{
    internal IFrameQueryProvider Provider { get; }
    public Expression Expression { get; }

    internal GroupedFrame(IFrameQueryProvider provider, Expression expression)
    {
        Provider = provider;
        Expression = expression;
    }
}

/// <summary>
/// Extension methods for <see cref="GroupedFrame{TKey,TSource}"/>.
/// </summary>
public static class GroupedFrameExtensions
{
    /// <summary>
    /// Aggregates a grouped frame, producing a new <see cref="TypedFrame{TResult}"/>.
    /// </summary>
    /// <typeparam name="TKey">The grouping key type.</typeparam>
    /// <typeparam name="TSource">The source row schema type.</typeparam>
    /// <typeparam name="TResult">The result schema type after aggregation.</typeparam>
    /// <param name="source">The grouped frame.</param>
    /// <param name="resultSelector">
    /// A projection from a <see cref="AggregationContext{TKey,TSource}"/> to the result schema.
    /// The context exposes typed aggregate functions (Avg, Sum, Count, Min, Max) and the key.
    /// </param>
    public static TypedFrame<TResult> Aggregate<TKey, TSource, TResult>(
      this GroupedFrame<TKey, TSource> source,
      Expression<Func<AggregationContext<TKey, TSource>, TResult>> resultSelector
    )
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(resultSelector);

        var method = (
          (Func<
            GroupedFrame<TKey, TSource>,
            Expression<Func<AggregationContext<TKey, TSource>, TResult>>,
            TypedFrame<TResult>
          >)
            Aggregate<TKey, TSource, TResult>
        ).Method;

        return (TypedFrame<TResult>)
          source.Provider.CreateQuery<TResult>(
            Expression.Call(null, method, source.Expression, Expression.Quote(resultSelector))
          );
    }
}

/// <summary>
/// Provides typed aggregate function placeholders within an
/// <see cref="GroupedFrameExtensions.Aggregate{TKey,TSource,TResult}"/> expression.
/// </summary>
/// <remarks>
/// Instances of this type are never constructed at runtime. The expression visitor
/// intercepts calls to its methods during expression tree translation and maps them
/// to the corresponding native aggregate functions (e.g., Spark's <c>avg()</c>).
/// </remarks>
/// <typeparam name="TKey">The grouping key type.</typeparam>
/// <typeparam name="TSource">The source row schema type.</typeparam>
public sealed class AggregationContext<TKey, TSource>
{
    private AggregationContext() { }

    /// <summary>The grouping key value for this group.</summary>
    public TKey Key => throw new InvalidOperationException(AggregationContextError);

    /// <summary>Computes the average of a numeric column.</summary>
    public double Avg(Expression<Func<TSource, double>> column) =>
      throw new InvalidOperationException(AggregationContextError);

    /// <summary>Computes the average of a numeric column.</summary>
    public decimal Avg(Expression<Func<TSource, decimal>> column) =>
      throw new InvalidOperationException(AggregationContextError);

    /// <summary>Computes the average of a numeric column.</summary>
    public double Avg(Expression<Func<TSource, int>> column) =>
      throw new InvalidOperationException(AggregationContextError);

    /// <summary>Computes the sum of a numeric column.</summary>
    public double Sum(Expression<Func<TSource, double>> column) =>
      throw new InvalidOperationException(AggregationContextError);

    /// <summary>Computes the sum of a numeric column.</summary>
    public decimal Sum(Expression<Func<TSource, decimal>> column) =>
      throw new InvalidOperationException(AggregationContextError);

    /// <summary>Computes the sum of a numeric column.</summary>
    public long Sum(Expression<Func<TSource, int>> column) =>
      throw new InvalidOperationException(AggregationContextError);

    /// <summary>Computes the maximum value of a column.</summary>
    public TValue Max<TValue>(Expression<Func<TSource, TValue>> column) =>
      throw new InvalidOperationException(AggregationContextError);

    /// <summary>Computes the minimum value of a column.</summary>
    public TValue Min<TValue>(Expression<Func<TSource, TValue>> column) =>
      throw new InvalidOperationException(AggregationContextError);

    /// <summary>Counts the number of rows in the group.</summary>
    public long Count() => throw new InvalidOperationException(AggregationContextError);

    private const string AggregationContextError =
      "AggregationContext methods are expression tree placeholders and must not be invoked directly. "
      + "They are translated to native aggregate functions by the provider's expression visitor.";
}
