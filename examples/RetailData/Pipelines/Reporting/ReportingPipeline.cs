using Flowthru.Pipelines;
using RetailData.Data;
using RetailData.Pipelines.Reporting.Nodes;

namespace RetailData.Pipelines.Reporting;

/// <summary>
/// Reporting pipeline that generates DTU and correlation visualizations.
/// Follows the three-stage pattern: Transform → Chart Generation → Parallel Export (JSON + PNG)
/// </summary>
public static class ReportingPipeline
{
  public static Pipeline Create(Catalog catalog)
  {
    return PipelineBuilder.CreatePipeline(pipeline =>
    {
      // ===== Country DTU Report =====

      // Transform country DTU data to unified schema
      pipeline.AddNode(
        label: "TransformCountryDtu",
        transform: TransformCountryDtuNode.Create(),
        input: catalog.DailyDtuByCountry,
        output: catalog.DtuTimeSeriesCountry
      );

      // Generate chart from transformed data
      pipeline.AddNode(
        label: "GenerateCountryDtuChart",
        transform: GenerateDtuChartNode.Create(),
        input: catalog.DtuTimeSeriesCountry,
        output: catalog.DtuChartCountry
      );

      // Export chart to JSON (parallel)
      pipeline.AddNode(
        label: "ExportCountryDtuJson",
        transform: PlotlyJsonExportNode.Create(),
        input: catalog.DtuChartCountry,
        output: catalog.DtuChartCountryJson
      );

      // Export chart to PNG (parallel)
      pipeline.AddNode(
        label: "ExportCountryDtuPng",
        transform: PlotlyImageExportNode.Create(),
        input: catalog.DtuChartCountry,
        output: catalog.DtuChartCountryPng
      );

      // ===== Region DTU Report =====

      // Transform region DTU data to unified schema
      pipeline.AddNode(
        label: "TransformRegionDtu",
        transform: TransformRegionDtuNode.Create(),
        input: catalog.DailyDtuByRegion,
        output: catalog.DtuTimeSeriesRegion
      );

      // Generate chart from transformed data
      pipeline.AddNode(
        label: "GenerateRegionDtuChart",
        transform: GenerateDtuChartNode.Create(),
        input: catalog.DtuTimeSeriesRegion,
        output: catalog.DtuChartRegion
      );

      // Export chart to JSON (parallel)
      pipeline.AddNode(
        label: "ExportRegionDtuJson",
        transform: PlotlyJsonExportNode.Create(),
        input: catalog.DtuChartRegion,
        output: catalog.DtuChartRegionJson
      );

      // Export chart to PNG (parallel)
      pipeline.AddNode(
        label: "ExportRegionDtuPng",
        transform: PlotlyImageExportNode.Create(),
        input: catalog.DtuChartRegion,
        output: catalog.DtuChartRegionPng
      );

      // ===== Country Correlation Report =====

      // Transform country correlation data to unified schema
      pipeline.AddNode(
        label: "TransformCountryCorrelation",
        transform: TransformCountryCorrelationNode.Create(),
        input: catalog.CountryCorrelations,
        output: catalog.CorrelationHeatmapCountry
      );

      // Generate heatmap from transformed data
      pipeline.AddNode(
        label: "GenerateCountryCorrelationChart",
        transform: GenerateCorrelationHeatmapNode.Create(),
        input: catalog.CorrelationHeatmapCountry,
        output: catalog.CorrelationChartCountry
      );

      // Export chart to JSON (parallel)
      pipeline.AddNode(
        label: "ExportCountryCorrelationJson",
        transform: PlotlyJsonExportNode.Create(),
        input: catalog.CorrelationChartCountry,
        output: catalog.CorrelationChartCountryJson
      );

      // Export chart to PNG (parallel)
      pipeline.AddNode(
        label: "ExportCountryCorrelationPng",
        transform: PlotlyImageExportNode.Create(),
        input: catalog.CorrelationChartCountry,
        output: catalog.CorrelationChartCountryPng
      );

      // ===== Region Correlation Report =====

      // Transform region correlation data to unified schema
      pipeline.AddNode(
        label: "TransformRegionCorrelation",
        transform: TransformRegionCorrelationNode.Create(),
        input: catalog.RegionCorrelations,
        output: catalog.CorrelationHeatmapRegion
      );

      // Generate heatmap from transformed data
      pipeline.AddNode(
        label: "GenerateRegionCorrelationChart",
        transform: GenerateCorrelationHeatmapNode.Create(),
        input: catalog.CorrelationHeatmapRegion,
        output: catalog.CorrelationChartRegion
      );

      // Export chart to JSON (parallel)
      pipeline.AddNode(
        label: "ExportRegionCorrelationJson",
        transform: PlotlyJsonExportNode.Create(),
        input: catalog.CorrelationChartRegion,
        output: catalog.CorrelationChartRegionJson
      );

      // Export chart to PNG (parallel)
      pipeline.AddNode(
        label: "ExportRegionCorrelationPng",
        transform: PlotlyImageExportNode.Create(),
        input: catalog.CorrelationChartRegion,
        output: catalog.CorrelationChartRegionPng
      );
    });
  }
}
