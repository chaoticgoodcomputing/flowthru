using Flowthru.Flows;
using Flowthru.Misc.ML.UMAP;
using Flowthru.Misc.ML.UMAP.Core;
using UmapReferenceComparisons.Data;
using UmapReferenceComparisons.Data._01_Raw.Schemas;
using UmapReferenceComparisons.Helpers.Nodes;
using UmapReferenceComparisons.Pipelines.IrisComparison.Nodes;

namespace UmapReferenceComparisons.Pipelines.IrisComparison;

/// <summary>
/// Pipeline to compare C# UMAP implementation against Python reference for Iris dataset.
/// </summary>
/// <remarks>
/// This pipeline performs four key operations:
/// 1. Applies C# UMAP with the same parameters to the input features
/// 2. Compares outputs using k-NN skeletal similarity (validates neighborhood preservation)
/// 3. Generates side-by-side visualization for visual validation
/// 4. Exports visualization to PNG for persistent storage
///
/// Note: Python and C# use different RNGs, so exact numerical matching is not expected.
/// Instead, we validate that both preserve similar neighborhood relationships.
/// </remarks>
public static class IrisComparisonPipeline
{
  public static Flow Create(Catalog catalog)
  {
    var umapParameters = new UmapParameters
    {
      NumberOfNeighbors = 50,
      LearningRate = 0.5f,
      MinDist = 0.001f,
      NumberOfComponents = 2,
      RandomSeed = 42,
      NumberOfEpochs = null,
      Verbosity = 2,
    };

    return FlowBuilder.CreateFlow(pipeline =>
    {
      pipeline.AddStep(
        label: "ConvertIrisToUmapInput",
        description: """
          Converts Iris-specific schema to universal UmapInput format.
          Extracts the 4 Iris features (sepal/petal length/width) into a float array.
        """,
        transform: ConvertIrisToUmapInputNode.Create(),
        input: catalog.IrisInput,
        output: catalog.IrisUmapInput
      );

      pipeline.AddStep(
        label: "TransformIrisWithCSharpUmap",
        description: """
          Applies C# UMAP to Iris input features using the same parameters
          as the Python reference implementation.
          
          Python reference (iris.py) uses:
          - n_neighbors=50
          - learning_rate=0.5
          - init="random"  (← explicit random initialization)
          - min_dist=0.001
          
          Outputs both the embedding and runtime performance metrics.
        """,
        transform: TransformWithUmapNode.Create(
          new TransformWithUmapNode.Params
          {
            DatasetName = "Iris",
            UmapParameters = umapParameters,
            InitStrategy = "random", // Match Python's init="random"
          }
        ),
        input: catalog.IrisUmapInput,
        output: (catalog.IrisCSharpOutput, catalog.IrisRuntimeReport)
      );

      pipeline.AddStep(
        label: "CompareIrisOutputs",
        description: """
          Compares C# UMAP output against Python reference output using neighborhood preservation validation.
          
          Metrics:
          1. Python neighborhood preservation: High-dim → Python embedding
          2. C# neighborhood preservation: High-dim → C# embedding (mean over multiple trials)
          
          Multi-trial approach accounts for random initialization variance.
        """,
        transform: CompareUmapImplementationsNode.Create(
          new CompareUmapImplementationsNode.Params
          {
            DatasetName = "iris",
            UmapParameters = umapParameters,
            KNeighbors = 15,
            MaxPreservationDifference = 0.1,
            MinimumConfidence = 0.68,
            NumTrials = 50,
            InitStrategy = "random", // Match Python's init="random"
          }
        ),
        input: (catalog.IrisUmapInput, catalog.IrisPythonOutput, catalog.IrisUmapInput),
        output: catalog.IrisComparison
      );

      pipeline.AddStep(
        label: "VisualizeComparison",
        description: """
          Creates a side-by-side scatter plot comparing Python and C# UMAP embeddings.
          Points are colored by iris species (setosa, versicolor, virginica).
          
          Visual validation checks:
          - Similar clustering patterns
          - Similar separation between species
          - Similar relative positioning of clusters
        """,
        transform: VisualizeUmapComparisonNode.Create(
          new VisualizeUmapComparisonNode.Params
          {
            DatasetName = "Iris",
            LabelFormatter = label =>
              label switch
              {
                "0" => "Setosa",
                "1" => "Versicolor",
                "2" => "Virginica",
                _ => $"Class {label}",
              },
          }
        ),
        input: (catalog.IrisUmapInput, catalog.IrisPythonOutput, catalog.IrisCSharpOutput),
        output: catalog.IrisVisualization
      );

      // NOTE: Commented out due to performance issues with Plotly.NET
      // pipeline.AddStep(
      //   label: "ExportVisualizationToPng",
      //   description: """
      //     Exports the side-by-side comparison chart to a PNG file.
      //     Uses Plotly.NET.ImageExport with PuppeteerSharp (headless Chromium)
      //     to render the interactive chart as a static image.
      //   """,
      //   transform: PlotlyImageExportNode.Create(),
      //   input: catalog.IrisVisualization,
      //   output: catalog.IrisVisualizationPng
      // );
    });
  }
}
