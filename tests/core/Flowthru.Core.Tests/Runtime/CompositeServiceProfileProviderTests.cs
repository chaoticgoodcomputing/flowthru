using Flowthru.Validation.Runtime;

namespace Flowthru.Core.Tests.Runtime;

/// <summary>
/// Tests for <see cref="CompositeServiceProfileProvider"/> — the default
/// <see cref="IServiceProfileProvider"/> that folds every registered
/// <see cref="IServiceProfileContributor"/> by conservative meet. (ADR-0019.)
/// </summary>
[TestFixture]
public class CompositeServiceProfileProviderTests
{
  private interface IResourceA { }
  private interface IResourceB { }

  /// <summary>Recognises one dependency by DagId; silent on all others.</summary>
  private sealed class Contributor : IServiceProfileContributor
  {
    private readonly string _dagId;
    private readonly ServiceProfile _profile;
    public Contributor(ServiceDependency dep, ServiceProfile profile)
    {
      _dagId = dep.DagId;
      _profile = profile;
    }
    public ServiceProfile? Contribute(ServiceDependency dependency) =>
      dependency.DagId == _dagId ? _profile : null;
  }

  [Test]
  public void NoContributors_ResolvesUnbounded()
  {
    var provider = new CompositeServiceProfileProvider(Array.Empty<IServiceProfileContributor>());
    var profile = provider.Resolve(ServiceDependency.Of<IResourceA>());
    Assert.That(profile.Capacity, Is.EqualTo(int.MaxValue));
    Assert.That(profile.AffectsOutputs, Is.True);
  }

  [Test]
  public void SingleContributor_ResolvesItsProfile_AndUnboundedForOthers()
  {
    var depA = ServiceDependency.Of<IResourceA>();
    var provider = new CompositeServiceProfileProvider(new IServiceProfileContributor[]
    {
      new Contributor(depA, new ServiceProfile { Capacity = 1, AffectsOutputs = false }),
    });

    var a = provider.Resolve(depA);
    Assert.That(a.Capacity, Is.EqualTo(1));
    Assert.That(a.AffectsOutputs, Is.False);

    var b = provider.Resolve(ServiceDependency.Of<IResourceB>());
    Assert.That(b.Capacity, Is.EqualTo(int.MaxValue),
      "A dependency no contributor recognises resolves to Unbounded.");
  }

  [Test]
  public void MultipleContributors_TakeConservativeMeet()
  {
    var dep = ServiceDependency.Of<IResourceA>();
    var provider = new CompositeServiceProfileProvider(new IServiceProfileContributor[]
    {
      new Contributor(dep, new ServiceProfile { Capacity = 4, ReadCapacity = 2, AffectsOutputs = false }),
      new Contributor(dep, new ServiceProfile { Capacity = 1, ReadCapacity = 8, AffectsOutputs = true }),
    });

    var profile = provider.Resolve(dep);
    Assert.That(profile.Capacity, Is.EqualTo(1), "Capacity meets to the minimum.");
    Assert.That(profile.ReadCapacity, Is.EqualTo(2), "ReadCapacity meets to the minimum.");
    Assert.That(profile.AffectsOutputs, Is.True, "AffectsOutputs ORs — any cache-affecting source wins.");
  }
}
