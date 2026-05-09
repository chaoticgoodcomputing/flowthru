using Flowthru.Data.Catalog;
using Minimal.Data._01_Raw.Schemas;

namespace Minimal.Data;

public partial class Catalog
{
  /// <summary>
  /// Raw name data imported from CSV file.
  /// </summary>
  public IItem<IEnumerable<NameSchema>> Names =>
    CreateItem(() => Item.Of<IEnumerable<NameSchema>>("Names")
      .Csv()
      .AtPath($"{_basePath}/Data/_01_Raw/Datasets/names.csv")
      .Build());
}
