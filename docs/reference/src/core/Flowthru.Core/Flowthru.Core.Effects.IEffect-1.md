# <a id="Flowthru_Core_Effects_IEffect_1"></a> Interface IEffect<T\>

Namespace: [Flowthru.Core.Effects](Flowthru.Core.Effects.md)  
Assembly: Flowthru.Core.dll  

A DAG node representing a general side effect — an operation that interacts
with an external system (webhook, deployment, DDL mutation, notification, etc.).

```csharp
public interface IEffect<T> : INode<T>, INode
```

#### Type Parameters

`T` 

The result type of the effect. Use <xref href="Flowthru.Core.Effects.FlowUnit" data-throw-if-not-resolved="false"></xref> for fire-and-forget
effects that produce no meaningful return value.

#### Implements

[INode<T\>](Flowthru.Core.Graph.INode\-1.md), 
[INode](Flowthru.Core.Graph.INode.md)

## Remarks

<p>
<xref href="Flowthru.Core.Effects.IEffect%601.Execute" data-throw-if-not-resolved="false"></xref> is the domain-specific alias for <xref href="Flowthru.Core.Graph.INode%601.Produce" data-throw-if-not-resolved="false"></xref>.
<xref href="Flowthru.Core.Graph.INode%601.Consume(%600)" data-throw-if-not-resolved="false"></xref> triggers the effect with a payload.
</p>
<p>
<xref href="Flowthru.Core.Graph.INode.Validate" data-throw-if-not-resolved="false"></xref> is required — effect nodes without validation are
incomplete. Implementations should perform healthchecks or reachability probes
appropriate to the external system.
</p>

## Properties

### <a id="Flowthru_Core_Effects_IEffect_1_EffectTraits"></a> EffectTraits

Effect-specific capability metadata.

```csharp
EffectTraits EffectTraits { get; }
```

#### Property Value

 [EffectTraits](Flowthru.Core.Effects.EffectTraits.md)

## Methods

### <a id="Flowthru_Core_Effects_IEffect_1_Execute"></a> Execute\(\)

Executes the side effect and returns a typed result.
This is the domain alias for <xref href="Flowthru.Core.Graph.INode%601.Produce" data-throw-if-not-resolved="false"></xref>.

```csharp
FlowIO<T> Execute()
```

#### Returns

 [FlowIO](Flowthru.Core.Effects.FlowIO\-1.md)<T\>

