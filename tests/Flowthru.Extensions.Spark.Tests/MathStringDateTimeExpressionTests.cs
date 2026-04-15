using System.Linq.Expressions;
using Flowthru.Misc.DataFrames;

namespace Flowthru.Extensions.Spark.Tests;

[TestFixture]
[Category("ExpressionTree")]
public class MathStringDateTimeExpressionTests
{
  private TestFrameProvider _provider = null!;

  [SetUp]
  public void SetUp()
  {
    _provider = new TestFrameProvider();
  }

  // ===================================================================
  //  Math methods
  // ===================================================================

  [Test]
  public void Select_WithMathAbs_BodyContainsMethodCall_Named_Abs()
  {
    var frame = new TypedFrame<MeasurementSchema>(_provider);

    var result = frame.Where(x => Math.Abs(x.Temperature) > 0);

    var mce = (MethodCallExpression)result.Expression;
    var lambda = (LambdaExpression)((UnaryExpression)mce.Arguments[1]).Operand;
    var binary = (BinaryExpression)lambda.Body;
    var absCall = binary.Left as MethodCallExpression;

    Assert.That(absCall, Is.Not.Null);
    Assert.That(absCall!.Method.Name, Is.EqualTo(nameof(Math.Abs)));
    Assert.That(absCall.Method.DeclaringType, Is.EqualTo(typeof(Math)));
  }

  [Test]
  public void Select_WithMathFloor_BodyContainsMethodCall_Named_Floor()
  {
    var frame = new TypedFrame<MeasurementSchema>(_provider);

    var result = frame.Where(x => Math.Floor(x.Value) > 0);

    var mce = (MethodCallExpression)result.Expression;
    var lambda = (LambdaExpression)((UnaryExpression)mce.Arguments[1]).Operand;
    var binary = (BinaryExpression)lambda.Body;
    var call = binary.Left as MethodCallExpression;

    Assert.That(call!.Method.Name, Is.EqualTo(nameof(Math.Floor)));
  }

  [Test]
  public void Select_WithMathCeiling_BodyContainsMethodCall_Named_Ceiling()
  {
    var frame = new TypedFrame<MeasurementSchema>(_provider);

    var result = frame.Where(x => Math.Ceiling(x.Value) > 0);

    var mce = (MethodCallExpression)result.Expression;
    var lambda = (LambdaExpression)((UnaryExpression)mce.Arguments[1]).Operand;
    var binary = (BinaryExpression)lambda.Body;
    var call = binary.Left as MethodCallExpression;

    Assert.That(call!.Method.Name, Is.EqualTo(nameof(Math.Ceiling)));
  }

  [Test]
  public void Select_WithMathRoundOneArg_BodyContainsMethodCall_Named_Round()
  {
    var frame = new TypedFrame<MeasurementSchema>(_provider);

    var result = frame.Where(x => Math.Round(x.Value) > 0);

    var mce = (MethodCallExpression)result.Expression;
    var lambda = (LambdaExpression)((UnaryExpression)mce.Arguments[1]).Operand;
    var binary = (BinaryExpression)lambda.Body;
    var call = binary.Left as MethodCallExpression;

    Assert.That(call!.Method.Name, Is.EqualTo(nameof(Math.Round)));
    Assert.That(call.Arguments, Has.Count.EqualTo(1));
  }

  [Test]
  public void Select_WithMathRoundTwoArgs_SecondArg_IsCorrectScale()
  {
    var frame = new TypedFrame<MeasurementSchema>(_provider);

    var result = frame.Where(x => Math.Round(x.Value, 2) > 0);

    var mce = (MethodCallExpression)result.Expression;
    var lambda = (LambdaExpression)((UnaryExpression)mce.Arguments[1]).Operand;
    var binary = (BinaryExpression)lambda.Body;
    var call = binary.Left as MethodCallExpression;

    Assert.That(call!.Method.Name, Is.EqualTo(nameof(Math.Round)));
    Assert.That(call.Arguments, Has.Count.EqualTo(2));
    var scale = call.Arguments[1] as ConstantExpression;
    Assert.That(scale!.Value, Is.EqualTo(2));
  }

  // ===================================================================
  //  New string methods: TrimStart / TrimEnd / Substring
  // ===================================================================

  [Test]
  public void Where_WithTrimStart_BodyContainsMethodCall_Named_TrimStart()
  {
    var frame = new TypedFrame<ProductSchema>(_provider);

    var result = frame.Where(x => x.Sku.TrimStart() == "BULK");

    var mce = (MethodCallExpression)result.Expression;
    var lambda = (LambdaExpression)((UnaryExpression)mce.Arguments[1]).Operand;
    var binary = (BinaryExpression)lambda.Body;
    var call = binary.Left as MethodCallExpression;

    Assert.That(call, Is.Not.Null);
    Assert.That(call!.Method.Name, Is.EqualTo(nameof(string.TrimStart)));
    Assert.That(call.Method.DeclaringType, Is.EqualTo(typeof(string)));
  }

  [Test]
  public void Where_WithTrimEnd_BodyContainsMethodCall_Named_TrimEnd()
  {
    var frame = new TypedFrame<ProductSchema>(_provider);

    var result = frame.Where(x => x.Sku.TrimEnd() == "BULK");

    var mce = (MethodCallExpression)result.Expression;
    var lambda = (LambdaExpression)((UnaryExpression)mce.Arguments[1]).Operand;
    var binary = (BinaryExpression)lambda.Body;
    var call = binary.Left as MethodCallExpression;

    Assert.That(call!.Method.Name, Is.EqualTo(nameof(string.TrimEnd)));
  }

  [Test]
  public void Where_WithSubstring_BodyContainsMethodCall_Named_Substring_WithTwoArgs()
  {
    var frame = new TypedFrame<ProductSchema>(_provider);

    var result = frame.Where(x => x.Sku.Substring(0, 3) == "SKU");

    var mce = (MethodCallExpression)result.Expression;
    var lambda = (LambdaExpression)((UnaryExpression)mce.Arguments[1]).Operand;
    var binary = (BinaryExpression)lambda.Body;
    var call = binary.Left as MethodCallExpression;

    Assert.That(call!.Method.Name, Is.EqualTo(nameof(string.Substring)));
    Assert.That(call.Arguments, Has.Count.EqualTo(2));

    var startArg = call.Arguments[0] as ConstantExpression;
    var lenArg = call.Arguments[1] as ConstantExpression;
    Assert.That(startArg!.Value, Is.EqualTo(0));
    Assert.That(lenArg!.Value, Is.EqualTo(3));
  }

  // ===================================================================
  //  DateTime property access
  // ===================================================================

  [Test]
  public void Where_WithDateTimeYear_BodyContainsMemberAccess_Named_Year()
  {
    var frame = new TypedFrame<OrderSchema>(_provider);

    var result = frame.Where(x => x.OrderDate.Year == 2024);

    var mce = (MethodCallExpression)result.Expression;
    var lambda = (LambdaExpression)((UnaryExpression)mce.Arguments[1]).Operand;
    var binary = (BinaryExpression)lambda.Body;

    var me = binary.Left as MemberExpression;
    Assert.That(me, Is.Not.Null);
    Assert.That(me!.Member.Name, Is.EqualTo(nameof(DateTime.Year)));
    Assert.That(me.Member.DeclaringType, Is.EqualTo(typeof(DateTime)));
  }

  [Test]
  public void Where_WithDateTimeMonth_BodyContainsMemberAccess_Named_Month()
  {
    var frame = new TypedFrame<OrderSchema>(_provider);

    var result = frame.Where(x => x.OrderDate.Month == 6);

    var mce = (MethodCallExpression)result.Expression;
    var lambda = (LambdaExpression)((UnaryExpression)mce.Arguments[1]).Operand;
    var binary = (BinaryExpression)lambda.Body;
    var me = binary.Left as MemberExpression;

    Assert.That(me!.Member.Name, Is.EqualTo(nameof(DateTime.Month)));
  }

  [Test]
  public void Where_WithDateTimeDay_BodyContainsMemberAccess_Named_Day()
  {
    var frame = new TypedFrame<OrderSchema>(_provider);

    var result = frame.Where(x => x.OrderDate.Day == 15);

    var mce = (MethodCallExpression)result.Expression;
    var lambda = (LambdaExpression)((UnaryExpression)mce.Arguments[1]).Operand;
    var binary = (BinaryExpression)lambda.Body;
    var me = binary.Left as MemberExpression;

    Assert.That(me!.Member.Name, Is.EqualTo(nameof(DateTime.Day)));
  }

  [Test]
  public void Where_WithDateTimeHour_BodyContainsMemberAccess_Named_Hour()
  {
    var frame = new TypedFrame<OrderSchema>(_provider);

    var result = frame.Where(x => x.OrderDate.Hour >= 9);

    var mce = (MethodCallExpression)result.Expression;
    var lambda = (LambdaExpression)((UnaryExpression)mce.Arguments[1]).Operand;
    var binary = (BinaryExpression)lambda.Body;
    var me = binary.Left as MemberExpression;

    Assert.That(me!.Member.Name, Is.EqualTo(nameof(DateTime.Hour)));
  }
}
