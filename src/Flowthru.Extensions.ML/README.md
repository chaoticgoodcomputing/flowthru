# Flowthru.Extensions.ML

Machine Learning extensions for Flowthru, providing advanced dimensionality reduction techniques.

## Features

### UMAP (Uniform Manifold Approximation and Projection)

A state-of-the-art dimensionality reduction technique for visualizing and analyzing high-dimensional data.

**Key Characteristics:**
- Preserves both local and global structure
- Faster than t-SNE on large datasets
- Supports multiple distance metrics (Euclidean, cosine, correlation, Manhattan)
- Can embed into any number of dimensions (not just 2D/3D)
- Suitable for both visualization and pre-processing for other ML tasks

## Attribution

This implementation is based on the original UMAP Python implementation by **Leland McInnes**.

**Original Repository:** https://github.com/lmcinnes/umap  
**License:** BSD 3-Clause (see `UMAP/UMAP_LICENSE.txt`)

### Citations

If you use this UMAP implementation in academic work, please cite:

```bibtex
@article{mcinnes2018umap,
  title={UMAP: Uniform Manifold Approximation and Projection for Dimension Reduction},
  author={McInnes, Leland and Healy, John and Melville, James},
  journal={arXiv preprint arXiv:1802.03426},
  year={2018}
}
```

Additional reference:
```bibtex
@article{healy2024umap,
  title={Uniform manifold approximation and projection},
  author={Healy, John and McInnes, Leland},
  journal={Nature Reviews Methods Primers},
  volume={4},
  number={1},
  pages={82},
  year={2024},
  publisher={Nature Publishing Group}
}
```

## Usage

### Standalone Usage with ML.NET

```csharp
using Microsoft.ML;
using Flowthru.Extensions.ML.UMAP;

var mlContext = new MLContext();

// Create UMAP trainer with default options
var trainer = mlContext.CreateUmapTrainer(
    nNeighbors: 15,      // Number of neighbors
    nComponents: 2,       // Target dimensions
    minDist: 0.1f,       // Minimum distance between points
    metric: "euclidean"  // Distance metric
);

// Train on your data (float[][])
float[][] data = LoadYourData();
var model = trainer.Fit(data);

// Access the embedding
var embedding = model.Embedding; // Matrix<float>

// Transform new data
var transformer = mlContext.CreateUmapTransformer(model);
float[][] newData = LoadNewData();
float[][] newEmbedding = transformer.Transform(newData);
```

### Advanced Configuration

```csharp
var options = new UmapOptions
{
    NumberOfNeighbors = 30,          // More neighbors = more global structure
    NumberOfComponents = 3,           // 3D embedding
    MinDist = 0.05f,                 // Tighter clustering
    Spread = 1.5f,                   // Spread of embedded points
    Metric = "cosine",               // Use cosine distance
    NumberOfEpochs = 500,            // More epochs = better optimization
    LearningRate = 1.0f,
    LocalConnectivity = 1.5f,
    RepulsionStrength = 1.0f,
    NegativeSampleRate = 5,
    SetOpMixRatio = 1.0f,
    RandomState = 42                 // For reproducibility
};

var trainer = mlContext.CreateUmapTrainer(options);
var model = trainer.Fit(data);
```

### Integration with Flowthru Pipelines

```csharp
using Flowthru.Extensions.ML.UMAP;
using Microsoft.ML;

public static class UmapReductionNode
{
    public record Params
    {
        public int NumberOfNeighbors { get; init; } = 15;
        public int NumberOfComponents { get; init; } = 2;
        public float MinDist { get; init; } = 0.1f;
        public string Metric { get; init; } = "euclidean";
    }

    public static Func<IEnumerable<InputSchema>, Task<IEnumerable<OutputSchema>>> Create(
        Params? parameters = null
    )
    {
        var opts = parameters ?? new Params();
        
        return async (input) =>
        {
            var mlContext = new MLContext(seed: 42);
            
            // Convert input to float[][]
            var data = input.Select(row => row.Features).ToArray();
            
            // Train UMAP
            var trainer = mlContext.CreateUmapTrainer(
                opts.NumberOfNeighbors,
                opts.NumberOfComponents,
                opts.MinDist,
                opts.Metric
            );
            
            var model = trainer.Fit(data);
            
            // Convert embedding back to output schema
            var embeddingRows = model.Embedding.ToRowArrays();
            var output = input.Zip(embeddingRows, (row, embedding) => new OutputSchema
            {
                Id = row.Id,
                Name = row.Name,
                UmapEmbedding = embedding
            });
            
            return await Task.FromResult(output);
        };
    }
}
```

## Parameters

### Core Parameters

| Parameter            | Default     | Description                                                                                                            |
| -------------------- | ----------- | ---------------------------------------------------------------------------------------------------------------------- |
| `NumberOfNeighbors`  | 15          | Number of neighboring points used in local approximations. Range: 2-100. Higher values preserve more global structure. |
| `NumberOfComponents` | 2           | Dimensionality of the target embedding space. Typically 2-3 for visualization, higher for pre-processing.              |
| `MinDist`            | 0.1         | Minimum distance between embedded points. Range: 0.0-0.5. Lower values allow tighter clustering.                       |
| `Metric`             | "euclidean" | Distance metric. Options: "euclidean", "cosine", "correlation", "manhattan"                                            |

### Advanced Parameters

| Parameter            | Default | Description                                                                       |
| -------------------- | ------- | --------------------------------------------------------------------------------- |
| `Spread`             | 1.0     | Effective scale of embedded points. Works with MinDist to control clustering.     |
| `NumberOfEpochs`     | auto    | Training iterations. Auto-selected: 500 for small datasets (<10k), 200 for large. |
| `LearningRate`       | 1.0     | Initial learning rate for SGD optimization.                                       |
| `LocalConnectivity`  | 1.0     | Number of nearest neighbors assumed to be connected locally.                      |
| `RepulsionStrength`  | 1.0     | Weight applied to negative samples. Higher values increase repulsion.             |
| `NegativeSampleRate` | 5       | Number of negative samples per positive sample during optimization.               |
| `SetOpMixRatio`      | 1.0     | Interpolation between fuzzy union (1.0) and intersection (0.0).                   |
| `RandomState`        | null    | Random seed for reproducibility.                                                  |

## Algorithm Overview

UMAP constructs a high-dimensional graph representation of the data and optimizes a low-dimensional graph to be as structurally similar as possible. The process involves:

1. **k-Nearest Neighbors**: Compute k nearest neighbors for each point
2. **Fuzzy Simplicial Set**: Build a topological representation using fuzzy set theory
3. **Initialization**: Initialize low-dimensional embedding (random or spectral)
4. **Optimization**: Use stochastic gradient descent to optimize embedding layout

## Performance Considerations

- **Dataset Size**: UMAP scales well to large datasets (100k+ samples)
- **Dimensionality**: Handles high-dimensional data (1000+ features) effectively
- **Computation**: O(n log n) for k-NN, O(n) per epoch for optimization
- **Memory**: Requires storing distance matrix and graph structure

## Supported Metrics

- **Euclidean**: Standard L2 distance
- **Cosine**: 1 - cosine similarity (good for text embeddings)
- **Correlation**: 1 - Pearson correlation
- **Manhattan**: L1 distance

## Dependencies

- **Microsoft.ML**: For ML.NET integration
- **MathNet.Numerics**: For matrix operations and linear algebra

## License

This UMAP implementation is provided under the BSD 3-Clause License, matching the original Python implementation. See `UMAP/UMAP_LICENSE.txt` for details.

The Flowthru.Extensions.ML wrapper code is provided under the same license as the Flowthru project.
