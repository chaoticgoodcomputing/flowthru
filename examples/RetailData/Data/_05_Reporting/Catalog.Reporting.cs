using Flowthru.Data;
using Plotly.NET;
using RetailData.Data._05_Reporting.Schemas;

namespace RetailData.Data;

public partial class Catalog
{
  // ===== Transformed Data for Reporting =====

  /// <summary>
  /// DTU time series data with Country grouping
  /// </summary>
  public ICatalogEntry<IEnumerable<DtuTimeSeriesSchema>> DtuTimeSeriesCountry =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Parquet<DtuTimeSeriesSchema>(
          label: "DtuTimeSeriesCountry",
          filePath: $"{_basePath}/_05_Reporting/Datasets/dtu_timeseries_country.parquet"
        )
    );

  /// <summary>
  /// DTU time series data with Region grouping
  /// </summary>
  public ICatalogEntry<IEnumerable<DtuTimeSeriesSchema>> DtuTimeSeriesRegion =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Parquet<DtuTimeSeriesSchema>(
          label: "DtuTimeSeriesRegion",
          filePath: $"{_basePath}/_05_Reporting/Datasets/dtu_timeseries_region.parquet"
        )
    );

  /// <summary>
  /// Correlation heatmap data with Country grouping
  /// </summary>
  public ICatalogEntry<IEnumerable<CorrelationHeatmapSchema>> CorrelationHeatmapCountry =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Parquet<CorrelationHeatmapSchema>(
          label: "CorrelationHeatmapCountry",
          filePath: $"{_basePath}/_05_Reporting/Datasets/correlation_heatmap_country.parquet"
        )
    );

  /// <summary>
  /// Correlation heatmap data with Region grouping
  /// </summary>
  public ICatalogEntry<IEnumerable<CorrelationHeatmapSchema>> CorrelationHeatmapRegion =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Parquet<CorrelationHeatmapSchema>(
          label: "CorrelationHeatmapRegion",
          filePath: $"{_basePath}/_05_Reporting/Datasets/correlation_heatmap_region.parquet"
        )
    );

  // ===== Charts (In-Memory) =====

  /// <summary>
  /// DTU chart for countries (GenericChart in memory)
  /// </summary>
  public ICatalogEntry<GenericChart> DtuChartCountry =>
    GetOrCreateEntry(() => CatalogEntries.Single.Memory<GenericChart>(label: "DtuChartCountry"));

  /// <summary>
  /// DTU chart for regions (GenericChart in memory)
  /// </summary>
  public ICatalogEntry<GenericChart> DtuChartRegion =>
    GetOrCreateEntry(() => CatalogEntries.Single.Memory<GenericChart>(label: "DtuChartRegion"));

  /// <summary>
  /// Correlation heatmap chart for countries (GenericChart in memory)
  /// </summary>
  public ICatalogEntry<GenericChart> CorrelationChartCountry =>
    GetOrCreateEntry(
      () => CatalogEntries.Single.Memory<GenericChart>(label: "CorrelationChartCountry")
    );

  /// <summary>
  /// Correlation heatmap chart for regions (GenericChart in memory)
  /// </summary>
  public ICatalogEntry<GenericChart> CorrelationChartRegion =>
    GetOrCreateEntry(
      () => CatalogEntries.Single.Memory<GenericChart>(label: "CorrelationChartRegion")
    );

  // ===== Chart Exports (JSON) =====

  /// <summary>
  /// DTU chart for countries exported as Plotly JSON
  /// </summary>
  public ICatalogEntry<string> DtuChartCountryJson =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Single.Text(
          label: "DtuChartCountryJson",
          filePath: $"{_basePath}/_05_Reporting/Datasets/dtu_chart_country.json"
        )
    );

  /// <summary>
  /// DTU chart for regions exported as Plotly JSON
  /// </summary>
  public ICatalogEntry<string> DtuChartRegionJson =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Single.Text(
          label: "DtuChartRegionJson",
          filePath: $"{_basePath}/_05_Reporting/Datasets/dtu_chart_region.json"
        )
    );

  /// <summary>
  /// Correlation heatmap for countries exported as Plotly JSON
  /// </summary>
  public ICatalogEntry<string> CorrelationChartCountryJson =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Single.Text(
          label: "CorrelationChartCountryJson",
          filePath: $"{_basePath}/_05_Reporting/Datasets/correlation_chart_country.json"
        )
    );

  /// <summary>
  /// Correlation heatmap for regions exported as Plotly JSON
  /// </summary>
  public ICatalogEntry<string> CorrelationChartRegionJson =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Single.Text(
          label: "CorrelationChartRegionJson",
          filePath: $"{_basePath}/_05_Reporting/Datasets/correlation_chart_region.json"
        )
    );

  // ===== Chart Exports (PNG) =====

  /// <summary>
  /// DTU chart for countries exported as PNG image.
  /// Stored as binary PNG file.
  /// </summary>
  public ICatalogEntry<byte[]> DtuChartCountryPng =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Single.Binary(
          label: "DtuChartCountryPng",
          filePath: $"{_basePath}/_05_Reporting/Datasets/dtu_chart_country.png"
        )
    );

  /// <summary>
  /// DTU chart for regions exported as PNG image.
  /// Stored as binary PNG file.
  /// </summary>
  public ICatalogEntry<byte[]> DtuChartRegionPng =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Single.Binary(
          label: "DtuChartRegionPng",
          filePath: $"{_basePath}/_05_Reporting/Datasets/dtu_chart_region.png"
        )
    );

  /// <summary>
  /// Correlation heatmap for countries exported as PNG image.
  /// Stored as binary PNG file.
  /// </summary>
  public ICatalogEntry<byte[]> CorrelationChartCountryPng =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Single.Binary(
          label: "CorrelationChartCountryPng",
          filePath: $"{_basePath}/_05_Reporting/Datasets/correlation_chart_country.png"
        )
    );

  /// <summary>
  /// Correlation heatmap for regions exported as PNG image.
  /// Stored as binary PNG file.
  /// </summary>
  public ICatalogEntry<byte[]> CorrelationChartRegionPng =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Single.Binary(
          label: "CorrelationChartRegionPng",
          filePath: $"{_basePath}/_05_Reporting/Datasets/correlation_chart_region.png"
        )
    );
}
