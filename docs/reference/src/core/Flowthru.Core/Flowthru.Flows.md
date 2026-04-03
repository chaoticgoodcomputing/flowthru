# <a id="Flowthru_Flows"></a> Namespace Flowthru.Flows

### Namespaces

 [Flowthru.Flows.Validation](Flowthru.Flows.Validation.md)

### Classes

 [ExecutionOptions](Flowthru.Flows.ExecutionOptions.md)

Configuration options for pipeline execution.

 [Flow](Flowthru.Flows.Flow.md)

Represents a complete data Flow with steps, dependencies, and execution order.

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

Fluent builder for constructing type-safe flows with function-based steps.

 [FlowResult](Flowthru.Flows.FlowResult.md)

Represents the result of a Flow execution.

 [FlowSliceStrategy](Flowthru.Flows.FlowSliceStrategy.md)

Defines a strategy for slicing a Flow to execute a subset of nodes.

 [FlowStep](Flowthru.Flows.FlowStep.md)

Represents a step within a flow, wrapping the transformation function with metadata
about its inputs, outputs, and dependencies.

 [StepResult](Flowthru.Flows.StepResult.md)

Represents the execution result of a single Flow step.

### Structs

 [DryRunOption](Flowthru.Flows.DryRunOption.md)

Represents a dry-run configuration. Can be assigned from a <xref href="System.Boolean" data-throw-if-not-resolved="false"></xref>
or a <xref href="Flowthru.Flows.ValidationDepth" data-throw-if-not-resolved="false"></xref> value.

### Enums

 [ValidationDepth](Flowthru.Flows.ValidationDepth.md)

Controls how deeply a dry run validates the pipeline before stopping.

