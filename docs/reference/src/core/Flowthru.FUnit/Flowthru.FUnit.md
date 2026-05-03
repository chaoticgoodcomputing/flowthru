# <a id="Flowthru_FUnit"></a> Namespace Flowthru.FUnit

### Namespaces

 [Flowthru.FUnit.Samples](Flowthru.FUnit.Samples.md)

### Classes

 [EffectTestAttribute](Flowthru.FUnit.EffectTestAttribute.md)

Links a test method to an effect node. Placeholder for future effect testing
support — no source generator behavior is attached to this attribute yet.

 [FUnitContext](Flowthru.FUnit.FUnitContext.md)

Framework-agnostic base class for Flowthru step and effect tests.
Subclass this in any test framework (NUnit, xUnit, MSTest) to gain
typed step invocation, pre-flight validation, sample data helpers,
and a DI service collection scoped to the test.

 [FUnitStubContainerAttribute](Flowthru.FUnit.FUnitStubContainerAttribute.md)

Marks a static class as a stub-service container for FUnit-driven tests.
At test-fixture instantiation time, <xref href="Flowthru.FUnit.FUnitContext" data-throw-if-not-resolved="false"></xref> discovers all
<xref href="Flowthru.FUnit.FUnitStubContainerAttribute" data-throw-if-not-resolved="false"></xref>-attributed types in the test assembly
via reflection and invokes their <code>Configure(IServiceCollection)</code> method to
populate the per-test DI container.

 [StepTestAttribute](Flowthru.FUnit.StepTestAttribute.md)

Links a test method to the step type it exercises.
Consumed by <code>Flowthru.FUnit.SourceGenerators</code> to build the
<code>StepTestRegistry</code> and emit <code>FU001</code> warnings for uncovered steps.

