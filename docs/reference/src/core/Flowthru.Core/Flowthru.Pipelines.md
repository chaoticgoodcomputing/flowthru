# <a id="Flowthru_Pipelines"></a> Namespace Flowthru.Flows

### Namespaces

 [Flowthru.Flows.Validation](Flowthru.Flows.Validation.md)

### Classes

 [ExecutionOptions](Flowthru.Flows.ExecutionOptions.md)

Configuration options for pipeline execution.

 [NodeResult](Flowthru.Flows.NodeResult.md)

Represents the execution result of a single pipeline node.

 [Pipeline](Flowthru.Flows.Pipeline.md)

Represents a complete data pipeline with nodes, dependencies, and execution order.

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

Fluent builder for constructing type-safe data pipelines with function-based nodes.

 [PipelineNode](Flowthru.Flows.PipelineNode.md)

Represents a node within a pipeline, wrapping the transformation function with metadata
about its inputs, outputs, and dependencies.

 [FlowResult](Flowthru.Flows.FlowResult.md)

Represents the result of a pipeline execution.

 [FlowSliceStrategy](Flowthru.Flows.FlowSliceStrategy.md)

Defines a strategy for slicing a pipeline to execute a subset of nodes.

### Structs

 [DryRunOption](Flowthru.Flows.DryRunOption.md)

Represents a dry-run configuration. Can be assigned from a <xref href="System.Boolean" data-throw-if-not-resolved="false"></xref>
or a <xref href="Flowthru.Flows.ValidationDepth" data-throw-if-not-resolved="false"></xref> value.

### Enums

 [ValidationDepth](Flowthru.Flows.ValidationDepth.md)

Controls how deeply a dry run validates the pipeline before stopping.

