using Flowthru.Core.Data;
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
        ItemFactory.Single.Json<ComparisonResult>(
          label: "IrisComparison",
          filePath: $"{_basePath}/_03_Reports/Datasets/iris_comparison.json"
        )
    );

  public IItem<string> IrisRuntimeReport =>
    CreateItem(
      () =>
        ItemFactory.Single.Text(
          label: "IrisRuntimeReport",
          filePath: $"{_basePath}/_03_Reports/Datasets/iris_runtime_report.txt"
        )
    );

  public IItem<GenericChart> IrisVisualization =>
    CreateItem(() => ItemFactory.Single.Memory<GenericChart>(label: "IrisVisualization"));

  public IItem<byte[]> IrisVisualizationPng =>
    CreateItem(
      () =>
        ItemFactory.Single.Binary(
          label: "IrisVisualizationPng",
          filePath: $"{_basePath}/_03_Reports/Datasets/iris_comparison.png"
        )
    );

  public IItem<ComparisonResult> DigitsComparison =>
    CreateItem(
      () =>
        ItemFactory.Single.Json<ComparisonResult>(
          label: "DigitsComparison",
          filePath: $"{_basePath}/_03_Reports/Datasets/digits_comparison.json"
        )
    );

  public IItem<string> DigitsRuntimeReport =>
    CreateItem(
      () =>
        ItemFactory.Single.Text(
          label: "DigitsRuntimeReport",
          filePath: $"{_basePath}/_03_Reports/Datasets/digits_runtime_report.txt"
        )
    );

  public IItem<GenericChart> DigitsVisualization =>
    CreateItem(() => ItemFactory.Single.Memory<GenericChart>(label: "DigitsVisualization"));

  public IItem<byte[]> DigitsVisualizationPng =>
    CreateItem(
      () =>
        ItemFactory.Single.Binary(
          label: "DigitsVisualizationPng",
          filePath: $"{_basePath}/_03_Reports/Datasets/digits_comparison.png"
        )
    );

  public IItem<ComparisonResult> MnistComparison =>
    CreateItem(
      () =>
        ItemFactory.Single.Json<ComparisonResult>(
          label: "MnistComparison",
          filePath: $"{_basePath}/_03_Reports/Datasets/mnist_comparison.json"
        )
    );

  public IItem<ComparisonResult> FashionMnistComparison =>
    CreateItem(
      () =>
        ItemFactory.Single.Json<ComparisonResult>(
          label: "FashionMnistComparison",
          filePath: $"{_basePath}/_03_Reports/Datasets/fashion_mnist_comparison.json"
        )
    );

  public IItem<string> FashionMnistRuntimeReport =>
    CreateItem(
      () =>
        ItemFactory.Single.Text(
          label: "FashionMnistRuntimeReport",
          filePath: $"{_basePath}/_03_Reports/Datasets/fashion_mnist_runtime_report.txt"
        )
    );

  public IItem<ComparisonResult> MnistTransformComparison =>
    CreateItem(
      () =>
        ItemFactory.Single.Json<ComparisonResult>(
          label: "MnistTransformComparison",
          filePath: $"{_basePath}/_03_Reports/Datasets/mnist_transform_comparison.json"
        )
    );

  public IItem<GenericChart> FashionMnistVisualization =>
    CreateItem(() => ItemFactory.Single.Memory<GenericChart>(label: "FashionMnistVisualization"));

  public IItem<byte[]> FashionMnistVisualizationPng =>
    CreateItem(
      () =>
        ItemFactory.Single.Binary(
          label: "FashionMnistVisualizationPng",
          filePath: $"{_basePath}/_03_Reports/Datasets/fashion_mnist_comparison.png"
        )
    );
}
