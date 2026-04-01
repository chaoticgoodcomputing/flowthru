# <a id="Flowthru_Misc_ML_UMAP_Core_UmapPipeline"></a> Class UmapPipeline

Namespace: [Flowthru.Misc.ML.UMAP.Core](Flowthru.Misc.ML.UMAP.Core.md)  
Assembly: Flowthru.Misc.ML.dll  

Fluent builder for UMAP pipelines with automatic strategy selection.

```csharp
public static class UmapPipeline
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[UmapPipeline](Flowthru.Misc.ML.UMAP.Core.UmapPipeline.md)

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
This provides a low skill floor (simple defaults) with a high skill ceiling (full customization).
Strategies are resolved lazily at FitTransform() time based on data characteristics.
</p>
<p>
<b>Usage patterns:</b>
</p>
<pre><code class="lang-csharp">// Beginner: Full auto-configuration (Euclidean metric)
var result = UmapPipeline.Create().FitTransform(data);

// Intermediate: Custom metric
var result = UmapPipeline.Create()
  .WithInputMetric(CosineMetric.Instance)
  .FitTransform(data);

// Advanced: Custom strategies for testing/benchmarking
var result = UmapPipeline.Create()
  .WithNeighborSearch(new NNDescentSearch { MaxIterations = 50 })
  .FitTransform(data);</code></pre>

## Methods

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapPipeline_Create_Flowthru_Misc_ML_UMAP_Core_UmapParameters_"></a> Create\(UmapParameters?\)

Creates a new UMAP pipeline with default settings.
Euclidean metric is used by default, and strategies will be auto-selected based on data shape.

```csharp
public static UmapPipelineBuilder Create(UmapParameters? parameters = null)
```

#### Parameters

`parameters` [UmapParameters](Flowthru.Misc.ML.UMAP.Core.UmapParameters.md)?

UMAP hyperparameters (n_neighbors, min_dist, etc.).
If null, uses defaults appropriate for the data.

#### Returns

 [UmapPipelineBuilder](Flowthru.Misc.ML.UMAP.Core.UmapPipelineBuilder.md)

