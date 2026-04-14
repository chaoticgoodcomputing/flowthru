using System.Linq.Expressions;
using Flowthru.DataFrames;

namespace Flowthru.Extensions.Spark.Tests;

[TestFixture]
[Category("ExpressionTree")]
public class ConditionalExpressionTests
{
  private TestFrameProvider _provider = null!;

  [SetUp]
  public void SetUp()
  {
    _provider = new TestFrameProvider();
  }

  // ===================================================================
  //  Conditional expression in Select projection
  // ===================================================================

  [Test]
  public void Select_WithTernaryInBody_ContainsConditionalExpression()
  {
    var frame = new TypedFrame<PersonSchema>(_provider);

    // x.Age >= 18 ? "adult" : "minor"
    var result = frame.Select(x => new NameOnlySchema { Name = x.Age >= 18 ? "adult" : "minor" });

    var mce = (MethodCallExpression)result.Expression;
    var lambda = (LambdaExpression)((UnaryExpression)mce.Arguments[1]).Operand;
    var mie = (MemberInitExpression)lambda.Body;
    var binding = mie.Bindings.OfType<MemberAssignment>().Single();

    Assert.That(binding.Expression.NodeType, Is.EqualTo(ExpressionType.Conditional));
  }

  [Test]
  public void Select_WithTernary_ConditionalExpression_HasCorrectTestNodeType()
  {
    var frame = new TypedFrame<PersonSchema>(_provider);

    var result = frame.Select(x => new NameOnlySchema { Name = x.Age >= 18 ? "adult" : "minor" });

    var conditional = ExtractConditionalFromSelect(result);

    // Test: x.Age >= 18 — binary GreaterThanOrEqual
    Assert.That(conditional.Test.NodeType, Is.EqualTo(ExpressionType.GreaterThanOrEqual));
  }

  [Test]
  public void Select_WithTernary_IfTrue_IsStringConstant()
  {
    var frame = new TypedFrame<PersonSchema>(_provider);

    var result = frame.Select(x => new NameOnlySchema { Name = x.Age >= 18 ? "adult" : "minor" });

    var conditional = ExtractConditionalFromSelect(result);

    var ifTrueConst = conditional.IfTrue as ConstantExpression;
    Assert.That(ifTrueConst, Is.Not.Null);
    Assert.That(ifTrueConst!.Value, Is.EqualTo("adult"));
  }

  [Test]
  public void Select_WithTernary_IfFalse_IsStringConstant()
  {
    var frame = new TypedFrame<PersonSchema>(_provider);

    var result = frame.Select(x => new NameOnlySchema { Name = x.Age >= 18 ? "adult" : "minor" });

    var conditional = ExtractConditionalFromSelect(result);

    var ifFalseConst = conditional.IfFalse as ConstantExpression;
    Assert.That(ifFalseConst, Is.Not.Null);
    Assert.That(ifFalseConst!.Value, Is.EqualTo("minor"));
  }

  [Test]
  public void Where_WithTernaryConvertedToBool_ContainsConditionalExpression()
  {
    // Although unusual, a conditional can appear inside a where predicate
    // via a bool expression:  x.IsActive ? x.Age > 18 : false
    var frame = new TypedFrame<PersonSchema>(_provider);

    var result = frame.Where(x => x.IsActive ? x.Age > 18 : false);

    var mce = (MethodCallExpression)result.Expression;
    var lambda = (LambdaExpression)((UnaryExpression)mce.Arguments[1]).Operand;

    Assert.That(lambda.Body.NodeType, Is.EqualTo(ExpressionType.Conditional));
  }

  // ===================================================================
  //  Helpers
  // ===================================================================

  private static ConditionalExpression ExtractConditionalFromSelect(
    TypedFrame<NameOnlySchema> frame
  )
  {
    var mce = (MethodCallExpression)frame.Expression;
    var lambda = (LambdaExpression)((UnaryExpression)mce.Arguments[1]).Operand;
    var mie = (MemberInitExpression)lambda.Body;
    var binding = mie.Bindings.OfType<MemberAssignment>().Single();
    return (ConditionalExpression)binding.Expression;
  }
}
