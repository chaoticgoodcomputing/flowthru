# <a id="Flowthru_Core_Graph"></a> Namespace Flowthru.Core.Graph

### Namespaces

 [Flowthru.Core.Graph.Scheduling](Flowthru.Core.Graph.Scheduling.md)

 [Flowthru.Core.Graph.Validation](Flowthru.Core.Graph.Validation.md)

### Classes

 [FlowSliceStrategy](Flowthru.Core.Graph.FlowSliceStrategy.md)

Defines a strategy for slicing a Flow to execute a subset of nodes.

 [FlowStep](Flowthru.Core.Graph.FlowStep.md)

Represents a step within a flow, wrapping the transformation function with metadata
about its inputs, outputs, and dependencies.

 [NodeTraits](Flowthru.Core.Graph.NodeTraits.md)

Base capability metadata for all DAG node types.

### Interfaces

 [INode](Flowthru.Core.Graph.INode.md)

Engine-level contract for all DAG nodes. The execution engine dispatches
<xref href="Flowthru.Core.Graph.INode.ProduceUntyped" data-throw-if-not-resolved="false"></xref>, <xref href="Flowthru.Core.Graph.INode.ConsumeUntyped(System.Object)" data-throw-if-not-resolved="false"></xref>, and <xref href="Flowthru.Core.Graph.INode.Validate" data-throw-if-not-resolved="false"></xref>
without knowing the node's specific archetype.

 [INode<T\>](Flowthru.Core.Graph.INode\-1.md)

Typed DAG node contract. Adds strongly-typed <xref href="Flowthru.Core.Graph.INode%601.Produce" data-throw-if-not-resolved="false"></xref> and
<xref href="Flowthru.Core.Graph.INode%601.Consume(%600)" data-throw-if-not-resolved="false"></xref> operations alongside the untyped engine dispatch surface.

