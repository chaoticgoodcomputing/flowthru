using Flowthru.Extensions.Python.Runtime;
using Flowthru.Extensions.Python.Services;

namespace Flowthru.Extensions.Python.Tests.Services;

/// <summary>
/// Tests for <see cref="PythonServiceBuilder"/> — the fluent builder
/// invoked from inside the lambda passed to
/// <see cref="PythonRuntimeOptions.RegisterService(string, Action{PythonServiceBuilder})"/>.
/// </summary>
/// <remarks>
/// The builder is constructed by <c>RegisterService</c> rather than directly
/// by users, so tests exercise it through the options object's public surface
/// to keep the test contract aligned with the consumer-facing API.
/// </remarks>
[TestFixture]
[Category("Python")]
[Category("Services")]
public class PythonServiceBuilderTests
{
  // ── Successful registration ─────────────────────────────────────────

  [Test]
  public void RegisterService_WithInspector_StoresRegistration()
  {
    var options = new PythonRuntimeOptions();
    options.RegisterService(
      "Services.X.Y",
      svc => svc.WithInspector("Services.x_inspector")
    );

    Assert.That(options.ServiceRegistrations.Count, Is.EqualTo(1));
    var reg = options.ServiceRegistrations["Services.X.Y"];
    Assert.Multiple(() =>
    {
      Assert.That(reg.ServiceClassPath, Is.EqualTo("Services.X.Y"));
      Assert.That(reg.InspectorModule, Is.EqualTo("Services.x_inspector"));
      Assert.That(reg.InspectorFunction, Is.EqualTo("inspect"));
    });
  }

  [Test]
  public void WithInspector_CustomFunctionName_IsPreserved()
  {
    var options = new PythonRuntimeOptions();
    options.RegisterService(
      "Services.X.Y",
      svc => svc.WithInspector("Services.x_inspector", function: "verify")
    );

    var reg = options.ServiceRegistrations["Services.X.Y"];
    Assert.That(reg.InspectorFunction, Is.EqualTo("verify"));
  }

  [Test]
  public void RegisterService_MultipleCalls_StoresAllRegistrations()
  {
    var options = new PythonRuntimeOptions();
    options.RegisterService("A.B", svc => svc.WithInspector("A.B_inspector"));
    options.RegisterService("C.D", svc => svc.WithInspector("C.D_inspector"));
    options.RegisterService("E.F", svc => svc.WithInspector("E.F_inspector"));

    Assert.That(options.ServiceRegistrations.Count, Is.EqualTo(3));
  }

  [Test]
  public void RegisterService_SamePathTwice_LastWins()
  {
    // Re-registering with the same class path replaces the prior entry —
    // matches the "user override" expectation from .NET DI's Add semantics.
    var options = new PythonRuntimeOptions();
    options.RegisterService("X.Y", svc => svc.WithInspector("first"));
    options.RegisterService("X.Y", svc => svc.WithInspector("second"));

    Assert.That(options.ServiceRegistrations.Count, Is.EqualTo(1));
    Assert.That(options.ServiceRegistrations["X.Y"].InspectorModule, Is.EqualTo("second"));
  }

  // ── Argument validation ─────────────────────────────────────────────

  [Test]
  public void WithInspector_NullModule_ThrowsArgumentException()
  {
    var options = new PythonRuntimeOptions();
    Assert.Throws<ArgumentException>(() =>
      options.RegisterService("X.Y", svc => svc.WithInspector(null!))
    );
  }

  [Test]
  public void WithInspector_WhitespaceModule_ThrowsArgumentException()
  {
    var options = new PythonRuntimeOptions();
    Assert.Throws<ArgumentException>(() =>
      options.RegisterService("X.Y", svc => svc.WithInspector("   "))
    );
  }

  [Test]
  public void WithInspector_WhitespaceFunction_ThrowsArgumentException()
  {
    var options = new PythonRuntimeOptions();
    Assert.Throws<ArgumentException>(() =>
      options.RegisterService(
        "X.Y",
        svc => svc.WithInspector("inspector", function: "   ")
      )
    );
  }

  [Test]
  public void RegisterService_NullServicePath_ThrowsArgumentException()
  {
    var options = new PythonRuntimeOptions();
    Assert.Throws<ArgumentException>(() =>
      options.RegisterService(null!, svc => svc.WithInspector("inspector"))
    );
  }

  [Test]
  public void RegisterService_WhitespaceServicePath_ThrowsArgumentException()
  {
    var options = new PythonRuntimeOptions();
    Assert.Throws<ArgumentException>(() =>
      options.RegisterService("  ", svc => svc.WithInspector("inspector"))
    );
  }

  [Test]
  public void RegisterService_NullConfigure_ThrowsArgumentNullException()
  {
    var options = new PythonRuntimeOptions();
    Assert.Throws<ArgumentNullException>(() =>
      options.RegisterService("X.Y", configure: null!)
    );
  }

  [Test]
  public void RegisterService_NoWithInspectorCall_ThrowsInvalidOperation()
  {
    // Every registration MUST declare an inspector — that's the entire
    // point of registration. The builder catches the missing call at
    // Build() time with a message naming the offending service.
    var options = new PythonRuntimeOptions();
    var ex = Assert.Throws<InvalidOperationException>(() =>
      options.RegisterService("Services.Forgotten", svc => { /* no WithInspector */ })
    );
    Assert.That(ex!.Message, Does.Contain("Services.Forgotten"));
    Assert.That(ex.Message, Does.Contain("WithInspector"));
  }

  // ── Method chaining ─────────────────────────────────────────────────

  [Test]
  public void RegisterService_ReturnsOptionsForChaining()
  {
    var options = new PythonRuntimeOptions();
    var returned = options.RegisterService("X.Y", svc => svc.WithInspector("inspector"));
    Assert.That(returned, Is.SameAs(options));
  }
}
