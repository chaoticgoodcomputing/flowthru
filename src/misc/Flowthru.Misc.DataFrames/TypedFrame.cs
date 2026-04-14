using System.Collections;
using System.Linq.Expressions;

namespace Flowthru.DataFrames;

/// <summary>
/// A phantom-typed wrapper around an untyped DataFrame-like object.
/// </summary>
/// <remarks>
/// <para>
/// <c>TypedFrame&lt;T&gt;</c> implements <see cref="IQueryable{T}"/> to leverage the standard
/// .NET expression tree infrastructure. The type parameter <typeparamref name="T"/> is a
/// phantom type — it carries schema information through the type system without being
/// instantiated at runtime.
/// </para>
/// <para>
/// Extension methods build expression trees via <see cref="IQueryProvider.CreateQuery{TElement}"/>,
/// threading type parameters through each operation (just as LINQ's <c>Queryable</c> methods do).
/// When the accumulated expression tree is compiled by the provider, it produces native
/// DataFrame operations (e.g., Spark Column expressions, ML.NET transforms) without
/// materializing data into .NET objects.
/// </para>
/// </remarks>
/// <typeparam name="T">
/// The schema type representing the row structure. Must be annotated with
/// <c>[FlowthruSchema]</c> to participate in compile-time and pre-flight validation.
/// </typeparam>
public class TypedFrame<T> : IQueryable<T>, IOrderedQueryable<T>
{
  private readonly IFrameQueryProvider _provider;
  private readonly Expression _expression;

  /// <summary>
  /// Creates a root frame node backed by a native DataFrame.
  /// The provider associates the native frame externally.
  /// </summary>
  public TypedFrame(IFrameQueryProvider provider)
  {
    _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    _expression = Expression.Constant(this);
  }

  /// <summary>
  /// Creates an intermediate frame node representing an accumulated operation.
  /// Used by the provider's <see cref="IQueryProvider.CreateQuery{TElement}"/>.
  /// </summary>
  public TypedFrame(IFrameQueryProvider provider, Expression expression)
  {
    _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    _expression = expression ?? throw new ArgumentNullException(nameof(expression));
  }

  /// <inheritdoc />
  public Expression Expression => _expression;

  /// <inheritdoc />
  public Type ElementType => typeof(T);

  /// <inheritdoc />
  public IQueryProvider Provider => _provider;

  /// <summary>
  /// Not supported. TypedFrame represents a distributed dataset that cannot be enumerated locally.
  /// </summary>
  /// <exception cref="NotSupportedException">Always thrown.</exception>
  public IEnumerator<T> GetEnumerator() =>
    throw new NotSupportedException(
      "TypedFrame<T> represents a distributed dataset and cannot be enumerated directly. "
        + "Use the provider's Compile() method to produce a native frame."
    );

  /// <inheritdoc cref="GetEnumerator"/>
  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
