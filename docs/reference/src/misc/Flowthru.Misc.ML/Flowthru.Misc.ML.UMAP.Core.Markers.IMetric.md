# <a id="Flowthru_Misc_ML_UMAP_Core_Markers_IMetric"></a> Interface IMetric

Namespace: [Flowthru.Misc.ML.UMAP.Core.Markers](Flowthru.Misc.ML.UMAP.Core.Markers.md)  
Assembly: Flowthru.Misc.ML.dll  

Base interface for distance metrics used in UMAP.
Provides the fundamental distance computation between points in high-dimensional space.

```csharp
public interface IMetric
```

## Remarks

<p>
Metrics define how distances are measured in the input space during k-NN search
and graph construction. Different metrics capture different notions of similarity.
</p>
<p>
Common implementations: Euclidean (L2), Manhattan (L1), Cosine (angular).
</p>

## Properties

### <a id="Flowthru_Misc_ML_UMAP_Core_Markers_IMetric_DisconnectionDistance"></a> DisconnectionDistance

Maximum meaningful distance for bounded metrics, or null for unbounded metrics.
Used to handle disconnected components in the k-NN graph.

```csharp
float? DisconnectionDistance { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)?

#### Remarks

<p>
Examples:
- Euclidean: null (unbounded)
- Cosine: 2.0 (ranges from 0 to 2)
- Jaccard: 1.0 (ranges from 0 to 1)
</p>
<p>
When set, distances at or beyond this value indicate maximally dissimilar points
that should be treated as disconnected in the manifold approximation.
</p>

### <a id="Flowthru_Misc_ML_UMAP_Core_Markers_IMetric_Name"></a> Name

Human-readable name of the metric (e.g., "euclidean", "cosine").
Used for logging and serialization.

```csharp
string Name { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="Flowthru_Misc_ML_UMAP_Core_Markers_IMetric_SupportsAngularProjection"></a> SupportsAngularProjection

Whether this metric benefits from angular (cosine-based) random projection forests.
Angular metrics (cosine, correlation) use different RP tree splits than Euclidean metrics.

```csharp
bool SupportsAngularProjection { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

## Methods

### <a id="Flowthru_Misc_ML_UMAP_Core_Markers_IMetric_Distance_System_ReadOnlySpan_System_Single__System_ReadOnlySpan_System_Single__"></a> Distance\(ReadOnlySpan<float\>, ReadOnlySpan<float\>\)

Compute the distance between two points.

```csharp
float Distance(ReadOnlySpan<float> x, ReadOnlySpan<float> y)
```

#### Parameters

`x` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[float](https://learn.microsoft.com/dotnet/api/system.single)\>

First point

`y` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[float](https://learn.microsoft.com/dotnet/api/system.single)\>

Second point

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

Distance value (non-negative)

#### Remarks

Must satisfy metric properties:
- Non-negativity: Distance(x, y) ≥ 0
- Identity: Distance(x, x) = 0
- Symmetry: Distance(x, y) = Distance(y, x)
- Triangle inequality: Distance(x, z) ≤ Distance(x, y) + Distance(y, z)

