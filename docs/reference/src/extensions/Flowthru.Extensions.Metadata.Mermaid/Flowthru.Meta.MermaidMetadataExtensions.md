# <a id="Flowthru_Meta_MermaidMetadataExtensions"></a> Class MermaidMetadataExtensions

Namespace: [Flowthru.Meta](Flowthru.Meta.md)  
Assembly: Flowthru.Extensions.Metadata.Mermaid.dll  

Extension methods for generating Mermaid diagram representations of DAG metadata.

```csharp
public static class MermaidMetadataExtensions
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[MermaidMetadataExtensions](Flowthru.Meta.MermaidMetadataExtensions.md)

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
Mermaid diagrams provide immediate visualization in Markdown-compatible tools
(GitHub, VS Code, etc.) without requiring a separate web application.
</p>
<p>
The generated diagram uses Mermaid flowchart syntax with:
- Steps as rectangles with rounded corners
- Catalog items as cylindrical database shapes
- Flow subgraphs grouping nodes by their origin flow
- External data (no producer) shown with special styling
- Produced data (has producer) inside their producer's Flow subgraph
</p>

## Methods

### <a id="Flowthru_Meta_MermaidMetadataExtensions_ToMermaidDiagram_Flowthru_Core_Graph_Meta_Models_DagMetadata_System_String_System_String_System_String_System_Boolean_"></a> ToMermaidDiagram\(DagMetadata, string, string, string, bool\)

Generates a Mermaid flowchart representation of the DAG, wrapped in a code fence.

```csharp
public static string ToMermaidDiagram(this DagMetadata dag, string direction = "TB", string activeStepColor = "#2E7D32", string activeItemColor = "#2E7D32", bool showFullDag = true)
```

#### Parameters

`dag` DagMetadata

The DAG metadata to visualize

`direction` [string](https://learn.microsoft.com/dotnet/api/system.string)

Flow direction code (TB, LR, BT, RL). Defaults to TB (Top to Bottom).

`activeStepColor` [string](https://learn.microsoft.com/dotnet/api/system.string)

Hex color for active (sliced) steps. Defaults to #2E7D32.

`activeItemColor` [string](https://learn.microsoft.com/dotnet/api/system.string)

Hex color for active (sliced) catalog items. Defaults to #2E7D32.

`showFullDag` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

When true (default), the full DAG is rendered with active nodes highlighted.
When false and a slice is applied, only nodes in the active slice are rendered.
Has no effect when no slice is applied.

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

Complete Markdown document with Mermaid code fence

