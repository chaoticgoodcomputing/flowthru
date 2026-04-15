using Flowthru.Core.Data;
using UmapReferenceComparisons.Data._01_Raw.Schemas;

namespace UmapReferenceComparisons.Data;

public partial class Catalog
{
  // ============================================================================
  // Inputs - feature/label files moved from Reference into Inputs/<dataset>
  // ============================================================================

  public IItem<IEnumerable<IrisInputRow>> IrisInput =>
    CreateItem(
      () =>
        ItemFactory.Enumerable.Parquet<IrisInputRow>(
          label: "IrisInput",
          filePath: $"{_basePath}/_01_Raw/Datasets/Inputs/iris/input.parquet"
        )
    );

  public IItem<IEnumerable<DigitsInputRow>> DigitsInput =>
    CreateItem(
      () =>
        ItemFactory.Enumerable.Parquet<DigitsInputRow>(
          label: "DigitsInput",
          filePath: $"{_basePath}/_01_Raw/Datasets/Inputs/digits/input.parquet"
        )
    );

  public IItem<IEnumerable<MnistInputRow>> MnistInput =>
    CreateItem(
      () =>
        ItemFactory.Enumerable.Parquet<MnistInputRow>(
          label: "MnistInput",
          filePath: $"{_basePath}/_01_Raw/Datasets/Inputs/mnist/input.parquet"
        )
    );

  public IItem<IEnumerable<MnistInputRow>> FashionMnistInput =>
    CreateItem(
      () =>
        ItemFactory.Enumerable.Parquet<MnistInputRow>(
          label: "FashionMnistInput",
          filePath: $"{_basePath}/_01_Raw/Datasets/Inputs/fashion-mnist/input.parquet"
        )
    );

  // MNIST-transform train/test inputs
  public IItem<IEnumerable<MnistInputRow>> MnistTransformTrainInput =>
    CreateItem(
      () =>
        ItemFactory.Enumerable.Parquet<MnistInputRow>(
          label: "MnistTransformTrainInput",
          filePath: $"{_basePath}/_01_Raw/Datasets/Inputs/mnist-transform/train_input.parquet"
        )
    );

  public IItem<IEnumerable<MnistInputRow>> MnistTransformTestInput =>
    CreateItem(
      () =>
        ItemFactory.Enumerable.Parquet<MnistInputRow>(
          label: "MnistTransformTestInput",
          filePath: $"{_basePath}/_01_Raw/Datasets/Inputs/mnist-transform/test_input.parquet"
        )
    );
}
