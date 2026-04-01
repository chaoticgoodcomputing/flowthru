# <a id="Flowthru_Nodes_Factory_NodeFactory"></a> Class NodeFactory

Namespace: [Flowthru.Nodes.Factory](Flowthru.Nodes.Factory.md)  
Assembly: Flowthru.Core.dll  

Factory for creating node instances using TypeActivator.

```csharp
public static class NodeFactory
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[NodeFactory](Flowthru.Nodes.Factory.NodeFactory.md)

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
<strong>Design Pattern:</strong> Factory Pattern - provides a centralized location for
node instantiation logic.
</p>
<p>
This is a thin wrapper around TypeActivator, providing a domain-specific API for
creating nodes. Could be extended in the future with:
- Node validation logic
- Pre/post-creation hooks
- Node decoration/wrapping
</p>

## Methods

### <a id="Flowthru_Nodes_Factory_NodeFactory_Create__1"></a> Create<TNode\>\(\)

Creates a new instance of the specified node type.

```csharp
public static TNode Create<TNode>() where TNode : new()
```

#### Returns

 TNode

A new node instance

#### Type Parameters

`TNode` 

The node type to instantiate

#### Remarks

<p>
<strong>Requirements:</strong>
- TNode must inherit from NodeBase&lt;TInput, TOutput&gt;
- TNode must have a parameterless constructor
</p>
<p>
These requirements are enforced at compile-time via generic constraints in
PipelineBuilder.AddNode methods.
</p>

