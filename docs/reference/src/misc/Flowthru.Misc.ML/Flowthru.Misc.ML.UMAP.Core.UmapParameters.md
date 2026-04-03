# <a id="Flowthru_Misc_ML_UMAP_Core_UmapParameters"></a> Class UmapParameters

Namespace: [Flowthru.Misc.ML.UMAP.Core](Flowthru.Misc.ML.UMAP.Core.md)  
Assembly: Flowthru.Misc.ML.dll  

Core parameters for UMAP algorithm configuration.
These parameters control the mathematical behavior of the algorithm across all strategies.

```csharp
public sealed record UmapParameters : IEquatable<UmapParameters>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[UmapParameters](Flowthru.Misc.ML.UMAP.Core.UmapParameters.md)

#### Implements

[IEquatable<UmapParameters\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

This record contains the fundamental UMAP hyperparameters that affect the global
structure of the embedding. Strategy-specific parameters are configured on individual
strategy instances.

## Properties

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapParameters_A"></a> A

Curve fitting parameter 'a' for the low-dimensional fuzzy simplicial set.
If null, automatically computed from <xref href="Flowthru.Misc.ML.UMAP.Core.UmapParameters.Spread" data-throw-if-not-resolved="false"></xref> and <xref href="Flowthru.Misc.ML.UMAP.Core.UmapParameters.MinDist" data-throw-if-not-resolved="false"></xref>.

```csharp
public float? A { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)?

#### Remarks

Default: null (auto-compute). Manual setting is for advanced use only.
This parameter controls the attractive force curve in the embedding space.

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapParameters_B"></a> B

Curve fitting parameter 'b' for the low-dimensional fuzzy simplicial set.
If null, automatically computed from <xref href="Flowthru.Misc.ML.UMAP.Core.UmapParameters.Spread" data-throw-if-not-resolved="false"></xref> and <xref href="Flowthru.Misc.ML.UMAP.Core.UmapParameters.MinDist" data-throw-if-not-resolved="false"></xref>.

```csharp
public float? B { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)?

#### Remarks

Default: null (auto-compute). Manual setting is for advanced use only.
This parameter controls the attractive force curve in the embedding space.

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapParameters_LearningRate"></a> LearningRate

Initial learning rate for stochastic gradient descent.

```csharp
public float LearningRate { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Remarks

Default: 1.0. Range: (0, ∞). Typical values: 0.5-2.0.
Learning rate decays linearly to 0 over training epochs.

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapParameters_LocalConnectivity"></a> LocalConnectivity

Local connectivity required at the manifold level.
Number of nearest neighbors assumed to be connected locally.

```csharp
public float LocalConnectivity { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Remarks

Default: 1.0. Range: [1, numberOfNeighbors]. Typical values: 1.0-5.0.
Higher values increase local connectivity, making the manifold more connected.
Should not exceed the local intrinsic dimension of the manifold.

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapParameters_MinDist"></a> MinDist

Effective minimum distance between embedded points.
Controls how tightly points are packed in clusters.

```csharp
public float MinDist { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Remarks

Default: 0.1. Range: [0, spread]. Typical values: 0.0-0.5.
- 0.0: Dense, tightly packed clusters
- 0.1: Balanced (default)
- 0.3-0.5: More spread out, emphasizes separation

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapParameters_NegativeSampleRate"></a> NegativeSampleRate

Number of negative samples per positive sample during optimization.

```csharp
public int NegativeSampleRate { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Remarks

Default: 5. Range: [1, ∞). Typical values: 5-20.
Higher values = stronger repulsive force but slower training.

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapParameters_NumberOfComponents"></a> NumberOfComponents

Dimensionality of the target embedding space.

```csharp
public int NumberOfComponents { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Remarks

Default: 2 (for visualization). Range: [1, ∞). Typical values: 2-100.
- 2D: Visualization and exploratory analysis
- 3D: Interactive 3D visualization
- Higher: Feature extraction, downstream ML tasks

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapParameters_NumberOfEpochs"></a> NumberOfEpochs

Number of optimization epochs (training iterations).
If null, automatically determined based on dataset size.

```csharp
public int? NumberOfEpochs { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)?

#### Remarks

Default: null (auto). If set, range: [0, ∞). Auto values: 500 (small data), 200 (large data).
More epochs = better convergence but longer training time.

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapParameters_NumberOfNeighbors"></a> NumberOfNeighbors

Number of nearest neighbors to consider for manifold approximation.
Larger values result in more global structure, smaller values preserve local details.

```csharp
public int NumberOfNeighbors { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Remarks

Default: 15. Range: [2, ∞). Typical values: 5-50.
- Small values (5-10): Emphasize local structure, fine details
- Medium values (15-30): Balanced local and global structure
- Large values (50+): Emphasize global structure, may lose fine details

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapParameters_ProgressReporter"></a> ProgressReporter

Optional progress reporter for programmatic progress tracking.

```csharp
public IProgress<UmapProgress>? ProgressReporter { get; init; }
```

#### Property Value

 [IProgress](https://learn.microsoft.com/dotnet/api/system.iprogress\-1)<[UmapProgress](Flowthru.Misc.ML.UMAP.Core.UmapProgress.md)\>?

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapParameters_RandomSeed"></a> RandomSeed

Random seed for reproducible results.
If null, uses non-deterministic randomization.

```csharp
public int? RandomSeed { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)?

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapParameters_RepulsionStrength"></a> RepulsionStrength

Weight applied to negative samples in optimization.
Controls repulsive force between non-neighboring points.

```csharp
public float RepulsionStrength { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Remarks

Default: 1.0. Range: [0, ∞). Typical values: 0.5-2.0.
- Lower values: Less repulsion, denser embedding
- Higher values: More repulsion, more spread out

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapParameters_SetOpMixRatio"></a> SetOpMixRatio

Interpolation between fuzzy union and intersection for combining local simplicial sets.

```csharp
public float SetOpMixRatio { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Remarks

Default: 1.0 (pure fuzzy union). Range: [0, 1].
- 1.0: Pure fuzzy union (standard UMAP)
- 0.0: Pure fuzzy intersection (more conservative connectivity)
- 0.5: Balanced between union and intersection

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapParameters_Spread"></a> Spread

Effective scale of embedded points.
Works with <xref href="Flowthru.Misc.ML.UMAP.Core.UmapParameters.MinDist" data-throw-if-not-resolved="false"></xref> to control clustering vs. dispersion.

```csharp
public float Spread { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Remarks

Default: 1.0. Range: (0, ∞). Typical values: 0.5-2.0.
Controls the overall scale at which embedded points spread out.

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapParameters_Verbosity"></a> Verbosity

Verbosity level for progress reporting.

```csharp
public int Verbosity { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Remarks

0 = Silent, 1 = Basic progress, 2 = Detailed progress

## Methods

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapParameters_GetA"></a> GetA\(\)

Gets the curve parameter 'a', computing it from spread and min_dist if not explicitly set.

```csharp
public float GetA()
```

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapParameters_GetB"></a> GetB\(\)

Gets the curve parameter 'b', computing it from spread and min_dist if not explicitly set.

```csharp
public float GetB()
```

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapParameters_Validate"></a> Validate\(\)

Validates the parameters and throws if any are invalid.

```csharp
public void Validate()
```

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

Thrown when parameters are out of valid ranges.

