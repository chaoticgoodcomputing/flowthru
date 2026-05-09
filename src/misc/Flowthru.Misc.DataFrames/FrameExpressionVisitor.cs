using System.Linq.Expressions;
using System.Reflection;

namespace Flowthru.Misc.DataFrames;

/// <summary>
/// Base class for translating <see cref="TypedFrame{T}"/> expression trees into native
/// frame operations.
/// </summary>
/// <remarks>
/// <para>
/// This class mirrors the role of EF Core's <c>QueryableMethodTranslatingExpressionVisitor</c>.
/// It walks the expression tree built by <see cref="TypedFrameExtensions"/> methods and
/// dispatches to abstract handler methods that providers implement for their native backend.
/// </para>
/// <para>
/// Providers subclass this and implement the <c>Translate*</c> methods to emit native
/// operations (e.g., Spark <c>Column</c> expressions, ML.NET <c>IEstimator</c> chains).
/// </para>
/// </remarks>
public abstract class FrameExpressionVisitor
{
  /// <summary>
  /// Compiles a full expression tree into a native frame object.
  /// </summary>
  /// <remarks>
  /// The expression tree is rooted at a <see cref="TypedFrame{T}"/> constant (a leaf
  /// backed by a native frame), with chained <see cref="MethodCallExpression"/> nodes
  /// representing operations like Where, Select, and Join.
  /// </remarks>
  public object CompileExpression(Expression expression)
  {
    return expression switch
    {
      MethodCallExpression mce => TranslateMethodCall(mce),
      ConstantExpression ce => TranslateConstant(ce),
      _ => throw new NotSupportedException(
        $"Expression node type '{expression.NodeType}' is not supported at the top level. "
          + "Only method call chains rooted at a TypedFrame<T> constant are supported."
      ),
    };
  }

  /// <summary>
  /// Dispatches a method call to the appropriate handler, or throws if unrecognized.
  /// </summary>
  protected virtual object TranslateMethodCall(MethodCallExpression node)
  {
    if (node.Method.DeclaringType == typeof(TypedFrameExtensions))
    {
      return node.Method.Name switch
      {
        nameof(TypedFrameExtensions.Where) => TranslateWhere(node),
        nameof(TypedFrameExtensions.Select) => TranslateSelect(node),
        nameof(TypedFrameExtensions.Join) => TranslateJoin(node),
        nameof(TypedFrameExtensions.OrderBy) => TranslateOrderBy(node, descending: false),
        nameof(TypedFrameExtensions.OrderByDescending) => TranslateOrderBy(node, descending: true),
        nameof(TypedFrameExtensions.Take) => TranslateTake(node),
        nameof(TypedFrameExtensions.Count) => TranslateCount(node),
        nameof(TypedFrameExtensions.GroupBy) => TranslateGroupBy(node),
        nameof(TypedFrameExtensions.Distinct) => TranslateDistinct(node),
        nameof(TypedFrameExtensions.Union) => TranslateUnion(node),
        nameof(TypedFrameExtensions.SelectOver) => TranslateSelectOver(node),
        _ => throw new NotSupportedException(
          $"TypedFrame operation '{node.Method.Name}' is not yet supported."
        ),
      };
    }

    if (node.Method.DeclaringType == typeof(GroupedFrameExtensions))
    {
      return node.Method.Name switch
      {
        nameof(GroupedFrameExtensions.Aggregate) => TranslateAggregate(node),
        _ => throw new NotSupportedException(
          $"GroupedFrame operation '{node.Method.Name}' is not yet supported."
        ),
      };
    }

    throw new NotSupportedException(
      $"Method '{node.Method.DeclaringType?.Name}.{node.Method.Name}' is not a recognized "
        + "TypedFrame operation. Only methods defined on TypedFrameExtensions or GroupedFrameExtensions are supported."
    );
  }

  /// <summary>
  /// Translates a <see cref="ConstantExpression"/> wrapping a root <see cref="TypedFrame{T}"/>
  /// into the native frame it represents.
  /// </summary>
  protected abstract object TranslateConstant(ConstantExpression node);

  /// <summary>
  /// Translates a <c>Where</c> operation into a native filter.
  /// </summary>
  /// <param name="node">
  /// Method call with arguments: [0] source expression, [1] quoted predicate lambda.
  /// </param>
  protected abstract object TranslateWhere(MethodCallExpression node);

  /// <summary>
  /// Translates a <c>Select</c> operation into a native projection.
  /// </summary>
  /// <param name="node">
  /// Method call with arguments: [0] source expression, [1] quoted selector lambda.
  /// </param>
  protected abstract object TranslateSelect(MethodCallExpression node);

  /// <summary>
  /// Translates a <c>Join</c> operation into a native join + projection.
  /// </summary>
  /// <param name="node">
  /// Method call with arguments: [0] outer source, [1] inner source,
  /// [2] outer key selector, [3] inner key selector, [4] result selector.
  /// </param>
  protected abstract object TranslateJoin(MethodCallExpression node);

  /// <summary>
  /// Translates an <c>OrderBy</c> or <c>OrderByDescending</c> operation into a native sort.
  /// </summary>
  /// <param name="node">Method call with arguments: [0] source, [1] quoted key selector.</param>
  /// <param name="descending">True for descending order; false for ascending.</param>
  protected abstract object TranslateOrderBy(MethodCallExpression node, bool descending);

  /// <summary>
  /// Translates a <c>Take</c> operation into a native row limit.
  /// </summary>
  /// <param name="node">Method call with arguments: [0] source, [1] count constant.</param>
  protected abstract object TranslateTake(MethodCallExpression node);

  /// <summary>
  /// Translates a <c>Count</c> operation into a scalar row count.
  /// </summary>
  /// <param name="node">Method call with arguments: [0] source.</param>
  protected abstract object TranslateCount(MethodCallExpression node);

  /// <summary>
  /// Translates a <c>Distinct</c> operation into a native deduplication.
  /// </summary>
  /// <param name="node">Method call with arguments: [0] source.</param>
  protected abstract object TranslateDistinct(MethodCallExpression node);

  /// <summary>
  /// Translates a <c>Union</c> operation into a native row-wise concatenation.
  /// </summary>
  /// <param name="node">Method call with arguments: [0] source, [1] other source.</param>
  protected abstract object TranslateUnion(MethodCallExpression node);

  /// <summary>
  /// Translates a <c>GroupBy</c> operation into a native grouped dataset.
  /// </summary>
  /// <param name="node">Method call with arguments: [0] source, [1] quoted key selector.</param>
  protected abstract object TranslateGroupBy(MethodCallExpression node);

  /// <summary>
  /// Translates an <c>Aggregate</c> operation on a grouped frame into a native aggregation.
  /// </summary>
  /// <param name="node">
  /// Method call with arguments: [0] grouped source expression, [1] quoted result selector.
  /// The result selector's body is a <c>MemberInitExpression</c> or <c>NewExpression</c>
  /// whose bindings reference <see cref="AggregationContext{TKey,TSource}"/> methods.
  /// </param>
  protected abstract object TranslateAggregate(MethodCallExpression node);

  /// <summary>
  /// Translates a <c>SelectOver</c> operation into a native windowed projection.
  /// </summary>
  /// <param name="node">
  /// Method call with arguments: [0] source expression,
  /// [1] quoted two-parameter selector <c>(TSource, WindowContext&lt;TSource&gt;) =&gt; TResult</c>.
  /// Bindings referencing the <c>WindowContext</c> parameter carry
  /// <see cref="FrameWindowSpec{TSource}"/> arguments that describe the window.
  /// </param>
  protected abstract object TranslateSelectOver(MethodCallExpression node);

  // ──────────────────────────────────────────────
  //  Shared helpers
  // ──────────────────────────────────────────────

  /// <summary>
  /// Resolves the external column name for a schema property. Honors
  /// any <c>[SerializedLabel(string)]</c>-shaped attribute attached to
  /// the member by structural lookup (attribute type name matches
  /// <c>SerializedLabelAttribute</c> and exposes a <c>Label</c>
  /// string property). Duck-typed deliberately so this utility stays
  /// framework-agnostic — it works with Flowthru's
  /// <c>[SerializedLabel]</c> when present but does not depend on
  /// <c>Flowthru.Core</c>.
  /// </summary>
  protected static string ResolveColumnName(MemberInfo member)
  {
    foreach (var attr in member.GetCustomAttributes())
    {
      var attrType = attr.GetType();
      if (attrType.Name != "SerializedLabelAttribute") continue;
      var labelProp = attrType.GetProperty("Label");
      if (labelProp is null) continue;
      if (labelProp.GetValue(attr) is string label && !string.IsNullOrEmpty(label))
        return label;
    }
    return member.Name;
  }

  /// <summary>
  /// Extracts the <see cref="LambdaExpression"/> from a quoted argument.
  /// </summary>
  protected static LambdaExpression Unquote(Expression expression)
  {
    if (expression is UnaryExpression { NodeType: ExpressionType.Quote } unary)
    {
      return (LambdaExpression)unary.Operand;
    }

    if (expression is LambdaExpression lambda)
    {
      return lambda;
    }

    throw new InvalidOperationException(
      $"Expected a quoted lambda expression, got {expression.NodeType}."
    );
  }

  /// <summary>
  /// Evaluates an expression that doesn't reference any DataFrame columns
  /// (e.g., a closure-captured variable or a static field) to its runtime value.
  /// </summary>
  protected static object? EvaluateConstant(Expression expression)
  {
    var lambda = Expression.Lambda<Func<object?>>(Expression.Convert(expression, typeof(object)));
    return lambda.Compile().Invoke();
  }
}
