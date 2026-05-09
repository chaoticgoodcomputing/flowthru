using Flowthru.Data.Catalog;
using KedroSpaceflightsPython.Data._05_ModelInput.Schemas;

namespace KedroSpaceflightsPython.Data;

/// <summary>
/// Model input data layer: Joined feature tables ("master tables"). Transient (memory only).
/// </summary>
public partial class Catalog
{
  public IItem<IEnumerable<XValues>> XTest =>
    CreateItem(() => Item.Of<IEnumerable<XValues>>("XTest").Memory().Build());

  public IItem<IEnumerable<XValues>> XTrain =>
    CreateItem(() => Item.Of<IEnumerable<XValues>>("XTrain").Memory().Build());

  public IItem<IEnumerable<YValues>> YTest =>
    CreateItem(() => Item.Of<IEnumerable<YValues>>("YTest").Memory().Build());

  public IItem<IEnumerable<YValues>> YTrain =>
    CreateItem(() => Item.Of<IEnumerable<YValues>>("YTrain").Memory().Build());
}
