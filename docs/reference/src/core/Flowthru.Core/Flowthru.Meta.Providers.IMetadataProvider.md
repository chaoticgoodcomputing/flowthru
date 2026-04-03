# <a id="Flowthru_Meta_Providers_IMetadataProvider"></a> Interface IMetadataProvider

Namespace: [Flowthru.Meta.Providers](Flowthru.Meta.Providers.md)  
Assembly: Flowthru.Core.dll  

Interface for metadata consumers.

```csharp
public interface IMetadataProvider
```

## Remarks

<p>
Metadata providers receive DAG metadata after pipeline builds and can
process it in any way: write files, send to APIs, store in memory, etc.
</p>
<p>
<strong>Built-in Providers:</strong>
</p>
<ul><li><xref href="Flowthru.Meta.Providers.JsonMetadataProvider" data-throw-if-not-resolved="false"></xref> - Exports JSON files</li><li><xref href="Flowthru.Meta.Providers.MermaidMetadataProvider" data-throw-if-not-resolved="false"></xref> - Exports Mermaid diagrams</li></ul>
<p>
<strong>Custom Provider Example:</strong>
</p>
<pre><code class="lang-csharp">public class DashboardMetadataProvider : IMetadataProvider
{
  private readonly IDashboardClient _client;

  public DashboardMetadataProvider(IDashboardClient client)
  {
    _client = client;
  }

  public string Name =&gt; "Dashboard";

  public void Consume(DagMetadata dag)
  {
    _client.SendVisualization(dag);
  }
}</code></pre>

## Properties

### <a id="Flowthru_Meta_Providers_IMetadataProvider_Name"></a> Name

Gets the unique name of this provider.

```csharp
string Name { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Methods

### <a id="Flowthru_Meta_Providers_IMetadataProvider_Consume_Flowthru_Meta_Models_DagMetadata_"></a> Consume\(DagMetadata\)

Consumes DAG metadata.

```csharp
void Consume(DagMetadata dag)
```

#### Parameters

`dag` [DagMetadata](Flowthru.Meta.Models.DagMetadata.md)

The DAG metadata to consume

#### Remarks

This method is called after pipeline builds. Providers can process
the metadata in any way: write files, send to APIs, store in memory, etc.

Implementations should handle their own error recovery - exceptions thrown
from this method will be logged but will not fail the pipeline execution.

