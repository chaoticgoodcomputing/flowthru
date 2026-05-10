using Flowthru.Flow;
using KedroIrisFUnit.Data;
using KedroIrisFUnit.Data._01_Raw.Schemas;
using KedroIrisFUnit.Data._04_Feature.Schemas;
using KedroIrisFUnit.Data._05_ModelInput.Schemas;
using KedroIrisFUnit.Flows.DataEngineering.Steps;

namespace KedroIrisFUnit.Flows.DataEngineering;

/// <summary>
/// Creates the data engineering pipeline that splits the iris
/// dataset and one-hot-encodes species labels. The single
/// <c>SplitAndEncodeStep</c> has arity 1×5 (one input — the raw
/// dataset — produces five outputs: features + train X/Y + test X/Y);
/// options are closed over via <see cref="FlowConfig"/> at flow
/// construction time per §2.6.
/// </summary>
public static class DataEngineeringFlow
{
  public static BuiltFlow Create(Catalog catalog, FlowConfig config)
  {
    var splitOptions = config.SplitOptions;
    var splitTransform = SplitAndEncodeStep.Create();

    return FlowBuilder.CreateFlow("DataEngineering", pipeline =>
    {
      pipeline.AddStep<
        IEnumerable<IrisRawSchema>,
        IEnumerable<IrisFeatureSchema>,
        IEnumerable<FeatureVectorSchema>,
        IEnumerable<TargetLabelSchema>,
        IEnumerable<FeatureVectorSchema>,
        IEnumerable<TargetLabelSchema>
      >(
        label: "SplitAndEncode",
        transform: rawData => splitTransform((rawData, splitOptions)),
        inputs: catalog.IrisRaw,
        outputs: (catalog.IrisFeatures, catalog.TrainX, catalog.TrainY, catalog.TestX, catalog.TestY)
      );
    });
  }
}
