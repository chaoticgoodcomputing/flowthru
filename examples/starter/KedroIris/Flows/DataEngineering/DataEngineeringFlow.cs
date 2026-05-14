using Flowthru.Flow;
using KedroIris.Data;
using KedroIris.Data._01_Raw.Schemas;
using KedroIris.Data._04_Feature.Schemas;
using KedroIris.Data._05_ModelInput.Schemas;
using KedroIris.Flows.DataEngineering.Steps;

namespace KedroIris.Flows.DataEngineering;

/// <summary>
/// Creates the data engineering pipeline that splits iris data and encodes species labels.
/// </summary>
public static class DataEngineeringFlow
{
  public static BuiltFlow Create(Catalog catalog)
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
        transform: SplitAndEncodeStep.Create(),
        inputs: (catalog.IrisRaw, catalog.SplitOptions),
        outputs: (catalog.IrisFeatures, catalog.TrainX, catalog.TrainY, catalog.TestX, catalog.TestY)
      );
    });
  }
}
