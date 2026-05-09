using Flowthru.Data.Catalog;
using KedroIrisPython.Data._01_Raw.Schemas;

namespace KedroIrisPython.Data;

/// <summary>
/// Raw data layer: Immutable source data, never modified.
/// </summary>
public partial class Catalog
{
  public IItem<IEnumerable<IrisRawSchema>> IrisRaw =>
    CreateItem(() => Item.Of<IEnumerable<IrisRawSchema>>("IrisRaw")
      .Csv()
      .AtPath($"{_basePath}/_01_Raw/Datasets/iris.csv")
      .Build());
}
