# <a id="Flowthru_Core_Graph_Meta_Models_RunMetadata"></a> Class RunMetadata

Namespace: [Flowthru.Core.Graph.Meta.Models](Flowthru.Core.Graph.Meta.Models.md)  
Assembly: Flowthru.Core.dll  

Composite metadata representing a completed pipeline run.

```csharp
public class RunMetadata
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[RunMetadata](Flowthru.Core.Graph.Meta.Models.RunMetadata.md)

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
Combines the structural pre-run DAG snapshot with the post-execution results,
giving post-run metadata providers access to both the pipeline topology and
the observed execution outcomes in a single call scope.
</p>
<p>
This is the primary argument type for <xref href="Flowthru.Core.Meta.Providers.IPostRunMetadataProvider" data-throw-if-not-resolved="false"></xref>.
</p>
<p>
<strong>Example use cases:</strong>
</p>
<ul><li>Coloring a Mermaid diagram by per-step execution duration</li><li>Exporting combined diagnostic JSON (DAG structure + per-step results) for bug reports</li><li>Persisting step timings for future scheduling optimization</li></ul>

## Properties

### <a id="Flowthru_Core_Graph_Meta_Models_RunMetadata_Dag"></a> Dag

The structural DAG snapshot built during pre-flight, before any steps executed.

```csharp
[JsonPropertyName("dag")]
public required DagMetadata Dag { get; init; }
```

#### Property Value

 [DagMetadata](Flowthru.Core.Graph.Meta.Models.DagMetadata.md)

### <a id="Flowthru_Core_Graph_Meta_Models_RunMetadata_Result"></a> Result

The outcome of the pipeline run, including per-step results and timing.

```csharp
[JsonPropertyName("result")]
public required FlowResult Result { get; init; }
```

#### Property Value

 [FlowResult](Flowthru.Core.Flows.FlowResult.md)

