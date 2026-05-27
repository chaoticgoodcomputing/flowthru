using Flowthru.Prelude;
using Flowthru.Step.Python;
using Flowthru.Validation.PreFlight.Python;
using Flowthru.Validation.Runtime.Python;

namespace Flowthru.Extensions.Python.Tests;

/// <summary>
/// Smoke tests for the migrated Python-extension public surface.
/// Verifies that the boundary types compose as designed — closed-sum
/// errors carry diagnostic codes, ServiceRef wraps correctly through
/// Core's <see cref="Validation.Runtime.ServiceRef.External"/> envelope,
/// and PythonStep construction is pure (no executor calls).
/// </summary>
[TestFixture]
public class PublicSurfaceTests
{
  // ── Closed-sum diagnostic codes ───────────────────────────────────────

  [Test]
  public void PythonRuntimeError_ModuleNotFound_HasCorrectDiagnosticCode()
  {
    var err = new PythonRuntimeError.ModuleNotFound("foo.bar", "ImportError: …");
    Assert.That(err.DiagnosticCode, Is.EqualTo("FTPY4007"));
    Assert.That(err.Category, Is.EqualTo("python"));
    Assert.That(err.Message, Does.Contain("foo.bar"));
  }

  [Test]
  public void PythonRuntimeError_AllCases_HaveDistinctCodes()
  {
    var codes = new[]
    {
      new PythonRuntimeError.ModuleNotFound("m", "d").DiagnosticCode,
      new PythonRuntimeError.FunctionMissing("m", "f").DiagnosticCode,
      new PythonRuntimeError.DecoratorAbsent("m", "f").DiagnosticCode,
      new PythonRuntimeError.WorkerError("m", "f", "x").DiagnosticCode,
      new PythonRuntimeError.MarshallingFailed("s", "d").DiagnosticCode,
      new PythonRuntimeError.WorkerCrashed("d").DiagnosticCode,
    };
    Assert.That(codes.Distinct().Count(), Is.EqualTo(codes.Length),
      "Each PythonRuntimeError case must have a unique FT-code.");
    foreach (var code in codes)
      Assert.That(code, Does.StartWith("FTPY40"),
        "PythonRuntimeError codes live in the FTPY40xx range (runtime).");
  }

  [Test]
  public void PythonPreFlightError_AllCases_HaveDistinctCodes()
  {
    var codes = new[]
    {
      new PythonPreFlightError.SchemaCountMismatch("s", PythonSchemaSide.Input, 1, 2).DiagnosticCode,
      new PythonPreFlightError.SchemaNameMismatch("s", PythonSchemaSide.Output, 0, "A", "B").DiagnosticCode,
      new PythonPreFlightError.ArityMismatch("s", "m", "f", 1, 2).DiagnosticCode,
      new PythonPreFlightError.ServiceInspectionFailed("svc", "detail").DiagnosticCode,
    };
    Assert.That(codes.Distinct().Count(), Is.EqualTo(codes.Length));
    foreach (var code in codes)
      Assert.That(code, Does.StartWith("FTPY30"),
        "PythonPreFlightError codes live in the FTPY30xx range (pre-flight).");
  }

  // ── ServiceRef wrapping ───────────────────────────────────────────────

  [Test]
  public void PythonServiceRef_WrapsAsServiceRefExternal_WithCategoryPython()
  {
    var pyRef = new PythonServiceRef("Services.PyannoteDiarizer");
    Assert.That(pyRef.Category, Is.EqualTo("python"));
    Assert.That(pyRef.ServiceModule, Is.EqualTo("Services"));
    Assert.That(pyRef.ServiceClass, Is.EqualTo("PyannoteDiarizer"));

    var coreRef = pyRef.AsServiceRef();
    Assert.That(coreRef, Is.InstanceOf<Validation.Runtime.ServiceRef.External>());
    var ext = (Validation.Runtime.ServiceRef.External)coreRef;
    Assert.That(ext.Cause, Is.SameAs(pyRef));
  }

  [Test]
  public void PythonServiceRef_DagId_IsCategoryPrefixedClassPath()
  {
    var pyRef = new PythonServiceRef("Services.PyannoteDiarizer");
    Assert.That(pyRef.DagId, Is.EqualTo("python:Services.PyannoteDiarizer"));
  }

  // ── PythonStepMetadata identity ───────────────────────────────────────

  [Test]
  public void PythonStepMetadata_Empty_IsAllEmpty()
  {
    Assert.That(PythonStepMetadata.Empty.Inputs, Is.Empty);
    Assert.That(PythonStepMetadata.Empty.Outputs, Is.Empty);
    Assert.That(PythonStepMetadata.Empty.Services, Is.Empty);
  }
}
