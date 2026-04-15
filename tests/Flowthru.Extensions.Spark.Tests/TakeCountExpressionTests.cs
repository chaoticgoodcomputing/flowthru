using System.Linq.Expressions;
using Flowthru.Misc.DataFrames;

namespace Flowthru.Extensions.Spark.Tests;

[TestFixture]
[Category("ExpressionTree")]
public class TakeCountExpressionTests
{
    private TestFrameProvider _provider = null!;

    [SetUp]
    public void SetUp()
    {
        _provider = new TestFrameProvider();
    }

    // ===================================================================
    //  Take
    // ===================================================================

    [Test]
    public void Take_ProducesMethodCallExpression_WithCorrectMethodName()
    {
        var frame = new TypedFrame<PersonSchema>(_provider);

        var result = frame.Take(10);

        var mce = result.Expression as MethodCallExpression;
        Assert.That(mce, Is.Not.Null);
        Assert.That(mce!.Method.Name, Is.EqualTo("Take"));
    }

    [Test]
    public void Take_PreservesSourceTypeParameter()
    {
        var frame = new TypedFrame<PersonSchema>(_provider);

        var result = frame.Take(5);

        Assert.That(result.ElementType, Is.EqualTo(typeof(PersonSchema)));
    }

    [Test]
    public void Take_HasTwoArguments_SourceAndCount()
    {
        var frame = new TypedFrame<PersonSchema>(_provider);

        var result = frame.Take(10);

        var mce = (MethodCallExpression)result.Expression;
        Assert.That(mce.Arguments, Has.Count.EqualTo(2));
    }

    [Test]
    public void Take_SecondArgument_IsConstantWithCorrectValue()
    {
        var frame = new TypedFrame<PersonSchema>(_provider);
        const int limit = 42;

        var result = frame.Take(limit);

        var mce = (MethodCallExpression)result.Expression;
        var constant = mce.Arguments[1] as ConstantExpression;
        Assert.That(constant, Is.Not.Null);
        Assert.That(constant!.Value, Is.EqualTo(limit));
    }

    [Test]
    public void Take_CanChainAfterWhere()
    {
        var frame = new TypedFrame<PersonSchema>(_provider);

        var result = frame.Where(x => x.IsActive).Take(100);

        var mce = result.Expression as MethodCallExpression;
        Assert.That(mce!.Method.Name, Is.EqualTo("Take"));

        var innerMce = mce.Arguments[0] as MethodCallExpression;
        Assert.That(innerMce!.Method.Name, Is.EqualTo("Where"));
    }

    // ===================================================================
    //  Count
    // ===================================================================

    [Test]
    public void Count_ProducesMethodCallExpression_WithCorrectMethodName()
    {
        var frame = new TypedFrame<PersonSchema>(_provider);

        // Count is a terminal operation — we capture the expression before Execute throws
        var mce = CaptureCountExpression(frame);

        Assert.That(mce, Is.Not.Null);
        Assert.That(mce!.Method.Name, Is.EqualTo("Count"));
    }

    [Test]
    public void Count_HasOneArgument_TheSourceExpression()
    {
        var frame = new TypedFrame<PersonSchema>(_provider);

        var mce = CaptureCountExpression(frame);

        Assert.That(mce!.Arguments, Has.Count.EqualTo(1));
    }

    [Test]
    public void Count_AfterWhere_SourceArgument_IsWhereCallExpression()
    {
        var frame = new TypedFrame<PersonSchema>(_provider);
        var filtered = frame.Where(x => x.Age > 18);

        var mce = CaptureCountExpression(filtered);

        var sourceArg = mce!.Arguments[0] as MethodCallExpression;
        Assert.That(sourceArg, Is.Not.Null);
        Assert.That(sourceArg!.Method.Name, Is.EqualTo("Where"));
    }

    // Count calls Execute<long> on the provider — the test provider throws,
    // so we intercept the expression by wrapping CreateQuery instead.
    private static MethodCallExpression? CaptureCountExpression(TypedFrame<PersonSchema> frame)
    {
        var capturer = new CountExpressionCapturer();
        var captureFrame = new TypedFrame<PersonSchema>(capturer, frame.Expression);
        try
        {
            captureFrame.Count();
        }
        catch (CountCapturedException)
        {
            // expected
        }
        return capturer.CapturedExpression as MethodCallExpression;
    }

    // ===================================================================
    //  Helpers
    // ===================================================================

    private sealed class CountCapturedException : Exception { }

    private sealed class CountExpressionCapturer : IFrameQueryProvider
    {
        public Expression? CapturedExpression { get; private set; }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression) =>
          new TypedFrame<TElement>(this, expression);

        public IQueryable CreateQuery(Expression expression) => throw new NotSupportedException();

        public object Compile(Expression expression) => throw new NotSupportedException();

        public IEnumerable<T> Materialize<T>(Expression expression) =>
          throw new NotSupportedException();

        public TResult Execute<TResult>(Expression expression)
        {
            CapturedExpression = expression;
            throw new CountCapturedException();
        }

        public object? Execute(Expression expression)
        {
            CapturedExpression = expression;
            throw new CountCapturedException();
        }
    }
}
