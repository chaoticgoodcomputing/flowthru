# <a id="Flowthru_Misc_ML_UMAP_Core_DataShape"></a> Class DataShape

Namespace: [Flowthru.Misc.ML.UMAP.Core](Flowthru.Misc.ML.UMAP.Core.md)  
Assembly: Flowthru.Misc.ML.dll  

Describes the shape and characteristics of input data.
Used by strategy factories to select appropriate default strategies.

```csharp
public sealed record DataShape : IEquatable<DataShape>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[DataShape](Flowthru.Misc.ML.UMAP.Core.DataShape.md)

#### Implements

[IEquatable<DataShape\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

Analyzing data shape allows the UMAP pipeline to automatically choose
optimal strategies. For example:
- Small datasets (&lt; 4096 samples) can use exact k-NN
- Large datasets benefit from approximate nearest neighbor search
- Sparse data requires specialized algorithms
- High-dimensional data may benefit from PCA initialization

## Properties

### <a id="Flowthru_Misc_ML_UMAP_Core_DataShape_EstimatedMemoryBytes"></a> EstimatedMemoryBytes

Approximate memory footprint in bytes.

```csharp
public long EstimatedMemoryBytes { get; init; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="Flowthru_Misc_ML_UMAP_Core_DataShape_Features"></a> Features

Number of features (columns) in the dataset.

```csharp
public required int Features { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="Flowthru_Misc_ML_UMAP_Core_DataShape_IsHighDimensional"></a> IsHighDimensional

Whether the dataset is high-dimensional (typically &gt; 100 features).
High-dimensional data may benefit from dimensionality reduction in initialization.

```csharp
public bool IsHighDimensional { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="Flowthru_Misc_ML_UMAP_Core_DataShape_IsLargeDataset"></a> IsLargeDataset

Whether the dataset is considered "large" (typically ≥ 4096 samples).
Large datasets should use approximate algorithms.

```csharp
public bool IsLargeDataset { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="Flowthru_Misc_ML_UMAP_Core_DataShape_IsSmallDataset"></a> IsSmallDataset

Whether the dataset is considered "small" (typically &lt; 4096 samples).
Small datasets can use exact algorithms.

```csharp
public bool IsSmallDataset { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="Flowthru_Misc_ML_UMAP_Core_DataShape_IsSparse"></a> IsSparse

Whether the data is stored in a sparse format.

```csharp
public required bool IsSparse { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="Flowthru_Misc_ML_UMAP_Core_DataShape_IsVeryHighDimensional"></a> IsVeryHighDimensional

Whether the dataset is very high-dimensional (typically &gt; 1000 features).
Very high-dimensional data may require PCA pre-processing.

```csharp
public bool IsVeryHighDimensional { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="Flowthru_Misc_ML_UMAP_Core_DataShape_RecommendedEpochs"></a> RecommendedEpochs

Recommended number of training epochs based on dataset size.
Follows Python UMAP heuristics: 500 for small datasets, 200 for large.

```csharp
public int RecommendedEpochs { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="Flowthru_Misc_ML_UMAP_Core_DataShape_RecommendedNeighbors"></a> RecommendedNeighbors

Recommended number of nearest neighbors based on dataset size.
Follows Python UMAP heuristics: typically 15, but adjusted for very small datasets.

```csharp
public int RecommendedNeighbors { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="Flowthru_Misc_ML_UMAP_Core_DataShape_Samples"></a> Samples

Number of samples (rows) in the dataset.

```csharp
public required int Samples { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="Flowthru_Misc_ML_UMAP_Core_DataShape_SparsityRatio"></a> SparsityRatio

Sparsity ratio (proportion of zero elements) if applicable.
Only meaningful when <xref href="Flowthru.Misc.ML.UMAP.Core.DataShape.IsSparse" data-throw-if-not-resolved="false"></xref> is true.

```csharp
public float? SparsityRatio { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)?

