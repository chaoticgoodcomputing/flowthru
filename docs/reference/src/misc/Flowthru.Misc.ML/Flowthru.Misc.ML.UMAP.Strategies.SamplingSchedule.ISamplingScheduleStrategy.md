# <a id="Flowthru_Misc_ML_UMAP_Strategies_SamplingSchedule_ISamplingScheduleStrategy"></a> Interface ISamplingScheduleStrategy

Namespace: [Flowthru.Misc.ML.UMAP.Strategies.SamplingSchedule](Flowthru.Misc.ML.UMAP.Strategies.SamplingSchedule.md)  
Assembly: Flowthru.Misc.ML.dll  

Strategy interface for computing edge sampling schedules during layout optimization.
This is the sixth phase of the UMAP algorithm.

```csharp
public interface ISamplingScheduleStrategy
```

## Remarks

<p>
The sampling schedule determines how frequently each edge in the fuzzy simplicial set
should be sampled during stochastic gradient descent. Edges with higher membership
strength (weight) are sampled more frequently.
</p>
<p>
<b>Standard approach (proportional sampling):</b>
</p>
<p>
Each edge is sampled proportionally to its weight. The number of epochs between samples
for an edge with weight <code>w</code> is:
</p>
<pre><code class="lang-csharp">epochs_per_sample[i] = n_epochs / (n_epochs * weight[i] / max_weight)
                     = max_weight / weight[i]</code></pre>
<p>
This ensures that stronger edges (higher membership) are sampled more often, while
weaker edges may not be sampled at all if their expected sample count is less than 1.
</p>
<p>
Python UMAP reference: <code>make_epochs_per_sample()</code> function in <code>umap_.py</code> (lines 906-927)
</p>

## Methods

### <a id="Flowthru_Misc_ML_UMAP_Strategies_SamplingSchedule_ISamplingScheduleStrategy_ComputeSchedule_System_Single___System_Int32_"></a> ComputeSchedule\(float\[\], int\)

Computes the sampling schedule for edges during SGD optimization.

```csharp
SamplingScheduleResult ComputeSchedule(float[] edgeWeights, int nEpochs)
```

#### Parameters

`edgeWeights` [float](https://learn.microsoft.com/dotnet/api/system.single)\[\]

Array of edge weights from the fuzzy simplicial set.
These are the membership strengths after fuzzy set operations.
Length: number of edges in the graph

`nEpochs` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Total number of optimization epochs to run.
Must be positive.

#### Returns

 [SamplingScheduleResult](Flowthru.Misc.ML.UMAP.Strategies.SamplingSchedule.SamplingScheduleResult.md)

Array of epochs-per-sample for each edge.
Value of <code>epochs_per_sample[i]</code> means edge <code>i</code> should be sampled
every <code>epochs_per_sample[i]</code> epochs (on average).
Edges with weight too small to be sampled are marked with -1.
Length: same as edgeWeights

#### Remarks

<p>
<b>Implementation requirements:</b>
</p>
<ol><li>Find maximum weight across all edges</li><li>Compute expected number of samples per edge: n_epochs * (weight / max_weight)</li><li>Invert to get epochs per sample: n_epochs / expected_samples</li><li>Mark edges with expected_samples ≤ 0 as -1 (never sampled)</li></ol>

