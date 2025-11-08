using Flowthru.Data;
using UmapReferenceComparisons.Data._02_ModelOutputs.Schemas;

namespace UmapReferenceComparisons.Data;

public partial class Catalog
{
  // ============================================================================
  // Reference - Python-produced outputs and any reference-only artifacts
  // ============================================================================

  public ICatalogEntry<IEnumerable<UmapEmbedding2D>> IrisPythonOutput =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Parquet<UmapEmbedding2D>(
          label: "IrisPythonOutput",
          filePath: $"{_basePath}/_01_Raw/Datasets/Reference/iris/output.parquet"
        )
    );

  public ICatalogEntry<IEnumerable<UmapEmbedding2D>> DigitsPythonOutput =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Parquet<UmapEmbedding2D>(
          label: "DigitsPythonOutput",
          filePath: $"{_basePath}/_01_Raw/Datasets/Reference/digits/output.parquet"
        )
    );

  public ICatalogEntry<IEnumerable<UmapEmbedding2D>> MnistPythonOutput =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Parquet<UmapEmbedding2D>(
          label: "MnistPythonOutput",
          filePath: $"{_basePath}/_01_Raw/Datasets/Reference/mnist/output.parquet"
        )
    );

  public ICatalogEntry<IEnumerable<UmapEmbedding2D>> FashionMnistPythonOutput =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Parquet<UmapEmbedding2D>(
          label: "FashionMnistPythonOutput",
          filePath: $"{_basePath}/_01_Raw/Datasets/Reference/fashion-mnist/output.parquet"
        )
    );

  // MNIST-transform reference outputs (train/test)
  public ICatalogEntry<IEnumerable<UmapEmbedding2D>> MnistTransformTrainOutput =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Parquet<UmapEmbedding2D>(
          label: "MnistTransformTrainOutput",
          filePath: $"{_basePath}/_01_Raw/Datasets/Reference/mnist-transform/train_output.parquet"
        )
    );

  public ICatalogEntry<IEnumerable<UmapEmbedding2D>> MnistTransformTestOutput =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Parquet<UmapEmbedding2D>(
          label: "MnistTransformTestOutput",
          filePath: $"{_basePath}/_01_Raw/Datasets/Reference/mnist-transform/test_output.parquet"
        )
    );
}
