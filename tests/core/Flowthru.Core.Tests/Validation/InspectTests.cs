using Flowthru.Prelude;
using Flowthru.Validation.PreFlight;
using Flowthru.Validation.Runtime;

namespace Flowthru.Core.Tests.Validation;

/// <summary>
/// Pins the <see cref="Inspect"/> helpers — the user-facing surface for
/// service-inspector authors. The helpers wrap a
/// <see cref="Validated{PreFlightError, FlowUnit}"/> behind an opaque
/// <see cref="InspectionResult"/>; the tests assert that the wrapped
/// validation has the expected shape so the dispatcher pipeline (which
/// pattern-matches on <c>Internal</c>) sees the right value.
/// </summary>
[TestFixture]
public class InspectTests
{
  [Test]
  public void Pass_WrapsAValidValidated()
  {
    var result = Inspect.Pass();
    Assert.That(result.Internal, Is.InstanceOf<Validated<PreFlightError, FlowUnit>.Valid>());
  }

  [Test]
  public void Fail_WrapsAnInvalidValidated_WithInspectionFailedError()
  {
    var result = Inspect.Fail("the database is unreachable", source: "MyDb");
    Assert.That(result.Internal, Is.InstanceOf<Validated<PreFlightError, FlowUnit>.Invalid>());

    var invalid = (Validated<PreFlightError, FlowUnit>.Invalid)result.Internal;
    Assert.That(invalid.Errors, Has.Count.EqualTo(1));
    Assert.That(invalid.Errors[0], Is.InstanceOf<PreFlightError.InspectionFailed>());

    var error = (PreFlightError.InspectionFailed)invalid.Errors[0];
    Assert.That(error.ItemId, Is.EqualTo("MyDb"));
    Assert.That(error.Detail, Is.EqualTo("the database is unreachable"));
  }

  [Test]
  public void Fail_DefaultSource_IsServiceLiteral()
  {
    var result = Inspect.Fail("something broke");
    var invalid = (Validated<PreFlightError, FlowUnit>.Invalid)result.Internal;
    var error = (PreFlightError.InspectionFailed)invalid.Errors[0];
    Assert.That(error.ItemId, Is.EqualTo("service"));
  }

  [Test]
  public void FailIf_FalseCondition_ReturnsPass()
  {
    var result = Inspect.FailIf(condition: false, message: "won't fire");
    Assert.That(result.Internal, Is.InstanceOf<Validated<PreFlightError, FlowUnit>.Valid>());
  }

  [Test]
  public void FailIf_TrueCondition_ReturnsFailWithMessage()
  {
    var result = Inspect.FailIf(condition: true, message: "boom", source: "X");
    var invalid = (Validated<PreFlightError, FlowUnit>.Invalid)result.Internal;
    var error = (PreFlightError.InspectionFailed)invalid.Errors[0];
    Assert.That(error.ItemId, Is.EqualTo("X"));
    Assert.That(error.Detail, Is.EqualTo("boom"));
  }
}
