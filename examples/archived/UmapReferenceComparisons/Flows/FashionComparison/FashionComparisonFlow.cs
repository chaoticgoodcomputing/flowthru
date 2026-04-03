using Flowthru.Flows;
using Flowthru.Misc.ML.UMAP;
using Flowthru.Misc.ML.UMAP.Core;
using UmapReferenceComparisons.Data;
using UmapReferenceComparisons.Flows.FashionComparison.Steps;
using UmapReferenceComparisons.Helpers.Steps;

namespace UmapReferenceComparisons.Flows.FashionComparison;

/// <summary>
/// Flow to compare C# UMAP implementation against Python reference for Fashion-MNIST dataset.
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
public static class FashionComparisonFlow
{
  public static Flow Create(Catalog catalog)
  {
    var umapParameters = new UmapParameters
    {
      NumberOfNeighbors = 15,
      LearningRate = 1.0f,
      MinDist = 0.1f,
      NumberOfComponents = 2,
      RandomSeed = 42,
      NumberOfEpochs = null,
      Verbosity = 2,
    };

    return FlowBuilder.CreateFlow(pipeline =>
    {
      pipeline.AddStep(
        label: "ConvertFashionMnistToUmapInput",
        description: "Converts Fashion-MNIST schema to universal UmapInput format (float[784] features, class label)",
        transform: ConvertFashionMnistToUmapInputStep.Create(),
        input: catalog.FashionMnistInput,
        output: catalog.FashionMnistUmapInput
      );

      pipeline.AddStep(
        label: "TransformFashionMnistWithCSharpUmap",
        description: "Applies C# UMAP to Fashion-MNIST input features using Python reference parameters. Outputs both the embedding and runtime performance metrics.",
        transform: TransformWithUmapStep.Create(
          new TransformWithUmapStep.Params
          {
            DatasetName = "Fashion-MNIST",
            UmapParameters = umapParameters,
          }
        ),
        input: catalog.FashionMnistUmapInput,
        output: (catalog.FashionMnistCSharpOutput, catalog.FashionMnistRuntimeReport)
      );

      // This node requires a brute force KNN, and is too slow to test currently.

      // pipeline.AddStep(
      //   label: "CompareFashionMnistOutputs",
      //   description: "Compares C# UMAP output against Python reference output using neighborhood preservation validation.",
      //   transform: CompareUmapImplementationsStep.Create(
      //     new CompareUmapImplementationsStep.Params
      //     {
      //       DatasetName = "fashion-mnist",
      //       UmapParameters = umapParameters,
      //       KNeighbors = 15,
      //       MaxPreservationDifference = 0.1,
      //       MinimumConfidence = 0.68,
      //       NumTrials = 1,
      //     }
      //   ),
      //   input: (
      //     catalog.FashionMnistUmapInput,
      //     catalog.FashionMnistPythonOutput,
      //     catalog.FashionMnistUmapInput
      //   ),
      //   output: catalog.FashionMnistComparison
      // );

      pipeline.AddStep(
        label: "VisualizeFashionMnistComparison",
        description: "Creates a side-by-side scatter plot comparing Python and C# UMAP embeddings. Points colored by class.",
        transform: VisualizeUmapComparisonStep.Create(
          new VisualizeUmapComparisonStep.Params
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

      // NOTE: Commented out due to performance issues with Plotly.NET
      // pipeline.AddStep(
      //   label: "ExportFashionMnistVisualizationToPng",
      //   description: "Exports the side-by-side comparison chart to a PNG file.",
      //   transform: PlotlyImageExportStep.Create(),
      //   input: catalog.FashionMnistVisualization,
      //   output: catalog.FashionMnistVisualizationPng
      // );
    });
  }
}
