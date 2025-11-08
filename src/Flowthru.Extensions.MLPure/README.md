# Flowthru.Extensions.MLPure

**Pure, unoptimized UMAP implementation** - Direct port from the Python reference implementation.

This project contains a line-by-line translation of the original Python UMAP implementation by Leland McInnes, without any optimizations or algorithmic changes. It serves as a baseline reference to verify correctness before applying optimizations.

## Purpose

This is a **reference implementation** used to:
- Verify the correctness of optimized UMAP implementations
- Establish baseline performance and accuracy metrics  
- Debug algorithmic differences between C# and Python versions

## Differences from Flowthru.Extensions.ML

| Aspect       | MLPure (this project)   | ML (optimized) |
| ------------ | ----------------------- | -------------- |
| **Goal**     | Algorithmic correctness | Performance    |
| **Source**   | Direct Python port      | Optimized C#   |
| **Speed**    | Slower                  | Faster         |
| **Memory**   | Higher usage            | Optimized      |
| **Use Case** | Testing/validation      | Production     |

## Attribution

This implementation is a direct port of the original UMAP Python implementation by **Leland McInnes**.

**Original Repository:** https://github.com/lmcinnes/umap  
**License:** BSD 3-Clause (see `UMAP/UMAP_LICENSE.txt`)

### Citations

```bibtex
@article{mcinnes2018umap,
  title={UMAP: Uniform Manifold Approximation and Projection for Dimension Reduction},
  author={McInnes, Leland and Healy, John and Melville, James},
  journal={arXiv preprint arXiv:1802.03426},
  year={2018}
}
```

## Usage

The API is identical to `Flowthru.Extensions.ML`:

```csharp
using Microsoft.ML;
using Flowthru.Extensions.MLPure.UMAP;

var mlContext = new MLContext();
var trainer = mlContext.CreateUmapTrainer();
var model = trainer.Fit(data);
var embedding = model.Embedding;
```

## Implementation Notes

### Porting Strategy

1. **No Optimizations**: Algorithms are ported as-is from Python
2. **Preserve Structure**: Function names, variable names, and logic flow match Python source
3. **Comments Reference Source**: Line numbers from Python files are referenced
4. **Readability over Speed**: Code prioritizes clarity and correctness

### Known Limitations

- Slower than optimized implementation
- Higher memory usage
- No SIMD optimizations
- No parallel processing optimizations
- Uses simpler data structures

These limitations are **intentional** to maintain algorithmic purity.
