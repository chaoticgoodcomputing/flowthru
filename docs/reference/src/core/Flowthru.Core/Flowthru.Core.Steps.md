# <a id="Flowthru_Core_Steps"></a> Namespace Flowthru.Core.Steps

### Classes

 [FlowthruStepAttribute](Flowthru.Core.Steps.FlowthruStepAttribute.md)

Marker attribute identifying a class as a Flowthru step definition.

 [NoParams](Flowthru.Core.Steps.NoParams.md)

Marker type for nodes that don't require parameters.
Used as the default TParameters type in StepBase&lt;TInput, TOutput, TParameters&gt;.

 [StepMetadataResolver](Flowthru.Core.Steps.StepMetadataResolver.md)

Runtime resolver that locates source-generated <code>{StepClassName}_Metadata</code> sibling
types and extracts step capability metadata from them. Used by
<code>FlowBuilder.AddStep</code> at flow-construction time to populate
<xref href="Flowthru.Core.Graph.FlowStep.ServiceDependencies" data-throw-if-not-resolved="false"></xref> from compile-time emitted metadata.

### Structs

 [StepTraits](Flowthru.Core.Steps.StepTraits.md)

Capability metadata for a step, extracted from <xref href="Flowthru.Core.Steps.FlowthruStepAttribute" data-throw-if-not-resolved="false"></xref>
at compile time and emitted into a sibling <code>_Metadata</code> static class by
<code>StepMetadataGenerator</code>.

