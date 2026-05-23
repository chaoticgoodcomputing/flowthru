using Flowthru.Flow;
using Iris.Data;
using Iris.Data._01_Raw.Schemas;
using Iris.Data._04_Feature.Schemas;
using Iris.Data._05_ModelInput.Schemas;
using Iris.Flows.DataEngineering.Steps;
using Microsoft.Extensions.Logging;

namespace Iris.Flows.DataEngineering;

/// <summary>
/// Creates the data engineering pipeline that splits iris data and encodes species labels.
/// </summary>
public static class DataEngineeringFlow
{
  public static BuiltFlow Create(Catalog catalog, ILogger logger)
  {
    return FlowBuilder.CreateFlow("DataEngineering", pipeline =>
    {
      pipeline.AddStep<
        IEnumerable<IrisRawSchema>,
        SplitAndEncodeStep.Options,
        IEnumerable<IrisFeatureSchema>,
        IEnumerable<FeatureVectorSchema>,
        IEnumerable<TargetLabelSchema>,
        IEnumerable<FeatureVectorSchema>,
        IEnumerable<TargetLabelSchema>
      >(
        label: "SplitAndEncode",
        transform: SplitAndEncodeStep.Create(logger),
        inputs: (catalog.IrisRaw, catalog.SplitOptions),
        outputs: (catalog.IrisFeatures, catalog.TrainX, catalog.TrainY, catalog.TestX, catalog.TestY)
      );
    });
  }
}
