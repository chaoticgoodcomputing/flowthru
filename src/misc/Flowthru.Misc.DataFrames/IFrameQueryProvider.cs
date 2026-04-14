using System.Linq.Expressions;

namespace Flowthru.DataFrames;

/// <summary>
/// A query provider that creates <see cref="TypedFrame{T}"/> instances and compiles
/// accumulated expression trees into native frame operations.
/// </summary>
/// <remarks>
/// This interface extends <see cref="IQueryProvider"/> with a
/// <see cref="Compile"/> method for producing native frame objects (e.g., a Spark
/// <c>DataFrame</c>) from the expression tree accumulated by chained operations.
/// Each provider implementation handles a specific DataFrame backend.
/// </remarks>
public interface IFrameQueryProvider : IQueryProvider
{
  /// <summary>
  /// Compiles the accumulated expression tree into a native frame object.
  /// </summary>
  /// <param name="expression">
  /// The expression tree rooted at a <see cref="TypedFrame{T}"/> constant,
  /// with chained method calls representing operations.
  /// </param>
  /// <returns>The native frame object (e.g., Spark <c>DataFrame</c>).</returns>
  object Compile(Expression expression);

  /// <summary>
  /// Materializes the accumulated expression tree into an enumerable sequence of rows.
  /// Called by <see cref="TypedFrame{T}.GetEnumerator"/> to enable transparent
  /// TypedFrame → IEnumerable conversion at catalog item boundaries.
  /// </summary>
  /// <typeparam name="T">The row schema type.</typeparam>
  /// <param name="expression">The accumulated expression tree.</param>
  /// <returns>The materialized rows.</returns>
  IEnumerable<T> Materialize<T>(Expression expression);
}
