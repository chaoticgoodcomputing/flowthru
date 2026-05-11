using Flowthru.Step.Python;

namespace Flowthru.Extensions.Python.Tests;

/// <summary>
/// Pins the user-facing service-registration surface — the
/// <see cref="PythonRuntimeOptions.RegisterService"/> entry point and the
/// <see cref="PythonServiceBuilder"/> it materialises through.
/// These tests don't touch any Python runtime; they exercise the
/// configuration-time builder logic and option records only.
/// </summary>
[TestFixture]
[Category("Python")]
public class PythonServiceBuilderTests
{
  // ── PythonRuntimeOptions defaults ───────────────────────────────────

  [Test]
  public void Options_Defaults_AreSane()
  {
    var opts = new PythonRuntimeOptions();
    Assert.That(opts.VenvPath, Is.Null);
    Assert.That(opts.UvPath, Is.EqualTo("uv"));
    Assert.That(opts.ModuleSearchPaths, Is.Empty);
    Assert.That(opts.ConfigurationSection, Is.EqualTo(string.Empty));
    Assert.That(opts.ServiceRegistrations, Is.Empty);
  }

  [Test]
  public void Options_PropertiesAreMutable()
  {
    var opts = new PythonRuntimeOptions
    {
      VenvPath = "/tmp/venv",
      ConfigurationSection = "Diarization",
    };
    opts.ModuleSearchPaths.Add("/src/python");

    Assert.That(opts.VenvPath, Is.EqualTo("/tmp/venv"));
    Assert.That(opts.ConfigurationSection, Is.EqualTo("Diarization"));
    Assert.That(opts.ModuleSearchPaths, Is.EqualTo(new[] { "/src/python" }));
  }

  // ── RegisterService argument validation ─────────────────────────────

  [TestCase(null)]
  [TestCase("")]
  [TestCase("   ")]
  public void RegisterService_NullOrWhitespaceClassPath_Throws(string? classPath)
  {
    var opts = new PythonRuntimeOptions();
    Assert.That(
      () => opts.RegisterService(classPath!, _ => { }),
      Throws.TypeOf<ArgumentException>()
        .With.Message.Contain("Service class path")
    );
  }

  [Test]
  public void RegisterService_NullConfigure_Throws()
  {
    var opts = new PythonRuntimeOptions();
    Assert.That(
      () => opts.RegisterService("Services.X", null!),
      Throws.TypeOf<ArgumentNullException>()
    );
  }

  // ── Builder + Build round-trip ──────────────────────────────────────

  [Test]
  public void RegisterService_BuilderWithoutInspector_ThrowsOnBuild()
  {
    var opts = new PythonRuntimeOptions();
    Assert.That(
      () => opts.RegisterService("Services.Foo", _ => { /* no WithInspector call */ }),
      Throws.TypeOf<InvalidOperationException>()
    );
  }

  [Test]
  public void RegisterService_WithInspector_PopulatesRegistration()
  {
    var opts = new PythonRuntimeOptions();
    opts.RegisterService("Services.PyannoteDiarizer", svc =>
      svc.WithInspector("Services.pyannote_diarizer_inspector")
    );

    Assert.That(opts.ServiceRegistrations, Has.Count.EqualTo(1));
    var reg = opts.ServiceRegistrations["Services.PyannoteDiarizer"];
    Assert.That(reg.ServiceClassPath, Is.EqualTo("Services.PyannoteDiarizer"));
    Assert.That(reg.InspectorModule, Is.EqualTo("Services.pyannote_diarizer_inspector"));
    Assert.That(reg.InspectorFunction, Is.EqualTo("inspect"),
      "Default inspector function name is 'inspect'.");
  }

  [Test]
  public void RegisterService_WithCustomInspectorFunction_PreservesFunctionName()
  {
    var opts = new PythonRuntimeOptions();
    opts.RegisterService("Services.X", svc =>
      svc.WithInspector("Services.x_inspector", function: "probe")
    );

    var reg = opts.ServiceRegistrations["Services.X"];
    Assert.That(reg.InspectorFunction, Is.EqualTo("probe"));
  }

  [Test]
  public void RegisterService_TwoServices_RegistersBoth()
  {
    var opts = new PythonRuntimeOptions();
    opts.RegisterService("Services.A", svc => svc.WithInspector("Services.a_inspector"));
    opts.RegisterService("Services.B", svc => svc.WithInspector("Services.b_inspector"));

    Assert.That(opts.ServiceRegistrations, Has.Count.EqualTo(2));
    Assert.That(opts.ServiceRegistrations.Keys,
      Is.EquivalentTo(new[] { "Services.A", "Services.B" }));
  }

  [Test]
  public void RegisterService_DuplicateClassPath_LastWriteWins()
  {
    var opts = new PythonRuntimeOptions();
    opts.RegisterService("Services.X", svc => svc.WithInspector("Services.x_first"));
    opts.RegisterService("Services.X", svc => svc.WithInspector("Services.x_second"));

    Assert.That(opts.ServiceRegistrations, Has.Count.EqualTo(1));
    Assert.That(opts.ServiceRegistrations["Services.X"].InspectorModule,
      Is.EqualTo("Services.x_second"));
  }

  [Test]
  public void RegisterService_ReturnsOptionsForChaining()
  {
    var opts = new PythonRuntimeOptions();
    Assert.That(
      opts.RegisterService("Services.X", svc => svc.WithInspector("Services.x_inspector")),
      Is.SameAs(opts)
    );
  }

  // ── PythonServiceBuilder.WithInspector argument validation ──────────

  [TestCase(null)]
  [TestCase("")]
  [TestCase("   ")]
  public void WithInspector_NullOrWhitespaceModule_Throws(string? module)
  {
    var opts = new PythonRuntimeOptions();
    Assert.That(
      () => opts.RegisterService("Services.X", svc => svc.WithInspector(module!)),
      Throws.TypeOf<ArgumentException>()
        .With.Message.Contain("Inspector module")
    );
  }

  [TestCase(null)]
  [TestCase("")]
  [TestCase("   ")]
  public void WithInspector_NullOrWhitespaceFunction_Throws(string? function)
  {
    var opts = new PythonRuntimeOptions();
    Assert.That(
      () => opts.RegisterService("Services.X", svc =>
        svc.WithInspector("Services.x_inspector", function: function!)
      ),
      Throws.TypeOf<ArgumentException>()
        .With.Message.Contain("Inspector function")
    );
  }

  // ── PythonServiceRegistration computed properties ──────────────────

  [Test]
  public void ServiceRegistration_ServiceModule_ExtractsBeforeLastDot()
  {
    var reg = new PythonServiceRegistration(
      ServiceClassPath: "Services.Sub.PyannoteDiarizer",
      InspectorModule: "Services.x_inspector",
      InspectorFunction: "inspect"
    );
    Assert.That(reg.ServiceModule, Is.EqualTo("Services.Sub"));
  }

  [Test]
  public void ServiceRegistration_ServiceClass_ExtractsAfterLastDot()
  {
    var reg = new PythonServiceRegistration(
      "Services.Sub.PyannoteDiarizer", "Services.x_inspector", "inspect"
    );
    Assert.That(reg.ServiceClass, Is.EqualTo("PyannoteDiarizer"));
  }

  [Test]
  public void ServiceRegistration_NoDot_ServiceModuleEmpty_ServiceClassWhole()
  {
    var reg = new PythonServiceRegistration(
      "PyannoteDiarizer", "Services.x_inspector", "inspect"
    );
    Assert.That(reg.ServiceModule, Is.EqualTo(string.Empty));
    Assert.That(reg.ServiceClass, Is.EqualTo("PyannoteDiarizer"));
  }
}
