using System.Linq.Expressions;
using Flowthru.DataFrames;

namespace Flowthru.Extensions.Spark.Tests;

/// <summary>
/// A minimal <see cref="IFrameQueryProvider"/> that builds expression trees without
/// requiring any native frame backend. Used for testing expression tree construction
/// in isolation from the Spark JVM runtime.
/// </summary>
internal sealed class TestFrameProvider : IFrameQueryProvider
{
  public IQueryable<TElement> CreateQuery<TElement>(Expression expression) =>
    new TypedFrame<TElement>(this, expression);

  public IQueryable CreateQuery(Expression expression) =>
    throw new NotSupportedException("Non-generic CreateQuery is not used in tests.");

  public object Compile(Expression expression) =>
    throw new NotSupportedException(
      "TestFrameProvider does not support compilation. Expression tree tests should "
        + "inspect the Expression property directly."
    );

  public IEnumerable<T> Materialize<T>(Expression expression) =>
    throw new NotSupportedException(
      "TestFrameProvider does not support materialization. Expression tree tests should "
        + "inspect the Expression property directly."
    );

  public TResult Execute<TResult>(Expression expression) => throw new NotSupportedException();

  public object? Execute(Expression expression) => throw new NotSupportedException();
}
