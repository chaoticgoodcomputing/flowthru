using System.Linq.Expressions;
using System.Reflection;
using Flowthru.Extensions.Spark.Shared;
using Flowthru.Misc.DataFrames;
using Flowthru.Spark.Sql;
using Flowthru.Spark.Sql.Expressions;
using SparkFunctions = Flowthru.Spark.Sql.Functions;

namespace Flowthru.Extensions.Spark;

/// <summary>
/// Translates <see cref="TypedFrame{T}"/> expression trees into Spark.NET
/// <see cref="DataFrame"/> operations.
/// </summary>
/// <remarks>
/// <para>
/// This visitor handles three categories of translation:
/// </para>
/// <list type="number">
/// <item>
/// <strong>Top-level operations</strong> (Where, Select, Join) — dispatched by the base
/// <see cref="FrameExpressionVisitor"/>. Each produces a new <see cref="DataFrame"/>.
/// </item>
/// <item>
/// <strong>Sub-expressions</strong> (predicates, selectors) — translated into Spark
/// <see cref="Column"/> expressions via <see cref="TranslateSubExpression"/>.
/// Property access becomes column references; binary ops use <see cref="Column"/>
/// operator overloads; constants use <see cref="SparkFunctions.Lit"/>.
/// </item>
/// <item>
/// <strong>Projections</strong> (<c>MemberInitExpression</c>, <c>NewExpression</c>) —
/// produce <see cref="Column"/> arrays with aliases matching the target schema property names.
/// </item>
/// </list>
/// </remarks>
internal sealed class SparkExpressionVisitor : FrameExpressionVisitor
{
    private readonly SparkFrameProvider _provider;

    internal SparkExpressionVisitor(SparkFrameProvider provider)
    {
        _provider = provider;
    }

    // ──────────────────────────────────────────────
    //  Root node: extract native DataFrame
    // ──────────────────────────────────────────────

    protected override object TranslateConstant(ConstantExpression node)
    {
        if (node.Value is null)
        {
            throw new InvalidOperationException("Null TypedFrame constant encountered.");
        }

        // The constant is a TypedFrame<T>; retrieve the native DataFrame the provider stored.
        return _provider.GetNativeFrame(node.Value);
    }

    // ──────────────────────────────────────────────
    //  Easy: Where (type-preserving filter)
    // ──────────────────────────────────────────────

    protected override object TranslateWhere(MethodCallExpression node)
    {
        // args[0] = source expression, args[1] = quoted predicate
        var sourceDf = (DataFrame)CompileExpression(node.Arguments[0]);
        var predicate = Unquote(node.Arguments[1]);

        var paramMap = new Dictionary<ParameterExpression, DataFrame>
        {
            [predicate.Parameters[0]] = sourceDf,
        };

        var condition = TranslateSubExpression(predicate.Body, paramMap);
        return sourceDf.Filter(condition);
    }

    // ──────────────────────────────────────────────
    //  Easy: OrderBy / OrderByDescending
    // ──────────────────────────────────────────────

    protected override object TranslateOrderBy(MethodCallExpression node, bool descending)
    {
        var sourceDf = (DataFrame)CompileExpression(node.Arguments[0]);
        var keyLambda = Unquote(node.Arguments[1]);

        var paramMap = new Dictionary<ParameterExpression, DataFrame>
        {
            [keyLambda.Parameters[0]] = sourceDf,
        };

        var keyCol = TranslateSubExpression(keyLambda.Body, paramMap);
        var sortCol = descending ? keyCol.Desc() : keyCol;
        return sourceDf.Sort(sortCol);
    }

    // ──────────────────────────────────────────────
    //  Easy: Take (row limit)
    // ──────────────────────────────────────────────

    protected override object TranslateTake(MethodCallExpression node)
    {
        var sourceDf = (DataFrame)CompileExpression(node.Arguments[0]);
        var count = (int)((ConstantExpression)node.Arguments[1]).Value!;
        return sourceDf.Limit(count);
    }

    // ──────────────────────────────────────────────
    //  Easy: Count (scalar)
    // ──────────────────────────────────────────────

    protected override object TranslateCount(MethodCallExpression node)
    {
        var sourceDf = (DataFrame)CompileExpression(node.Arguments[0]);
        return sourceDf.Count();
    }

    protected override object TranslateDistinct(MethodCallExpression node)
    {
        var sourceDf = (DataFrame)CompileExpression(node.Arguments[0]);
        return sourceDf.Distinct();
    }

    protected override object TranslateUnion(MethodCallExpression node)
    {
        var leftDf = (DataFrame)CompileExpression(node.Arguments[0]);
        var rightDf = (DataFrame)CompileExpression(node.Arguments[1]);
        return leftDf.Union(rightDf);
    }

    // ──────────────────────────────────────────────
    //  Intermediate: Select (type-projecting)
    // ──────────────────────────────────────────────

    protected override object TranslateSelect(MethodCallExpression node)
    {
        // args[0] = source expression, args[1] = quoted selector
        var sourceDf = (DataFrame)CompileExpression(node.Arguments[0]);
        var selector = Unquote(node.Arguments[1]);

        var paramMap = new Dictionary<ParameterExpression, DataFrame>
        {
            [selector.Parameters[0]] = sourceDf,
        };

        var columns = TranslateProjection(selector.Body, paramMap);
        return sourceDf.Select(columns);
    }

    // ──────────────────────────────────────────────
    //  Hard: Join (multi-frame)
    // ──────────────────────────────────────────────

    protected override object TranslateJoin(MethodCallExpression node)
    {
        // args: [0] outer, [1] inner, [2] outerKey, [3] innerKey, [4] resultSelector
        var outerDf = (DataFrame)CompileExpression(node.Arguments[0]);
        var innerDf = (DataFrame)CompileExpression(node.Arguments[1]);
        var outerKeyLambda = Unquote(node.Arguments[2]);
        var innerKeyLambda = Unquote(node.Arguments[3]);
        var resultLambda = Unquote(node.Arguments[4]);

        // Translate key selectors to Column references
        var outerKeyMap = new Dictionary<ParameterExpression, DataFrame>
        {
            [outerKeyLambda.Parameters[0]] = outerDf,
        };
        var innerKeyMap = new Dictionary<ParameterExpression, DataFrame>
        {
            [innerKeyLambda.Parameters[0]] = innerDf,
        };

        var outerKeyCol = TranslateSubExpression(outerKeyLambda.Body, outerKeyMap);
        var innerKeyCol = TranslateSubExpression(innerKeyLambda.Body, innerKeyMap);

        // Equi-join: outerKey == innerKey
        var joinCondition = outerKeyCol == innerKeyCol;
        var joinedDf = outerDf.Join(innerDf, joinCondition, "inner");

        // Translate result selector into projection columns.
        // The result lambda has two parameters: (outer, inner).
        var resultMap = new Dictionary<ParameterExpression, DataFrame>
        {
            [resultLambda.Parameters[0]] = outerDf,
            [resultLambda.Parameters[1]] = innerDf,
        };

        var resultColumns = TranslateProjection(resultLambda.Body, resultMap);
        return joinedDf.Select(resultColumns);
    }

    // ──────────────────────────────────────────────
    //  Medium: GroupBy + Aggregate
    // ──────────────────────────────────────────────

    protected override object TranslateGroupBy(MethodCallExpression node)
    {
        // GroupBy returns a RelationalGroupedDataset — stored as the "native" value in
        // the GroupedFrame expression chain, picked up by TranslateAggregate.
        var sourceDf = (DataFrame)CompileExpression(node.Arguments[0]);
        var keyLambda = Unquote(node.Arguments[1]);

        var paramMap = new Dictionary<ParameterExpression, DataFrame>
        {
            [keyLambda.Parameters[0]] = sourceDf,
        };

        var keyCol = TranslateSubExpression(keyLambda.Body, paramMap);
        return sourceDf.GroupBy(keyCol);
    }

    protected override object TranslateAggregate(MethodCallExpression node)
    {
        // args[0] = GroupBy expression (produces RelationalGroupedDataset)
        // args[1] = quoted result selector: AggregationContext<TKey,TSource> → TResult
        var grouped = (RelationalGroupedDataset)CompileExpression(node.Arguments[0]);
        var resultLambda = Unquote(node.Arguments[1]);
        var keyColName = ExtractGroupByKeyColumnName(node.Arguments[0]);

        // Split bindings into:
        //   aggCols    — passed to .Agg(); excludes ctx.Key (Spark projects it automatically)
        //   outputCols — final Select to reorder/rename all columns to match target schema
        var (aggCols, outputCols) = BuildAggregateColumns(
          resultLambda.Body,
          resultLambda.Parameters[0],
          keyColName
        );

        if (aggCols.Count == 0)
        {
            throw new NotSupportedException(
              "Aggregate result selector must contain at least one aggregate function "
                + "(Avg, Sum, Min, Max, or Count). Key-only projections are not supported."
            );
        }

        var agged = grouped.Agg(aggCols[0], aggCols.Skip(1).ToArray());

        // Reorder/rename to match target schema order
        return agged.Select(outputCols.ToArray());
    }

    /// <summary>
    /// Splits an Aggregate result selector into two lists:
    /// <list type="bullet">
    ///   <item><c>aggCols</c> — columns for <c>.Agg()</c>; key bindings excluded because
    ///   Spark already projects the group-by key into the <c>Agg</c> output automatically.</item>
    ///   <item><c>outputCols</c> — all columns in target schema order, for the final
    ///   <c>.Select()</c> that renames/reorders the <c>Agg</c> result.</item>
    /// </list>
    /// </summary>
    private (List<Column> aggCols, List<Column> outputCols) BuildAggregateColumns(
      Expression body,
      ParameterExpression contextParam,
      string keyColName
    )
    {
        var aggCols = new List<Column>();
        var outputCols = new List<Column>();

        IEnumerable<(Expression expr, MemberInfo targetMember)> bindings = body switch
        {
            MemberInitExpression mie => mie
              .Bindings.Cast<MemberAssignment>()
              .Select(b => (b.Expression, b.Member)),
            NewExpression ne when ne.Members is not null => ne.Arguments.Zip(
              ne.Members,
              (a, m) => (a, (MemberInfo)m)
            ),
            _ => throw new NotSupportedException(
              "Aggregate result selector must be a member initializer or positional constructor."
            ),
        };

        foreach (var (expr, member) in bindings)
        {
            var targetName = ResolveColumnName(member);

            if (IsKeyAccess(expr, contextParam))
            {
                // The key column is already in the Agg output under keyColName.
                // Reference it (with rename if the target property name differs).
                outputCols.Add(SparkFunctions.Col(keyColName).As(targetName));
            }
            else
            {
                // Aggregate function — include in Agg() call, then reference by target name in Select.
                var aggCol = TranslateAggregateExpression(expr, contextParam, keyColName).As(targetName);
                aggCols.Add(aggCol);
                outputCols.Add(SparkFunctions.Col(targetName));
            }
        }

        return (aggCols, outputCols);
    }

    private static bool IsKeyAccess(Expression expr, ParameterExpression contextParam) =>
      expr is MemberExpression { Member.Name: "Key" } me && me.Expression == contextParam;

    /// <summary>
    /// Extracts the key column name string from the GroupBy MethodCallExpression that is the
    /// source argument of an Aggregate call (i.e., <c>args[0]</c> of the Aggregate node).
    /// </summary>
    private static string ExtractGroupByKeyColumnName(Expression groupByExpr)
    {
        // groupByExpr may itself be wrapped in a ConstantExpression if already compiled,
        // but in the expression tree it is always a MethodCallExpression for GroupBy.
        if (
          groupByExpr is MethodCallExpression gbe
          && gbe.Method.Name == nameof(TypedFrameExtensions.GroupBy)
        )
        {
            var keyLambda = Unquote(gbe.Arguments[1]);
            if (keyLambda.Body is MemberExpression me)
            {
                return ResolveColumnName(me.Member);
            }
        }

        throw new NotSupportedException(
          "Could not extract key column name from GroupBy source expression. "
            + "ctx.Key is only supported when the GroupBy key is a direct property access."
        );
    }

    protected override object TranslateSelectOver(MethodCallExpression node)
    {
        // args[0] = source expression
        // args[1] = quoted selector: (TSource row, WindowContext<TSource> win) => TResult
        var sourceDf = (DataFrame)CompileExpression(node.Arguments[0]);
        var selector = Unquote(node.Arguments[1]);

        var rowParam = selector.Parameters[0]; // TSource row → maps to sourceDf
        var winParam = selector.Parameters[1]; // WindowContext<TSource> win → intercepted below

        var paramMap = new Dictionary<ParameterExpression, DataFrame> { [rowParam] = sourceDf };

        var columns = TranslateSelectOverProjection(selector.Body, winParam, paramMap);
        return sourceDf.Select(columns);
    }

    /// <summary>
    /// Walks a <c>SelectOver</c> projection body and translates each binding, intercepting
    /// <see cref="WindowContext{TSource}"/> method calls and delegating everything else to
    /// the standard <see cref="TranslateSubExpression"/> path.
    /// </summary>
    private Column[] TranslateSelectOverProjection(
      Expression body,
      ParameterExpression winParam,
      Dictionary<ParameterExpression, DataFrame> paramMap
    )
    {
        IEnumerable<(Expression Expr, MemberInfo Member)> bindings = body switch
        {
            MemberInitExpression mie => mie
              .Bindings.Cast<MemberAssignment>()
              .Select(b => (b.Expression, b.Member)),
            NewExpression ne when ne.Members is not null => ne.Arguments.Zip(
              ne.Members,
              (a, m) => (a, (MemberInfo)m)
            ),
            _ => throw new NotSupportedException(
              "SelectOver projection must be a member initializer (new T { ... }) or positional constructor."
            ),
        };

        return bindings
          .Select(b =>
          {
              var col =
            b.Expr is MethodCallExpression mce && mce.Object == winParam
              ? TranslateWindowContextCall(mce, paramMap)
              : TranslateSubExpression(b.Expr, paramMap);
              return col.As(ResolveColumnName(b.Member));
          })
          .ToArray();
    }

    /// <summary>
    /// Translates a <see cref="WindowContext{TSource}"/> method call into a Spark windowed
    /// <see cref="Column"/> by mapping each method to its <c>SparkFunctions</c> equivalent
    /// and applying the translated <see cref="WindowSpec"/>.
    /// </summary>
    private Column TranslateWindowContextCall(
      MethodCallExpression mce,
      Dictionary<ParameterExpression, DataFrame> paramMap
    )
    {
        // The FrameWindowSpec is always the last argument. Evaluate it from the expression
        // tree at translation time (it is a closed-over constant in the step lambda).
        var specExpr = mce.Arguments[^1];
        var spec = (IFrameWindowSpec)Expression.Lambda(specExpr).Compile().DynamicInvoke()!;
        var ws = BuildNativeWindowSpec(spec);

        return mce.Method.Name switch
        {
            // ── Ranking functions (spec only) ──
            nameof(WindowContext<object>.RowNumber) => SparkFunctions.RowNumber().Over(ws),
            nameof(WindowContext<object>.Rank) => SparkFunctions.Rank().Over(ws),
            nameof(WindowContext<object>.DenseRank) => SparkFunctions.DenseRank().Over(ws),
            nameof(WindowContext<object>.CumeDist) => SparkFunctions.CumeDist().Over(ws),
            nameof(WindowContext<object>.PercentRank) => SparkFunctions.PercentRank().Over(ws),
            nameof(WindowContext<object>.Count) => SparkFunctions.Count(SparkFunctions.Lit(1)).Over(ws),

            // ── Aggregate window functions (column selector + spec) ──
            nameof(WindowContext<object>.Avg) => SparkFunctions
              .Avg(ExtractWindowColumnName(mce.Arguments[0]))
              .Over(ws),
            nameof(WindowContext<object>.Sum) => SparkFunctions
              .Sum(ExtractWindowColumnName(mce.Arguments[0]))
              .Over(ws),

            // ── Offset functions (column selector + offset + spec) ──
            nameof(WindowContext<object>.Lag) => SparkFunctions
              .Lag(
                ExtractWindowColumnName(mce.Arguments[0]),
                (int)((ConstantExpression)mce.Arguments[1]).Value!
              )
              .Over(ws),
            nameof(WindowContext<object>.Lead) => SparkFunctions
              .Lead(
                ExtractWindowColumnName(mce.Arguments[0]),
                (int)((ConstantExpression)mce.Arguments[1]).Value!
              )
              .Over(ws),

            _ => throw new NotSupportedException(
              $"WindowContext method '{mce.Method.Name}' has no Spark translation."
            ),
        };
    }

    /// <summary>
    /// Translates a <see cref="IFrameWindowSpec"/> into a Spark <see cref="WindowSpec"/>
    /// by mapping its partition and order key lambdas to column name strings.
    /// </summary>
    private static WindowSpec BuildNativeWindowSpec(IFrameWindowSpec spec)
    {
        var partCols = spec
          .PartitionByExpressions.Select(lambda => SparkFunctions.Col(ExtractLambdaColumnName(lambda)))
          .ToArray();

        var orderCols = spec
          .OrderByExpressions.Select(pair =>
          {
              var col = SparkFunctions.Col(ExtractLambdaColumnName(pair.KeySelector));
              return pair.Descending ? col.Desc() : col;
          })
          .ToArray();

        if (partCols.Length > 0)
        {
            var ws = Window.PartitionBy(partCols);
            return orderCols.Length > 0 ? ws.OrderBy(orderCols) : ws;
        }

        if (orderCols.Length > 0)
        {
            return Window.OrderBy(orderCols);
        }

        throw new NotSupportedException(
          "A FrameWindowSpec with no partition or order keys cannot be translated. "
            + "Provide at least one PartitionBy or OrderBy key."
        );
    }

    /// <summary>
    /// Extracts the serialized column name from a <see cref="FrameWindowSpec{TSource}"/>
    /// partition or order key lambda (e.g., <c>r =&gt; r.ShuttleType</c>).
    /// </summary>
    private static string ExtractLambdaColumnName(LambdaExpression lambda)
    {
        var body = lambda.Body;
        while (body is UnaryExpression { NodeType: ExpressionType.Convert } ue)
        {
            body = ue.Operand;
        }

        if (body is MemberExpression me)
        {
            return ResolveColumnName(me.Member);
        }

        throw new NotSupportedException(
          $"Window spec key selectors must be simple property access expressions "
            + $"(x => x.Property). Got: {lambda.Body}."
        );
    }

    /// <summary>
    /// Extracts the serialized column name from a quoted lambda argument used as a window
    /// aggregate selector (e.g., the <c>r =&gt; r.Price</c> in <c>win.Avg(r =&gt; r.Price, spec)</c>).
    /// </summary>
    private static string ExtractWindowColumnName(Expression lambdaExpr)
    {
        var lambda = lambdaExpr is UnaryExpression { NodeType: ExpressionType.Quote } q
          ? (LambdaExpression)q.Operand
          : (LambdaExpression)lambdaExpr;

        return ExtractLambdaColumnName(lambda);
    }

    /// <summary>
    /// Translates a single aggregate function call on <see cref="AggregationContext{TKey,TSource}"/>
    /// into its Spark <see cref="Column"/> equivalent.
    /// </summary>
    /// <remarks>
    /// Key bindings (<c>ctx.Key</c>) are handled upstream in <c>BuildAggregateColumns</c>
    /// and must not reach this method.
    /// </remarks>
    private Column TranslateAggregateExpression(
      Expression expr,
      ParameterExpression contextParam,
      string keyColName
    )
    {
        // ctx.Avg(x => x.Price), ctx.Sum(x => x.Quantity), etc.
        if (
          expr is MethodCallExpression mce
          && mce.Object is ParameterExpression pe
          && pe == contextParam
        )
        {
            var innerColName = ExtractAggregateColumnName(mce.Arguments[0]);

            return mce.Method.Name switch
            {
                "Avg" => SparkFunctions.Avg(innerColName),
                "Sum" => SparkFunctions.Sum(innerColName),
                "Max" => SparkFunctions.Max(innerColName),
                "Min" => SparkFunctions.Min(innerColName),
                "Count" => SparkFunctions.Count(SparkFunctions.Lit(1)),
                _ => throw new NotSupportedException(
                  $"Aggregate function '{mce.Method.Name}' is not supported."
                ),
            };
        }

        throw new NotSupportedException(
          $"Expression '{expr}' in aggregate result selector is not supported. "
            + "Only AggregationContext method calls (Avg, Sum, Count, Min, Max) are valid. "
            + "ctx.Key bindings are handled separately and should not reach this method."
        );
    }

    /// <summary>
    /// Extracts the column name string from the inner lambda argument of an aggregate call
    /// (e.g., the <c>x => x.Price</c> in <c>ctx.Avg(x => x.Price)</c>).
    /// </summary>
    private static string ExtractAggregateColumnName(Expression lambdaExpr)
    {
        var lambda = lambdaExpr is UnaryExpression { NodeType: ExpressionType.Quote } q
          ? (LambdaExpression)q.Operand
          : (LambdaExpression)lambdaExpr;

        // Peel off any implicit cast (e.g., (double)s.PassengerCapacity → s.PassengerCapacity)
        var body = lambda.Body;
        while (body is UnaryExpression { NodeType: ExpressionType.Convert } ue)
        {
            body = ue.Operand;
        }

        if (body is MemberExpression me)
        {
            return ResolveColumnName(me.Member);
        }

        throw new NotSupportedException(
          "Aggregate column selectors must be simple property access expressions (x => x.Property) "
            + "or cast expressions ((double)x.Property). "
            + $"Got: {lambda.Body.NodeType} ({lambda.Body})."
        );
    }

    // ──────────────────────────────────────────────
    //  Sub-expression translation → Column
    // ──────────────────────────────────────────────

    /// <summary>
    /// Translates a LINQ expression node into a Spark <see cref="Column"/>.
    /// </summary>
    /// <param name="expr">The expression node (predicate body, selector fragment, etc.).</param>
    /// <param name="paramMap">Maps lambda parameters to their backing DataFrames.</param>
    private Column TranslateSubExpression(
      Expression expr,
      Dictionary<ParameterExpression, DataFrame> paramMap
    )
    {
        return expr switch
        {
            // Property access on a schema type → column reference
            MemberExpression me => TranslateMemberAccess(me, paramMap),

            // Binary operation → Column operator overload
            BinaryExpression be => TranslateBinary(be, paramMap),

            // Ternary / conditional → When(...).Otherwise(...)
            ConditionalExpression ce => TranslateConditional(ce, paramMap),

            // Method call → string methods, Math methods, etc.
            MethodCallExpression mce => TranslateMethodCallExpression(mce, paramMap),

            // Constant value → Lit()
            ConstantExpression ce => SparkFunctions.Lit(ce.Value),

            // Implicit cast / convert → recurse through
            UnaryExpression { NodeType: ExpressionType.Convert } ue => TranslateSubExpression(
              ue.Operand,
              paramMap
            ),

            // Boolean negation
            UnaryExpression { NodeType: ExpressionType.Not } ue => !TranslateSubExpression(
              ue.Operand,
              paramMap
            ),

            _ => throw new NotSupportedException(
              $"Expression node type '{expr.NodeType}' ({expr.GetType().Name}) "
                + "is not supported in Spark sub-expressions. "
                + "Supported: property access, binary operations, constants, negation, "
                + "conditionals, and selected string/math method calls."
            ),
        };
    }

    // ──────────────────────────────────────────────
    //  Conditional / ternary translation
    // ──────────────────────────────────────────────

    /// <summary>
    /// Translates a <c>ConditionalExpression</c> (<c>x ? a : b</c>) into
    /// <c>Functions.When(condition, ifTrue).Otherwise(ifFalse)</c>.
    /// </summary>
    private Column TranslateConditional(
      ConditionalExpression ce,
      Dictionary<ParameterExpression, DataFrame> paramMap
    )
    {
        var condition = TranslateSubExpression(ce.Test, paramMap);
        var ifTrue = (object)TranslateSubExpression(ce.IfTrue, paramMap);
        var ifFalse = (object)TranslateSubExpression(ce.IfFalse, paramMap);
        return SparkFunctions.When(condition, ifTrue).Otherwise(ifFalse);
    }

    // ──────────────────────────────────────────────
    //  Method call translation (string, Math, etc.)
    // ──────────────────────────────────────────────

    // Method name whitelists are defined in SparkTranslatableOperations (shared with the analyzer).
    // Update that file when adding new translations — do not add sets here.

    /// <summary>
    /// Routes a method call in a sub-expression to the appropriate translator.
    /// </summary>
    private Column TranslateMethodCallExpression(
      MethodCallExpression mce,
      Dictionary<ParameterExpression, DataFrame> paramMap
    )
    {
        if (
          mce.Method.DeclaringType == typeof(string)
          && SparkTranslatableOperations.SupportedStringMethods.Contains(mce.Method.Name)
        )
        {
            return TranslateStringMethod(mce, paramMap);
        }

        if (
          mce.Method.DeclaringType == typeof(Math)
          && SparkTranslatableOperations.SupportedMathMethods.Contains(mce.Method.Name)
        )
        {
            return TranslateMathMethod(mce, paramMap);
        }

        throw new NotSupportedException(
          $"Method '{mce.Method.DeclaringType?.Name}.{mce.Method.Name}' "
            + "is not supported in Spark sub-expressions. "
            + "Supported string methods: Replace, Contains, StartsWith, EndsWith, "
            + "ToUpper, ToLower, Trim, TrimStart, TrimEnd, Substring. "
            + "Supported Math methods: Round, Abs, Floor, Ceiling."
        );
    }

    /// <summary>
    /// Translates <c>string</c> instance methods to their Spark <c>Column</c> equivalents.
    /// </summary>
    /// <remarks>
    /// <strong>Indexing convention:</strong> all index and position arguments follow C#'s
    /// idiomatic zero-based indexing in the expression passed by the caller. Where the
    /// underlying Spark function uses a different convention (e.g., <c>Substring</c> takes a
    /// 1-based position), this method adjusts transparently so callers never need to be aware
    /// of Spark's internal convention.
    /// </remarks>
    private Column TranslateStringMethod(
      MethodCallExpression mce,
      Dictionary<ParameterExpression, DataFrame> paramMap
    )
    {
        // All string instance methods have a non-null Object (the receiver)
        var col = TranslateSubExpression(mce.Object!, paramMap);

        return mce.Method.Name switch
        {
            // s.ToUpper() → Upper(col)
            nameof(string.ToUpper) => SparkFunctions.Upper(col),

            // s.ToLower() → Lower(col)
            nameof(string.ToLower) => SparkFunctions.Lower(col),

            // s.Trim() → Trim(col)
            nameof(string.Trim) => SparkFunctions.Trim(col),

            // s.Contains(x) → col.Contains(x)
            nameof(string.Contains) => col.Contains(TranslateSubExpression(mce.Arguments[0], paramMap)),

            // s.StartsWith(x) → col.StartsWith(x)
            nameof(string.StartsWith) => col.StartsWith(
              TranslateSubExpression(mce.Arguments[0], paramMap)
            ),

            // s.EndsWith(x) → col.EndsWith(x)
            nameof(string.EndsWith) => col.EndsWith(TranslateSubExpression(mce.Arguments[0], paramMap)),

            // s.TrimStart() → Ltrim(col)
            nameof(string.TrimStart) => SparkFunctions.Ltrim(col),

            // s.TrimEnd() → Rtrim(col)
            nameof(string.TrimEnd) => SparkFunctions.Rtrim(col),

            // s.Substring(startIndex, length) → Substring(col, startIndex + 1, length)
            // C# uses zero-based startIndex; Spark's Substring uses 1-based position.
            // This translator adjusts transparently — callers always pass zero-based indices.
            nameof(string.Substring) => TranslateStringSubstring(mce, col, paramMap),

            // s.Replace(old, new) → RegexpReplace(col, old, new)
            // ⚠ SEMANTIC GAP: Spark's RegexpReplace interprets the first argument as a
            // regular expression pattern, not a literal string. C#'s string.Replace
            // treats it as a literal. Callers using regex-special characters (., *, +, etc.)
            // in the search string must escape them (e.g., Regex.Escape) or the translation
            // will produce different results than the equivalent in-process C# expression.
            nameof(string.Replace) => SparkFunctions.RegexpReplace(
              col,
              TranslateSubExpression(mce.Arguments[0], paramMap),
              TranslateSubExpression(mce.Arguments[1], paramMap)
            ),

            _ => throw new NotSupportedException(
              $"String method '{mce.Method.Name}' has no Spark translation."
            ),
        };
    }

    /// <summary>
    /// Handles <c>string.Substring(startIndex)</c> and <c>string.Substring(startIndex, length)</c>,
    /// adjusting from C#'s zero-based <paramref name="startIndex"/> to Spark's 1-based position.
    /// </summary>
    private Column TranslateStringSubstring(
      MethodCallExpression mce,
      Column col,
      Dictionary<ParameterExpression, DataFrame> paramMap
    )
    {
        // Spark Substring(col, pos, len): pos is 1-based.
        // We add 1 to the caller's 0-based startIndex here so callers never see Spark's convention.
        // startIndex must be a compile-time constant (C# string.Substring requires an int literal).
        var startIndex = (int)((ConstantExpression)mce.Arguments[0]).Value!;
        var oneBasedPos = startIndex + 1;

        if (mce.Arguments.Count == 1)
        {
            // Substring(startIndex) — no length; use Int32.MaxValue as "rest of string"
            return SparkFunctions.Substring(col, oneBasedPos, int.MaxValue);
        }

        var length = (int)((ConstantExpression)mce.Arguments[1]).Value!;
        return SparkFunctions.Substring(col, oneBasedPos, length);
    }

    // ──────────────────────────────────────────────
    //  Math method translation
    // ──────────────────────────────────────────────

    /// <summary>
    /// Translates <c>System.Math</c> static methods to their Spark <c>Column</c> equivalents.
    /// </summary>
    private Column TranslateMathMethod(
      MethodCallExpression mce,
      Dictionary<ParameterExpression, DataFrame> paramMap
    )
    {
        // All supported Math methods take a single numeric argument (the column expression).
        // Math.Round has overloads with a decimal-places argument — we support both.
        var col = TranslateSubExpression(mce.Arguments[0], paramMap);

        return mce.Method.Name switch
        {
            // Math.Abs(x) → Abs(col)
            nameof(Math.Abs) => SparkFunctions.Abs(col),

            // Math.Floor(x) → Floor(col)
            nameof(Math.Floor) => SparkFunctions.Floor(col),

            // Math.Ceiling(x) → Ceil(col)
            nameof(Math.Ceiling) => SparkFunctions.Ceil(col),

            // Math.Round(x) → Round(col, 0)
            // Math.Round(x, decimals) → Round(col, decimals)
            nameof(Math.Round) when mce.Arguments.Count == 1 => SparkFunctions.Round(col, 0),
            nameof(Math.Round) => SparkFunctions.Round(
              col,
              (int)((ConstantExpression)mce.Arguments[1]).Value!
            ),

            _ => throw new NotSupportedException(
              $"Math method '{mce.Method.Name}' has no Spark translation."
            ),
        };
    }

    // ──────────────────────────────────────────────
    //  Member access translation (columns + string.Length)
    // ──────────────────────────────────────────────

    /// <summary>
    /// Translates a member access into a Spark <see cref="Column"/> reference.
    /// Handles <c>string.Length</c> as a special case before resolving column names.
    /// </summary>
    private Column TranslateMemberAccess(
      MemberExpression me,
      Dictionary<ParameterExpression, DataFrame> paramMap
    )
    {
        // string.Length → Length(col)
        if (
          me.Member is PropertyInfo { Name: nameof(string.Length) }
          && me.Member.DeclaringType == typeof(string)
          && me.Expression is not null
        )
        {
            var inner = TranslateSubExpression(me.Expression, paramMap);
            return SparkFunctions.Length(inner);
        }

        // DateTime part properties → Spark date/time functions
        if (
          me.Member is PropertyInfo dateTimeProp
          && me.Member.DeclaringType == typeof(DateTime)
          && me.Expression is not null
        )
        {
            // Supported DateTime properties are listed in SparkTranslatableOperations.SupportedDateTimeProperties.
            // Update that file when adding new translations.
            var col = TranslateSubExpression(me.Expression, paramMap);
            return dateTimeProp.Name switch
            {
                nameof(DateTime.Year) => SparkFunctions.Year(col),
                nameof(DateTime.Month) => SparkFunctions.Month(col),
                nameof(DateTime.Day) => SparkFunctions.DayOfMonth(col),
                nameof(DateTime.Hour) => SparkFunctions.Hour(col),
                nameof(DateTime.Minute) => SparkFunctions.Minute(col),
                nameof(DateTime.Second) => SparkFunctions.Second(col),
                _ => throw new NotSupportedException(
                  $"DateTime property '{dateTimeProp.Name}' has no Spark translation. "
                    + "Supported: Year, Month, Day, Hour, Minute, Second."
                ),
            };
        }

        // Direct property on a lambda parameter: x.Age → df["Age"]
        if (me.Expression is ParameterExpression pe && paramMap.TryGetValue(pe, out var df))
        {
            var colName = ResolveColumnName(me.Member);
            return df[colName];
        }

        // Closure-captured variable or static field: evaluate to a constant
        var value = EvaluateConstant(me);
        return SparkFunctions.Lit(value);
    }

    /// <summary>
    /// Translates a binary expression into a Spark <see cref="Column"/> operation
    /// using Column operator overloads.
    /// </summary>
    private Column TranslateBinary(
      BinaryExpression be,
      Dictionary<ParameterExpression, DataFrame> paramMap
    )
    {
        // Null checks: x.Prop == null → IsNull(col), x.Prop != null → IsNotNull(col).
        // Spark's col == null produces col = NULL (always false in SQL three-valued logic),
        // so we must intercept before translating operands into Column objects.
        if (be.NodeType is ExpressionType.Equal or ExpressionType.NotEqual)
        {
            var isLeftNull = be.Left is ConstantExpression { Value: null };
            var isRightNull = be.Right is ConstantExpression { Value: null };

            if (isLeftNull || isRightNull)
            {
                var nonNullSide = isRightNull ? be.Left : be.Right;
                var col = TranslateSubExpression(nonNullSide, paramMap);
                return be.NodeType == ExpressionType.Equal ? SparkFunctions.IsNull(col) : col.IsNotNull();
            }
        }

        var left = TranslateSubExpression(be.Left, paramMap);
        var right = TranslateSubExpression(be.Right, paramMap);

        return be.NodeType switch
        {
            // Comparison
            ExpressionType.Equal => left == (object)right,
            ExpressionType.NotEqual => left != (object)right,
            ExpressionType.GreaterThan => left > (object)right,
            ExpressionType.GreaterThanOrEqual => left >= (object)right,
            ExpressionType.LessThan => left < (object)right,
            ExpressionType.LessThanOrEqual => left <= (object)right,

            // Arithmetic
            ExpressionType.Add => left + (object)right,
            ExpressionType.Subtract => left - (object)right,
            ExpressionType.Multiply => left * (object)right,
            ExpressionType.Divide => left / (object)right,
            ExpressionType.Modulo => left % (object)right,

            // Logical
            ExpressionType.AndAlso => left & right,
            ExpressionType.OrElse => left | right,

            _ => throw new NotSupportedException(
              $"Binary operator '{be.NodeType}' is not supported in Spark expressions."
            ),
        };
    }

    // ──────────────────────────────────────────────
    //  Projection translation → Column[]
    // ──────────────────────────────────────────────

    /// <summary>
    /// Translates a projection expression (<c>MemberInitExpression</c> or <c>NewExpression</c>)
    /// into an array of aliased Spark <see cref="Column"/> objects for <c>DataFrame.Select</c>.
    /// </summary>
    private Column[] TranslateProjection(
      Expression body,
      Dictionary<ParameterExpression, DataFrame> paramMap
    )
    {
        return body switch
        {
            // new OutputSchema { Name = x.Name, Age = x.Age }
            MemberInitExpression mie => TranslateMemberInit(mie, paramMap),

            // new OutputSchema(x.Name, x.Age) — positional constructor
            NewExpression ne => TranslateNewExpression(ne, paramMap),

            // Single expression (identity projection: x => x.Name)
            _ => [TranslateSubExpression(body, paramMap)],
        };
    }

    /// <summary>
    /// Translates <c>new T { Prop1 = expr1, Prop2 = expr2 }</c> into aliased columns.
    /// </summary>
    private Column[] TranslateMemberInit(
      MemberInitExpression mie,
      Dictionary<ParameterExpression, DataFrame> paramMap
    )
    {
        var columns = new Column[mie.Bindings.Count];

        for (var i = 0; i < mie.Bindings.Count; i++)
        {
            if (mie.Bindings[i] is not MemberAssignment assignment)
            {
                throw new NotSupportedException(
                  $"Only simple property assignments are supported in projections. "
                    + $"Got {mie.Bindings[i].BindingType} for '{mie.Bindings[i].Member.Name}'."
                );
            }

            var col = TranslateSubExpression(assignment.Expression, paramMap);
            var targetName = ResolveColumnName(assignment.Member);
            columns[i] = col.As(targetName);
        }

        return columns;
    }

    /// <summary>
    /// Translates <c>new T(expr1, expr2)</c> into aliased columns, matching constructor
    /// parameter positions to the <c>NewExpression.Members</c> metadata.
    /// </summary>
    private Column[] TranslateNewExpression(
      NewExpression ne,
      Dictionary<ParameterExpression, DataFrame> paramMap
    )
    {
        if (ne.Members is null || ne.Members.Count == 0)
        {
            throw new NotSupportedException(
              "Positional constructor projections require member metadata. "
                + "Use a record type or object initializer syntax."
            );
        }

        var columns = new Column[ne.Arguments.Count];

        for (var i = 0; i < ne.Arguments.Count; i++)
        {
            var col = TranslateSubExpression(ne.Arguments[i], paramMap);
            var targetName = ResolveColumnName(ne.Members[i]);
            columns[i] = col.As(targetName);
        }

        return columns;
    }
}
