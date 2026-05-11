using System.Collections;
using System.Linq.Expressions;
using Flowthru.Misc.DataFrames;

namespace Flowthru.Misc.DataFrames.Tests.Fixtures;

/// <summary>
/// A test-only <see cref="IFrameQueryProvider"/> that records every call it
/// receives and returns deterministic synthetic results. Used to verify that
/// the LINQ-style extension methods build the expression trees they advertise
/// and dispatch them through the provider correctly.
/// </summary>
internal sealed class RecordingFrameQueryProvider : IFrameQueryProvider
{
  public List<Expression> CreateQueryCalls { get; } = new();
  public List<Expression> ExecuteCalls { get; } = new();
  public List<Expression> CompileCalls { get; } = new();
  public List<Expression> MaterializeCalls { get; } = new();

  /// <summary>
  /// Pre-seeded materialized rows the provider hands back from
  /// <see cref="Materialize{T}"/>. Each call returns the next list in line;
  /// out-of-band calls return an empty sequence.
  /// </summary>
  public Queue<IEnumerable<object>> MaterializeResults { get; } = new();

  /// <summary>
  /// Pre-seeded scalar Execute result. <c>Count</c> reads it back as a <c>long</c>.
  /// </summary>
  public long ExecuteScalarResult { get; set; } = 0L;

  public IQueryable CreateQuery(Expression expression) =>
    throw new NotSupportedException("Non-generic CreateQuery is not used by TypedFrameExtensions.");

  public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
  {
    CreateQueryCalls.Add(expression);
    return new TypedFrame<TElement>(this, expression);
  }

  public object? Execute(Expression expression) =>
    throw new NotSupportedException("Non-generic Execute is not used by TypedFrameExtensions.");

  public TResult Execute<TResult>(Expression expression)
  {
    ExecuteCalls.Add(expression);
    // The only typed Execute call from the library is Count → long.
    if (typeof(TResult) == typeof(long))
    {
      return (TResult)(object)ExecuteScalarResult;
    }
    return default!;
  }

  public object Compile(Expression expression)
  {
    CompileCalls.Add(expression);
    return expression;
  }

  public IEnumerable<T> Materialize<T>(Expression expression)
  {
    MaterializeCalls.Add(expression);
    if (MaterializeResults.Count == 0)
    {
      return Enumerable.Empty<T>();
    }
    return MaterializeResults.Dequeue().Cast<T>();
  }
}

/// <summary>
/// Trivial row schemas used across the test suite. They have no Flowthru
/// schema attributes — the DataFrame library is framework-agnostic and
/// the only attribute it duck-types on is <c>SerializedLabelAttribute</c>,
/// which a dedicated FrameExpressionVisitor test exercises directly.
/// </summary>
internal sealed class Person
{
  public string Name { get; set; } = "";
  public int Age { get; set; }
  public string Department { get; set; } = "";
  public decimal Salary { get; set; }
}

internal sealed class Department
{
  public string Code { get; set; } = "";
  public string Title { get; set; } = "";
}

internal sealed class PersonSummary
{
  public string Department { get; set; } = "";
  public long Headcount { get; set; }
  public decimal AvgSalary { get; set; }
}
