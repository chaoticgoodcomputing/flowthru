# <a id="Flowthru_Core_Graph_INode"></a> Interface INode

Namespace: [Flowthru.Core.Graph](Flowthru.Core.Graph.md)  
Assembly: Flowthru.Core.dll  

Engine-level contract for all DAG nodes. The execution engine dispatches
<xref href="Flowthru.Core.Graph.INode.ProduceUntyped" data-throw-if-not-resolved="false"></xref>, <xref href="Flowthru.Core.Graph.INode.ConsumeUntyped(System.Object)" data-throw-if-not-resolved="false"></xref>, and <xref href="Flowthru.Core.Graph.INode.Validate" data-throw-if-not-resolved="false"></xref>
without knowing the node's specific archetype.

```csharp
public interface INode
```

## Remarks

<p>
The canonical concrete <xref href="Flowthru.Core.Graph.INode" data-throw-if-not-resolved="false"></xref> implementation is
<xref href="Flowthru.Core.Data.IItem" data-throw-if-not-resolved="false"></xref> (catalog entries — data sources and sinks).
<xref href="Flowthru.Core.Graph.DependencyAnalyzer" data-throw-if-not-resolved="false"></xref> resolves dependencies using only
<xref href="Flowthru.Core.Graph.INode.Label" data-throw-if-not-resolved="false"></xref>; the engine is archetype-agnostic. Side-effect operations live
inside steps as the canonical pattern (see CONTRIBUTING.md "Side effects in flows"),
not as a separate node archetype.
</p>

## Properties

### <a id="Flowthru_Core_Graph_INode_DataType"></a> DataType

The runtime type of the value this node produces.
For singletons: typeof(T). For collections: typeof(IEnumerable&lt;T&gt;).

```csharp
Type DataType { get; }
```

#### Property Value

 [Type](https://learn.microsoft.com/dotnet/api/system.type)

### <a id="Flowthru_Core_Graph_INode_Label"></a> Label

Unique label identifying this node within the DAG.
Used for dependency resolution and wiring.

```csharp
string Label { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="Flowthru_Core_Graph_INode_Traits"></a> Traits

Capability metadata describing this node's properties and constraints.

```csharp
NodeTraits Traits { get; }
```

#### Property Value

 [NodeTraits](Flowthru.Core.Graph.NodeTraits.md)

## Methods

### <a id="Flowthru_Core_Graph_INode_ConsumeUntyped_System_Object_"></a> ConsumeUntyped\(object\)

Consumes an untyped value into this node.
The engine calls this to save output data from upstream steps.

```csharp
FlowIO<FlowUnit> ConsumeUntyped(object data)
```

#### Parameters

`data` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [FlowIO](Flowthru.Core.Effects.FlowIO\-1.md)<[FlowUnit](Flowthru.Core.Effects.FlowUnit.md)\>

### <a id="Flowthru_Core_Graph_INode_ProduceUntyped"></a> ProduceUntyped\(\)

Produces this node's value as an untyped object.
The engine calls this to load input data for downstream steps.

```csharp
FlowIO<object> ProduceUntyped()
```

#### Returns

 [FlowIO](Flowthru.Core.Effects.FlowIO\-1.md)<[object](https://learn.microsoft.com/dotnet/api/system.object)\>

### <a id="Flowthru_Core_Graph_INode_Validate"></a> Validate\(\)

Pre-flight validation. Semantics vary by archetype:
data items check existence and schema, effects perform healthchecks,
steps return success (correctness validated via tests).

```csharp
FlowIO<ValidationResult> Validate()
```

#### Returns

 [FlowIO](Flowthru.Core.Effects.FlowIO\-1.md)<[ValidationResult](Flowthru.Core.Data.Validation.ValidationResult.md)\>

