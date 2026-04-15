using System.Linq.Expressions;
using Flowthru.Misc.DataFrames;

namespace Flowthru.Extensions.Spark.Tests;

[TestFixture]
[Category("ExpressionTree")]
public class NullCheckExpressionTests
{
    private TestFrameProvider _provider = null!;

    [SetUp]
    public void SetUp()
    {
        _provider = new TestFrameProvider();
    }

    // ===================================================================
    //  x.Prop == null  →  IsNull-shaped expression
    // ===================================================================

    [Test]
    public void Where_NullEqualityCheck_ProducesEqualExpression()
    {
        var frame = new TypedFrame<OrderSchema>(_provider);

        var result = frame.Where(x => x.Region == null);

        var mce = (MethodCallExpression)result.Expression;
        var lambda = (LambdaExpression)((UnaryExpression)mce.Arguments[1]).Operand;

        // The body represents == null — should be an Equal binary expression
        Assert.That(lambda.Body.NodeType, Is.EqualTo(ExpressionType.Equal));
    }

    [Test]
    public void Where_NullEqualityCheck_RightSide_IsNullConstant()
    {
        var frame = new TypedFrame<OrderSchema>(_provider);

        var result = frame.Where(x => x.Region == null);

        var mce = (MethodCallExpression)result.Expression;
        var lambda = (LambdaExpression)((UnaryExpression)mce.Arguments[1]).Operand;
        var binary = (BinaryExpression)lambda.Body;

        var right = binary.Right as ConstantExpression;
        Assert.That(right, Is.Not.Null);
        Assert.That(right!.Value, Is.Null);
    }

    [Test]
    public void Where_NullEqualityCheck_LeftSide_IsMemberAccess_ForNullableProperty()
    {
        var frame = new TypedFrame<OrderSchema>(_provider);

        var result = frame.Where(x => x.Region == null);

        var mce = (MethodCallExpression)result.Expression;
        var lambda = (LambdaExpression)((UnaryExpression)mce.Arguments[1]).Operand;
        var binary = (BinaryExpression)lambda.Body;

        var left = binary.Left as MemberExpression;
        Assert.That(left, Is.Not.Null);
        Assert.That(left!.Member.Name, Is.EqualTo(nameof(OrderSchema.Region)));
    }

    // ===================================================================
    //  x.Prop != null  →  IsNotNull-shaped expression
    // ===================================================================

    [Test]
    public void Where_NullInequalityCheck_ProducesNotEqualExpression()
    {
        var frame = new TypedFrame<OrderSchema>(_provider);

        var result = frame.Where(x => x.Region != null);

        var mce = (MethodCallExpression)result.Expression;
        var lambda = (LambdaExpression)((UnaryExpression)mce.Arguments[1]).Operand;

        Assert.That(lambda.Body.NodeType, Is.EqualTo(ExpressionType.NotEqual));
    }

    [Test]
    public void Where_NullInequalityCheck_RightSide_IsNullConstant()
    {
        var frame = new TypedFrame<OrderSchema>(_provider);

        var result = frame.Where(x => x.Region != null);

        var mce = (MethodCallExpression)result.Expression;
        var lambda = (LambdaExpression)((UnaryExpression)mce.Arguments[1]).Operand;
        var binary = (BinaryExpression)lambda.Body;

        var right = binary.Right as ConstantExpression;
        Assert.That(right, Is.Not.Null);
        Assert.That(right!.Value, Is.Null);
    }

    // ===================================================================
    //  null == x.Prop  →  same but null on left side
    // ===================================================================

    [Test]
    public void Where_NullOnLeft_ProducesEqualExpression_WithNullLeftSide()
    {
        var frame = new TypedFrame<OrderSchema>(_provider);

#pragma warning disable CS8073 // always-null comparison — intentional for translation test
        var result = frame.Where(x => null == x.Region);
#pragma warning restore CS8073

        var mce = (MethodCallExpression)result.Expression;
        var lambda = (LambdaExpression)((UnaryExpression)mce.Arguments[1]).Operand;
        var binary = (BinaryExpression)lambda.Body;

        var left = binary.Left as ConstantExpression;
        Assert.That(left, Is.Not.Null);
        Assert.That(left!.Value, Is.Null);
    }
}
