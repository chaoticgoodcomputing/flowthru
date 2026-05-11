using Flowthru.Diagnostics;
using Flowthru.Validation.PreFlight;

namespace Flowthru.Core.Tests.Diagnostics;

/// <summary>
/// Pins the FT3xxx diagnostic-code dispatch in
/// <see cref="PreFlightErrorClassifier"/>. Each closed-sum case must map to
/// its declared <c>FlowthruDiagnosticCodes.PreFlight*</c> constant; the
/// <see cref="PreFlightError.External"/> variant defers to its embedded
/// <see cref="IExtensionPreFlightError"/> payload.
/// </summary>
[TestFixture]
public class PreFlightErrorClassifierTests
{
  [Test]
  public void Classify_DuplicateProducer_AssignsFT3001()
  {
    var report = PreFlightErrorClassifier.Classify(
      new PreFlightError.DuplicateProducer("item.A", new[] { "step.X", "step.Y" })
    );
    Assert.That(
      report.DiagnosticCode,
      Is.EqualTo(FlowthruDiagnosticCodes.PreFlightDuplicateProducer)
    );
    Assert.That(report.Category, Is.EqualTo("DuplicateProducer"));
  }

  [Test]
  public void Classify_CircularDependency_AssignsFT3002()
  {
    var report = PreFlightErrorClassifier.Classify(
      new PreFlightError.CircularDependency(new[] { "step.A", "step.B", "step.A" })
    );
    Assert.That(
      report.DiagnosticCode,
      Is.EqualTo(FlowthruDiagnosticCodes.PreFlightCircularDependency)
    );
    Assert.That(report.Category, Is.EqualTo("CircularDependency"));
  }

  [Test]
  public void Classify_MissingInput_AssignsFT3003()
  {
    var report = PreFlightErrorClassifier.Classify(
      new PreFlightError.MissingInput("item.A", "/path/to/file.csv")
    );
    Assert.That(
      report.DiagnosticCode,
      Is.EqualTo(FlowthruDiagnosticCodes.PreFlightMissingInput)
    );
    Assert.That(report.Category, Is.EqualTo("MissingInput"));
  }

  [Test]
  public void Classify_SchemaDrift_AssignsFT3004()
  {
    var report = PreFlightErrorClassifier.Classify(
      new PreFlightError.SchemaDrift("item.A", Expected: "int", Actual: "string")
    );
    Assert.That(
      report.DiagnosticCode,
      Is.EqualTo(FlowthruDiagnosticCodes.PreFlightSchemaDrift)
    );
    Assert.That(report.Category, Is.EqualTo("SchemaDrift"));
  }

  [Test]
  public void Classify_InspectionFailed_AssignsFT3005()
  {
    var report = PreFlightErrorClassifier.Classify(
      new PreFlightError.InspectionFailed("item.A", "probe rejected")
    );
    Assert.That(
      report.DiagnosticCode,
      Is.EqualTo(FlowthruDiagnosticCodes.PreFlightInspectionFailed)
    );
    Assert.That(report.Category, Is.EqualTo("InspectionFailed"));
  }

  [Test]
  public void Classify_RegistrationCheckFailed_AssignsFT3006()
  {
    var report = PreFlightErrorClassifier.Classify(
      new PreFlightError.RegistrationCheckFailed(
        HookId: "EFCore.Connection[Db]",
        CheckMessage: "db unreachable"
      )
    );
    Assert.That(
      report.DiagnosticCode,
      Is.EqualTo(FlowthruDiagnosticCodes.PreFlightRegistrationCheckFailed)
    );
    Assert.That(report.Category, Is.EqualTo("RegistrationCheckFailed"));
  }

  [Test]
  public void Classify_ExtensionError_DispatchesToExtensionPayload()
  {
    var ext = new FakeExtensionPreFlightError(
      "decorator references unknown schema",
      "PythonDecorator",
      "FT3500"
    );
    var report = PreFlightErrorClassifier.Classify(new PreFlightError.External(ext));
    Assert.That(
      report.DiagnosticCode,
      Is.EqualTo("FT3500"),
      "Classifier should pull the diagnostic code from the IExtensionPreFlightError payload, "
        + "not assign FT3xxx itself."
    );
    Assert.That(report.Category, Is.EqualTo("PythonDecorator"));
  }

  [Test]
  public void Classify_NullError_Throws()
  {
    Assert.That(
      () => PreFlightErrorClassifier.Classify(null!),
      Throws.TypeOf<ArgumentNullException>()
    );
  }

  private sealed record FakeExtensionPreFlightError(
    string Message,
    string Category,
    string DiagnosticCode
  ) : IExtensionPreFlightError;
}
