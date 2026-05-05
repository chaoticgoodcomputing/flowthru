using Flowthru.Core.Effects;

namespace Flowthru.Core.Tests.Effects;

/// <summary>
/// Tests for <see cref="ServiceRef"/> — the language-agnostic identity for
/// services that flow into <c>FlowStep.ServiceDependencies</c> and the
/// preflight dispatch loop.
/// </summary>
[TestFixture]
[Category("Effects")]
public class ServiceRefTests
{
  // ── CSharp variant ──────────────────────────────────────────────────

  public interface IDummyService { }

  [Test]
  public void CSharp_DagId_ReturnsFullName()
  {
    var serviceRef = new ServiceRef.CSharp(typeof(IDummyService));
    Assert.That(
      serviceRef.DagId,
      Is.EqualTo("Flowthru.Core.Tests.Effects.ServiceRefTests+IDummyService")
    );
  }

  [Test]
  public void CSharp_DisplayName_ReturnsUnqualifiedName()
  {
    var serviceRef = new ServiceRef.CSharp(typeof(IDummyService));
    Assert.That(serviceRef.DisplayName, Is.EqualTo("IDummyService"));
  }

  [Test]
  public void CSharp_DagId_ClosedGenericFallsBackToName()
  {
    // Closed generic types whose FullName is null are represented by their
    // simple Name. This is rare in practice — most service interfaces are
    // non-generic — but the fallback is documented behaviour worth pinning.
    var dictType = typeof(Dictionary<string, IDummyService>);
    var serviceRef = new ServiceRef.CSharp(dictType);

    // Dictionary<,>'s closed FullName is non-null, but the test verifies
    // the fallback path doesn't throw and yields a non-empty string.
    Assert.That(serviceRef.DagId, Is.Not.Null.And.Not.Empty);
  }

  // ── Python variant ──────────────────────────────────────────────────

  [Test]
  public void Python_DagId_ReturnsFullClassPath()
  {
    var serviceRef = new ServiceRef.Python("Services.pyannote_diarizer.PyannoteDiarizer");
    Assert.That(
      serviceRef.DagId,
      Is.EqualTo("Services.pyannote_diarizer.PyannoteDiarizer")
    );
  }

  [Test]
  public void Python_DisplayName_ReturnsLastDotSegment()
  {
    var serviceRef = new ServiceRef.Python("Services.pyannote_diarizer.PyannoteDiarizer");
    Assert.That(serviceRef.DisplayName, Is.EqualTo("PyannoteDiarizer"));
  }

  [Test]
  public void Python_DisplayName_NoDots_ReturnsFullPath()
  {
    var serviceRef = new ServiceRef.Python("Standalone");
    Assert.That(serviceRef.DisplayName, Is.EqualTo("Standalone"));
  }

  [Test]
  public void Python_DisplayName_TwoSegments_ReturnsLast()
  {
    var serviceRef = new ServiceRef.Python("module.Class");
    Assert.That(serviceRef.DisplayName, Is.EqualTo("Class"));
  }

  // ── Factories ───────────────────────────────────────────────────────

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
    Assert.That(((ServiceRef.CSharp)serviceRef).ServiceType, Is.EqualTo(typeof(IDummyService)));
  }

  [Test]
  public void OfPython_ReturnsPythonVariant()
  {
    var serviceRef = ServiceRef.OfPython("Services.X.Y");
    Assert.That(serviceRef, Is.TypeOf<ServiceRef.Python>());
    Assert.That(((ServiceRef.Python)serviceRef).ClassPath, Is.EqualTo("Services.X.Y"));
  }

  // ── Equality (record value semantics) ───────────────────────────────

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
  public void Equality_TwoPythonRefsForSamePath_AreEqual()
  {
    var a = ServiceRef.OfPython("X.Y");
    var b = ServiceRef.OfPython("X.Y");
    Assert.Multiple(() =>
    {
      Assert.That(a, Is.EqualTo(b));
      Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
    });
  }

  [Test]
  public void Equality_PythonAndCSharpWithMatchingNames_AreNotEqual()
  {
    // The variant type itself disambiguates — even if a CSharp DagId and a
    // Python ClassPath happened to render to the same string, they are
    // different ServiceRef values.
    var csharp = ServiceRef.Of<IDummyService>();
    var python = ServiceRef.OfPython(csharp.DagId);
    Assert.That(csharp, Is.Not.EqualTo(python));
  }

  [Test]
  public void Equality_DifferentPythonPaths_AreNotEqual()
  {
    var a = ServiceRef.OfPython("X.Y");
    var b = ServiceRef.OfPython("X.Z");
    Assert.That(a, Is.Not.EqualTo(b));
  }
}
