# <a id="Flowthru_Core_Meta_Providers_IPostRunMetadataProvider"></a> Interface IPostRunMetadataProvider

Namespace: [Flowthru.Core.Meta.Providers](Flowthru.Core.Meta.Providers.md)  
Assembly: Flowthru.Core.dll  

Optional interface for metadata providers that also want to receive post-run execution data.

```csharp
public interface IPostRunMetadataProvider
```

## Remarks

<p>
Implement this interface alongside <xref href="Flowthru.Core.Meta.Providers.IMetadataProvider" data-throw-if-not-resolved="false"></xref> to opt into the post-run
metadata lifecycle. The infrastructure checks for this interface after each real pipeline
execution and calls <xref href="Flowthru.Core.Meta.Providers.IPostRunMetadataProvider.Consume(Flowthru.Core.Graph.Meta.Models.RunMetadata)" data-throw-if-not-resolved="false"></xref> with a composite of the pre-run DAG snapshot
and the completed <xref href="Flowthru.Core.Flows.FlowResult" data-throw-if-not-resolved="false"></xref>.
</p>
<p>
Post-run providers are <strong>not</strong> invoked during dry runs.
</p>
<p>
Errors thrown from <xref href="Flowthru.Core.Meta.Providers.IPostRunMetadataProvider.Consume(Flowthru.Core.Graph.Meta.Models.RunMetadata)" data-throw-if-not-resolved="false"></xref> are logged and suppressed — they will never
fail the pipeline execution.
</p>
<p>
<strong>Example — coloring a Mermaid diagram by step duration:</strong>
</p>
<pre><code class="lang-csharp">public class TimingMermaidProvider : IMetadataProvider, IPostRunMetadataProvider
{
    public string Name =&gt; "TimingMermaid";

    // Pre-run: export a plain structural diagram
    public void Consume(DagMetadata dag) { ... }

    // Post-run: export a diagram color-coded by actual execution time
    public void Consume(RunMetadata run)
    {
        foreach (var step in run.Dag.Steps)
        {
            if (run.Result.StepResults.TryGetValue(step.Id, out var stepResult))
            {
                // use stepResult.ExecutionTime to drive node styling
            }
        }
    }
}</code></pre>

## Methods

### <a id="Flowthru_Core_Meta_Providers_IPostRunMetadataProvider_Consume_Flowthru_Core_Graph_Meta_Models_RunMetadata_"></a> Consume\(RunMetadata\)

Consumes composite post-run metadata combining the DAG snapshot and execution results.

```csharp
void Consume(RunMetadata run)
```

#### Parameters

`run` [RunMetadata](Flowthru.Core.Graph.Meta.Models.RunMetadata.md)

The combined run metadata, containing both the pre-run DAG structure and
the execution outcome for all steps.

