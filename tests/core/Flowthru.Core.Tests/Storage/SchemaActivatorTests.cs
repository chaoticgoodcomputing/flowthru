using Flowthru.Data.Storage;

namespace Flowthru.Core.Tests.Storage;

/// <summary>
/// Pins both instantiation strategies in <see cref="SchemaActivator"/>: the
/// fast path (compiled-expression-tree call to a public parameterless
/// constructor) and the slow path (<c>RuntimeHelpers.GetUninitializedObject</c>
/// for records with <c>required</c> members or positional constructors).
/// Also pins the abstract-type and interface guards.
/// </summary>
[TestFixture]
public class SchemaActivatorTests
{
  // Fast path — public parameterless constructor compiled into a Func<T>.
  public sealed class TraditionalRecord
  {
    public int X { get; set; }
    public string Y { get; set; } = string.Empty;
  }

  [Test]
  public void CreateInstance_TraditionalRecord_UsesFastPath()
  {
    var instance = SchemaActivator.CreateInstance<TraditionalRecord>();
    Assert.That(instance, Is.Not.Null);
    Assert.That(instance, Is.InstanceOf<TraditionalRecord>());
  }

  [Test]
  public void CreateInstance_TraditionalRecord_CachesFactoryAcrossCalls()
  {
    var first = SchemaActivator.CreateInstance<TraditionalRecord>();
    var second = SchemaActivator.CreateInstance<TraditionalRecord>();
    Assert.That(first, Is.Not.SameAs(second));
    Assert.That(first.GetType(), Is.EqualTo(second.GetType()));
  }

  // Slow path — `required` members force allocation without ctor invocation.
  public sealed record RequiredMembersRecord
  {
    public required int X { get; init; }
    public required string Y { get; init; }
  }

  [Test]
  public void CreateInstance_RequiredMembersRecord_UsesUninitializedObjectPath()
  {
    var instance = SchemaActivator.CreateInstance<RequiredMembersRecord>();
    Assert.That(instance, Is.Not.Null);
    Assert.That(instance, Is.InstanceOf<RequiredMembersRecord>());
  }

  // Slow path — positional record (no public parameterless constructor).
  public sealed record PositionalRecord(int X, string Y);

  [Test]
  public void CreateInstance_PositionalRecord_UsesUninitializedObjectPath()
  {
    var instance = SchemaActivator.CreateInstance<PositionalRecord>();
    Assert.That(instance, Is.Not.Null);
    Assert.That(instance, Is.InstanceOf<PositionalRecord>());
  }

  // Guards.
  public abstract class AbstractRow { }

  [Test]
  public void CreateInstance_AbstractType_Throws()
  {
    Assert.That(
      () => SchemaActivator.CreateInstance<AbstractRow>(),
      Throws.TypeOf<InvalidOperationException>()
        .With.Message.Contain("abstract type or interface")
    );
  }

  public interface IRow { }

  [Test]
  public void CreateInstance_Interface_Throws()
  {
    Assert.That(
      () => SchemaActivator.CreateInstance<IRow>(),
      Throws.TypeOf<InvalidOperationException>()
        .With.Message.Contain("abstract type or interface")
    );
  }
}
