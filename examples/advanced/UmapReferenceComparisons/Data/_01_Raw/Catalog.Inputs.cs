using Flowthru.Data;
using UmapReferenceComparisons.Data._01_Raw.Schemas;

namespace UmapReferenceComparisons.Data;

public partial class Catalog
{
  // ============================================================================
  // Inputs - feature/label files moved from Reference into Inputs/<dataset>
  // ============================================================================

  public ICatalogEntry<IEnumerable<IrisInputRow>> IrisInput =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Parquet<IrisInputRow>(
          label: "IrisInput",
          filePath: $"{_basePath}/_01_Raw/Datasets/Inputs/iris/input.parquet"
        )
    );

  public ICatalogEntry<IEnumerable<DigitsInputRow>> DigitsInput =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Parquet<DigitsInputRow>(
          label: "DigitsInput",
          filePath: $"{_basePath}/_01_Raw/Datasets/Inputs/digits/input.parquet"
        )
    );

  public ICatalogEntry<IEnumerable<MnistInputRow>> MnistInput =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Parquet<MnistInputRow>(
          label: "MnistInput",
          filePath: $"{_basePath}/_01_Raw/Datasets/Inputs/mnist/input.parquet"
        )
    );

  public ICatalogEntry<IEnumerable<MnistInputRow>> FashionMnistInput =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Parquet<MnistInputRow>(
          label: "FashionMnistInput",
          filePath: $"{_basePath}/_01_Raw/Datasets/Inputs/fashion-mnist/input.parquet"
        )
    );

  // MNIST-transform train/test inputs
  public ICatalogEntry<IEnumerable<MnistInputRow>> MnistTransformTrainInput =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Parquet<MnistInputRow>(
          label: "MnistTransformTrainInput",
          filePath: $"{_basePath}/_01_Raw/Datasets/Inputs/mnist-transform/train_input.parquet"
        )
    );

  public ICatalogEntry<IEnumerable<MnistInputRow>> MnistTransformTestInput =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Parquet<MnistInputRow>(
          label: "MnistTransformTestInput",
          filePath: $"{_basePath}/_01_Raw/Datasets/Inputs/mnist-transform/test_input.parquet"
        )
    );
}
