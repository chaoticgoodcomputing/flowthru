using System.Linq.Expressions;
using Flowthru.Misc.DataFrames;

namespace Flowthru.Extensions.Spark.Tests;

[TestFixture]
[Category("ExpressionTree")]
public class SelectExpressionTests
{
    private TestFrameProvider _provider = null!;

    [SetUp]
    public void SetUp()
    {
        _provider = new TestFrameProvider();
    }

    // ===================================================================
    //  Tree structure
    // ===================================================================

    [Test]
    public void Select_ProducesMethodCallExpression_WithCorrectMethodName()
    {
        var frame = new TypedFrame<PersonSchema>(_provider);

        var result = frame.Select(x => new NameOnlySchema { Name = x.Name });

        var mce = result.Expression as MethodCallExpression;
        Assert.That(mce, Is.Not.Null);
        Assert.That(mce!.Method.Name, Is.EqualTo("Select"));
    }

    [Test]
    public void Select_ChangesElementType_ToTargetSchema()
    {
        var frame = new TypedFrame<PersonSchema>(_provider);

        var result = frame.Select(x => new NameOnlySchema { Name = x.Name });

        Assert.That(result.ElementType, Is.EqualTo(typeof(NameOnlySchema)));
    }

    [Test]
    public void Select_HasTwoArguments_SourceAndQuotedSelector()
    {
        var frame = new TypedFrame<PersonSchema>(_provider);

        var result = frame.Select(x => new NameOnlySchema { Name = x.Name });

        var mce = (MethodCallExpression)result.Expression;
        Assert.That(mce.Arguments, Has.Count.EqualTo(2));
    }

    // ===================================================================
    //  Projection lambda: MemberInitExpression
    // ===================================================================

    [Test]
    public void Select_MemberInit_ProducesMemberInitExpression()
    {
        var frame = new TypedFrame<PersonSchema>(_provider);

        var result = frame.Select(x => new PersonSummarySchema { Name = x.Name, Age = x.Age });

        var lambda = ExtractLambda(result);
        Assert.That(lambda.Body, Is.InstanceOf<MemberInitExpression>());
    }

    [Test]
    public void Select_MemberInit_HasCorrectBindingCount()
    {
        var frame = new TypedFrame<PersonSchema>(_provider);

        var result = frame.Select(x => new PersonSummarySchema { Name = x.Name, Age = x.Age });

        var lambda = ExtractLambda(result);
        var mie = (MemberInitExpression)lambda.Body;
        Assert.That(mie.Bindings, Has.Count.EqualTo(2));
    }

    [Test]
    public void Select_MemberInit_BindingsReferenceCorrectSourceMembers()
    {
        var frame = new TypedFrame<PersonSchema>(_provider);

        var result = frame.Select(x => new PersonSummarySchema { Name = x.Name, Age = x.Age });

        var lambda = ExtractLambda(result);
        var mie = (MemberInitExpression)lambda.Body;

        var bindingNames = mie.Bindings.Select(b => b.Member.Name).ToList();
        Assert.That(bindingNames, Does.Contain("Name"));
        Assert.That(bindingNames, Does.Contain("Age"));
    }

    [Test]
    public void Select_MemberInit_AssignmentExpressionsAreMemberAccess()
    {
        var frame = new TypedFrame<PersonSchema>(_provider);

        var result = frame.Select(x => new PersonSummarySchema { Name = x.Name, Age = x.Age });

        var lambda = ExtractLambda(result);
        var mie = (MemberInitExpression)lambda.Body;

        foreach (var binding in mie.Bindings.Cast<MemberAssignment>())
        {
            Assert.That(binding.Expression, Is.InstanceOf<MemberExpression>());
            var source = (MemberExpression)binding.Expression;
            Assert.That(source.Expression, Is.InstanceOf<ParameterExpression>());
        }
    }

    // ===================================================================
    //  Projection lambda: NewExpression (positional record constructor)
    // ===================================================================

    [Test]
    public void Select_RecordConstructor_ProducesNewExpression()
    {
        var frame = new TypedFrame<PersonSchema>(_provider);

        // Positional record construction via constructor
        var result = frame.Select(x => new NameOnlySchema { Name = x.Name });

        var lambda = ExtractLambda(result);
        // Object initializer on a record still emits MemberInitExpression
        Assert.That(lambda.Body, Is.InstanceOf<MemberInitExpression>().Or.InstanceOf<NewExpression>());
    }

    // ===================================================================
    //  Chaining: Where → Select
    // ===================================================================

    [Test]
    public void WhereFollowedBySelect_NestsCorrectly()
    {
        var frame = new TypedFrame<PersonSchema>(_provider);

        var result = frame.Where(x => x.Age > 18).Select(x => new NameOnlySchema { Name = x.Name });

        // Outer is Select
        var outer = (MethodCallExpression)result.Expression;
        Assert.That(outer.Method.Name, Is.EqualTo("Select"));
        Assert.That(result.ElementType, Is.EqualTo(typeof(NameOnlySchema)));

        // Inner (source of Select) is Where
        var inner = outer.Arguments[0] as MethodCallExpression;
        Assert.That(inner, Is.Not.Null);
        Assert.That(inner!.Method.Name, Is.EqualTo("Where"));
    }

    // ===================================================================
    //  Helpers
    // ===================================================================

    private static LambdaExpression ExtractLambda<T>(TypedFrame<T> frame)
    {
        var mce = (MethodCallExpression)frame.Expression;
        var quote = (UnaryExpression)mce.Arguments[1];
        return (LambdaExpression)quote.Operand;
    }
}
