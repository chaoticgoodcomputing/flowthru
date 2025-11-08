using Flowthru.Pipelines;
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
  public static Pipeline Create(Catalog catalog)
  {
    return PipelineBuilder.CreatePipeline(pipeline =>
    {
      pipeline.AddNode(
        label: "TransformIrisWithCSharpUmap",
        description: """
          Applies C# UMAP to Iris input features using the same parameters
          as the Python reference implementation.
        """,
        transform: TransformIrisNode.Create(),
        input: catalog.IrisInput,
        output: catalog.IrisCSharpOutput
      );

      pipeline.AddNode(
        label: "CompareIrisOutputs",
        description: """
          Compares C# UMAP output against Python reference output.
          
          Skeletal similarity measures what proportion of k-nearest neighbor
          relationships are preserved between the two embeddings. Higher scores
          indicate better preservation of local structure.
        """,
        transform: CompareOutputsNode.Create(
          "iris",
          new CompareOutputsNode.Params { KNeighbors = 15, MinimumSimilarity = 0.7 }
        ),
        input: (catalog.IrisInput, catalog.IrisPythonOutput, catalog.IrisCSharpOutput),
        output: catalog.IrisComparison
      );

      pipeline.AddNode(
        label: "VisualizeComparison",
        description: """
          Creates a side-by-side scatter plot comparing Python and C# UMAP embeddings.
          Points are colored by iris species (setosa, versicolor, virginica).
          
          Visual validation checks:
          - Similar clustering patterns
          - Similar separation between species
          - Similar relative positioning of clusters
        """,
        transform: VisualizeComparisonNode.Create("Iris"),
        input: (catalog.IrisInput, catalog.IrisPythonOutput, catalog.IrisCSharpOutput),
        output: catalog.IrisVisualization
      );

      pipeline.AddNode(
        label: "ExportVisualizationToPng",
        description: """
          Exports the side-by-side comparison chart to a PNG file.
          Uses Plotly.NET.ImageExport with PuppeteerSharp (headless Chromium)
          to render the interactive chart as a static image.
        """,
        transform: PlotlyImageExportNode.Create(),
        input: catalog.IrisVisualization,
        output: catalog.IrisVisualizationPng
      );
    });
  }
}
