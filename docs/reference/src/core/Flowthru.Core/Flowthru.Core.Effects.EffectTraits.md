# <a id="Flowthru_Core_Effects_EffectTraits"></a> Class EffectTraits

Namespace: [Flowthru.Core.Effects](Flowthru.Core.Effects.md)  
Assembly: Flowthru.Core.dll  

Capability metadata for side-effect nodes.

```csharp
public record EffectTraits : NodeTraits, IEquatable<NodeTraits>, IEquatable<EffectTraits>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[NodeTraits](Flowthru.Core.Graph.NodeTraits.md) ← 
[EffectTraits](Flowthru.Core.Effects.EffectTraits.md)

#### Implements

[IEquatable<NodeTraits\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1), 
[IEquatable<EffectTraits\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

#### Inherited Members

[NodeTraits.RequiresNetwork](Flowthru.Core.Graph.NodeTraits.md\#Flowthru\_Core\_Graph\_NodeTraits\_RequiresNetwork), 
[NodeTraits.CanInspect](Flowthru.Core.Graph.NodeTraits.md\#Flowthru\_Core\_Graph\_NodeTraits\_CanInspect), 
[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

Extends <xref href="Flowthru.Core.Graph.NodeTraits" data-throw-if-not-resolved="false"></xref> with properties specific to effects —
operations that interact with external systems (webhooks, deployments,
DDL mutations, notifications).

## Properties

### <a id="Flowthru_Core_Effects_EffectTraits_HasSideEffects"></a> HasSideEffects

Whether the effect modifies external state when executed.

```csharp
public bool HasSideEffects { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="Flowthru_Core_Effects_EffectTraits_IsIdempotent"></a> IsIdempotent

Whether the effect is safe to retry without changing the outcome.

```csharp
public bool IsIdempotent { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

