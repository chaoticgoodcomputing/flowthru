using Flowthru.Data.Catalog;
using IrisFUnit.Data._01_Raw.Schemas;

namespace IrisFUnit.Data;

/// <summary>
/// Raw data layer: Immutable source data, never modified.
/// </summary>
public partial class Catalog
{
  public IItem<IEnumerable<IrisRawSchema>> IrisRaw =>
    CreateItem(() => Item.Of<IEnumerable<IrisRawSchema>>("IrisRaw")
      .Json()
      .AtPath($"{_basePath}/_01_Raw/Datasets/iris.json")
      .Build());
}
