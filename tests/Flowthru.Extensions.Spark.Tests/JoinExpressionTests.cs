using System.Linq.Expressions;
using Flowthru.DataFrames;

namespace Flowthru.Extensions.Spark.Tests;

[TestFixture]
[Category("ExpressionTree")]
public class JoinExpressionTests
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
  public void Join_ProducesMethodCallExpression_WithCorrectMethodName()
  {
    var left = new TypedFrame<EmployeeSchema>(_provider);
    var right = new TypedFrame<DepartmentSchema>(_provider);

    var result = left.Join(
      right,
      emp => emp.DeptId,
      dept => dept.DeptId,
      (emp, dept) => new EmployeeDeptSchema { EmployeeName = emp.Name, DepartmentName = dept.Name }
    );

    var mce = result.Expression as MethodCallExpression;
    Assert.That(mce, Is.Not.Null);
    Assert.That(mce!.Method.Name, Is.EqualTo("Join"));
  }

  [Test]
  public void Join_ChangesElementType_ToResultSchema()
  {
    var left = new TypedFrame<EmployeeSchema>(_provider);
    var right = new TypedFrame<DepartmentSchema>(_provider);

    var result = left.Join(
      right,
      emp => emp.DeptId,
      dept => dept.DeptId,
      (emp, dept) => new EmployeeDeptSchema { EmployeeName = emp.Name, DepartmentName = dept.Name }
    );

    Assert.That(result.ElementType, Is.EqualTo(typeof(EmployeeDeptSchema)));
  }

  [Test]
  public void Join_HasFiveArguments()
  {
    var left = new TypedFrame<EmployeeSchema>(_provider);
    var right = new TypedFrame<DepartmentSchema>(_provider);

    var result = left.Join(
      right,
      emp => emp.DeptId,
      dept => dept.DeptId,
      (emp, dept) => new EmployeeDeptSchema { EmployeeName = emp.Name, DepartmentName = dept.Name }
    );

    var mce = (MethodCallExpression)result.Expression;
    // outer source, inner source, outerKey, innerKey, resultSelector
    Assert.That(mce.Arguments, Has.Count.EqualTo(5));
  }

  // ===================================================================
  //  Key selectors
  // ===================================================================

  [Test]
  public void Join_KeySelectors_ReferenceCorrectMembers()
  {
    var left = new TypedFrame<EmployeeSchema>(_provider);
    var right = new TypedFrame<DepartmentSchema>(_provider);

    var result = left.Join(
      right,
      emp => emp.DeptId,
      dept => dept.DeptId,
      (emp, dept) => new EmployeeDeptSchema { EmployeeName = emp.Name, DepartmentName = dept.Name }
    );

    var mce = (MethodCallExpression)result.Expression;

    // args[2] = outer key selector (quoted)
    var outerKeyLambda = ExtractQuotedLambda(mce.Arguments[2]);
    var outerBody = outerKeyLambda.Body as MemberExpression;
    Assert.That(outerBody, Is.Not.Null);
    Assert.That(outerBody!.Member.Name, Is.EqualTo("DeptId"));

    // args[3] = inner key selector (quoted)
    var innerKeyLambda = ExtractQuotedLambda(mce.Arguments[3]);
    var innerBody = innerKeyLambda.Body as MemberExpression;
    Assert.That(innerBody, Is.Not.Null);
    Assert.That(innerBody!.Member.Name, Is.EqualTo("DeptId"));
  }

  // ===================================================================
  //  Result selector
  // ===================================================================

  [Test]
  public void Join_ResultSelector_HasTwoParameters()
  {
    var left = new TypedFrame<EmployeeSchema>(_provider);
    var right = new TypedFrame<DepartmentSchema>(_provider);

    var result = left.Join(
      right,
      emp => emp.DeptId,
      dept => dept.DeptId,
      (emp, dept) => new EmployeeDeptSchema { EmployeeName = emp.Name, DepartmentName = dept.Name }
    );

    var mce = (MethodCallExpression)result.Expression;
    var resultLambda = ExtractQuotedLambda(mce.Arguments[4]);

    Assert.That(resultLambda.Parameters, Has.Count.EqualTo(2));
  }

  [Test]
  public void Join_ResultSelector_ProducesMemberInitExpression()
  {
    var left = new TypedFrame<EmployeeSchema>(_provider);
    var right = new TypedFrame<DepartmentSchema>(_provider);

    var result = left.Join(
      right,
      emp => emp.DeptId,
      dept => dept.DeptId,
      (emp, dept) => new EmployeeDeptSchema { EmployeeName = emp.Name, DepartmentName = dept.Name }
    );

    var mce = (MethodCallExpression)result.Expression;
    var resultLambda = ExtractQuotedLambda(mce.Arguments[4]);

    Assert.That(resultLambda.Body, Is.InstanceOf<MemberInitExpression>());
  }

  [Test]
  public void Join_ResultSelector_BindingsReferenceCorrectParameters()
  {
    var left = new TypedFrame<EmployeeSchema>(_provider);
    var right = new TypedFrame<DepartmentSchema>(_provider);

    var result = left.Join(
      right,
      emp => emp.DeptId,
      dept => dept.DeptId,
      (emp, dept) => new EmployeeDeptSchema { EmployeeName = emp.Name, DepartmentName = dept.Name }
    );

    var mce = (MethodCallExpression)result.Expression;
    var resultLambda = ExtractQuotedLambda(mce.Arguments[4]);
    var mie = (MemberInitExpression)resultLambda.Body;

    // EmployeeName = emp.Name → source parameter is emp (index 0)
    var empBinding = (MemberAssignment)mie.Bindings.First(b => b.Member.Name == "EmployeeName");
    var empSource = (MemberExpression)empBinding.Expression;
    Assert.That(empSource.Expression, Is.SameAs(resultLambda.Parameters[0]));
    Assert.That(empSource.Member.Name, Is.EqualTo("Name"));

    // DepartmentName = dept.Name → source parameter is dept (index 1)
    var deptBinding = (MemberAssignment)mie.Bindings.First(b => b.Member.Name == "DepartmentName");
    var deptSource = (MemberExpression)deptBinding.Expression;
    Assert.That(deptSource.Expression, Is.SameAs(resultLambda.Parameters[1]));
    Assert.That(deptSource.Member.Name, Is.EqualTo("Name"));
  }

  // ===================================================================
  //  Helpers
  // ===================================================================

  private static LambdaExpression ExtractQuotedLambda(Expression expression)
  {
    var quote = (UnaryExpression)expression;
    return (LambdaExpression)quote.Operand;
  }
}
