using Flowthru.Data;
using KedroIrisPython.Data._01_Raw.Schemas;

namespace KedroIrisPython.Data;

/// <summary>
/// Raw data layer: Immutable source data, never modified.
/// </summary>
public partial class Catalog
{
  /// <summary>
  /// Raw iris dataset with measurements and species labels.
  /// </summary>
  public IItem<IEnumerable<IrisRawSchema>> IrisRaw =>
    CreateItem(
      () =>
        ItemFactory.Enumerable.Csv<IrisRawSchema>(
          label: "IrisRaw",
          filePath: $"{_basePath}/_01_Raw/Datasets/iris.csv"
        )
    );
}
