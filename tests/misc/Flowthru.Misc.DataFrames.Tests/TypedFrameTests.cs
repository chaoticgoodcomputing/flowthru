using System.Collections;
using System.Linq.Expressions;
using Flowthru.Misc.DataFrames.Tests.Fixtures;

namespace Flowthru.Misc.DataFrames.Tests;

/// <summary>
/// Pins the <see cref="TypedFrame{T}"/> IQueryable surface: constructor
/// validation, Expression/ElementType/Provider invariants, and the
/// GetEnumerator → provider.Materialize wiring that lets a frame stand in
/// for an <see cref="IEnumerable{T}"/> at catalog item boundaries.
/// </summary>
[TestFixture]
public class TypedFrameTests
{
  [Test]
  public void RootCtor_NullProvider_Throws()
  {
    Assert.That(
      () => new TypedFrame<Person>(provider: null!),
      Throws.TypeOf<ArgumentNullException>()
    );
  }

  [Test]
  public void RootCtor_SetsExpressionToConstantOfSelf()
  {
    var provider = new RecordingFrameQueryProvider();
    var frame = new TypedFrame<Person>(provider);

    Assert.That(frame.Expression, Is.TypeOf<ConstantExpression>());
    Assert.That(((ConstantExpression)frame.Expression).Value, Is.SameAs(frame));
  }

  [Test]
  public void RootCtor_ExposesProviderAndElementType()
  {
    var provider = new RecordingFrameQueryProvider();
    var frame = new TypedFrame<Person>(provider);

    Assert.That(frame.Provider, Is.SameAs(provider));
    Assert.That(frame.ElementType, Is.EqualTo(typeof(Person)));
  }

  [Test]
  public void IntermediateCtor_NullProvider_Throws()
  {
    var expression = Expression.Constant(new object());
    Assert.That(
      () => new TypedFrame<Person>(provider: null!, expression),
      Throws.TypeOf<ArgumentNullException>()
    );
  }

  [Test]
  public void IntermediateCtor_NullExpression_Throws()
  {
    var provider = new RecordingFrameQueryProvider();
    Assert.That(
      () => new TypedFrame<Person>(provider, expression: null!),
      Throws.TypeOf<ArgumentNullException>()
    );
  }

  [Test]
  public void IntermediateCtor_PreservesGivenExpression()
  {
    var provider = new RecordingFrameQueryProvider();
    var expression = Expression.Constant(42);

    var frame = new TypedFrame<Person>(provider, expression);

    Assert.That(frame.Expression, Is.SameAs(expression));
  }

  [Test]
  public void GetEnumerator_DelegatesToProviderMaterialize()
  {
    var provider = new RecordingFrameQueryProvider();
    var people = new[]
    {
      new Person { Name = "Ada", Age = 30 },
      new Person { Name = "Bob", Age = 40 },
    };
    provider.MaterializeResults.Enqueue(people);

    var frame = new TypedFrame<Person>(provider);

    var materialized = frame.ToList();

    Assert.That(provider.MaterializeCalls, Has.Count.EqualTo(1));
    Assert.That(provider.MaterializeCalls[0], Is.SameAs(frame.Expression));
    Assert.That(materialized.Select(p => p.Name), Is.EqualTo(new[] { "Ada", "Bob" }));
  }

  [Test]
  public void GetEnumerator_PassesAccumulatedExpression_NotJustRoot()
  {
    // A Where call replaces the frame's expression with a method call node.
    // GetEnumerator must hand THAT to Materialize — otherwise downstream
    // operations evaporate at materialization time.
    var provider = new RecordingFrameQueryProvider();
    provider.MaterializeResults.Enqueue(Array.Empty<Person>());

    var frame = new TypedFrame<Person>(provider).Where(p => p.Age > 18);
    _ = frame.ToList();

    Assert.That(provider.MaterializeCalls, Has.Count.EqualTo(1));
    Assert.That(provider.MaterializeCalls[0], Is.InstanceOf<MethodCallExpression>());
  }

  [Test]
  public void NonGenericEnumerable_RoutesThroughTypedEnumerator()
  {
    var provider = new RecordingFrameQueryProvider();
    provider.MaterializeResults.Enqueue(new[] { new Person { Name = "Ada" } });

    IEnumerable frame = new TypedFrame<Person>(provider);

    var rows = frame.Cast<Person>().ToList();
    Assert.That(rows, Has.Count.EqualTo(1));
    Assert.That(rows[0].Name, Is.EqualTo("Ada"));
  }
}
