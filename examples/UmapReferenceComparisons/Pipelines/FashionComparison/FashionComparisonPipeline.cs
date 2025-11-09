using Flowthru.Extensions.ML.UMAP;
using Flowthru.Pipelines;
using UmapReferenceComparisons.Data;
using UmapReferenceComparisons.Helpers.Nodes;
using UmapReferenceComparisons.Pipelines.FashionComparison.Nodes;

namespace UmapReferenceComparisons.Pipelines.FashionComparison;

/// <summary>
/// Pipeline to compare C# UMAP implementation against Python reference for Fashion-MNIST dataset.
/// </summary>
/// <remarks>
/// <para>
/// The Fashion-MNIST dataset contains 70,000 samples of 28x28 grayscale images of clothing items (10 classes).
/// </para>
/// <para>
/// This pipeline uses the default UMAP parameters from the Python reference:
/// - n_neighbors: 15 (default)
/// - learning_rate: 1.0 (default)
/// - min_dist: 0.1 (default)
/// - n_components: 2
/// - random_state: 42
/// </para>
/// </remarks>
public static class FashionComparisonPipeline
{
  public static Pipeline Create(Catalog catalog)
  {
    var umapOptions = new UmapOptions
    {
      NumberOfNeighbors = 15,
      LearningRate = 1.0f,
      MinDist = 0.1f,
      NumberOfComponents = 2,
      RandomState = 42,
      Metric = "euclidean",
      NumberOfEpochs = null,
      Verbosity = 2,
    };

    return PipelineBuilder.CreatePipeline(pipeline =>
    {
      pipeline.AddNode(
        label: "ConvertFashionMnistToUmapInput",
        description: "Converts Fashion-MNIST schema to universal UmapInput format (float[784] features, class label)",
        transform: ConvertFashionMnistToUmapInputNode.Create(),
        input: catalog.FashionMnistInput,
        output: catalog.FashionMnistUmapInput
      );

      pipeline.AddNode(
        label: "TransformFashionMnistWithCSharpUmap",
        description: "Applies C# UMAP to Fashion-MNIST input features using Python reference parameters.",
        transform: TransformWithUmapNode.Create(
          new TransformWithUmapNode.Params
          {
            DatasetName = "Fashion-MNIST",
            UmapOptions = umapOptions,
          }
        ),
        input: catalog.FashionMnistUmapInput,
        output: catalog.FashionMnistCSharpOutput
      );

      pipeline.AddNode(
        label: "CompareFashionMnistOutputs",
        description: "Compares C# UMAP output against Python reference output using neighborhood preservation validation.",
        transform: CompareUmapImplementationsNode.Create(
          new CompareUmapImplementationsNode.Params
          {
            DatasetName = "fashion-mnist",
            UmapOptions = umapOptions,
            KNeighbors = 15,
            MaxPreservationDifference = 0.1,
            MinimumConfidence = 0.68,
            NumTrials = 1,
          }
        ),
        input: (
          catalog.FashionMnistUmapInput,
          catalog.FashionMnistPythonOutput,
          catalog.FashionMnistUmapInput
        ),
        output: catalog.FashionMnistComparison
      );

      pipeline.AddNode(
        label: "VisualizeFashionMnistComparison",
        description: "Creates a side-by-side scatter plot comparing Python and C# UMAP embeddings. Points colored by class.",
        transform: VisualizeUmapComparisonNode.Create(
          new VisualizeUmapComparisonNode.Params
          {
            DatasetName = "Fashion-MNIST",
            LabelFormatter = label => $"Class {label}",
          }
        ),
        input: (
          catalog.FashionMnistUmapInput,
          catalog.FashionMnistPythonOutput,
          catalog.FashionMnistCSharpOutput
        ),
        output: catalog.FashionMnistVisualization
      );

      pipeline.AddNode(
        label: "ExportFashionMnistVisualizationToPng",
        description: "Exports the side-by-side comparison chart to a PNG file.",
        transform: PlotlyImageExportNode.Create(),
        input: catalog.FashionMnistVisualization,
        output: catalog.FashionMnistVisualizationPng
      );
    });
  }
}
