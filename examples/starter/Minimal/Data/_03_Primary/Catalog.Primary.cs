using Flowthru.Data.Catalog;
using Minimal.Data._03_Primary.Schemas;

namespace Minimal.Data;

public partial class Catalog
{
  /// <summary>
  /// Farewell greetings with "Goodbye" prefix.
  /// </summary>
  public IItem<IEnumerable<GoodbyeSchema>> Goodbyes =>
    CreateItem(() => Item.Of<IEnumerable<GoodbyeSchema>>("Goodbyes")
      .Csv()
      .AtPath($"{_basePath}/Data/_03_Primary/Datasets/goodbyes.csv")
      .Build());

  /// <summary>
  /// Farewell greetings with "So long" prefix.
  /// </summary>
  public IItem<IEnumerable<SoLongSchema>> SoLongs =>
    CreateItem(() => Item.Of<IEnumerable<SoLongSchema>>("SoLongs")
      .Csv()
      .AtPath($"{_basePath}/Data/_03_Primary/Datasets/solongs.csv")
      .Build());
}
