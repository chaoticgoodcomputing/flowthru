# <a id="Flowthru_Core_Flows"></a> Namespace Flowthru.Core.Flows

### Classes

 [ExecutionOptions](Flowthru.Core.Flows.ExecutionOptions.md)

Configuration options for pipeline execution.

 [Flow](Flowthru.Core.Flows.Flow.md)

Represents a complete data Flow with steps, dependencies, and execution order.

 [FlowBuilder](Flowthru.Core.Flows.FlowBuilder.md)

Fluent builder for constructing type-safe flows with function-based steps.

 [FlowResult](Flowthru.Core.Flows.FlowResult.md)

Represents the result of a Flow execution.

 [StepResult](Flowthru.Core.Flows.StepResult.md)

Represents the execution result of a single Flow step.

### Structs

 [DryRunOption](Flowthru.Core.Flows.DryRunOption.md)

Represents a dry-run configuration. Can be assigned from a <xref href="System.Boolean" data-throw-if-not-resolved="false"></xref>
or a <xref href="Flowthru.Core.Flows.ValidationDepth" data-throw-if-not-resolved="false"></xref> value.

### Enums

 [ValidationDepth](Flowthru.Core.Flows.ValidationDepth.md)

Controls how deeply a dry run validates the pipeline before stopping.

