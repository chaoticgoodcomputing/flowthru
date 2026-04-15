using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Flowthru.Core.Abstractions;
using Flowthru.DataFrames;
using Flowthru.Extensions.Spark.Runtime;
using Flowthru.Spark.Sql;

namespace Flowthru.Extensions.Spark;

/// <summary>
/// An <see cref="IFrameQueryProvider"/> that backs <see cref="TypedFrame{T}"/> instances
/// with Spark.NET <see cref="DataFrame"/> objects.
/// </summary>
/// <remarks>
/// <para>
/// This provider manages the association between <see cref="TypedFrame{T}"/> phantom wrappers
/// and the native <see cref="DataFrame"/> objects they represent. It follows the standard
/// <see cref="IQueryProvider"/> contract:
/// </para>
/// <list type="bullet">
/// <item><see cref="CreateQuery{TElement}"/> wraps an expression in a new <see cref="TypedFrame{T}"/>.</item>
/// <item><see cref="Compile"/> walks the accumulated expression tree via <see cref="SparkExpressionVisitor"/>
/// and produces a native <see cref="DataFrame"/>.</item>
/// </list>
/// </remarks>
public sealed class SparkFrameProvider : IFrameQueryProvider
{
  private readonly ConditionalWeakTable<object, DataFrame> _nativeFrames = new();
  private readonly SparkExpressionVisitor _visitor;
  private readonly SparkSession _session;

  /// <summary>
  /// Initializes a new <see cref="SparkFrameProvider"/>.
  /// </summary>
  /// <remarks>
  /// Calls <see cref="SparkRuntime.Initialize"/> to ensure the JVM backend is running before
  /// creating the Spark session. Consuming code (catalogs, flows, steps) does not need to
  /// interact with <see cref="SparkRuntime"/> or <see cref="SparkSession"/> directly.
  /// </remarks>
  public SparkFrameProvider(SparkRuntime runtime)
  {
    runtime.Initialize();
    _session = SparkSession.Builder().GetOrCreate();
    _visitor = new SparkExpressionVisitor(this);
  }

  /// <summary>
  /// Initializes a <see cref="SparkFrameProvider"/> without starting the JVM backend.
  /// For use in unit tests that validate schema logic without a live Spark session.
  /// </summary>
  internal SparkFrameProvider()
  {
    _session = null!;
    _visitor = new SparkExpressionVisitor(this);
  }

  /// <summary>
  /// Creates a root <see cref="TypedFrame{T}"/> backed by a native Spark <see cref="DataFrame"/>.
  /// </summary>
  /// <typeparam name="T">The schema type representing the DataFrame's row structure.</typeparam>
  /// <param name="dataFrame">The native Spark DataFrame.</param>
  /// <returns>A typed frame that can be transformed via LINQ-style extension methods.</returns>
  public TypedFrame<T> CreateFromNative<T>(DataFrame dataFrame)
  {
    var frame = new TypedFrame<T>(this);
    _nativeFrames.AddOrUpdate(frame, dataFrame);
    return frame;
  }

  /// <summary>
  /// Creates a root <see cref="TypedFrame{T}"/> by ingesting an <see cref="IEnumerable{T}"/>
  /// into Spark via the managed session.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The <see cref="StructType"/> is derived automatically from <typeparamref name="T"/>'s
  /// property metadata using <see cref="SparkSchemaInference" />, so no manual schema
  /// declaration is required in the calling code.
  /// </para>
  /// <para>
  /// This is the standard entry point for preprocessing steps that produce typed data from
  /// raw <see cref="IEnumerable{T}"/> sources. Steps calling this method do not need to
  /// reference <see cref="SparkSession"/>, <see cref="Flowthru.Spark.Sql.Types.StructType"/>,
  /// or <see cref="Flowthru.Spark.Sql.GenericRow"/> directly.
  /// </para>
  /// </remarks>
  /// <typeparam name="T">A flat schema type whose properties define the Spark column layout.</typeparam>
  /// <param name="source">The rows to ingest. Enumerated once.</param>
  /// <returns>A typed frame backed by the ingested DataFrame.</returns>
  public TypedFrame<T> CreateFromEnumerable<T>(IEnumerable<T> source)
    where T : notnull, IFlatSchema
  {
    var schema = SparkSchemaInference.InferStructType<T>();
    var rows = SparkSchemaInference.ToGenericRows(source);
    var df = _session.CreateDataFrame(rows, schema);
    return CreateFromNative<T>(df);
  }

  /// <summary>
  /// Retrieves the native <see cref="DataFrame"/> associated with a root
  /// <see cref="TypedFrame{T}"/>.
  /// </summary>
  internal DataFrame GetNativeFrame(object frame)
  {
    if (_nativeFrames.TryGetValue(frame, out var df))
      return df;

    throw new InvalidOperationException(
      "TypedFrame is not associated with a native Spark DataFrame. "
        + "Root frames must be created via SparkFrameProvider.CreateFromNative<T>()."
    );
  }

  /// <summary>
  /// Compiles the expression tree into a native Spark <see cref="DataFrame"/>.
  /// </summary>
  /// <param name="expression">
  /// The accumulated expression tree from chained <see cref="TypedFrame{T}"/> operations.
  /// </param>
  /// <returns>A Spark <see cref="DataFrame"/>.</returns>
  public DataFrame CompileToNative(Expression expression)
  {
    return (DataFrame)Compile(expression);
  }

  /// <summary>
  /// Compiles the expression tree of a <see cref="TypedFrame{T}"/> into a native DataFrame.
  /// </summary>
  public DataFrame CompileToNative<T>(TypedFrame<T> frame)
  {
    return CompileToNative(frame.Expression);
  }

  // ──────────────────────────────────────────────
  //  IFrameQueryProvider
  // ──────────────────────────────────────────────

  /// <inheritdoc />
  public object Compile(Expression expression)
  {
    return _visitor.CompileExpression(expression);
  }

  /// <inheritdoc />
  /// <remarks>
  /// <para>
  /// <see cref="SparkRowHydrator{T}"/> carries an <c>IFlatSchema</c> constraint that cannot
  /// be expressed on this method. The constraint is already enforced at catalog construction
  /// via <see cref="FrameItemFactory.Memory{TRow}"/>, so every <see cref="TypedFrame{T}"/>
  /// in the system is guaranteed to carry a flat schema at runtime.
  /// </para>
  /// <para>
  /// The hydrator is instantiated via reflection to bypass the compile-time constraint.
  /// </para>
  /// </remarks>
  public IEnumerable<T> Materialize<T>(Expression expression)
  {
    var df = CompileToNative(expression);
    var hydratorType = typeof(SparkRowHydrator<>).MakeGenericType(typeof(T));
    var hydrator = Activator.CreateInstance(hydratorType, this)!;
    var collectRows = hydratorType.GetMethod(
      "CollectRows",
      System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
    )!;
    return (IEnumerable<T>)collectRows.Invoke(hydrator, [df])!;
  }

  // ──────────────────────────────────────────────
  //  IQueryProvider
  // ──────────────────────────────────────────────

  /// <inheritdoc />
  public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
  {
    return new TypedFrame<TElement>(this, expression);
  }

  /// <inheritdoc />
  public IQueryable CreateQuery(Expression expression)
  {
    var elementType =
      expression.Type.GetGenericArguments().FirstOrDefault()
      ?? throw new ArgumentException(
        "Cannot determine element type from expression.",
        nameof(expression)
      );

    var frameType = typeof(TypedFrame<>).MakeGenericType(elementType);
    return (IQueryable)Activator.CreateInstance(frameType, this, expression)!;
  }

  /// <summary>
  /// Executes a scalar terminal operation (e.g., <c>Count()</c>) by compiling the
  /// expression tree and returning the result.
  /// </summary>
  public TResult Execute<TResult>(Expression expression)
  {
    var result = _visitor.CompileExpression(expression);
    return (TResult)result;
  }

  /// <summary>Not supported — use the generic overload.</summary>
  public object? Execute(Expression expression) =>
    throw new NotSupportedException("Use Execute<TResult> for scalar terminal operations.");
}
