# <a id="Flowthru_Pipelines"></a> Namespace Flowthru.Pipelines

### Namespaces

 [Flowthru.Pipelines.Validation](Flowthru.Pipelines.Validation.md)

### Classes

 [ExecutionOptions](Flowthru.Pipelines.ExecutionOptions.md)

Configuration options for pipeline execution.

 [NodeResult](Flowthru.Pipelines.NodeResult.md)

Represents the execution result of a single pipeline node.

 [Pipeline](Flowthru.Pipelines.Pipeline.md)

Represents a complete data pipeline with nodes, dependencies, and execution order.

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

Fluent builder for constructing type-safe data pipelines with function-based nodes.

 [PipelineNode](Flowthru.Pipelines.PipelineNode.md)

Represents a node within a pipeline, wrapping the transformation function with metadata
about its inputs, outputs, and dependencies.

 [PipelineResult](Flowthru.Pipelines.PipelineResult.md)

Represents the result of a pipeline execution.

 [PipelineSliceStrategy](Flowthru.Pipelines.PipelineSliceStrategy.md)

Defines a strategy for slicing a pipeline to execute a subset of nodes.

### Structs

 [DryRunOption](Flowthru.Pipelines.DryRunOption.md)

Represents a dry-run configuration. Can be assigned from a <xref href="System.Boolean" data-throw-if-not-resolved="false"></xref>
or a <xref href="Flowthru.Pipelines.ValidationDepth" data-throw-if-not-resolved="false"></xref> value.

### Enums

 [ValidationDepth](Flowthru.Pipelines.ValidationDepth.md)

Controls how deeply a dry run validates the pipeline before stopping.

