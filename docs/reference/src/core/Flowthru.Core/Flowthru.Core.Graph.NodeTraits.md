# <a id="Flowthru_Core_Graph_NodeTraits"></a> Class NodeTraits

Namespace: [Flowthru.Core.Graph](Flowthru.Core.Graph.md)  
Assembly: Flowthru.Core.dll  

Base capability metadata for all DAG node types.

```csharp
public record NodeTraits : IEquatable<NodeTraits>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[NodeTraits](Flowthru.Core.Graph.NodeTraits.md)

#### Derived

[StorageTraits](Flowthru.Core.Data.Capabilities.StorageTraits.md)

#### Implements

[IEquatable<NodeTraits\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

<p>
Describes universal properties that apply to any node in the DAG. The canonical
archetype-specific extension is
<xref href="Flowthru.Core.Data.Capabilities.StorageTraits" data-throw-if-not-resolved="false"></xref> for data I/O nodes
(catalog entries — IItem). Step-level traits like <code>IsIdempotent</code> /
<code>HasSideEffects</code> are emitted by the source generator into per-step
<code>StepTraits</code> values rather than carried via NodeTraits inheritance.
</p>

## Properties

### <a id="Flowthru_Core_Graph_NodeTraits_CanInspect"></a> CanInspect

Whether this node supports pre-flight inspection / validation.

```csharp
public bool CanInspect { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="Flowthru_Core_Graph_NodeTraits_RequiresNetwork"></a> RequiresNetwork

Whether this node requires network access to operate.

```csharp
public bool RequiresNetwork { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

