using Flowthru.Pipelines;
using UmapReferenceComparisons.Data;
using UmapReferenceComparisons.Pipelines.IrisComparison.Nodes;

namespace UmapReferenceComparisons.Pipelines.IrisComparison;

/// <summary>
/// Pipeline to compare C# UMAP implementation against Python reference for Iris dataset.
/// </summary>
/// <remarks>
/// This pipeline performs three key operations:
/// 1. Loads reference data from Python UMAP (input features and output embeddings)
/// 2. Applies C# UMAP with the same parameters to the input features
/// 3. Compares the outputs to validate count and schema compatibility
/// </remarks>
public static class IrisComparisonPipeline
{
  public static Pipeline Create(Catalog catalog)
  {
    return PipelineBuilder.CreatePipeline(pipeline =>
    {
      // Node 1: Apply C# UMAP to Iris input data
      pipeline.AddNode(
        label: "TransformIrisWithCSharpUmap",
        description: """
          Applies C# UMAP to Iris input features using the same parameters
          as the Python reference implementation.
          
          Parameters:
          - n_neighbors: 50
          - learning_rate: 0.5
          - init: random
          - min_dist: 0.001
          - n_components: 2
          - random_state: 42
        """,
        transform: TransformIrisNode.Create(),
        input: catalog.IrisInput,
        output: catalog.IrisCSharpOutput
      );

      // Node 2: Compare C# output against Python reference
      pipeline.AddNode(
        label: "CompareIrisOutputs",
        description: """
          Compares C# UMAP output against Python reference output.
          
          Validation checks:
          - Sample count equality
          - Dimension count equality
          - Schema compatibility
        """,
        transform: CompareOutputsNode.Create("iris"),
        input: (catalog.IrisPythonOutput, catalog.IrisCSharpOutput),
        output: catalog.IrisComparison
      );
    });
  }
}
