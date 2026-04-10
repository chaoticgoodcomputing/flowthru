# <a id="Flowthru_Misc_ML_UMAP_Core_UmapFlowBuilder"></a> Class UmapFlowBuilder

Namespace: [Flowthru.Misc.ML.UMAP.Core](Flowthru.Misc.ML.UMAP.Core.md)  
Assembly: Flowthru.Misc.ML.dll  

Builder for configuring and executing UMAP dimensionality reduction with flexible strategy selection.

```csharp
public sealed class UmapFlowBuilder
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[UmapFlowBuilder](Flowthru.Misc.ML.UMAP.Core.UmapFlowBuilder.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Methods

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapFlowBuilder_FitTransform_System_Single_____"></a> FitTransform\(float\[\]\[\]\)

Fits UMAP and transforms data in one step.
Auto-selects strategies based on data characteristics if not explicitly set.

```csharp
public float[][] FitTransform(float[][] data)
```

#### Parameters

`data` [float](https://learn.microsoft.com/dotnet/api/system.single)\[\]\[\]

Input data as jagged array (n_samples, n_features)

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)\[\]\[\]

Low-dimensional embedding (n_samples, n_components)

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapFlowBuilder_FitTransform_MathNet_Numerics_LinearAlgebra_Matrix_System_Single__"></a> FitTransform\(Matrix<float\>\)

Fits UMAP and transforms data in one step.
Auto-selects strategies based on data characteristics if not explicitly set.

```csharp
public Matrix<float> FitTransform(Matrix<float> data)
```

#### Parameters

`data` Matrix<[float](https://learn.microsoft.com/dotnet/api/system.single)\>

Input data matrix (n_samples, n_features)

#### Returns

 Matrix<[float](https://learn.microsoft.com/dotnet/api/system.single)\>

Low-dimensional embedding (n_samples, n_components)

#### Remarks

TODO: Consider deprecating this overload. Matrix&lt;float&gt; adds virtual call overhead
and intermediate allocations compared to float[][]. Only kept for compatibility with
SpectralInit which uses Math.Net for eigenvalue decomposition.

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapFlowBuilder_FitTransformWithReport_MathNet_Numerics_LinearAlgebra_Matrix_System_Single__"></a> FitTransformWithReport\(Matrix<float\>\)

Fits UMAP and transforms data in one step, returning full result including runtime report.
Auto-selects strategies based on data characteristics if not explicitly set.

```csharp
public UmapFitResult FitTransformWithReport(Matrix<float> data)
```

#### Parameters

`data` Matrix<[float](https://learn.microsoft.com/dotnet/api/system.single)\>

Input data matrix (n_samples, n_features)

#### Returns

 [UmapFitResult](Flowthru.Misc.ML.UMAP.Core.UmapFitResult.md)

Complete UMAP result including embedding and runtime metrics

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapFlowBuilder_WithGraphRefinement_Flowthru_Misc_ML_UMAP_Strategies_GraphRefinement_IGraphRefinementStrategy_"></a> WithGraphRefinement\(IGraphRefinementStrategy\)

Sets the graph refinement strategy for UMAP. If not set, a strategy will be auto-selected based on data characteristics and verbosity level.
Graph refinement strategies modify the initial k-nearest neighbor graph to improve the quality of the embedding. Examples include:
- Mutual kNN: Retains only edges where both points are in each other's kNN
- Local connectivity: Ensures each point is connected to at least one neighbor
- Edge weighting: Adjusts edge weights based on distance or local density
By allowing users to specify a graph refinement strategy, we enable them to enhance UMAP's performance on their specific dataset, while still providing sensible defaults for those who do not wish to configure this aspect.
This flexibility is important for accommodating the wide variety of datasets and requirements that users may have when applying UMAP.

```csharp
public UmapFlowBuilder WithGraphRefinement(IGraphRefinementStrategy strategy)
```

#### Parameters

`strategy` [IGraphRefinementStrategy](Flowthru.Misc.ML.UMAP.Strategies.GraphRefinement.IGraphRefinementStrategy.md)

#### Returns

 [UmapFlowBuilder](Flowthru.Misc.ML.UMAP.Core.UmapFlowBuilder.md)

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapFlowBuilder_WithInputMetric_Flowthru_Misc_ML_UMAP_Core_Markers_IMetric_"></a> WithInputMetric\(IMetric\)

Sets the input distance metric for UMAP. Defaults to Euclidean if not set.

```csharp
public UmapFlowBuilder WithInputMetric(IMetric metric)
```

#### Parameters

`metric` [IMetric](Flowthru.Misc.ML.UMAP.Core.Markers.IMetric.md)

#### Returns

 [UmapFlowBuilder](Flowthru.Misc.ML.UMAP.Core.UmapFlowBuilder.md)

#### Exceptions

 [ArgumentNullException](https://learn.microsoft.com/dotnet/api/system.argumentnullexception)

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapFlowBuilder_WithLayoutInit_Flowthru_Misc_ML_UMAP_Strategies_LayoutInit_ILayoutInitStrategy_"></a> WithLayoutInit\(ILayoutInitStrategy\)

Sets the layout initialization strategy for UMAP. If not set, a strategy will be auto-selected based on data characteristics and verbosity level.
Layout initialization strategies determine how the initial low-dimensional embedding is generated before optimization. Examples include:
- Spectral embedding: Uses eigenvectors of the graph Laplacian for initialization
- Random initialization: Assigns random coordinates to each point
- PCA-based initialization: Uses the top principal components for initialization
By allowing users to specify a layout initialization strategy, we enable them to improve convergence and

```csharp
public UmapFlowBuilder WithLayoutInit(ILayoutInitStrategy strategy)
```

#### Parameters

`strategy` [ILayoutInitStrategy](Flowthru.Misc.ML.UMAP.Strategies.LayoutInit.ILayoutInitStrategy.md)

#### Returns

 [UmapFlowBuilder](Flowthru.Misc.ML.UMAP.Core.UmapFlowBuilder.md)

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapFlowBuilder_WithLayoutOptimization_Flowthru_Misc_ML_UMAP_Strategies_LayoutOptimization_ILayoutOptimizationStrategy_"></a> WithLayoutOptimization\(ILayoutOptimizationStrategy\)

Sets the layout optimization strategy for UMAP. If not set, a strategy will be auto-selected based on data characteristics and verbosity level.

```csharp
public UmapFlowBuilder WithLayoutOptimization(ILayoutOptimizationStrategy strategy)
```

#### Parameters

`strategy` [ILayoutOptimizationStrategy](Flowthru.Misc.ML.UMAP.Strategies.LayoutOptimization.ILayoutOptimizationStrategy.md)

#### Returns

 [UmapFlowBuilder](Flowthru.Misc.ML.UMAP.Core.UmapFlowBuilder.md)

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapFlowBuilder_WithLocalMetric_Flowthru_Misc_ML_UMAP_Strategies_LocalMetric_ILocalMetricStrategy_"></a> WithLocalMetric\(ILocalMetricStrategy\)

Sets the local metric strategy for UMAP. If not set, a strategy will be auto-selected based on data characteristics and verbosity level.
Local metric strategies determine how distances are computed in the high-dimensional space and can significantly impact the quality of the embedding.
By allowing users to specify a local metric strategy, we enable them to tailor UMAP to their specific data and use case, while still providing sensible defaults for those who do not wish to configure this aspect.
This flexibility is important for accommodating the wide variety of datasets and requirements that users may have when applying UMAP.

```csharp
public UmapFlowBuilder WithLocalMetric(ILocalMetricStrategy strategy)
```

#### Parameters

`strategy` [ILocalMetricStrategy](Flowthru.Misc.ML.UMAP.Strategies.LocalMetric.ILocalMetricStrategy.md)

#### Returns

 [UmapFlowBuilder](Flowthru.Misc.ML.UMAP.Core.UmapFlowBuilder.md)

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapFlowBuilder_WithMembershipStrength_Flowthru_Misc_ML_UMAP_Strategies_MembershipStrength_IMembershipStrengthStrategy_"></a> WithMembershipStrength\(IMembershipStrengthStrategy\)

Sets the membership strength strategy for UMAP. If not set, a strategy will be auto-selected based on data characteristics and verbosity level.

```csharp
public UmapFlowBuilder WithMembershipStrength(IMembershipStrengthStrategy strategy)
```

#### Parameters

`strategy` [IMembershipStrengthStrategy](Flowthru.Misc.ML.UMAP.Strategies.MembershipStrength.IMembershipStrengthStrategy.md)

#### Returns

 [UmapFlowBuilder](Flowthru.Misc.ML.UMAP.Core.UmapFlowBuilder.md)

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapFlowBuilder_WithNeighborSearch_Flowthru_Misc_ML_UMAP_Strategies_NeighborSearch_INeighborSearchStrategy_"></a> WithNeighborSearch\(INeighborSearchStrategy\)

Sets the neighbor search strategy for UMAP. If not set, a strategy will be auto-selected based on data size and dimensionality.

```csharp
public UmapFlowBuilder WithNeighborSearch(INeighborSearchStrategy strategy)
```

#### Parameters

`strategy` [INeighborSearchStrategy](Flowthru.Misc.ML.UMAP.Strategies.NeighborSearch.INeighborSearchStrategy.md)

#### Returns

 [UmapFlowBuilder](Flowthru.Misc.ML.UMAP.Core.UmapFlowBuilder.md)

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapFlowBuilder_WithOutputMetric_Flowthru_Misc_ML_UMAP_Core_Markers_IOutputMetric_"></a> WithOutputMetric\(IOutputMetric\)

Sets the output metric for evaluating embedding quality. Optional, as many strategies do not require it.
If not set, strategies that can utilize an output metric will default to a standard choice (e.g., KNN preservation).
This allows users to benefit from output-aware strategies without needing to specify a metric if they don't have a specific one in mind.
Providing an output metric can enable more sophisticated strategies that optimize for that metric, but is not required for basic UMAP functionality.

```csharp
public UmapFlowBuilder WithOutputMetric(IOutputMetric metric)
```

#### Parameters

`metric` [IOutputMetric](Flowthru.Misc.ML.UMAP.Core.Markers.IOutputMetric.md)

#### Returns

 [UmapFlowBuilder](Flowthru.Misc.ML.UMAP.Core.UmapFlowBuilder.md)

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapFlowBuilder_WithSamplingSchedule_Flowthru_Misc_ML_UMAP_Strategies_SamplingSchedule_ISamplingScheduleStrategy_"></a> WithSamplingSchedule\(ISamplingScheduleStrategy\)

Sets the sampling schedule strategy for UMAP. If not set, a strategy will be auto-selected based on data characteristics and verbosity level.
Sampling schedule strategies determine how data points are sampled during the optimization process, which can impact convergence speed and embedding quality. Examples include:
- Uniform sampling: Samples points uniformly at random
- Density-based sampling: Samples points based on local density to ensure underrepresented regions are adequately sampled
- Adaptive sampling: Adjusts sampling probabilities based on optimization progress to focus on points that are not yet well-embedded
By allowing users to specify a sampling schedule strategy, we enable them to improve convergence and embedding quality for their specific dataset, while still providing sensible defaults

```csharp
public UmapFlowBuilder WithSamplingSchedule(ISamplingScheduleStrategy strategy)
```

#### Parameters

`strategy` [ISamplingScheduleStrategy](Flowthru.Misc.ML.UMAP.Strategies.SamplingSchedule.ISamplingScheduleStrategy.md)

#### Returns

 [UmapFlowBuilder](Flowthru.Misc.ML.UMAP.Core.UmapFlowBuilder.md)

