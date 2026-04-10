# <a id="Flowthru_FUnit"></a> Namespace Flowthru.FUnit

### Namespaces

 [Flowthru.FUnit.Samples](Flowthru.FUnit.Samples.md)

### Classes

 [EffectTestAttribute](Flowthru.FUnit.EffectTestAttribute.md)

Links a test method to an effect node. Placeholder for future effect testing
support — no source generator behavior is attached to this attribute yet.

 [FunitContext](Flowthru.FUnit.FunitContext.md)

Framework-agnostic base class for Flowthru step and effect tests.
Subclass this in any test framework (NUnit, xUnit, MSTest) to gain
typed step invocation, pre-flight validation, sample data helpers,
and a DI service collection scoped to the test.

 [StepTestAttribute](Flowthru.FUnit.StepTestAttribute.md)

Links a test method to the step type it exercises.
Consumed by <code>Flowthru.FUnit.SourceGenerators</code> to build the
<code>StepTestRegistry</code> and emit <code>FU001</code> warnings for uncovered steps.

