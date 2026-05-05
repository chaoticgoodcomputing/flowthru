using Flowthru.Extensions.Python.Services;

namespace Flowthru.Extensions.Python.Tests.Services;

/// <summary>
/// Tests for <see cref="PythonServiceRegistration"/>'s
/// <see cref="PythonServiceRegistration.ServiceModule"/> and
/// <see cref="PythonServiceRegistration.ServiceClass"/> accessors, which
/// split the dotted class path at the last component for the worker's
/// <c>service_module</c> + <c>service_class</c> request fields.
/// </summary>
[TestFixture]
[Category("Python")]
[Category("Services")]
public class PythonServiceRegistrationTests
{
  // ── Multi-segment paths ─────────────────────────────────────────────

  [Test]
  public void ServiceModuleAndClass_MultiSegmentPath_SplitsAtLastDot()
  {
    var reg = new PythonServiceRegistration(
      ServiceClassPath: "Services.pyannote_diarizer.PyannoteDiarizer",
      InspectorModule: "Services.pyannote_diarizer_inspector",
      InspectorFunction: "inspect"
    );

    Assert.Multiple(() =>
    {
      Assert.That(reg.ServiceModule, Is.EqualTo("Services.pyannote_diarizer"));
      Assert.That(reg.ServiceClass, Is.EqualTo("PyannoteDiarizer"));
    });
  }

  [Test]
  public void ServiceModuleAndClass_TwoSegmentPath_SplitsCorrectly()
  {
    var reg = new PythonServiceRegistration(
      ServiceClassPath: "module.MyClass",
      InspectorModule: "module_inspector",
      InspectorFunction: "inspect"
    );

    Assert.Multiple(() =>
    {
      Assert.That(reg.ServiceModule, Is.EqualTo("module"));
      Assert.That(reg.ServiceClass, Is.EqualTo("MyClass"));
    });
  }

  // ── No-dot edge case ────────────────────────────────────────────────

  [Test]
  public void ServiceModule_NoDot_IsEmpty()
  {
    // A class registered at the top-level (no module prefix) — the worker
    // imports an empty string, which Python interprets as the current
    // package; not generally useful but the splitting must still produce
    // a sensible empty module rather than throwing.
    var reg = new PythonServiceRegistration(
      ServiceClassPath: "TopLevelClass",
      InspectorModule: "inspector",
      InspectorFunction: "inspect"
    );
    Assert.That(reg.ServiceModule, Is.EqualTo(string.Empty));
  }

  [Test]
  public void ServiceClass_NoDot_IsFullPath()
  {
    var reg = new PythonServiceRegistration(
      ServiceClassPath: "TopLevelClass",
      InspectorModule: "inspector",
      InspectorFunction: "inspect"
    );
    Assert.That(reg.ServiceClass, Is.EqualTo("TopLevelClass"));
  }

  // ── Trailing-dot edge case ──────────────────────────────────────────

  [Test]
  public void ServiceClass_TrailingDot_IsEmpty()
  {
    // A path that ends with a dot is malformed user input. We don't throw;
    // ServiceClass is whatever follows the last dot — empty here. The
    // failure surfaces later at worker-side getattr() time with a clear
    // diagnostic, which is preferable to misattributing the failure to
    // construction.
    var reg = new PythonServiceRegistration(
      ServiceClassPath: "module.",
      InspectorModule: "inspector",
      InspectorFunction: "inspect"
    );

    Assert.Multiple(() =>
    {
      Assert.That(reg.ServiceModule, Is.EqualTo("module"));
      Assert.That(reg.ServiceClass, Is.EqualTo(string.Empty));
    });
  }

  // ── Record value equality ───────────────────────────────────────────

  [Test]
  public void Equality_TwoIdenticalRegistrations_AreEqual()
  {
    var a = new PythonServiceRegistration("M.C", "I", "inspect");
    var b = new PythonServiceRegistration("M.C", "I", "inspect");
    Assert.Multiple(() =>
    {
      Assert.That(a, Is.EqualTo(b));
      Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
    });
  }

  [Test]
  public void Equality_DifferingInspectorFunction_AreNotEqual()
  {
    var a = new PythonServiceRegistration("M.C", "I", "inspect");
    var b = new PythonServiceRegistration("M.C", "I", "verify");
    Assert.That(a, Is.Not.EqualTo(b));
  }
}
