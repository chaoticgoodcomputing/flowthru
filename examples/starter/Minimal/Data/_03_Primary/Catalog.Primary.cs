using Flowthru.Core.Data;
using Minimal.Data._03_Primary.Schemas;

namespace Minimal.Data;

public partial class Catalog
{
    /// <summary>
    /// Farewell greetings with "Goodbye" prefix.
    /// </summary>
    public IItem<IEnumerable<GoodbyeSchema>> Goodbyes =>
      CreateItem(
        () =>
          ItemFactory.Enumerable.Csv<GoodbyeSchema>(
            label: "Goodbyes",
            filePath: $"{_basePath}/Data/_03_Primary/Datasets/goodbyes.csv"
          )
      );

    /// <summary>
    /// Farewell greetings with "So long" prefix.
    /// </summary>
    public IItem<IEnumerable<SoLongSchema>> SoLongs =>
      CreateItem(
        () =>
          ItemFactory.Enumerable.Csv<SoLongSchema>(
            label: "SoLongs",
            filePath: $"{_basePath}/Data/_03_Primary/Datasets/solongs.csv"
          )
      );
}
