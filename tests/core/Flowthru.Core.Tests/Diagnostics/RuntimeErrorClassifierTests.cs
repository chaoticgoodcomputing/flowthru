using Flowthru.Diagnostics;
using Flowthru.Validation.PreFlight;
using Flowthru.Validation.Runtime;

namespace Flowthru.Core.Tests.Diagnostics;

[TestFixture]
public class RuntimeErrorClassifierTests
{
  [Test]
  public void Classify_External_AssignsFT4001()
  {
    var report = RuntimeErrorClassifier.Classify(
      new RuntimeError.External("source", new Exception("boom"))
    );
    Assert.That(report.DiagnosticCode, Is.EqualTo(FlowthruDiagnosticCodes.RuntimeExternalFailure));
    Assert.That(report.Category, Is.EqualTo("External"));
  }

  [Test]
  public void Classify_StepFailed_AssignsFT4002()
  {
    var report = RuntimeErrorClassifier.Classify(
      new RuntimeError.StepFailed("step.A", new RuntimeError.External("src", new Exception("boom")))
    );
    Assert.That(report.DiagnosticCode, Is.EqualTo(FlowthruDiagnosticCodes.RuntimeStepFailed));
  }

  [Test]
  public void Classify_Cancelled_AssignsFT4003()
  {
    var report = RuntimeErrorClassifier.Classify(new RuntimeError.Cancelled("user"));
    Assert.That(report.DiagnosticCode, Is.EqualTo(FlowthruDiagnosticCodes.RuntimeCancelled));
  }

  [Test]
  public void Classify_InvariantViolated_AssignsFT4004()
  {
    var report = RuntimeErrorClassifier.Classify(
      new RuntimeError.InvariantViolated("check.X", "reason")
    );
    Assert.That(report.DiagnosticCode, Is.EqualTo(FlowthruDiagnosticCodes.RuntimeInvariantViolated));
  }

  [Test]
  public void Classify_ExtensionError_DispatchesToExtensionPayload()
  {
    var ext = new FakeExtensionRuntimeError("ext-message", "PythonRuntime", "FT4500");
    var report = RuntimeErrorClassifier.Classify(new RuntimeError.ExtensionError(ext));
    Assert.That(report.DiagnosticCode, Is.EqualTo("FT4500"),
      "Classifier should pull the diagnostic code from the IExtensionRuntimeError payload, not assign FT4xxx itself.");
    Assert.That(report.Category, Is.EqualTo("PythonRuntime"));
  }

  [Test]
  public void Format_InvariantViolated_RendersFileAnIssueAffordance()
  {
    var report = RuntimeErrorClassifier.Classify(
      new RuntimeError.InvariantViolated("preflight", "missing-check")
    );
    var rendered = ConsoleErrorFormatter.Format(report);
    Assert.That(rendered, Does.Contain("FT4004"));
    Assert.That(rendered, Does.Contain("bug in Flowthru"),
      "InvariantViolated should render with a 'this is a Flowthru bug' affordance per §2.5.");
  }

  // ── PreFlightFailed: surfaces inner FT3xxx code, NOT FT4004 ───────────

  [Test]
  public void Classify_PreFlightFailed_WithMissingInput_AssignsFT3003_NotFT4004()
  {
    var report = RuntimeErrorClassifier.Classify(
      new RuntimeError.PreFlightFailed(
        new PreFlightError.MissingInput("MyInput", "Source 'foo.csv' does not exist")
      )
    );
    Assert.That(
      report.DiagnosticCode,
      Is.EqualTo(FlowthruDiagnosticCodes.PreFlightMissingInput),
      "Missing input is a user-actionable pre-flight error — it must surface as FT3003, "
        + "not get wrapped in FT4004 ('bug in Flowthru'). Users should not be told to file "
        + "an issue when their own input file is missing."
    );
    Assert.That(report.Category, Is.EqualTo("MissingInput"));
  }

  [Test]
  public void Classify_PreFlightFailed_WithDuplicateProducer_AssignsFT3001()
  {
    var report = RuntimeErrorClassifier.Classify(
      new RuntimeError.PreFlightFailed(
        new PreFlightError.DuplicateProducer("itemA", new[] { "step.X", "step.Y" })
      )
    );
    Assert.That(report.DiagnosticCode, Is.EqualTo(FlowthruDiagnosticCodes.PreFlightDuplicateProducer));
    Assert.That(report.Category, Is.EqualTo("DuplicateProducer"));
  }

  [Test]
  public void Classify_PreFlightFailed_WithCircularDependency_AssignsFT3002()
  {
    var report = RuntimeErrorClassifier.Classify(
      new RuntimeError.PreFlightFailed(
        new PreFlightError.CircularDependency(new[] { "step.A", "step.B", "step.A" })
      )
    );
    Assert.That(report.DiagnosticCode, Is.EqualTo(FlowthruDiagnosticCodes.PreFlightCircularDependency));
  }

  [Test]
  public void Format_PreFlightFailed_DoesNotMentionBugInFlowthru()
  {
    var report = RuntimeErrorClassifier.Classify(
      new RuntimeError.PreFlightFailed(
        new PreFlightError.MissingInput("SrcInventory", "data source does not exist")
      )
    );
    var rendered = ConsoleErrorFormatter.Format(report);
    Assert.That(rendered, Does.Not.Contain("bug in Flowthru"),
      "A legitimate user-actionable pre-flight failure must not be rendered as a Flowthru "
        + "bug. The user fixes their inputs; they do not file a framework issue.");
    Assert.That(rendered, Does.Contain("FT3003"));
  }

  private sealed record FakeExtensionRuntimeError(
    string Message,
    string Category,
    string DiagnosticCode
  ) : IExtensionRuntimeError;
}
