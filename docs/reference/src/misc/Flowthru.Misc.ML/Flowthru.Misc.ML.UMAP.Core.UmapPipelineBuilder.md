# <a id="Flowthru_Misc_ML_UMAP_Core_UmapPipelineBuilder"></a> Class UmapPipelineBuilder

Namespace: [Flowthru.Misc.ML.UMAP.Core](Flowthru.Misc.ML.UMAP.Core.md)  
Assembly: Flowthru.Misc.ML.dll  

```csharp
public sealed class UmapPipelineBuilder
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[UmapPipelineBuilder](Flowthru.Misc.ML.UMAP.Core.UmapPipelineBuilder.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Methods

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapPipelineBuilder_FitTransform_System_Single_____"></a> FitTransform\(float\[\]\[\]\)

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

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapPipelineBuilder_FitTransform_MathNet_Numerics_LinearAlgebra_Matrix_System_Single__"></a> FitTransform\(Matrix<float\>\)

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

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapPipelineBuilder_FitTransformWithReport_MathNet_Numerics_LinearAlgebra_Matrix_System_Single__"></a> FitTransformWithReport\(Matrix<float\>\)

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

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapPipelineBuilder_WithGraphRefinement_Flowthru_Misc_ML_UMAP_Strategies_GraphRefinement_IGraphRefinementStrategy_"></a> WithGraphRefinement\(IGraphRefinementStrategy\)

```csharp
public UmapPipelineBuilder WithGraphRefinement(IGraphRefinementStrategy strategy)
```

#### Parameters

`strategy` [IGraphRefinementStrategy](Flowthru.Misc.ML.UMAP.Strategies.GraphRefinement.IGraphRefinementStrategy.md)

#### Returns

 [UmapPipelineBuilder](Flowthru.Misc.ML.UMAP.Core.UmapPipelineBuilder.md)

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapPipelineBuilder_WithInputMetric_Flowthru_Misc_ML_UMAP_Core_Markers_IMetric_"></a> WithInputMetric\(IMetric\)

```csharp
public UmapPipelineBuilder WithInputMetric(IMetric metric)
```

#### Parameters

`metric` [IMetric](Flowthru.Misc.ML.UMAP.Core.Markers.IMetric.md)

#### Returns

 [UmapPipelineBuilder](Flowthru.Misc.ML.UMAP.Core.UmapPipelineBuilder.md)

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapPipelineBuilder_WithLayoutInit_Flowthru_Misc_ML_UMAP_Strategies_LayoutInit_ILayoutInitStrategy_"></a> WithLayoutInit\(ILayoutInitStrategy\)

```csharp
public UmapPipelineBuilder WithLayoutInit(ILayoutInitStrategy strategy)
```

#### Parameters

`strategy` [ILayoutInitStrategy](Flowthru.Misc.ML.UMAP.Strategies.LayoutInit.ILayoutInitStrategy.md)

#### Returns

 [UmapPipelineBuilder](Flowthru.Misc.ML.UMAP.Core.UmapPipelineBuilder.md)

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapPipelineBuilder_WithLayoutOptimization_Flowthru_Misc_ML_UMAP_Strategies_LayoutOptimization_ILayoutOptimizationStrategy_"></a> WithLayoutOptimization\(ILayoutOptimizationStrategy\)

```csharp
public UmapPipelineBuilder WithLayoutOptimization(ILayoutOptimizationStrategy strategy)
```

#### Parameters

`strategy` [ILayoutOptimizationStrategy](Flowthru.Misc.ML.UMAP.Strategies.LayoutOptimization.ILayoutOptimizationStrategy.md)

#### Returns

 [UmapPipelineBuilder](Flowthru.Misc.ML.UMAP.Core.UmapPipelineBuilder.md)

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapPipelineBuilder_WithLocalMetric_Flowthru_Misc_ML_UMAP_Strategies_LocalMetric_ILocalMetricStrategy_"></a> WithLocalMetric\(ILocalMetricStrategy\)

```csharp
public UmapPipelineBuilder WithLocalMetric(ILocalMetricStrategy strategy)
```

#### Parameters

`strategy` [ILocalMetricStrategy](Flowthru.Misc.ML.UMAP.Strategies.LocalMetric.ILocalMetricStrategy.md)

#### Returns

 [UmapPipelineBuilder](Flowthru.Misc.ML.UMAP.Core.UmapPipelineBuilder.md)

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapPipelineBuilder_WithMembershipStrength_Flowthru_Misc_ML_UMAP_Strategies_MembershipStrength_IMembershipStrengthStrategy_"></a> WithMembershipStrength\(IMembershipStrengthStrategy\)

```csharp
public UmapPipelineBuilder WithMembershipStrength(IMembershipStrengthStrategy strategy)
```

#### Parameters

`strategy` [IMembershipStrengthStrategy](Flowthru.Misc.ML.UMAP.Strategies.MembershipStrength.IMembershipStrengthStrategy.md)

#### Returns

 [UmapPipelineBuilder](Flowthru.Misc.ML.UMAP.Core.UmapPipelineBuilder.md)

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapPipelineBuilder_WithNeighborSearch_Flowthru_Misc_ML_UMAP_Strategies_NeighborSearch_INeighborSearchStrategy_"></a> WithNeighborSearch\(INeighborSearchStrategy\)

```csharp
public UmapPipelineBuilder WithNeighborSearch(INeighborSearchStrategy strategy)
```

#### Parameters

`strategy` [INeighborSearchStrategy](Flowthru.Misc.ML.UMAP.Strategies.NeighborSearch.INeighborSearchStrategy.md)

#### Returns

 [UmapPipelineBuilder](Flowthru.Misc.ML.UMAP.Core.UmapPipelineBuilder.md)

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapPipelineBuilder_WithOutputMetric_Flowthru_Misc_ML_UMAP_Core_Markers_IOutputMetric_"></a> WithOutputMetric\(IOutputMetric\)

```csharp
public UmapPipelineBuilder WithOutputMetric(IOutputMetric metric)
```

#### Parameters

`metric` [IOutputMetric](Flowthru.Misc.ML.UMAP.Core.Markers.IOutputMetric.md)

#### Returns

 [UmapPipelineBuilder](Flowthru.Misc.ML.UMAP.Core.UmapPipelineBuilder.md)

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapPipelineBuilder_WithSamplingSchedule_Flowthru_Misc_ML_UMAP_Strategies_SamplingSchedule_ISamplingScheduleStrategy_"></a> WithSamplingSchedule\(ISamplingScheduleStrategy\)

```csharp
public UmapPipelineBuilder WithSamplingSchedule(ISamplingScheduleStrategy strategy)
```

#### Parameters

`strategy` [ISamplingScheduleStrategy](Flowthru.Misc.ML.UMAP.Strategies.SamplingSchedule.ISamplingScheduleStrategy.md)

#### Returns

 [UmapPipelineBuilder](Flowthru.Misc.ML.UMAP.Core.UmapPipelineBuilder.md)

