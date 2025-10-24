using Flowthru.Nodes;
using Flowthru.Pipelines;
using Flowthru.Pipelines.Mapping;
using Flowthru.Tests.KedroSpaceflights.Data;
using Flowthru.Tests.KedroSpaceflights.Pipelines.Reporting.Nodes;

namespace Flowthru.Tests.KedroSpaceflights.Pipelines.Reporting;

/// <summary>
/// Reporting pipeline that generates visualizations from processed data.
/// Matches Kedro spaceflights reporting pipeline structure with improved architecture.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Pipeline Purpose:</strong> Generate interactive and static visualizations for data
/// exploration and model evaluation using Plotly.NET. Charts are first created in memory,
/// then exported to multiple formats (JSON and PNG).
/// </para>
/// <para>
/// <strong>Architecture Improvements:</strong>
/// This pipeline follows a three-stage pattern for each visualization:
/// 1. Chart Generation (data → GenericChart in memory)
/// 2. JSON Export (GenericChart → plotly.js JSON file)
/// 3. Image Export (GenericChart → PNG static image)
/// 
/// This separation enables:
/// - Reusable export nodes across different chart types
/// - Multiple output formats from single chart generation
/// - Better type safety with compile-time checked data flow
/// - Clear separation between visualization logic and serialization
/// </para>
/// <para>
/// <strong>Kedro Equivalence:</strong> This pipeline matches the Kedro spaceflights reporting
/// pipeline, replacing matplotlib/plotly.express with Plotly.NET for .NET-native visualization.
/// </para>
/// </remarks>
public static class ReportingPipeline {
  public static Pipeline Create(SpaceflightsCatalog catalog) {
    return PipelineBuilder.CreatePipeline(pipeline => {

      // Step 1: Generate chart from processed shuttle data
      pipeline.AddNode<ComparePassengerCapacityNode>(
          input: catalog.CleanedShuttles,
          output: catalog.ShuttlePassengerCapacityChart,
          name: "GeneratePassengerCapacityChart"
      );

      // Step 2: Export chart to JSON for interactive visualization
      pipeline.AddNode<PlotlyJsonExportNode>(
          input: catalog.ShuttlePassengerCapacityChart,
          output: catalog.ShuttlePassengerCapacityPlot,
          name: "ExportPassengerCapacityJson"
      );

      // Step 3: Export chart to PNG for static reports
      pipeline.AddNode<PlotlyImageExportNode>(
          input: catalog.ShuttlePassengerCapacityChart,
          output: NoData.Discard,
          name: "ExportPassengerCapacityPng",
          configure: node => node.Parameters.OutputPath = "Data/Datasets/06_Reports/shuttle_passenger_capacity_plot"
      );

      // ===== Confusion Matrix Visualization =====

      // Step 1: Generate confusion matrix heatmap from company data
      pipeline.AddNode<CreateConfusionMatrixNode>(
          input: catalog.CleanedCompanies,
          output: catalog.ConfusionMatrixChart,
          name: "GenerateConfusionMatrixChart"
      );

      // Step 2: Export chart to JSON for interactive visualization
      pipeline.AddNode<PlotlyJsonExportNode>(
          input: catalog.ConfusionMatrixChart,
          output: catalog.ConfusionMatrixPlot,
          name: "ExportConfusionMatrixJson"
      );

      // Step 3: Export chart to PNG for static reports
      pipeline.AddNode<PlotlyImageExportNode>(
          input: catalog.ConfusionMatrixChart,
          output: NoData.Discard,
          name: "ExportConfusionMatrixPng",
          configure: node => node.Parameters.OutputPath = "Data/Datasets/06_Reports/confusion_matrix_plot"
      );
    });
  }
}
