using Flowthru.Step.Python;
using Flowthru.Step.Python.Internal;
using Microsoft.Extensions.Options;

namespace Flowthru.Extensions.Python.Tests;

/// <summary>
/// Pins the <see cref="PythonServiceInspectorRegistry"/> snapshot
/// behaviour. The registry is constructed once (DI singleton), takes a
/// snapshot of <see cref="PythonRuntimeOptions.ServiceRegistrations"/>
/// at construction, and exposes lookups by class path.
/// </summary>
[TestFixture]
[Category("Python")]
public class PythonServiceInspectorRegistryTests
{
  private static IPythonServiceInspectorRegistry Build(PythonRuntimeOptions opts) =>
    new PythonServiceInspectorRegistry(Options.Create(opts));

  [Test]
  public void Constructor_NullOptions_Throws()
  {
    Assert.That(
      () => new PythonServiceInspectorRegistry(null!),
      Throws.TypeOf<ArgumentNullException>()
    );
  }

  [Test]
  public void EmptyOptions_RegistrationsIsEmpty()
  {
    var registry = Build(new PythonRuntimeOptions());
    Assert.That(registry.Registrations, Is.Empty);
  }

  [Test]
  public void Registrations_ReturnsAllRegistered()
  {
    var opts = new PythonRuntimeOptions();
    opts.RegisterService("Services.A", svc => svc.WithInspector("Services.a_inspector"));
    opts.RegisterService("Services.B", svc => svc.WithInspector("Services.b_inspector"));

    var registry = Build(opts);
    Assert.That(registry.Registrations, Has.Count.EqualTo(2));
  }

  [Test]
  public void TryGet_RegisteredPath_ReturnsTrueWithRegistration()
  {
    var opts = new PythonRuntimeOptions();
    opts.RegisterService("Services.X", svc => svc.WithInspector("Services.x_inspector"));

    var registry = Build(opts);
    Assert.That(registry.TryGet("Services.X", out var reg), Is.True);
    Assert.That(reg, Is.Not.Null);
    Assert.That(reg!.InspectorModule, Is.EqualTo("Services.x_inspector"));
  }

  [Test]
  public void TryGet_UnregisteredPath_ReturnsFalseWithNull()
  {
    var registry = Build(new PythonRuntimeOptions());
    Assert.That(registry.TryGet("Services.Missing", out var reg), Is.False);
    Assert.That(reg, Is.Null);
  }

  [Test]
  public void Registry_SnapshotsAtConstruction_DoesNotSeeLaterAdds()
  {
    var opts = new PythonRuntimeOptions();
    opts.RegisterService("Services.A", svc => svc.WithInspector("Services.a_inspector"));

    var registry = Build(opts);
    // After construction, mutate the underlying options.
    opts.RegisterService("Services.B", svc => svc.WithInspector("Services.b_inspector"));

    Assert.That(registry.Registrations, Has.Count.EqualTo(1),
      "Registry should snapshot at construction; later registry mutations don't leak in.");
    Assert.That(registry.TryGet("Services.B", out _), Is.False);
  }
}
