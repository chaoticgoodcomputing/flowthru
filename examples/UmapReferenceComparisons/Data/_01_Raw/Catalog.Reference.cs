using Flowthru.Data;
using UmapReferenceComparisons.Data._01_Raw.Schemas;

namespace UmapReferenceComparisons.Data;

public partial class Catalog
{
  // ============================================================================
  // Reference - Python-produced outputs and any reference-only artifacts
  // ============================================================================

  public ICatalogEntry<IEnumerable<UmapOutputRow>> IrisPythonOutput =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Parquet<UmapOutputRow>(
          label: "IrisPythonOutput",
          filePath: $"{_basePath}/_01_Raw/Datasets/Reference/iris/output.parquet"
        )
    );

  public ICatalogEntry<IEnumerable<UmapOutputRow>> DigitsPythonOutput =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Parquet<UmapOutputRow>(
          label: "DigitsPythonOutput",
          filePath: $"{_basePath}/_01_Raw/Datasets/Reference/digits/output.parquet"
        )
    );

  public ICatalogEntry<IEnumerable<UmapOutputRow>> MnistPythonOutput =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Parquet<UmapOutputRow>(
          label: "MnistPythonOutput",
          filePath: $"{_basePath}/_01_Raw/Datasets/Reference/mnist/output.parquet"
        )
    );

  public ICatalogEntry<IEnumerable<UmapOutputRow>> FashionMnistPythonOutput =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Parquet<UmapOutputRow>(
          label: "FashionMnistPythonOutput",
          filePath: $"{_basePath}/_01_Raw/Datasets/Reference/fashion-mnist/output.parquet"
        )
    );

  // MNIST-transform reference outputs (train/test)
  public ICatalogEntry<IEnumerable<UmapOutputRow>> MnistTransformTrainOutput =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Parquet<UmapOutputRow>(
          label: "MnistTransformTrainOutput",
          filePath: $"{_basePath}/_01_Raw/Datasets/Reference/mnist-transform/train_output.parquet"
        )
    );

  public ICatalogEntry<IEnumerable<UmapOutputRow>> MnistTransformTestOutput =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Parquet<UmapOutputRow>(
          label: "MnistTransformTestOutput",
          filePath: $"{_basePath}/_01_Raw/Datasets/Reference/mnist-transform/test_output.parquet"
        )
    );
}
