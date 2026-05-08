using Flowthru.Validation.PreFlight;
using Flowthru.Validation.Runtime;

namespace Flowthru.Core.Tests.Runtime;

/// <summary>
/// Tests for the new <see cref="ServiceRef"/> closed sum: the
/// <see cref="ServiceRef.CSharp"/> Core variant plus the
/// <see cref="ServiceRef.External"/> open extension point. Replaces
/// the legacy two-variant ServiceRefTests; the Python variant moves
/// to <c>Flowthru.Python</c> as an
/// <see cref="IExtensionServiceRef"/> implementation in Phase 8.
/// </summary>
[TestFixture]
public class ServiceRefTests
{
  public interface IDummyService { }

  [Test]
  public void CSharp_DagId_ReturnsFullName()
  {
    var serviceRef = new ServiceRef.CSharp(typeof(IDummyService));
    Assert.That(
      serviceRef.DagId,
      Is.EqualTo("Flowthru.Core.Tests.Runtime.ServiceRefTests+IDummyService")
    );
  }

  [Test]
  public void CSharp_DisplayName_ReturnsUnqualifiedName()
  {
    var serviceRef = new ServiceRef.CSharp(typeof(IDummyService));
    Assert.That(serviceRef.DisplayName, Is.EqualTo("IDummyService"));
  }

  [Test]
  public void Of_GenericType_ReturnsCSharpVariant()
  {
    var serviceRef = ServiceRef.Of<IDummyService>();
    Assert.That(serviceRef, Is.TypeOf<ServiceRef.CSharp>());
    Assert.That(((ServiceRef.CSharp)serviceRef).ServiceType, Is.EqualTo(typeof(IDummyService)));
  }

  [Test]
  public void Of_RuntimeType_ReturnsCSharpVariant()
  {
    var serviceRef = ServiceRef.Of(typeof(IDummyService));
    Assert.That(serviceRef, Is.TypeOf<ServiceRef.CSharp>());
  }

  [Test]
  public void Equality_TwoCSharpRefsForSameType_AreEqual()
  {
    var a = ServiceRef.Of<IDummyService>();
    var b = ServiceRef.Of<IDummyService>();
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
    var serviceRef = new ServiceRef.External(ext);

    Assert.That(serviceRef.DagId, Is.EqualTo("ext.python.X.Y"));
    Assert.That(serviceRef.DisplayName, Is.EqualTo("Y"));
  }

  [Test]
  public void External_DistinctFromCSharpEvenWithSameDagId()
  {
    var csharp = ServiceRef.Of<IDummyService>();
    var external = new ServiceRef.External(new FakeExternalRef(csharp.DagId, csharp.DisplayName, "ext"));
    Assert.That(csharp, Is.Not.EqualTo(external),
      "The variant tag itself disambiguates — even matching DagIds don't make the values equal.");
  }

  private sealed record FakeExternalRef(string DagId, string DisplayName, string Category)
    : IExtensionServiceRef;
}
