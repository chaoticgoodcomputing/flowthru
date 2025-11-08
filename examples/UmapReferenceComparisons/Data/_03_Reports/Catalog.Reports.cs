using Flowthru.Data;
using Plotly.NET;
using UmapReferenceComparisons.Data._01_Raw.Schemas;
using UmapReferenceComparisons.Data._03_Reports.Schemas;

namespace UmapReferenceComparisons.Data;

public partial class Catalog
{
  // ============================================================================
  // Reports - Comparison results and analysis outputs
  // ============================================================================

  public ICatalogEntry<ComparisonResult> IrisComparison =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Single.Json<ComparisonResult>(
          label: "IrisComparison",
          filePath: $"{_basePath}/_03_Reports/Datasets/iris_comparison.json"
        )
    );

  public ICatalogEntry<GenericChart> IrisVisualization =>
    GetOrCreateEntry(() => CatalogEntries.Single.Memory<GenericChart>(label: "IrisVisualization"));

  public ICatalogEntry<byte[]> IrisVisualizationPng =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Single.Binary(
          label: "IrisVisualizationPng",
          filePath: $"{_basePath}/_03_Reports/Datasets/iris_comparison.png"
        )
    );

  public ICatalogEntry<ComparisonResult> DigitsComparison =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Single.Json<ComparisonResult>(
          label: "DigitsComparison",
          filePath: $"{_basePath}/_03_Reports/Datasets/digits_comparison.json"
        )
    );

  public ICatalogEntry<GenericChart> DigitsVisualization =>
    GetOrCreateEntry(
      () => CatalogEntries.Single.Memory<GenericChart>(label: "DigitsVisualization")
    );

  public ICatalogEntry<byte[]> DigitsVisualizationPng =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Single.Binary(
          label: "DigitsVisualizationPng",
          filePath: $"{_basePath}/_03_Reports/Datasets/digits_comparison.png"
        )
    );

  public ICatalogEntry<ComparisonResult> MnistComparison =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Single.Json<ComparisonResult>(
          label: "MnistComparison",
          filePath: $"{_basePath}/_03_Reports/Datasets/mnist_comparison.json"
        )
    );

  public ICatalogEntry<ComparisonResult> FashionMnistComparison =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Single.Json<ComparisonResult>(
          label: "FashionMnistComparison",
          filePath: $"{_basePath}/_03_Reports/Datasets/fashion_mnist_comparison.json"
        )
    );

  public ICatalogEntry<ComparisonResult> MnistTransformComparison =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Single.Json<ComparisonResult>(
          label: "MnistTransformComparison",
          filePath: $"{_basePath}/_03_Reports/Datasets/mnist_transform_comparison.json"
        )
    );
}
