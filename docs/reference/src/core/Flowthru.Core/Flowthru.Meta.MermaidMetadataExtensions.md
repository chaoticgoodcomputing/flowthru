# <a id="Flowthru_Meta_MermaidMetadataExtensions"></a> Class MermaidMetadataExtensions

Namespace: [Flowthru.Meta](Flowthru.Meta.md)  
Assembly: Flowthru.Core.dll  

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
- Nodes as rectangles with rounded corners
- Catalog entries as cylindrical database shapes
- Pipeline subgraphs grouping nodes by their origin pipeline
- External data (no producer) shown with special styling
- Produced data (has producer) inside their producer's pipeline subgraph
</p>

## Methods

### <a id="Flowthru_Meta_MermaidMetadataExtensions_ToMermaidDiagram_Flowthru_Meta_Models_DagMetadata_System_String_System_String_System_String_"></a> ToMermaidDiagram\(DagMetadata, string, string, string\)

Generates a Mermaid flowchart representation of the DAG, wrapped in a code fence.

```csharp
public static string ToMermaidDiagram(this DagMetadata dag, string direction = "TB", string activeNodeColor = "#2E7D32", string activeDataColor = "#2E7D32")
```

#### Parameters

`dag` [DagMetadata](Flowthru.Meta.Models.DagMetadata.md)

The DAG metadata to visualize

`direction` [string](https://learn.microsoft.com/dotnet/api/system.string)

Flow direction code (TB, LR, BT, RL). Defaults to TB (Top to Bottom).

`activeNodeColor` [string](https://learn.microsoft.com/dotnet/api/system.string)

Hex color for active (sliced) nodes. Defaults to #2E7D32.

`activeDataColor` [string](https://learn.microsoft.com/dotnet/api/system.string)

Hex color for active (sliced) catalog entries. Defaults to #2E7D32.

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

Complete Markdown document with Mermaid code fence

#### Remarks

<p>
The output is a valid Markdown document that can be saved as a .md file
and rendered by any Mermaid-compatible viewer.
</p>
<p>
<strong>Example output:</strong>
</p>
<pre><code class="lang-csharp">```mermaid
flowchart TB
    RawCompanies[("Raw Companies")]

    subgraph DataProcessing["DataProcessing"]
        PreprocessCompanies["Preprocess Companies"]
        CleanedCompanies[("Cleaned Companies")]
        RawCompanies --&gt; PreprocessCompanies
        PreprocessCompanies --&gt; CleanedCompanies
    end
```</code></pre>

