using Flowthru.Data.Catalog;
using Minimal.Data._02_Intermediate.Schemas;

namespace Minimal.Data;

public partial class Catalog
{
  /// <summary>
  /// Greetings with "Hello" prefix.
  /// </summary>
  public IItem<IEnumerable<GreetingSchema>> HelloGreetings =>
    CreateItem(() => Item.Of<IEnumerable<GreetingSchema>>("HelloGreetings")
      .Csv()
      .AtPath($"{_basePath}/Data/_02_Intermediate/Datasets/hello_greetings.csv")
      .Build());
}
