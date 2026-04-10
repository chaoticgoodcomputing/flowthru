# <a id="Flowthru_Core_Graph_INode_1"></a> Interface INode<T\>

Namespace: [Flowthru.Core.Graph](Flowthru.Core.Graph.md)  
Assembly: Flowthru.Core.dll  

Typed DAG node contract. Adds strongly-typed <xref href="Flowthru.Core.Graph.INode%601.Produce" data-throw-if-not-resolved="false"></xref> and
<xref href="Flowthru.Core.Graph.INode%601.Consume(%600)" data-throw-if-not-resolved="false"></xref> operations alongside the untyped engine dispatch surface.

```csharp
public interface INode<T> : INode
```

#### Type Parameters

`T` 

The data type this node produces and consumes.
Cardinality is encoded in T itself (e.g., IEnumerable&lt;TRow&gt; for collections).

#### Implements

[INode](Flowthru.Core.Graph.INode.md)

## Remarks

<p>
Default interface implementations bridge typed operations to the untyped engine surface:
<xref href="Flowthru.Core.Graph.INode.ProduceUntyped" data-throw-if-not-resolved="false"></xref> boxes the result of <xref href="Flowthru.Core.Graph.INode%601.Produce" data-throw-if-not-resolved="false"></xref>,
and <xref href="Flowthru.Core.Graph.INode.ConsumeUntyped(System.Object)" data-throw-if-not-resolved="false"></xref> casts and delegates to <xref href="Flowthru.Core.Graph.INode%601.Consume(%600)" data-throw-if-not-resolved="false"></xref>.
</p>

## Methods

### <a id="Flowthru_Core_Graph_INode_1_Consume__0_"></a> Consume\(T\)

Consumes a typed value into this node.

```csharp
FlowIO<FlowUnit> Consume(T data)
```

#### Parameters

`data` T

#### Returns

 [FlowIO](Flowthru.Core.Effects.FlowIO\-1.md)<[FlowUnit](Flowthru.Core.Effects.FlowUnit.md)\>

### <a id="Flowthru_Core_Graph_INode_1_Produce"></a> Produce\(\)

Produces this node's value as a typed effect.

```csharp
FlowIO<T> Produce()
```

#### Returns

 [FlowIO](Flowthru.Core.Effects.FlowIO\-1.md)<T\>

