using Flowthru.Extensions.Python.Runtime;
using Flowthru.Extensions.Python.Services;
using Microsoft.Extensions.Options;

namespace Flowthru.Extensions.Python.Tests.Services;

/// <summary>
/// Tests for <see cref="PythonServiceInspectorRegistry"/>'s lookup surface.
/// The registry snapshots <see cref="PythonRuntimeOptions.ServiceRegistrations"/>
/// at construction so the in-DI behaviour is independent of any later
/// mutation to the options object.
/// </summary>
[TestFixture]
[Category("Python")]
[Category("Services")]
public class PythonServiceInspectorRegistryTests
{
  // ── Empty / missing lookups ─────────────────────────────────────────

  [Test]
  public void Empty_Registrations_ReturnsEmptyCollection()
  {
    var registry = BuildRegistry(opts => { /* no registrations */ });
    Assert.That(registry.Registrations, Is.Empty);
  }

  [Test]
  public void TryGet_UnknownPath_ReturnsFalse()
  {
    var registry = BuildRegistry(opts =>
      opts.RegisterService("Services.Known", svc => svc.WithInspector("Services.known_inspector"))
    );

    var found = registry.TryGet("Services.Unknown", out var registration);

    Assert.Multiple(() =>
    {
      Assert.That(found, Is.False);
      Assert.That(registration, Is.Null);
    });
  }

  // ── Successful lookup ───────────────────────────────────────────────

  [Test]
  public void TryGet_KnownPath_ReturnsRegistration()
  {
    var registry = BuildRegistry(opts =>
      opts.RegisterService(
        "Services.pyannote_diarizer.PyannoteDiarizer",
        svc => svc.WithInspector("Services.pyannote_diarizer_inspector")
      )
    );

    var found = registry.TryGet(
      "Services.pyannote_diarizer.PyannoteDiarizer",
      out var registration
    );

    Assert.Multiple(() =>
    {
      Assert.That(found, Is.True);
      Assert.That(registration, Is.Not.Null);
      Assert.That(
        registration!.ServiceClassPath,
        Is.EqualTo("Services.pyannote_diarizer.PyannoteDiarizer")
      );
      Assert.That(
        registration.InspectorModule,
        Is.EqualTo("Services.pyannote_diarizer_inspector")
      );
      Assert.That(registration.InspectorFunction, Is.EqualTo("inspect"));
    });
  }

  [Test]
  public void Registrations_ExposesAllEntries()
  {
    var registry = BuildRegistry(opts =>
    {
      opts.RegisterService("A.B", svc => svc.WithInspector("a_inspector"));
      opts.RegisterService("C.D", svc => svc.WithInspector("c_inspector"));
      opts.RegisterService("E.F", svc => svc.WithInspector("e_inspector"));
    });

    Assert.That(registry.Registrations.Count, Is.EqualTo(3));
    Assert.That(
      registry.Registrations.Select(r => r.ServiceClassPath),
      Is.EquivalentTo(new[] { "A.B", "C.D", "E.F" })
    );
  }

  // ── Snapshot semantics ──────────────────────────────────────────────

  [Test]
  public void Snapshot_LaterMutationOfOptions_DoesNotLeakIntoRegistry()
  {
    // The registry copies the dictionary at construction. Any registration
    // added to the options after the registry was built is invisible to
    // it. This pins the singleton-resolves-once contract.
    var options = new PythonRuntimeOptions();
    options.RegisterService("Initial", svc => svc.WithInspector("initial_inspector"));

    var registry = new PythonServiceInspectorRegistry(Options.Create(options));

    // Mutate after construction.
    options.RegisterService("LaterAdded", svc => svc.WithInspector("later_inspector"));

    Assert.That(registry.Registrations.Count, Is.EqualTo(1));
    Assert.That(registry.TryGet("Initial", out _), Is.True);
    Assert.That(registry.TryGet("LaterAdded", out _), Is.False);
  }

  // ── Constructor validation ──────────────────────────────────────────

  [Test]
  public void Constructor_NullOptions_Throws()
  {
    Assert.Throws<ArgumentNullException>(() =>
      new PythonServiceInspectorRegistry(null!)
    );
  }

  // ── Helper ──────────────────────────────────────────────────────────

  private static PythonServiceInspectorRegistry BuildRegistry(
    Action<PythonRuntimeOptions> configure
  )
  {
    var options = new PythonRuntimeOptions();
    configure(options);
    return new PythonServiceInspectorRegistry(Options.Create(options));
  }
}
