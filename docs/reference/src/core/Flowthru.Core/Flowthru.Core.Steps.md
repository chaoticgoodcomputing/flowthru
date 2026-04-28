# <a id="Flowthru_Core_Steps"></a> Namespace Flowthru.Core.Steps

### Classes

 [FlowthruStepAttribute](Flowthru.Core.Steps.FlowthruStepAttribute.md)

Marker attribute identifying a class as a Flowthru step definition.

 [NoData](Flowthru.Core.Steps.NoData.md)

Marker type representing "no meaningful data" for nodes with side-effects or data generation.
Used as input/output type when a step doesn't consume or produce meaningful data.

 [NoParams](Flowthru.Core.Steps.NoParams.md)

Marker type for nodes that don't require parameters.
Used as the default TParameters type in StepBase&lt;TInput, TOutput, TParameters&gt;.

