using System.Linq.Expressions;
using System.Reflection;
using Flowthru.DataFrames;
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

  // Transient state active only during SelectOver projection traversal.
  // Set in TranslateSelectOver, cleared in its finally block.
  private ParameterExpression? _windowContextParam;
  private DataFrame? _windowSourceDf;

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
      throw new InvalidOperationException("Null TypedFrame constant encountered.");

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

  // ──────────────────────────────────────────────
  //  Easy: Distinct (deduplication)
  // ──────────────────────────────────────────────

  protected override object TranslateDistinct(MethodCallExpression node)
  {
    var sourceDf = (DataFrame)CompileExpression(node.Arguments[0]);
    return sourceDf.Distinct();
  }

  // ──────────────────────────────────────────────
  //  Easy: Union (row-wise concatenation)
  // ──────────────────────────────────────────────

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

    // Extract the key column name from the GroupBy sub-expression so that ctx.Key
    // projections in the result selector can be resolved to the correct column reference.
    string? keyColumnName = null;
    if (
      node.Arguments[0] is MethodCallExpression groupByCall
      && groupByCall.Method.Name == "GroupBy"
      && Unquote(groupByCall.Arguments[1]) is { Body: MemberExpression keyMember }
    )
    {
      keyColumnName = ResolveColumnName(keyMember.Member);
    }

    var grouped = (RelationalGroupedDataset)CompileExpression(node.Arguments[0]);
    var resultLambda = Unquote(node.Arguments[1]);

    // Build two separate lists:
    //   aggCols   – passed to Agg(); contains only aggregate function calls (Avg, Sum, …).
    //               ctx.Key columns are intentionally excluded: Spark already includes
    //               every GroupBy key column in the Agg() output schema, so adding them
    //               again would produce a duplicate-column error.
    //   selectCols – passed to a final Select() that reassembles all output columns in
    //               the correct order and with the target schema aliases.
    var (aggCols, selectCols) = TranslateAggregateProjection(
      resultLambda.Body, resultLambda.Parameters[0], keyColumnName
    );

    if (aggCols.Length == 0)
      throw new NotSupportedException(
        "Aggregate result selector must contain at least one aggregate function call "
          + "(Avg, Sum, Count, Min, Max)."
      );

    var df = grouped.Agg(aggCols[0], aggCols.Skip(1).ToArray());
    return df.Select(selectCols);
  }

  /// <summary>
  /// Translates the body of an Aggregate result selector into two column arrays:
  /// the aggregate columns for the <c>Agg()</c> call and the select columns
  /// for the final <c>Select()</c> reordering projection.
  /// </summary>
  private (Column[] aggCols, Column[] selectCols) TranslateAggregateProjection(
    Expression body,
    ParameterExpression contextParam,
    string? keyColumnName = null
  )
  {
    return body switch
    {
      MemberInitExpression mie => TranslateAggregateMemberInit(mie, contextParam, keyColumnName),
      NewExpression ne => TranslateAggregateNewExpression(ne, contextParam, keyColumnName),
      _ => throw new NotSupportedException(
        "Aggregate result selector must be a member initializer or positional constructor."
      ),
    };
  }

  private (Column[] aggCols, Column[] selectCols) TranslateAggregateMemberInit(
    MemberInitExpression mie,
    ParameterExpression contextParam,
    string? keyColumnName = null
  )
  {
    var aggCols = new List<Column>();
    var selectCols = new Column[mie.Bindings.Count];

    for (var i = 0; i < mie.Bindings.Count; i++)
    {
      if (mie.Bindings[i] is not MemberAssignment assignment)
        throw new NotSupportedException(
          $"Only simple property assignments are supported in aggregate projections. "
            + $"Got {mie.Bindings[i].BindingType} for '{mie.Bindings[i].Member.Name}'."
        );

      var targetName = ResolveColumnName(assignment.Member);

      if (IsKeyExpression(assignment.Expression, contextParam))
      {
        // ctx.Key: the GroupBy key column is already present in the Agg() output.
        // Add it to selectCols with the correct target alias, but do NOT add it to
        // aggCols — passing it to Agg() would create a duplicate column.
        if (keyColumnName == null)
          throw new NotSupportedException(
            "ctx.Key projection requires a simple property-access GroupBy key selector "
              + "(e.g. GroupBy(x => x.ShuttleType))."
          );
        selectCols[i] = SparkFunctions.Col(keyColumnName).As(targetName);
      }
      else
      {
        var aggCol = TranslateAggregateExpression(assignment.Expression, contextParam, keyColumnName);
        aggCols.Add(aggCol.As(targetName));
        selectCols[i] = SparkFunctions.Col(targetName);
      }
    }

    return (aggCols.ToArray(), selectCols);
  }

  private (Column[] aggCols, Column[] selectCols) TranslateAggregateNewExpression(
    NewExpression ne,
    ParameterExpression contextParam,
    string? keyColumnName = null
  )
  {
    if (ne.Members is null || ne.Members.Count == 0)
      throw new NotSupportedException(
        "Positional constructor projections require member metadata in aggregate selectors."
      );

    var aggCols = new List<Column>();
    var selectCols = new Column[ne.Arguments.Count];

    for (var i = 0; i < ne.Arguments.Count; i++)
    {
      var targetName = ResolveColumnName(ne.Members[i]);

      if (IsKeyExpression(ne.Arguments[i], contextParam))
      {
        if (keyColumnName == null)
          throw new NotSupportedException(
            "ctx.Key projection requires a simple property-access GroupBy key selector "
              + "(e.g. GroupBy(x => x.ShuttleType))."
          );
        selectCols[i] = SparkFunctions.Col(keyColumnName).As(targetName);
      }
      else
      {
        var aggCol = TranslateAggregateExpression(ne.Arguments[i], contextParam, keyColumnName);
        aggCols.Add(aggCol.As(targetName));
        selectCols[i] = SparkFunctions.Col(targetName);
      }
    }

    return (aggCols.ToArray(), selectCols);
  }

  /// <summary>Returns true when <paramref name="expr"/> is a <c>ctx.Key</c> member access.</summary>
  private static bool IsKeyExpression(Expression expr, ParameterExpression contextParam) =>
    expr is MemberExpression { Member.Name: "Key" } me
    && me.Expression is ParameterExpression kpe
    && kpe == contextParam;

  /// <summary>
  /// Translates a single expression inside an aggregate result selector into a Spark Column.
  /// Handles AggregationContext method calls (Avg, Sum, Count, Min, Max) and Key access.
  /// </summary>
  private Column TranslateAggregateExpression(
    Expression expr,
    ParameterExpression contextParam,
    string? keyColumnName = null
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

    // ctx.Key — the group-by key column is already part of the grouped dataset's output;
    // emit a plain column reference using the name extracted from the GroupBy selector.
    if (
      expr is MemberExpression { Member.Name: "Key" } me
      && me.Expression is ParameterExpression kpe
      && kpe == contextParam
    )
    {
      if (keyColumnName == null)
        throw new NotSupportedException(
          "ctx.Key projection requires a simple property-access GroupBy key selector "
            + "(e.g. GroupBy(x => x.ShuttleType)). Composite or computed keys are not supported."
        );

      return SparkFunctions.Col(keyColumnName);
    }

    throw new NotSupportedException(
      $"Expression '{expr}' in aggregate result selector is not supported. "
        + "Only AggregationContext method calls (Avg, Sum, Count, Min, Max) and ctx.Key are valid."
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

    // Unwrap any numeric conversion cast, e.g. (double)x.PassengerCapacity
    var body = lambda.Body is UnaryExpression { NodeType: ExpressionType.Convert } conv
      ? conv.Operand
      : lambda.Body;

    if (body is MemberExpression me)
      return ResolveColumnName(me.Member);

    throw new NotSupportedException(
      "Aggregate column selectors must be simple property access expressions (x => x.Property) "
        + "or a numeric cast of one (x => (double)x.Property)."
    );
  }

  // ──────────────────────────────────────────────
  //  Hard: SelectOver (windowed projection)
  // ──────────────────────────────────────────────

  protected override object TranslateSelectOver(MethodCallExpression node)
  {
    // args[0] = source, args[1] = quoted (TSource, WindowContext<TSource>) => TResult
    var sourceDf = (DataFrame)CompileExpression(node.Arguments[0]);
    var selector = Unquote(node.Arguments[1]); // 2-parameter lambda

    // selector.Parameters[0] = row (TSource)  → maps to sourceDf
    // selector.Parameters[1] = win (WindowContext<TSource>) → intercepted via _windowContextParam
    _windowContextParam = selector.Parameters[1];
    _windowSourceDf = sourceDf;

    try
    {
      var paramMap = new Dictionary<ParameterExpression, DataFrame>
      {
        [selector.Parameters[0]] = sourceDf,
      };
      var columns = TranslateProjection(selector.Body, paramMap);
      return sourceDf.Select(columns);
    }
    finally
    {
      _windowContextParam = null;
      _windowSourceDf = null;
    }
  }

  /// <summary>
  /// Translates a <see cref="WindowContext{TSource}"/> method call into a Spark window
  /// <see cref="Column"/> expression (<c>function().Over(windowSpec)</c>).
  /// </summary>
  private Column TranslateWindowFunction(MethodCallExpression mce)
  {
    // FrameWindowSpec is always the last argument in every WindowContext method.
    var spec = (IFrameWindowSpec)EvaluateConstant(mce.Arguments[^1])!;
    var windowSpec = BuildSparkWindowSpec(spec);

    return mce.Method.Name switch
    {
      // Ranking — no column selector
      "RowNumber"   => SparkFunctions.RowNumber().Over(windowSpec),
      "Rank"        => SparkFunctions.Rank().Over(windowSpec),
      "DenseRank"   => SparkFunctions.DenseRank().Over(windowSpec),
      "CumeDist"    => SparkFunctions.CumeDist().Over(windowSpec),
      "PercentRank" => SparkFunctions.PercentRank().Over(windowSpec),
      "Count"       => SparkFunctions.Count(SparkFunctions.Lit(1)).Over(windowSpec),

      // Aggregate over window — column selector is arg[0]
      "Sum" => SparkFunctions.Sum(ExtractWindowColumnName(mce.Arguments[0])).Over(windowSpec),
      "Avg" => SparkFunctions.Avg(ExtractWindowColumnName(mce.Arguments[0])).Over(windowSpec),
      "Max" => SparkFunctions.Max(ExtractWindowColumnName(mce.Arguments[0])).Over(windowSpec),
      "Min" => SparkFunctions.Min(ExtractWindowColumnName(mce.Arguments[0])).Over(windowSpec),

      // Offset functions — column selector is arg[0], offset is arg[1]
      "Lag" => SparkFunctions
        .Lag(
          ExtractWindowColumnName(mce.Arguments[0]),
          (int)((ConstantExpression)mce.Arguments[1]).Value!
        )
        .Over(windowSpec),

      "Lead" => SparkFunctions
        .Lead(
          ExtractWindowColumnName(mce.Arguments[0]),
          (int)((ConstantExpression)mce.Arguments[1]).Value!
        )
        .Over(windowSpec),

      _ => throw new NotSupportedException(
        $"Window function '{mce.Method.Name}' is not supported in Spark window expressions."
      ),
    };
  }

  /// <summary>
  /// Builds a Spark <see cref="WindowSpec"/> from a framework-agnostic
  /// <see cref="IFrameWindowSpec"/>.
  /// </summary>
  private WindowSpec BuildSparkWindowSpec(IFrameWindowSpec spec)
  {
    var sourceDf = _windowSourceDf!;

    Column TranslateSpecColumn(LambdaExpression lambda, bool descending)
    {
      var specParamMap = new Dictionary<ParameterExpression, DataFrame>
      {
        [lambda.Parameters[0]] = sourceDf,
      };
      var col = TranslateSubExpression(lambda.Body, specParamMap);
      return descending ? col.Desc() : col;
    }

    var partitionCols = spec.PartitionByExpressions
      .Select(lambda => TranslateSpecColumn(lambda, false))
      .ToArray();

    var orderCols = spec.OrderByExpressions
      .Select(t => TranslateSpecColumn(t.KeySelector, t.Descending))
      .ToArray();

    return (partitionCols.Length, orderCols.Length) switch
    {
      (> 0, > 0) => Window.PartitionBy(partitionCols).OrderBy(orderCols),
      (> 0, _  ) => Window.PartitionBy(partitionCols),
      (_,   > 0) => Window.OrderBy(orderCols),
      _          => Window.PartitionBy(), // truly global: no partition, no order
    };
  }

  /// <summary>
  /// Extracts the column name string from the selector lambda argument of a window
  /// aggregate or offset function (e.g., the <c>x =&gt; x.Salary</c> in
  /// <c>win.Sum(x =&gt; x.Salary, spec)</c>).
  /// </summary>
  private static string ExtractWindowColumnName(Expression lambdaExpr)
  {
    var lambda = lambdaExpr is UnaryExpression { NodeType: ExpressionType.Quote } q
      ? (LambdaExpression)q.Operand
      : (LambdaExpression)lambdaExpr;

    if (lambda.Body is MemberExpression me)
      return ResolveColumnName(me.Member);

    throw new NotSupportedException(
      "Window column selectors must be simple property access expressions (x => x.Property)."
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

  private static readonly HashSet<string> StringMethodNames =
  [
    nameof(string.Replace),
    nameof(string.Contains),
    nameof(string.StartsWith),
    nameof(string.EndsWith),
    nameof(string.ToUpper),
    nameof(string.ToLower),
    nameof(string.Trim),
    nameof(string.TrimStart),
    nameof(string.TrimEnd),
    nameof(string.Substring),
  ];

  private static readonly HashSet<string> MathMethodNames =
  [
    nameof(Math.Round),
    nameof(Math.Abs),
    nameof(Math.Floor),
    nameof(Math.Ceiling),
  ];

  /// <summary>
  /// Routes a method call in a sub-expression to the appropriate translator.
  /// </summary>
  private Column TranslateMethodCallExpression(
    MethodCallExpression mce,
    Dictionary<ParameterExpression, DataFrame> paramMap
  )
  {
    // Window context method calls — only active during SelectOver projection traversal.
    if (
      _windowContextParam is not null
      && mce.Object is ParameterExpression wpe
      && wpe == _windowContextParam
    )
    {
      return TranslateWindowFunction(mce);
    }

    if (mce.Method.DeclaringType == typeof(string) && StringMethodNames.Contains(mce.Method.Name))
      return TranslateStringMethod(mce, paramMap);

    if (mce.Method.DeclaringType == typeof(Math) && MathMethodNames.Contains(mce.Method.Name))
      return TranslateMathMethod(mce, paramMap);

    throw new NotSupportedException(
      $"Method '{mce.Method.DeclaringType?.Name}.{mce.Method.Name}' "
        + "is not supported in Spark sub-expressions. "
        + "Supported: string (Replace, Contains, StartsWith, EndsWith, ToUpper, ToLower, "
        + "Trim, TrimStart, TrimEnd, Substring) and Math (Round, Abs, Floor, Ceiling)."
    );
  }

  /// <summary>
  /// Translates <c>string</c> instance methods to their Spark <c>Column</c> equivalents.
  /// </summary>
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

      // s.TrimStart() → Ltrim(col)
      nameof(string.TrimStart) => SparkFunctions.Ltrim(col),

      // s.TrimEnd() → Rtrim(col)
      nameof(string.TrimEnd) => SparkFunctions.Rtrim(col),

      // s.Substring(startIndex, length) → Substring(col, startIndex + 1, length)
      // Note: C# Substring uses 0-based indexing; Spark Substring uses 1-based.
      nameof(string.Substring) when mce.Arguments.Count == 2 => SparkFunctions.Substring(
        col,
        (int)((ConstantExpression)mce.Arguments[0]).Value! + 1,
        (int)((ConstantExpression)mce.Arguments[1]).Value!
      ),

      // s.Contains(x) → col.Contains(x)
      nameof(string.Contains) => col.Contains(TranslateSubExpression(mce.Arguments[0], paramMap)),

      // s.StartsWith(x) → col.StartsWith(x)
      nameof(string.StartsWith) => col.StartsWith(
        TranslateSubExpression(mce.Arguments[0], paramMap)
      ),

      // s.EndsWith(x) → col.EndsWith(x)
      nameof(string.EndsWith) => col.EndsWith(TranslateSubExpression(mce.Arguments[0], paramMap)),

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
    // Math methods are static; the column is always the first argument.
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
      // Math.Round(x, digits) → Round(col, digits)
      // Math.Round with MidpointRounding is not supported — Spark has no equivalent.
      nameof(Math.Round) when mce.Arguments.Count == 1 => SparkFunctions.Round(col),
      nameof(Math.Round) when mce.Arguments.Count == 2
        && mce.Arguments[1].Type == typeof(int) => SparkFunctions.Round(
          col,
          (int)((ConstantExpression)mce.Arguments[1]).Value!
        ),

      _ => throw new NotSupportedException(
        $"Math.{mce.Method.Name} overload (arg count: {mce.Arguments.Count}) "
          + "has no Spark translation."
      ),
    };
  }

  // ──────────────────────────────────────────────
  //  Member access translation (columns + string.Length + DateTime parts)
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

    // DateTime property access on a column → Spark date/time functions.
    // The column is the inner expression (x.CreatedAt.Year → col = x.CreatedAt).
    if (
      me.Member is PropertyInfo
      && me.Member.DeclaringType == typeof(DateTime)
      && me.Expression is not null
    )
    {
      var innerCol = TranslateSubExpression(me.Expression, paramMap);
      return me.Member.Name switch
      {
        nameof(DateTime.Year) => SparkFunctions.Year(innerCol),
        nameof(DateTime.Month) => SparkFunctions.Month(innerCol),
        nameof(DateTime.Day) => SparkFunctions.DayOfMonth(innerCol),
        nameof(DateTime.Hour) => SparkFunctions.Hour(innerCol),
        nameof(DateTime.Minute) => SparkFunctions.Minute(innerCol),
        nameof(DateTime.Second) => SparkFunctions.Second(innerCol),
        nameof(DateTime.DayOfWeek) => SparkFunctions.DayOfWeek(innerCol),
        nameof(DateTime.DayOfYear) => SparkFunctions.DayOfYear(innerCol),
        _ => throw new NotSupportedException(
          $"DateTime property '{me.Member.Name}' has no Spark translation."
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
    // Null-check special cases: x.Prop == null / x.Prop != null.
    // Spark's == operator emits `col = NULL` (always false); IsNull/IsNotNull is correct.
    if (be.NodeType is ExpressionType.Equal or ExpressionType.NotEqual)
    {
      var isRightNull = be.Right is ConstantExpression { Value: null };
      var isLeftNull = be.Left is ConstantExpression { Value: null };
      if (isRightNull || isLeftNull)
      {
        var colExpr = isRightNull ? be.Left : be.Right;
        var col = TranslateSubExpression(colExpr, paramMap);
        return be.NodeType == ExpressionType.Equal ? col.IsNull() : col.IsNotNull();
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
