using Flowthru.Data.Catalog;
using Flowthru.Data.Catalog.Configuration;
using MnistDistributed.Data._05_ModelInput.Schemas;

namespace MnistDistributed.Data;

public partial class Catalog
{
  /// <summary>
  /// Training hyperparameters from <c>Flowthru:Flows:Train:TrainingConfig</c>.
  /// Flows into the distributed-training Python step as a JSON singleton.
  /// </summary>
  public IItem<TrainingConfigSchema> TrainingConfig =>
    CreateItem(() =>
      Item.Of<TrainingConfigSchema>("TrainingConfig")
        .FromConfiguration(_configuration)
        .AtSection("Flowthru:Flows:Train:TrainingConfig")
        .Build());
}
