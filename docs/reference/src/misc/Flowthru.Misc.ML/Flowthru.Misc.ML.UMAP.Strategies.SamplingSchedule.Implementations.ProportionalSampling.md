# <a id="Flowthru_Misc_ML_UMAP_Strategies_SamplingSchedule_Implementations_ProportionalSampling"></a> Class ProportionalSampling

Namespace: [Flowthru.Misc.ML.UMAP.Strategies.SamplingSchedule.Implementations](Flowthru.Misc.ML.UMAP.Strategies.SamplingSchedule.Implementations.md)  
Assembly: Flowthru.Misc.ML.dll  

Proportional sampling schedule where edges are sampled proportionally to their weights.
This is the standard UMAP sampling strategy.

```csharp
public sealed class ProportionalSampling : ISamplingScheduleStrategy
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[ProportionalSampling](Flowthru.Misc.ML.UMAP.Strategies.SamplingSchedule.Implementations.ProportionalSampling.md)

#### Implements

[ISamplingScheduleStrategy](Flowthru.Misc.ML.UMAP.Strategies.SamplingSchedule.ISamplingScheduleStrategy.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

<p>
This implementation follows the Python UMAP reference implementation exactly.
Each edge is sampled with frequency proportional to its membership strength,
ensuring that stronger connections in the fuzzy simplicial set receive more
optimization attention.
</p>
<p>
<b>Time complexity:</b> O(E) where E is the number of edges
</p>
<p>
<b>Space complexity:</b> O(E) for the output array
</p>
<p>
Python UMAP reference: <code>make_epochs_per_sample()</code> in <code>umap_.py</code> (lines 906-927)
</p>

## Methods

### <a id="Flowthru_Misc_ML_UMAP_Strategies_SamplingSchedule_Implementations_ProportionalSampling_ComputeSchedule_System_Single___System_Int32_"></a> ComputeSchedule\(float\[\], int\)

Computes proportional sampling schedule for edges.

```csharp
public SamplingScheduleResult ComputeSchedule(float[] edgeWeights, int nEpochs)
```

#### Parameters

`edgeWeights` [float](https://learn.microsoft.com/dotnet/api/system.single)\[\]

`nEpochs` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [SamplingScheduleResult](Flowthru.Misc.ML.UMAP.Strategies.SamplingSchedule.SamplingScheduleResult.md)

