using System.Linq.Expressions;
using Flowthru.DataFrames;

namespace Flowthru.Extensions.Spark.Tests;

[TestFixture]
[Category("ExpressionTree")]
public class WindowExpressionTests
{
  private TestFrameProvider _provider = null!;

  // Window specs shared across tests
  private static readonly FrameWindowSpec<StaffSchema> DeptWindow =
    FrameWindowSpec<StaffSchema>.PartitionBy(x => x.Department).OrderByDescending(x => x.Salary);

  private static readonly FrameWindowSpec<StaffSchema> HireWindow =
    FrameWindowSpec<StaffSchema>.Global.OrderBy(x => x.HireDate);

  [SetUp]
  public void SetUp()
  {
    _provider = new TestFrameProvider();
  }

  // ===================================================================
  //  SelectOver — top-level tree structure
  // ===================================================================

  [Test]
  public void SelectOver_ProducesMethodCallExpression_WithCorrectMethodName()
  {
    var frame = new TypedFrame<StaffSchema>(_provider);

    var result = frame.SelectOver((x, win) => new StaffRankedSchema
    {
      Name = x.Name,
      Department = x.Department,
      Salary = x.Salary,
      DeptRank = win.Rank(DeptWindow),
      RunningTotal = win.Sum(s => s.Salary, DeptWindow),
      PrevSalary = win.Lag(s => s.Salary, 1, DeptWindow),
    });

    var mce = result.Expression as MethodCallExpression;
    Assert.That(mce, Is.Not.Null);
    Assert.That(mce!.Method.Name, Is.EqualTo("SelectOver"));
  }

  [Test]
  public void SelectOver_ProducesCorrectResultType()
  {
    var frame = new TypedFrame<StaffSchema>(_provider);

    var result = frame.SelectOver((x, win) => new StaffRankedSchema
    {
      Name = x.Name,
      Department = x.Department,
      Salary = x.Salary,
      DeptRank = win.Rank(DeptWindow),
      RunningTotal = win.Sum(s => s.Salary, DeptWindow),
      PrevSalary = win.Lag(s => s.Salary, 1, DeptWindow),
    });

    Assert.That(result.ElementType, Is.EqualTo(typeof(StaffRankedSchema)));
  }

  [Test]
  public void SelectOver_HasTwoArguments_SourceAndQuotedSelector()
  {
    var frame = new TypedFrame<StaffSchema>(_provider);

    var result = frame.SelectOver((x, win) => new StaffRankedSchema
    {
      Name = x.Name,
      Department = x.Department,
      Salary = x.Salary,
      DeptRank = win.DenseRank(DeptWindow),
      RunningTotal = x.Salary,
      PrevSalary = null,
    });

    var mce = (MethodCallExpression)result.Expression;
    Assert.That(mce.Arguments, Has.Count.EqualTo(2));
  }

  [Test]
  public void SelectOver_SecondArgument_IsQuotedLambda()
  {
    var frame = new TypedFrame<StaffSchema>(_provider);

    var result = frame.SelectOver((x, win) => new StaffRankedSchema
    {
      Name = x.Name,
      Department = x.Department,
      Salary = x.Salary,
      DeptRank = win.RowNumber(DeptWindow),
      RunningTotal = x.Salary,
      PrevSalary = null,
    });

    var mce = (MethodCallExpression)result.Expression;
    var quoted = mce.Arguments[1] as UnaryExpression;
    Assert.That(quoted, Is.Not.Null);
    Assert.That(quoted!.NodeType, Is.EqualTo(ExpressionType.Quote));
    Assert.That(quoted.Operand, Is.InstanceOf<LambdaExpression>());
  }

  [Test]
  public void SelectOver_SelectorLambda_HasExactlyTwoParameters()
  {
    var frame = new TypedFrame<StaffSchema>(_provider);

    var result = frame.SelectOver((x, win) => new StaffRankedSchema
    {
      Name = x.Name,
      Department = x.Department,
      Salary = x.Salary,
      DeptRank = win.Rank(DeptWindow),
      RunningTotal = x.Salary,
      PrevSalary = null,
    });

    var mce = (MethodCallExpression)result.Expression;
    var lambda = (LambdaExpression)((UnaryExpression)mce.Arguments[1]).Operand;
    Assert.That(lambda.Parameters, Has.Count.EqualTo(2));
  }

  [Test]
  public void SelectOver_SelectorLambda_SecondParameter_IsWindowContextType()
  {
    var frame = new TypedFrame<StaffSchema>(_provider);

    var result = frame.SelectOver((x, win) => new StaffRankedSchema
    {
      Name = x.Name,
      Department = x.Department,
      Salary = x.Salary,
      DeptRank = win.Rank(DeptWindow),
      RunningTotal = x.Salary,
      PrevSalary = null,
    });

    var mce = (MethodCallExpression)result.Expression;
    var lambda = (LambdaExpression)((UnaryExpression)mce.Arguments[1]).Operand;
    Assert.That(
      lambda.Parameters[1].Type,
      Is.EqualTo(typeof(WindowContext<StaffSchema>))
    );
  }

  // ===================================================================
  //  Ranking functions — expression structure
  // ===================================================================

  [Test]
  [TestCase("Rank")]
  [TestCase("DenseRank")]
  [TestCase("RowNumber")]
  [TestCase("PercentRank")]
  [TestCase("CumeDist")]
  public void SelectOver_RankingFunction_BodyContainsMethodCallExpression_OnWinParameter(
    string methodName
  )
  {
    var frame = new TypedFrame<StaffSchema>(_provider);

    // Build the SelectOver call with the right method via a helper
    MethodCallExpression? windowCall = null;
    var mce = BuildRankingCall(frame, methodName, out windowCall);

    Assert.That(windowCall, Is.Not.Null);
    Assert.That(windowCall!.Method.Name, Is.EqualTo(methodName));
    Assert.That(windowCall.Object, Is.InstanceOf<ParameterExpression>());
    Assert.That(
      ((ParameterExpression)windowCall.Object!).Type,
      Is.EqualTo(typeof(WindowContext<StaffSchema>))
    );
  }

  [Test]
  [TestCase("Rank")]
  [TestCase("DenseRank")]
  [TestCase("RowNumber")]
  [TestCase("PercentRank")]
  [TestCase("CumeDist")]
  public void SelectOver_RankingFunction_HasOneArgument_TheSpec(string methodName)
  {
    var frame = new TypedFrame<StaffSchema>(_provider);
    BuildRankingCall(frame, methodName, out var windowCall);

    // Only argument: the FrameWindowSpec (no column selector)
    Assert.That(windowCall!.Arguments, Has.Count.EqualTo(1));
  }

  // ===================================================================
  //  Aggregate window functions — expression structure
  // ===================================================================

  [Test]
  public void SelectOver_Sum_BodyContainsMethodCallExpression_WithTwoArgs()
  {
    var frame = new TypedFrame<StaffSchema>(_provider);

    var result = frame.SelectOver((x, win) => new StaffRankedSchema
    {
      Name = x.Name,
      Department = x.Department,
      Salary = x.Salary,
      DeptRank = win.Rank(DeptWindow),
      RunningTotal = win.Sum(s => s.Salary, DeptWindow),
      PrevSalary = null,
    });

    var windowCall = ExtractWindowCall(result, nameof(StaffRankedSchema.RunningTotal));
    Assert.That(windowCall, Is.Not.Null);
    Assert.That(windowCall!.Method.Name, Is.EqualTo("Sum"));
    // arg[0] = column selector lambda, arg[1] = spec
    Assert.That(windowCall.Arguments, Has.Count.EqualTo(2));
  }

  [Test]
  public void SelectOver_Sum_FirstArgument_IsLambdaReferencingSalaryProperty()
  {
    var frame = new TypedFrame<StaffSchema>(_provider);

    var result = frame.SelectOver((x, win) => new StaffRankedSchema
    {
      Name = x.Name,
      Department = x.Department,
      Salary = x.Salary,
      DeptRank = win.Rank(DeptWindow),
      RunningTotal = win.Sum(s => s.Salary, DeptWindow),
      PrevSalary = null,
    });

    var windowCall = ExtractWindowCall(result, nameof(StaffRankedSchema.RunningTotal));
    var selectorLambda = ExtractLambdaFromArg(windowCall!.Arguments[0]);
    var memberBody = selectorLambda.Body as MemberExpression;

    Assert.That(memberBody, Is.Not.Null);
    Assert.That(memberBody!.Member.Name, Is.EqualTo(nameof(StaffSchema.Salary)));
  }

  // ===================================================================
  //  Offset functions — expression structure
  // ===================================================================

  [Test]
  public void SelectOver_Lag_HasThreeArguments_Selector_Offset_Spec()
  {
    var frame = new TypedFrame<StaffSchema>(_provider);

    var result = frame.SelectOver((x, win) => new StaffRankedSchema
    {
      Name = x.Name,
      Department = x.Department,
      Salary = x.Salary,
      DeptRank = win.Rank(DeptWindow),
      RunningTotal = x.Salary,
      PrevSalary = win.Lag(s => s.Salary, 1, DeptWindow),
    });

    var windowCall = ExtractWindowCall(result, nameof(StaffRankedSchema.PrevSalary));
    Assert.That(windowCall, Is.Not.Null);
    Assert.That(windowCall!.Method.Name, Is.EqualTo("Lag"));
    Assert.That(windowCall.Arguments, Has.Count.EqualTo(3));
  }

  [Test]
  public void SelectOver_Lag_SecondArgument_IsConstant_WithCorrectOffset()
  {
    var frame = new TypedFrame<StaffSchema>(_provider);

    var result = frame.SelectOver((x, win) => new StaffRankedSchema
    {
      Name = x.Name,
      Department = x.Department,
      Salary = x.Salary,
      DeptRank = win.Rank(DeptWindow),
      RunningTotal = x.Salary,
      PrevSalary = win.Lag(s => s.Salary, 2, DeptWindow),
    });

    var windowCall = ExtractWindowCall(result, nameof(StaffRankedSchema.PrevSalary));
    var offsetArg = windowCall!.Arguments[1] as ConstantExpression;
    Assert.That(offsetArg, Is.Not.Null);
    Assert.That(offsetArg!.Value, Is.EqualTo(2));
  }

  // ===================================================================
  //  Multi-window — different specs in the same projection
  // ===================================================================

  [Test]
  public void SelectOver_MultiWindow_DeptRank_And_HireOrder_UseDifferentSpecs()
  {
    var frame = new TypedFrame<StaffSchema>(_provider);

    var result = frame.SelectOver((x, win) => new StaffMultiWindowSchema
    {
      Name = x.Name,
      DeptRank = win.Rank(DeptWindow),
      HireOrder = win.RowNumber(HireWindow),
    });

    var mce = (MethodCallExpression)result.Expression;
    var lambda = (LambdaExpression)((UnaryExpression)mce.Arguments[1]).Operand;
    var mie = (MemberInitExpression)lambda.Body;

    var deptRankCall = GetBindingCall(mie, nameof(StaffMultiWindowSchema.DeptRank));
    var hireOrderCall = GetBindingCall(mie, nameof(StaffMultiWindowSchema.HireOrder));

    // Each call's last argument is the spec — they should be different references
    var deptSpecExpr = deptRankCall.Arguments[^1];
    var hireSpecExpr = hireOrderCall.Arguments[^1];

    // Both refer to FrameWindowSpec<StaffSchema> but different instances
    Assert.That(deptSpecExpr.Type, Is.EqualTo(typeof(FrameWindowSpec<StaffSchema>)));
    Assert.That(hireSpecExpr.Type, Is.EqualTo(typeof(FrameWindowSpec<StaffSchema>)));

    // The captured spec objects must be distinct instances
    var deptSpec = (FrameWindowSpec<StaffSchema>)EvaluateCapture(deptSpecExpr);
    var hireSpec = (FrameWindowSpec<StaffSchema>)EvaluateCapture(hireSpecExpr);
    Assert.That(deptSpec, Is.Not.SameAs(hireSpec));
  }

  [Test]
  public void SelectOver_MultiWindow_DeptSpec_HasPartitionByExpressions()
  {
    var spec = (FrameWindowSpec<StaffSchema>)EvaluateCapture(
      GetWindowCallFromField(
        new TypedFrame<StaffSchema>(_provider)
          .SelectOver((x, win) => new StaffMultiWindowSchema
          {
            Name = x.Name,
            DeptRank = win.Rank(DeptWindow),
            HireOrder = win.RowNumber(HireWindow),
          }),
        nameof(StaffMultiWindowSchema.DeptRank)
      ).Arguments[^1]
    );

    Assert.That(spec.PartitionByExpressions, Has.Count.EqualTo(1));
    Assert.That(spec.OrderByExpressions, Has.Count.EqualTo(1));
    Assert.That(spec.OrderByExpressions[0].Descending, Is.True);
  }

  [Test]
  public void SelectOver_MultiWindow_HireSpec_HasNoPartitionByExpressions()
  {
    var spec = (FrameWindowSpec<StaffSchema>)EvaluateCapture(
      GetWindowCallFromField(
        new TypedFrame<StaffSchema>(_provider)
          .SelectOver((x, win) => new StaffMultiWindowSchema
          {
            Name = x.Name,
            DeptRank = win.Rank(DeptWindow),
            HireOrder = win.RowNumber(HireWindow),
          }),
        nameof(StaffMultiWindowSchema.HireOrder)
      ).Arguments[^1]
    );

    Assert.That(spec.PartitionByExpressions, Has.Count.EqualTo(0));
    Assert.That(spec.OrderByExpressions, Has.Count.EqualTo(1));
    Assert.That(spec.OrderByExpressions[0].Descending, Is.False);
  }

  // ===================================================================
  //  FrameWindowSpec builder — spec construction
  // ===================================================================

  [Test]
  public void FrameWindowSpec_PartitionBy_StoresOnePartitionExpression()
  {
    var spec = FrameWindowSpec<StaffSchema>.PartitionBy(x => x.Department);

    Assert.That(spec.PartitionByExpressions, Has.Count.EqualTo(1));
    Assert.That(spec.OrderByExpressions, Has.Count.EqualTo(0));
  }

  [Test]
  public void FrameWindowSpec_ThenPartitionBy_AddssSecondPartitionExpression()
  {
    var spec = FrameWindowSpec<StaffSchema>
      .PartitionBy(x => x.Department)
      .ThenPartitionBy(x => x.Name);

    Assert.That(spec.PartitionByExpressions, Has.Count.EqualTo(2));
  }

  [Test]
  public void FrameWindowSpec_OrderBy_StoresAscendingExpression()
  {
    var spec = FrameWindowSpec<StaffSchema>.PartitionBy(x => x.Department).OrderBy(x => x.Salary);

    Assert.That(spec.OrderByExpressions, Has.Count.EqualTo(1));
    Assert.That(spec.OrderByExpressions[0].Descending, Is.False);
  }

  [Test]
  public void FrameWindowSpec_OrderByDescending_StoresDescendingExpression()
  {
    var spec = FrameWindowSpec<StaffSchema>
      .PartitionBy(x => x.Department)
      .OrderByDescending(x => x.Salary);

    Assert.That(spec.OrderByExpressions, Has.Count.EqualTo(1));
    Assert.That(spec.OrderByExpressions[0].Descending, Is.True);
  }

  [Test]
  public void FrameWindowSpec_IsImmutable_ChainProducesNewInstances()
  {
    var base1 = FrameWindowSpec<StaffSchema>.PartitionBy(x => x.Department);
    var extended = base1.OrderBy(x => x.Salary);

    // base1 is unchanged
    Assert.That(base1.OrderByExpressions, Has.Count.EqualTo(0));
    Assert.That(extended.OrderByExpressions, Has.Count.EqualTo(1));
    Assert.That(base1, Is.Not.SameAs(extended));
  }

  [Test]
  public void FrameWindowSpec_Global_HasNoPartitionOrOrderExpressions()
  {
    var spec = FrameWindowSpec<StaffSchema>.Global;

    Assert.That(spec.PartitionByExpressions, Has.Count.EqualTo(0));
    Assert.That(spec.OrderByExpressions, Has.Count.EqualTo(0));
  }

  // ===================================================================
  //  Helpers
  // ===================================================================

  private static MethodCallExpression BuildRankingCall(
    TypedFrame<StaffSchema> frame,
    string methodName,
    out MethodCallExpression? windowCall
  )
  {
    // Use Rank as default for all test variants — the method name itself is what we vary
    TypedFrame<StaffRankedSchema> result = methodName switch
    {
      "Rank" => frame.SelectOver((x, win) => new StaffRankedSchema
      {
        Name = x.Name,
        Department = x.Department,
        Salary = x.Salary,
        DeptRank = win.Rank(DeptWindow),
        RunningTotal = x.Salary,
        PrevSalary = null,
      }),
      "DenseRank" => frame.SelectOver((x, win) => new StaffRankedSchema
      {
        Name = x.Name,
        Department = x.Department,
        Salary = x.Salary,
        DeptRank = win.DenseRank(DeptWindow),
        RunningTotal = x.Salary,
        PrevSalary = null,
      }),
      "RowNumber" => frame.SelectOver((x, win) => new StaffRankedSchema
      {
        Name = x.Name,
        Department = x.Department,
        Salary = x.Salary,
        DeptRank = win.RowNumber(DeptWindow),
        RunningTotal = x.Salary,
        PrevSalary = null,
      }),
      "PercentRank" => frame.SelectOver((x, win) => new StaffRankedSchema
      {
        Name = x.Name,
        Department = x.Department,
        Salary = x.Salary,
        DeptRank = (long)win.PercentRank(DeptWindow),
        RunningTotal = x.Salary,
        PrevSalary = null,
      }),
      _ /* CumeDist */ => frame.SelectOver((x, win) => new StaffRankedSchema
      {
        Name = x.Name,
        Department = x.Department,
        Salary = x.Salary,
        DeptRank = (long)win.CumeDist(DeptWindow),
        RunningTotal = x.Salary,
        PrevSalary = null,
      }),
    };

    windowCall = ExtractWindowCall(result, nameof(StaffRankedSchema.DeptRank))
      ?? ExtractWindowCall(result, nameof(StaffRankedSchema.RunningTotal));
    return (MethodCallExpression)result.Expression;
  }

  private static MethodCallExpression? ExtractWindowCall<T>(
    TypedFrame<T> frame,
    string memberName
  )
  {
    var mce = (MethodCallExpression)frame.Expression;
    var lambda = (LambdaExpression)((UnaryExpression)mce.Arguments[1]).Operand;
    var mie = (MemberInitExpression)lambda.Body;
    var binding = mie.Bindings.OfType<MemberAssignment>().FirstOrDefault(b =>
      b.Member.Name == memberName
    );
    if (binding is null)
      return null;

    // May be wrapped in a Convert for numeric coercions
    var expr = binding.Expression;
    if (expr is UnaryExpression { NodeType: ExpressionType.Convert } ue)
      expr = ue.Operand;

    return expr as MethodCallExpression;
  }

  private static MethodCallExpression GetBindingCall(
    MemberInitExpression mie,
    string memberName
  )
  {
    var binding = mie.Bindings
      .OfType<MemberAssignment>()
      .Single(b => b.Member.Name == memberName);
    return (MethodCallExpression)binding.Expression;
  }

  private static MethodCallExpression GetWindowCallFromField<T>(
    TypedFrame<T> frame,
    string memberName
  )
  {
    var mce = (MethodCallExpression)frame.Expression;
    var lambda = (LambdaExpression)((UnaryExpression)mce.Arguments[1]).Operand;
    var mie = (MemberInitExpression)lambda.Body;
    return GetBindingCall(mie, memberName);
  }

  private static LambdaExpression ExtractLambdaFromArg(Expression arg)
  {
    if (arg is UnaryExpression { NodeType: ExpressionType.Quote } q)
      return (LambdaExpression)q.Operand;
    return (LambdaExpression)arg;
  }

  private static object EvaluateCapture(Expression expr)
  {
    var lambda = Expression.Lambda<Func<object>>(Expression.Convert(expr, typeof(object)));
    return lambda.Compile().Invoke();
  }
}
