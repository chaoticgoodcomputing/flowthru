using Flowthru.Extensions.ML.UMAP;
using Flowthru.Extensions.ML.UMAP.Core;
using Flowthru.Pipelines;
using UmapReferenceComparisons.Data;
using UmapReferenceComparisons.Helpers.Nodes;
using UmapReferenceComparisons.Pipelines.DigitsComparison.Nodes;

namespace UmapReferenceComparisons.Pipelines.DigitsComparison;

/// <summary>
/// Pipeline to compare C# UMAP implementation against Python reference for Digits dataset.
/// </summary>
/// <remarks>
/// <para>
/// The Digits dataset contains 1,797 samples of 8x8 grayscale images of handwritten
/// digits (0-9), resulting in 64-dimensional feature space.
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
public static class DigitsComparisonPipeline
{
  public static Pipeline Create(Catalog catalog)
  {
    var umapParameters = new UmapParameters
    {
      // Python default parameters from load_digits() example
      NumberOfNeighbors = 15, // n_neighbors (default)
      LearningRate = 1.0f, // learning_rate (default)
      MinDist = 0.1f, // min_dist (default)
      NumberOfComponents = 2,
      RandomSeed = 42,
      NumberOfEpochs = null, // Use default calculation
      Verbosity = 2,
    };

    return PipelineBuilder.CreatePipeline(pipeline =>
    {
      pipeline.AddNode(
        label: "ConvertDigitsToUmapInput",
        description: """
          Converts Digits-specific schema to universal UmapInput format.
          Extracts all 64 pixel values (8x8 image) into a float array.
        """,
        transform: ConvertDigitsToUmapInputNode.Create(),
        input: catalog.DigitsInput,
        output: catalog.DigitsUmapInput
      );

      pipeline.AddNode(
        label: "TransformDigitsWithCSharpUmap",
        description: """
          Applies C# UMAP to Digits input features using the same parameters
          as the Python reference implementation.
          
          Outputs both the embedding and runtime performance metrics.
        """,
        transform: TransformWithUmapNode.Create(
          new TransformWithUmapNode.Params
          {
            DatasetName = "Digits",
            UmapParameters = umapParameters,
          }
        ),
        input: catalog.DigitsUmapInput,
        output: (catalog.DigitsCSharpOutput, catalog.DigitsRuntimeReport)
      );

      pipeline.AddNode(
        label: "CompareDigitsOutputs",
        description: """
          Compares C# UMAP output against Python reference output using neighborhood preservation validation.
          
          The Digits dataset (8x8 grayscale images) is embedded into 2D space using
          default UMAP parameters matching the Python reference implementation.
          
          Validation metrics:
          1. Python neighborhood preservation: High-dim (64D) → Python embedding (2D)
          2. C# neighborhood preservation: High-dim (64D) → C# embedding (2D) over multiple trials
          
          Multi-trial approach accounts for random initialization variance.
        """,
        transform: CompareUmapImplementationsNode.Create(
          new CompareUmapImplementationsNode.Params
          {
            DatasetName = "digits",
            UmapParameters = umapParameters,
            KNeighbors = 15,
            MaxPreservationDifference = 0.1,
            MinimumConfidence = 0.68,
            NumTrials = 3,
          }
        ),
        input: (catalog.DigitsUmapInput, catalog.DigitsPythonOutput, catalog.DigitsUmapInput),
        output: catalog.DigitsComparison
      );

      pipeline.AddNode(
        label: "VisualizeComparison",
        description: """
          Creates a side-by-side scatter plot comparing Python and C# UMAP embeddings.
          Points are colored by digit class (0-9).
          
          Visual validation checks:
          - Similar clustering patterns for each digit
          - Similar separation between digit classes
          - Similar relative positioning of clusters
        """,
        transform: VisualizeUmapComparisonNode.Create(
          new VisualizeUmapComparisonNode.Params
          {
            DatasetName = "Digits",
            LabelFormatter = label => $"Digit {label}",
          }
        ),
        input: (catalog.DigitsUmapInput, catalog.DigitsPythonOutput, catalog.DigitsCSharpOutput),
        output: catalog.DigitsVisualization
      );

      pipeline.AddNode(
        label: "ExportVisualizationToPng",
        description: """
          Exports the side-by-side comparison chart to a PNG file.
          Uses Plotly.NET.ImageExport with PuppeteerSharp (headless Chromium)
          to render the interactive chart as a static image.
        """,
        transform: PlotlyImageExportNode.Create(),
        input: catalog.DigitsVisualization,
        output: catalog.DigitsVisualizationPng
      );
    });
  }
}
