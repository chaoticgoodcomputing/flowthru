using Flowthru.Core.Data;
using UmapReferenceComparisons.Data._01_Raw.Schemas;

namespace UmapReferenceComparisons.Data;

public partial class Catalog
{
    // ============================================================================
    // Reference - Python-produced outputs and any reference-only artifacts
    // ============================================================================

    public IItem<IEnumerable<UmapOutputRow>> IrisPythonOutput =>
      CreateItem(
        () =>
          ItemFactory.Enumerable.Parquet<UmapOutputRow>(
            label: "IrisPythonOutput",
            filePath: $"{_basePath}/_01_Raw/Datasets/Reference/iris/output.parquet"
          )
      );

    public IItem<IEnumerable<UmapOutputRow>> DigitsPythonOutput =>
      CreateItem(
        () =>
          ItemFactory.Enumerable.Parquet<UmapOutputRow>(
            label: "DigitsPythonOutput",
            filePath: $"{_basePath}/_01_Raw/Datasets/Reference/digits/output.parquet"
          )
      );

    public IItem<IEnumerable<UmapOutputRow>> MnistPythonOutput =>
      CreateItem(
        () =>
          ItemFactory.Enumerable.Parquet<UmapOutputRow>(
            label: "MnistPythonOutput",
            filePath: $"{_basePath}/_01_Raw/Datasets/Reference/mnist/output.parquet"
          )
      );

    public IItem<IEnumerable<UmapOutputRow>> FashionMnistPythonOutput =>
      CreateItem(
        () =>
          ItemFactory.Enumerable.Parquet<UmapOutputRow>(
            label: "FashionMnistPythonOutput",
            filePath: $"{_basePath}/_01_Raw/Datasets/Reference/fashion-mnist/output.parquet"
          )
      );

    // MNIST-transform reference outputs (train/test)
    public IItem<IEnumerable<UmapOutputRow>> MnistTransformTrainOutput =>
      CreateItem(
        () =>
          ItemFactory.Enumerable.Parquet<UmapOutputRow>(
            label: "MnistTransformTrainOutput",
            filePath: $"{_basePath}/_01_Raw/Datasets/Reference/mnist-transform/train_output.parquet"
          )
      );

    public IItem<IEnumerable<UmapOutputRow>> MnistTransformTestOutput =>
      CreateItem(
        () =>
          ItemFactory.Enumerable.Parquet<UmapOutputRow>(
            label: "MnistTransformTestOutput",
            filePath: $"{_basePath}/_01_Raw/Datasets/Reference/mnist-transform/test_output.parquet"
          )
      );
}
