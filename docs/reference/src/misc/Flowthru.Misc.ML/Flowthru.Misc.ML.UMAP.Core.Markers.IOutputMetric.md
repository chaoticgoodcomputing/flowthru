# <a id="Flowthru_Misc_ML_UMAP_Core_Markers_IOutputMetric"></a> Interface IOutputMetric

Namespace: [Flowthru.Misc.ML.UMAP.Core.Markers](Flowthru.Misc.ML.UMAP.Core.Markers.md)  
Assembly: Flowthru.Misc.ML.dll  

Output space metric that provides distance gradients for layout optimization.
Required for embedding into non-Euclidean spaces (spherical, hyperbolic, toroidal, etc.).

```csharp
public interface IOutputMetric : IMetric
```

#### Implements

[IMetric](Flowthru.Misc.ML.UMAP.Core.Markers.IMetric.md)

## Remarks

<p>
During layout optimization (SGD phase), UMAP needs both the distance and its gradient
to update point positions. Standard Euclidean SGD has a specialized, highly optimized
implementation. For other output spaces, the generic SGD implementation requires gradients.
</p>
<p>
Examples of non-Euclidean output spaces:
- Spherical (haversine distance): Embeddings constrained to sphere surface
- Hyperbolic (Poincaré/hyperboloid): For hierarchical data
- Toroidal (wrap-around): For periodic data
</p>

## Methods

### <a id="Flowthru_Misc_ML_UMAP_Core_Markers_IOutputMetric_DistanceWithGradient_System_ReadOnlySpan_System_Single__System_ReadOnlySpan_System_Single__System_Single__System_Span_System_Single__"></a> DistanceWithGradient\(ReadOnlySpan<float\>, ReadOnlySpan<float\>, out float, Span<float\>\)

Compute distance and its gradient with respect to the first argument.
Used during stochastic gradient descent to optimize the embedding layout.

```csharp
void DistanceWithGradient(ReadOnlySpan<float> x, ReadOnlySpan<float> y, out float distance, Span<float> gradient)
```

#### Parameters

`x` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[float](https://learn.microsoft.com/dotnet/api/system.single)\>

First point (the point being optimized)

`y` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[float](https://learn.microsoft.com/dotnet/api/system.single)\>

Second point (reference/anchor point)

`distance` [float](https://learn.microsoft.com/dotnet/api/system.single)

Output: distance between x and y

`gradient` [Span](https://learn.microsoft.com/dotnet/api/system.span\-1)<[float](https://learn.microsoft.com/dotnet/api/system.single)\>

Output: gradient of distance with respect to x (∂distance/∂x).
Must be pre-allocated by caller with length equal to dimensionality.

#### Remarks

<p>
The gradient represents the direction and magnitude of steepest increase in distance
when moving x. During SGD, we use this to either attract or repel points.
</p>
<p>
For Euclidean distance d = ||x - y||:
- ∇d/∂x = (x - y) / ||x - y||
</p>

