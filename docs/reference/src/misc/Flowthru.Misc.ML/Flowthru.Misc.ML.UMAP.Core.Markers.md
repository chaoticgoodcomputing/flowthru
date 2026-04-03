# <a id="Flowthru_Misc_ML_UMAP_Core_Markers"></a> Namespace Flowthru.Misc.ML.UMAP.Core.Markers

### Interfaces

 [IMetric](Flowthru.Misc.ML.UMAP.Core.Markers.IMetric.md)

Base interface for distance metrics used in UMAP.
Provides the fundamental distance computation between points in high-dimensional space.

 [IOutputMetric](Flowthru.Misc.ML.UMAP.Core.Markers.IOutputMetric.md)

Output space metric that provides distance gradients for layout optimization.
Required for embedding into non-Euclidean spaces (spherical, hyperbolic, toroidal, etc.).

