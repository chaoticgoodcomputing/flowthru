using Flowthru.Step;
using Flowthru.Step.Marshalling;
using Flowthru.Step.Python;

namespace Flowthru.Extensions.Python.Tests;

/// <summary>
/// Contract tests for the Phase 9 <see cref="PythonStepExtension"/>
/// descriptor. The class is purely declarative — its only behaviour
/// is the attribute it carries and the marker interfaces it
/// implements, so the tests assert on type metadata rather than
/// runtime invocation.
/// </summary>
/// <remarks>
/// The corresponding analyzer-level coverage lives in
/// <c>Ft1301ExtensionMinimumContainerSupportTests</c> and
/// <c>Ft1303ExtensionCapabilityMarshallerAlignmentTests</c> in the
/// Core source-gen test project. Those validate the analyzers
/// against synthetic descriptors; these tests confirm the real
/// Python descriptor lines up.
/// </remarks>
[TestFixture]
public class PythonStepExtensionDescriptorTests
{
  [Test]
  public void PythonStepExtension_ImplementsIStepExtension()
  {
    Assert.That(typeof(IStepExtension).IsAssignableFrom(typeof(PythonStepExtension)), Is.True,
      "PythonStepExtension must implement IStepExtension so analyzers can locate it.");
  }

  [Test]
  public void PythonStepExtension_ImplementsContainerMarshaller()
  {
    Assert.That(
      typeof(IContainerMarshaller<PythonStepExtension>).IsAssignableFrom(typeof(PythonStepExtension)),
      Is.True,
      "PythonStepExtension must implement IContainerMarshaller<Self> as evidence "
      + "of Singleton | Enumerable support.");
  }

  [Test]
  public void PythonStepExtension_DoesNotImplementQueryableMarshaller()
  {
    // The subprocess executor doesn't push computation down into the
    // data source; declaring Queryable would be a false claim, and
    // FT1303 would flag it.
    Assert.That(
      typeof(IQueryableMarshaller<PythonStepExtension>).IsAssignableFrom(typeof(PythonStepExtension)),
      Is.False);
  }

  [Test]
  public void PythonStepExtension_DeclaresProductionFloorCapabilities()
  {
    var attr = typeof(PythonStepExtension)
      .GetCustomAttributes(typeof(StepExtensionCapabilitiesAttribute), inherit: false)
      .OfType<StepExtensionCapabilitiesAttribute>()
      .SingleOrDefault();

    Assert.That(attr, Is.Not.Null,
      "PythonStepExtension must carry [StepExtensionCapabilities].");
    Assert.That(attr!.Status, Is.EqualTo(ExtensionStatus.Production),
      "The descriptor is intended for production use (default status).");

    var floor = StepContainerKind.Singleton | StepContainerKind.Enumerable;
    Assert.That(attr.Inputs & floor, Is.EqualTo(floor),
      "Inputs must include the Singleton | Enumerable floor.");
    Assert.That(attr.Outputs & floor, Is.EqualTo(floor),
      "Outputs must include the Singleton | Enumerable floor.");

    // Negative assertion — Phase 9's deliberate scope:
    Assert.That(attr.Inputs & StepContainerKind.Queryable, Is.EqualTo(StepContainerKind.None),
      "The Python extension does not currently support Queryable inputs.");
    Assert.That(attr.Inputs & StepContainerKind.Source, Is.EqualTo(StepContainerKind.None),
      "The Python extension does not currently declare Source (FlowSource) inputs — "
      + "it consumes the eager Enumerable view and marshals via Arrow (ADR-0023).");
  }
}
