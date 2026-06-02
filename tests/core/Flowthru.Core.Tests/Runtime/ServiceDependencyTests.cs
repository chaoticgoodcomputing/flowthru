using Flowthru.Validation.PreFlight;
using Flowthru.Validation.Runtime;

namespace Flowthru.Core.Tests.Runtime;

/// <summary>
/// Tests for the new <see cref="ServiceDependency"/> closed sum: the
/// <see cref="ServiceDependency.CSharp"/> Core variant plus the
/// <see cref="ServiceDependency.External"/> open extension point. Replaces
/// the legacy two-variant ServiceDependencyTests; the Python variant moves
/// to <c>Flowthru.Python</c> as an
/// <see cref="IExtensionServiceDependency"/> implementation in Phase 8.
/// </summary>
[TestFixture]
public class ServiceDependencyTests
{
  public interface IDummyService { }

  [Test]
  public void CSharp_DagId_ReturnsFullName()
  {
    var serviceRef = new ServiceDependency.CSharp(typeof(IDummyService));
    Assert.That(
      serviceRef.DagId,
      Is.EqualTo("Flowthru.Core.Tests.Runtime.ServiceDependencyTests+IDummyService")
    );
  }

  [Test]
  public void CSharp_DisplayName_ReturnsUnqualifiedName()
  {
    var serviceRef = new ServiceDependency.CSharp(typeof(IDummyService));
    Assert.That(serviceRef.DisplayName, Is.EqualTo("IDummyService"));
  }

  [Test]
  public void Of_GenericType_ReturnsCSharpVariant()
  {
    var serviceRef = ServiceDependency.Of<IDummyService>();
    Assert.That(serviceRef, Is.TypeOf<ServiceDependency.CSharp>());
    Assert.That(((ServiceDependency.CSharp)serviceRef).ServiceType, Is.EqualTo(typeof(IDummyService)));
  }

  [Test]
  public void Of_RuntimeType_ReturnsCSharpVariant()
  {
    var serviceRef = ServiceDependency.Of(typeof(IDummyService));
    Assert.That(serviceRef, Is.TypeOf<ServiceDependency.CSharp>());
  }

  [Test]
  public void Equality_TwoCSharpRefsForSameType_AreEqual()
  {
    var a = ServiceDependency.Of<IDummyService>();
    var b = ServiceDependency.Of<IDummyService>();
    Assert.Multiple(() =>
    {
      Assert.That(a, Is.EqualTo(b));
      Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
    });
  }

  [Test]
  public void External_ProxiesDagIdAndDisplayNameFromExtension()
  {
    var ext = new FakeExternalRef("ext.python.X.Y", "Y", "python");
    var serviceRef = new ServiceDependency.External(ext);

    Assert.That(serviceRef.DagId, Is.EqualTo("ext.python.X.Y"));
    Assert.That(serviceRef.DisplayName, Is.EqualTo("Y"));
  }

  [Test]
  public void External_DistinctFromCSharpEvenWithSameDagId()
  {
    var csharp = ServiceDependency.Of<IDummyService>();
    var external = new ServiceDependency.External(new FakeExternalRef(csharp.DagId, csharp.DisplayName, "ext"));
    Assert.That(csharp, Is.Not.EqualTo(external),
      "The variant tag itself disambiguates — even matching DagIds don't make the values equal.");
  }

  private sealed record FakeExternalRef(string DagId, string DisplayName, string Category)
    : IExtensionServiceDependency;
}
