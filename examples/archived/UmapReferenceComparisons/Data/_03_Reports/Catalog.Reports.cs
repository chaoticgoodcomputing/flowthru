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

  public IItem<ComparisonResult> IrisComparison =>
    CreateItem(
      () =>
        Items.Single.Json<ComparisonResult>(
          label: "IrisComparison",
          filePath: $"{_basePath}/_03_Reports/Datasets/iris_comparison.json"
        )
    );

  public IItem<string> IrisRuntimeReport =>
    CreateItem(
      () =>
        Items.Single.Text(
          label: "IrisRuntimeReport",
          filePath: $"{_basePath}/_03_Reports/Datasets/iris_runtime_report.txt"
        )
    );

  public IItem<GenericChart> IrisVisualization =>
    CreateItem(() => Items.Single.Memory<GenericChart>(label: "IrisVisualization"));

  public IItem<byte[]> IrisVisualizationPng =>
    CreateItem(
      () =>
        Items.Single.Binary(
          label: "IrisVisualizationPng",
          filePath: $"{_basePath}/_03_Reports/Datasets/iris_comparison.png"
        )
    );

  public IItem<ComparisonResult> DigitsComparison =>
    CreateItem(
      () =>
        Items.Single.Json<ComparisonResult>(
          label: "DigitsComparison",
          filePath: $"{_basePath}/_03_Reports/Datasets/digits_comparison.json"
        )
    );

  public IItem<string> DigitsRuntimeReport =>
    CreateItem(
      () =>
        Items.Single.Text(
          label: "DigitsRuntimeReport",
          filePath: $"{_basePath}/_03_Reports/Datasets/digits_runtime_report.txt"
        )
    );

  public IItem<GenericChart> DigitsVisualization =>
    CreateItem(() => Items.Single.Memory<GenericChart>(label: "DigitsVisualization"));

  public IItem<byte[]> DigitsVisualizationPng =>
    CreateItem(
      () =>
        Items.Single.Binary(
          label: "DigitsVisualizationPng",
          filePath: $"{_basePath}/_03_Reports/Datasets/digits_comparison.png"
        )
    );

  public IItem<ComparisonResult> MnistComparison =>
    CreateItem(
      () =>
        Items.Single.Json<ComparisonResult>(
          label: "MnistComparison",
          filePath: $"{_basePath}/_03_Reports/Datasets/mnist_comparison.json"
        )
    );

  public IItem<ComparisonResult> FashionMnistComparison =>
    CreateItem(
      () =>
        Items.Single.Json<ComparisonResult>(
          label: "FashionMnistComparison",
          filePath: $"{_basePath}/_03_Reports/Datasets/fashion_mnist_comparison.json"
        )
    );

  public IItem<string> FashionMnistRuntimeReport =>
    CreateItem(
      () =>
        Items.Single.Text(
          label: "FashionMnistRuntimeReport",
          filePath: $"{_basePath}/_03_Reports/Datasets/fashion_mnist_runtime_report.txt"
        )
    );

  public IItem<ComparisonResult> MnistTransformComparison =>
    CreateItem(
      () =>
        Items.Single.Json<ComparisonResult>(
          label: "MnistTransformComparison",
          filePath: $"{_basePath}/_03_Reports/Datasets/mnist_transform_comparison.json"
        )
    );

  public IItem<GenericChart> FashionMnistVisualization =>
    CreateItem(() => Items.Single.Memory<GenericChart>(label: "FashionMnistVisualization"));

  public IItem<byte[]> FashionMnistVisualizationPng =>
    CreateItem(
      () =>
        Items.Single.Binary(
          label: "FashionMnistVisualizationPng",
          filePath: $"{_basePath}/_03_Reports/Datasets/fashion_mnist_comparison.png"
        )
    );
}
