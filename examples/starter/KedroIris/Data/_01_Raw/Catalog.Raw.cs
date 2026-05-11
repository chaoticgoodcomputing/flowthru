using Flowthru.Data.Catalog;
using KedroIris.Data._01_Raw.Schemas;

namespace KedroIris.Data;

/// <summary>
/// Raw data layer: Immutable source data, never modified.
/// </summary>
public partial class Catalog
{
  /// <summary>
  /// Raw iris dataset with measurements and species labels.
  /// </summary>
  public IItem<IEnumerable<IrisRawSchema>> IrisRaw =>
    CreateItem(() => Item.Of<IEnumerable<IrisRawSchema>>("IrisRaw")
      .Json()
      .AtPath($"{_basePath}/_01_Raw/Datasets/iris.json")
      .Build());
}
